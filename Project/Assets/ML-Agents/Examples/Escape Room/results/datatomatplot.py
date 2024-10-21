import tensorflow as tf
import matplotlib.pyplot as plt

# Path to the event file (replace with your actual path)
log_file = r'C:\Users\alexi\dev\OpenAI_EscapeRoom\Project\Assets\ML-Agents\Examples\Escape Room\results\escaperoomraycast10\EscapeArtist\events.out.tfevents.1729490508.DESKTOP-F9VHGOO.11008.0'

# Load the event file using TFRecordDataset
dataset = tf.data.TFRecordDataset(log_file)

# Initialize lists for storing steps and cumulative rewards
steps = []
cumulative_rewards = []

for raw_record in dataset:
    event = tf.compat.v1.Event.FromString(raw_record.numpy())
    for value in event.summary.value:
        if value.tag == 'Environment/Cumulative Reward':  # Adjust the tag as needed
            steps.append(event.step)
            cumulative_rewards.append(value.simple_value)

# Plot the extracted data
plt.plot(steps, cumulative_rewards)
plt.xlabel('Steps')
plt.ylabel('Cumulative Reward')
plt.title('Training Progress - Cumulative Reward over Time')
plt.show()
