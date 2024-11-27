import tensorflow as tf
import matplotlib.pyplot as plt

# Path to the event file (replace with your actual path)
log_file = r'C:\Users\alexi\dev\OpenAI_EscapeRoom\Project\Assets\ML-Agents\Examples\Escape Room\results\escaperoomfb33\EscapeArtist\events.out.tfevents.1732466551.DESKTOP-F9VHGOO.15232.0'

# Load the event file using TFRecordDataset
dataset = tf.data.TFRecordDataset(log_file)

# Initialize dictionaries to store data for each metric
data = {
    "Episode Length": {"steps": [], "values": []},
    "Policy Loss": {"steps": [], "values": []},
    "Cumulative Reward": {"steps": [], "values": [],},
    "Losses": {"steps": [], "values": [],}
}

# Loop through the records and extract data for each metric
for raw_record in dataset:
    event = tf.compat.v1.Event.FromString(raw_record.numpy())
    for value in event.summary.value:
        if value.tag == 'Environment/Episode Length':  # Adjust tag for Episode Length
            data["Episode Length"]["steps"].append(event.step)
            data["Episode Length"]["values"].append(value.simple_value)
        elif value.tag == 'Losses/Policy Loss':  # Adjust tag for Policy Loss
            data["Policy Loss"]["steps"].append(event.step)
            data["Policy Loss"]["values"].append(value.simple_value)
        elif value.tag == 'Environment/Cumulative Reward':  # Adjust tag for Cumulative Reward
            data["Cumulative Reward"]["steps"].append(event.step)
            data["Cumulative Reward"]["values"].append(value.simple_value)
        elif value.tag == 'Losses/Value Loss':
            data["Losses"]["steps"].append(event.step)
            data["Losses"]["values"].append(value.simple_value)

# Plot the metrics
plt.figure(figsize=(12, 6))
for metric, metric_data in data.items():
    if metric_data["steps"] and metric_data["values"]:  # Check if data exists for the metric
        plt.figure(figsize=(12, 6))
        plt.plot(metric_data["steps"], metric_data["values"])
        plt.xlabel('Steps')
        plt.ylabel(metric)
        plt.title(f'Training Progress - {metric} over Time using PPO')
        plt.grid(True)
        plt.tight_layout()
        plt.show()
