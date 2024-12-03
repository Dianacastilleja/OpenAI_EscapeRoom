from typing import Dict, cast
import attr
from mlagents.torch_utils import torch, default_device
from mlagents.trainers.buffer import AgentBuffer, BufferKey, RewardSignalUtil
from mlagents_envs.timers import timed
from mlagents.trainers.policy.torch_policy import TorchPolicy
from mlagents.trainers.optimizer.torch_optimizer import TorchOptimizer
from mlagents.trainers.settings import (
    TrainerSettings,
    OnPolicyHyperparamSettings,
    ScheduleType,
)
from mlagents.trainers.torch_entities.networks import ValueNetwork
from mlagents.trainers.torch_entities.utils import ModelUtils
from mlagents_envs.logging_util import get_logger

logger = get_logger(__name__)

@attr.s(auto_attribs=True)
class PPOSettings(OnPolicyHyperparamSettings):
    beta: float = 5.0e-3
    epsilon: float = 0.2
    lambd: float = 0.95
    num_epoch: int = 3
    shared_critic: bool = False
    learning_rate_schedule: ScheduleType = ScheduleType.LINEAR
    beta_schedule: ScheduleType = ScheduleType.LINEAR
    epsilon_schedule: ScheduleType = ScheduleType.LINEAR

class PPONewOptimizer(TorchOptimizer):
    """Custom optimizer for 'ppo_new' trainer with adaptive entropy coefficient."""

    def __init__(self, policy: TorchPolicy, trainer_settings: TrainerSettings):
        super().__init__(policy, trainer_settings)

        self.hyperparameters: PPOSettings = cast(
            PPOSettings, trainer_settings.hyperparameters
        )

        params = list(self.policy.actor.parameters())
        if self.hyperparameters.shared_critic:
            self._critic = policy.actor
        else:
            self._critic = ValueNetwork(
                observation_specs=policy.behavior_spec.observation_specs,
                network_settings=trainer_settings.network_settings,
                stream_names=["extrinsic"],  # Specify the reward streams being used
            )


            self._critic.to(default_device())
            params += list(self._critic.parameters())

        # Adaptive entropy decay
        self.decay_beta = ModelUtils.DecayedValue(
            self.hyperparameters.beta_schedule,
            self.hyperparameters.beta,
            1e-5,
            self.trainer_settings.max_steps,
        )

        self.optimizer = torch.optim.Adam(
            params, lr=self.trainer_settings.hyperparameters.learning_rate
        )

    @property
    def critic(self):
        return self._critic

    @timed
    def update(self, batch: AgentBuffer, num_sequences: int) -> Dict[str, float]:
        """
        Perform a PPO update.
        """
        decay_beta = self.decay_beta.get_value(self.policy.get_current_step())

        n_obs = len(self.policy.behavior_spec.observation_specs)
        current_obs = ModelUtils.list_to_tensor(
            [batch[BufferKey.OBSERVATIONS][i] for i in range(n_obs)]
        )
        actions = ModelUtils.list_to_tensor(batch[BufferKey.CONTINUOUS_ACTION])
        advantages = ModelUtils.list_to_tensor(batch[BufferKey.ADVANTAGES])
        old_log_probs = ModelUtils.list_to_tensor(batch[BufferKey.LOG_PROBS])

        # Calculate losses
        policy_loss, entropy_loss = self.policy.calculate_loss(
            current_obs, actions, advantages, old_log_probs, decay_beta
        )

        loss = policy_loss - decay_beta * entropy_loss

        # Perform optimization
        self.optimizer.zero_grad()
        loss.backward()
        self.optimizer.step()

        # Log stats
        update_stats = {
            "Losses/Policy Loss": policy_loss.item(),
            "Losses/Entropy Loss": entropy_loss.item(),
            "Policy/Beta": decay_beta,
        }

        return update_stats

    def get_modules(self):
        return {"Optimizer": self.optimizer, "Critic": self._critic}
