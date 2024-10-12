using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class EscapeRoomAgent : Agent
{
    private Rigidbody rb;
    public GameObject pressurePlate; // Reference to the PressurePlate object
    public GameObject endGoal;       // Reference to the EndGoal object
    public GameObject door;          // Reference to the Door object
    public float spawnRange = 8f;

    private bool platePressed = false; // To track if plate was pressed
    private bool goalReached = false;  // To track if goal was reached
    private float episodeStartTime;    // Track episode time

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        // Reset agent's velocity
        rb.velocity = Vector3.zero;

        // Reset task progress
        platePressed = false;
        goalReached = false;

        // Call the ResetPlate method on the PressurePlate component
        pressurePlate.GetComponent<PressurePlate>().ResetPlate();

        // Track episode start time
        episodeStartTime = Time.time;

        // Randomize agent position
        float randomX = Random.Range(-spawnRange, spawnRange);
        float randomZ = Random.Range(-spawnRange, spawnRange);
        transform.localPosition = new Vector3(randomX, 0.5f, randomZ);

        // Randomize pressure plate position within defined bounds
        float plateRandomX = Random.Range(-6f, 13.0f);  // Adjust bounds based on level design
        float plateRandomZ = Random.Range(-18, 1.0f);  // Adjust bounds based on level design
        pressurePlate.transform.localPosition = new Vector3(plateRandomX, 0f, plateRandomZ);  // Keep Y at 0f for floor level


        // Optionally, reset the door if not already managed by the plate
        door.SetActive(true); // Close the door if you have this mechanism

        // Log the start of a new episode
        Debug.Log("New episode started. Agent and objects repositioned.");
    }


    public override void CollectObservations(VectorSensor sensor)
    {
        // Collect agent's position
        sensor.AddObservation(transform.localPosition);

        // Collect pressure plate's relative position
        sensor.AddObservation(pressurePlate.transform.localPosition - transform.localPosition);

        // Collect end goal's relative position
        sensor.AddObservation(endGoal.transform.localPosition - transform.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Check for time limit (85 seconds)
        if (Time.time - episodeStartTime >= 85f)
        {
            Debug.Log("Time limit reached. Ending episode.");
            EndEpisode(); // End episode after 1 minute 25 seconds
        }

        // Movement logic
        float moveX = actions.ContinuousActions[0]; // Forward/Backward
        float turn = actions.ContinuousActions[1];  // Left/Right

        // Log the received movement actions
        Debug.Log($"MoveX: {moveX}, Turn: {turn}");

        // Apply movement and rotation to the agent
        transform.localPosition += transform.forward * moveX * Time.deltaTime * 10f; // Adjust speed multiplier as needed
        transform.Rotate(new Vector3(0, turn * Time.deltaTime * 300f, 0)); // Adjust rotation speed as needed

        // Check if agent pressed the plate (you can also use TriggerEnter for more accuracy)
        if (!platePressed && Vector3.Distance(transform.localPosition, pressurePlate.transform.localPosition) < 1f)
        {
            platePressed = true;
            SetReward(5.0f); // Significant reward for pressing the plate
            door.SetActive(false); // Optionally open the door
            Debug.Log("Pressure plate pressed. Reward given: 5.0");
        }

        // Check if agent reached the end goal
        if (!goalReached && Vector3.Distance(transform.localPosition, endGoal.transform.localPosition) < 1f)
        {
            goalReached = true;
            SetReward(3.0f); // Reward for reaching the goal
            Debug.Log("End goal reached. Reward given: 3.0");
            EndEpisode();    // End the episode after reaching the goal
        }
    }

    // Detect collision with a wall (assume walls are tagged as "Wall")
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Wall"))
        {
            SetReward(-1.0f); // Negative reward for hitting a wall
            Debug.Log("Collision with wall. Penalty given: -1.0. Ending episode.");
            EndEpisode();     // End the episode
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // For manual testing using keyboard input
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Vertical");
        continuousActions[1] = Input.GetAxis("Horizontal");
    }
}
