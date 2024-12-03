# setup.py
from setuptools import setup
from mlagents.plugins import ML_AGENTS_TRAINER_TYPE

setup(
    name="mlagents_trainer_plugin_ppo_new",
    version="0.0.1",
    entry_points={
        ML_AGENTS_TRAINER_TYPE: [
            "ppo_new=mlagents_trainer_plugin_ppo_new.ppo_new.ppo_new_trainer:get_type_and_setting",
        ]
    },
)
