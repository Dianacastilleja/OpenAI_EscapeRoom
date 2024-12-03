from typing import cast, Type, Union, Dict, Any
import numpy as np
from mlagents_envs.base_env import BehaviorSpec
from mlagents_envs.logging_util import get_logger
from mlagents.trainers.buffer import BufferKey, RewardSignalUtil
from mlagents.trainers.trainer.on_policy_trainer import OnPolicyTrainer
from mlagents.trainers.policy.policy import Policy
from mlagents.trainers.trainer.trainer_utils import get_gae
from mlagents.trainers.optimizer.torch_optimizer import TorchOptimizer
from mlagents.trainers.policy.torch_policy import TorchPolicy
from .ppo_new_optimizer import PPONewOptimizer, PPOSettings
from mlagents.trainers.trajectory import Trajectory
from mlagents.trainers.behavior_id_utils import BehaviorIdentifiers
from mlagents.trainers.settings import TrainerSettings
from mlagents.trainers.torch_entities.networks import SimpleActor, SharedActorCritic

logger = get_logger(__name__)

TRAINER_NAME = "ppo_new"

class PPONewTrainer(OnPolicyTrainer):
    """Custom PPO trainer named 'ppo_new'."""

    def __init__(
        self,
        behavior_name: str,
        reward_buff_cap: int,
        trainer_settings: TrainerSettings,
        training: bool,
        load: bool,
        seed: int,
        artifact_path: str,
    ):
        super().__init__(
            behavior_name,
            reward_buff_cap,
            trainer_settings,
            training,
            load,
            seed,
            artifact_path,
        )
        self.hyperparameters: PPOSettings = cast(
            PPOSettings, self.trainer_settings.hyperparameters
        )
        self.seed = seed
        self.shared_critic = self.hyperparameters.shared_critic
        self.policy: TorchPolicy = None  # type: ignore

    def _compute_advantages_and_returns(self, trajectory: Trajectory) -> None:
        """
        Compute advantages and returns using GAE for extrinsic rewards.
        """
        agent_buffer = trajectory.to_agentbuffer()
        value_estimates, value_next, value_memories = self.optimizer.get_trajectory_value_estimates(
            agent_buffer,
            trajectory.next_obs,
            trajectory.done_reached and not trajectory.interrupted,
        )

        # Store value memories if available
        if value_memories is not None:
            agent_buffer[BufferKey.CRITIC_MEMORY].set(value_memories)

        # Extrinsic rewards only
        rewards = agent_buffer[RewardSignalUtil.rewards_key("extrinsic")].get_batch()
        value_est = agent_buffer[RewardSignalUtil.value_estimates_key("extrinsic")].get_batch()

        bootstrap_value = value_next.get("extrinsic", 0.0)

        # Calculate GAE and returns
        advantages = get_gae(
            rewards,
            value_est,
            bootstrap_value,
            self.optimizer.reward_signals["extrinsic"].gamma,
            self.hyperparameters.lambd,
        )
        returns = advantages + value_est

        # Store computed values
        agent_buffer[RewardSignalUtil.returns_key("extrinsic")].set(returns)
        agent_buffer[RewardSignalUtil.advantage_key("extrinsic")].set(advantages)

        # Normalize global advantages
        global_advantages = agent_buffer[BufferKey.ADVANTAGES].get_batch()
        normalized_advantages = (global_advantages - np.mean(global_advantages)) / (
            np.std(global_advantages) + 1e-10
        )
        agent_buffer[BufferKey.ADVANTAGES].set(normalized_advantages)

        # Log extrinsic rewards to TensorBoard
        self._stats_reporter.add_stat("Reward/Extrinsic", np.mean(rewards))


    def _process_trajectory(self, trajectory: Trajectory) -> None:
        """
        Process trajectory and compute advantages and returns.
        """
        super()._process_trajectory(trajectory)
        self._compute_advantages_and_returns(trajectory)

    def create_optimizer(self) -> TorchOptimizer:
        return PPONewOptimizer(
            cast(TorchPolicy, self.policy), self.trainer_settings
        )

    def create_policy(
        self, parsed_behavior_id: BehaviorIdentifiers, behavior_spec: BehaviorSpec
    ) -> TorchPolicy:
        actor_cls: Union[Type[SimpleActor], Type[SharedActorCritic]] = SimpleActor
        actor_kwargs: Dict[str, Any] = {
            "conditional_sigma": False,
            "tanh_squash": False,
        }
        if self.shared_critic:
            reward_signal_configs = self.trainer_settings.reward_signals
            reward_signal_names = [
                key.value for key, _ in reward_signal_configs.items()
            ]
            actor_cls = SharedActorCritic
            actor_kwargs.update({"stream_names": reward_signal_names})

        policy = TorchPolicy(
            self.seed,
            behavior_spec,
            self.trainer_settings.network_settings,
            actor_cls,
            actor_kwargs,
        )
        return policy

    def get_policy(self, name_behavior_id: str) -> Policy:
        return self.policy

    @staticmethod
    def get_trainer_name() -> str:
        return TRAINER_NAME


def get_type_and_setting():
    return {PPONewTrainer.get_trainer_name(): PPONewTrainer}, {
        PPONewTrainer.get_trainer_name(): PPOSettings
    }
