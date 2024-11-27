/// AgentController.cs
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class EscapeRoomAgent : Agent
{
    private Rigidbody rb;
    public GameObject pressurePlate;
    public GameObject endGoal;
    public GameObject door;


    public float stuckThreshold = 0.1f;
    public int stuckCheckFrequency = 100;
    public int stuckGracePeriod = 5;
    private int stepsSinceLastMoveCheck = 0;
    private int stuckChecks = 0;

    private Vector3 lastPosition;
    private float lastDistanceToGoal;
    private float lastDistanceToPlate;

    private bool goalReached = false;

    private Door doorScript;
    private PressurePlate plateScript;

    public enum TrainingStage { PlatePressing, ReachingGoal }
    public TrainingStage currentStage = TrainingStage.PlatePressing;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        lastPosition = transform.localPosition;

        // Cache the door and pressure plate scripts
        doorScript = door.GetComponent<Door>();
        plateScript = pressurePlate.GetComponent<PressurePlate>();
    }

    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        goalReached = false;
        
        stepsSinceLastMoveCheck = 0;
        stuckChecks = 0;

        currentStage = TrainingStage.PlatePressing;

        // Reset door and pressure plate states
        if (doorScript != null)
        {
            doorScript.CloseDoor();
        }

        if (plateScript != null)
        {
            plateScript.ResetPlate();
        }

        RandomizePositions();

        lastDistanceToPlate = Vector3.Distance(transform.localPosition, pressurePlate.transform.localPosition);
        lastDistanceToGoal = Vector3.Distance(transform.localPosition, endGoal.transform.localPosition);
    }

    private void RandomizePositions()
    {
        // Adjusted ranges to prevent immediate activation
        float agentRandomX = Random.Range(-4f, 14.0f);
        float agentRandomZ = Random.Range(-4f, 14.0f);
        transform.localPosition = new Vector3(agentRandomX, -0.75f, agentRandomZ);

        float plateRandomX = Random.Range(-6f, 13.0f);
        float plateRandomZ = Random.Range(-18f, -5.0f); // Keep plate away from agent's area
        pressurePlate.transform.localPosition = new Vector3(plateRandomX, 0f, plateRandomZ);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float maxRange = 20f;

        // Agent's position and orientation
        sensor.AddObservation(transform.localPosition / maxRange);
        sensor.AddObservation(Mathf.Repeat(transform.eulerAngles.y, 360f) / 180f - 1f);

        // Agent's velocity
        sensor.AddObservation(rb.velocity.x);
        sensor.AddObservation(rb.velocity.z);

        // Door state
        sensor.AddObservation(doorScript.IsOpen ? 1.0f : 0.0f);

        // Relative positions
        sensor.AddObservation((door.transform.localPosition - transform.localPosition) / maxRange);
        sensor.AddObservation((pressurePlate.transform.localPosition - transform.localPosition) / maxRange);
        sensor.AddObservation((endGoal.transform.localPosition - transform.localPosition) / maxRange);

        // Directional observations
        Vector3 directionToPlate = (pressurePlate.transform.localPosition - transform.localPosition).normalized;
        sensor.AddObservation(Vector3.Dot(transform.forward, directionToPlate));

        Vector3 directionToGoal = (endGoal.transform.localPosition - transform.localPosition).normalized;
        sensor.AddObservation(Vector3.Dot(transform.forward, directionToGoal));

        // Angle to the end goal
        float angleToGoal = Vector3.SignedAngle(transform.forward, directionToGoal, Vector3.up) / 180f;
        sensor.AddObservation(angleToGoal);

        // Raycast observations
        float rayDistance = 10f;
        float[] rayAngles = { -90f, -45f, -22.5f, 0f, 22.5f, 45f, 90f };
        foreach (float angle in rayAngles)
        {
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, rayDistance))
            {
                sensor.AddObservation(hit.distance / rayDistance);
                sensor.AddObservation(hit.collider.CompareTag("Wall") ? 1.0f : 0.0f); // Indicates if wall was hit
            }
            else
            {
                sensor.AddObservation(1f);
                sensor.AddObservation(0.0f); // No wall detected
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        HandleMovement(actions);
        HandlePenalties();
        HandleRewards();

        AddReward(-0.001f); // Small time penalty to encourage efficiency

        if (StepCount >= MaxStep && !goalReached)
        {
            AddReward(-0.5f); // Penalty for failing to complete the task in time
            EndEpisode();
        }
    }

    private void HandleMovement(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float turn = actions.ContinuousActions[1];
        float moveSpeed = 5f;
        float rotationSpeed = 100f;

        Vector3 movement = transform.forward * moveX * Time.fixedDeltaTime * moveSpeed;
        rb.MovePosition(rb.position + movement);

        Quaternion rotation = Quaternion.Euler(0f, turn * Time.fixedDeltaTime * rotationSpeed, 0f);
        rb.MoveRotation(rb.rotation * rotation);
    }

    private void HandlePenalties()
    {
        CheckForStuckAgent();
        PenalizeProximityToDoor();
        PenalizeNearPlateAfterPress();
        PenalizeProximityToWalls();
    }

    private void PenalizeProximityToDoor()
    {
        if (!doorScript.IsOpen && Vector3.Distance(transform.localPosition, door.transform.localPosition) <= 1f)
        {
            AddReward(-0.1f); // Penalty for being near the door when it's closed
        }
    }

    private void PenalizeNearPlateAfterPress()
    {
        if (plateScript.IsActivated && Vector3.Distance(transform.localPosition, pressurePlate.transform.localPosition) <= 1f)
        {
            AddReward(-0.05f); // Penalty for lingering on the plate after activation
        }
    }

    private void PenalizeProximityToWalls()
    {
        float wallProximityThreshold = 1.0f; // Distance at which to start penalizing
        RaycastHit hit;
        // Check in multiple directions
        foreach (float angle in new[] { -90f, -45f, 0f, 45f, 90f })
        {
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
            if (Physics.Raycast(transform.position, direction, out hit, wallProximityThreshold))
            {
                if (hit.collider.CompareTag("Wall"))
                {
                    AddReward(-0.01f); // Small penalty for being close to a wall
                }
            }
        }
    }

    private void CheckForStuckAgent()
    {
        stepsSinceLastMoveCheck++;
        if (stepsSinceLastMoveCheck >= stuckCheckFrequency)
        {
            if (Vector3.Distance(transform.localPosition, lastPosition) < stuckThreshold)
            {
                stuckChecks++;
                if (stuckChecks > stuckGracePeriod)
                {
                    AddReward(-0.1f); // Penalty for being stuck
                    EndEpisode();      // End episode if stuck for too long
                }
            }
            else
            {
                stuckChecks = 0;
            }
            lastPosition = transform.localPosition;
            stepsSinceLastMoveCheck = 0;
        }
    }

    public void ReachedEndGoal()
    {
        if (!goalReached)
        {
            goalReached = true;
            AddReward(5.0f); // Reward for successfully reaching the goal
            EndEpisode();
        }
    }

    private void HandleRewards()
    {
        if (currentStage == TrainingStage.PlatePressing && !plateScript.IsActivated)
        {
            // Encourage the agent to move closer to the pressure plate
            float distanceToPlate = Vector3.Distance(transform.localPosition, pressurePlate.transform.localPosition);
            float progress = lastDistanceToPlate - distanceToPlate;

            AddReward(progress * 0.1f);
            lastDistanceToPlate = distanceToPlate;
        }
        else if (currentStage == TrainingStage.PlatePressing && plateScript.IsActivated)
        {
            AddReward(2.0f); // Reward for activating the pressure plate
            currentStage = TrainingStage.ReachingGoal;

            lastDistanceToGoal = Vector3.Distance(transform.localPosition, endGoal.transform.localPosition);
        }
        else if (currentStage == TrainingStage.ReachingGoal && doorScript.IsOpen)
        {
            // Encourage the agent to move towards the end goal
            float distanceToGoal = Vector3.Distance(transform.localPosition, endGoal.transform.localPosition);
            float progress = lastDistanceToGoal - distanceToGoal;
            AddReward(progress * 0.1f);
            lastDistanceToGoal = distanceToGoal;

            Vector3 directionToGoal = (endGoal.transform.localPosition - transform.localPosition).normalized;
            float angleToGoal = Vector3.Angle(transform.forward, directionToGoal);

            float orientationPenalty = -Mathf.Abs(angleToGoal) / 180f * 0.001f;
            AddReward(orientationPenalty);

            if (distanceToGoal < 1f)
            {
                ReachedEndGoal();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Wall"))
        {
            AddReward(-0.1f);
            Debug.Log("Agent collided with wall and received penalty.");
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // For manual testing with keyboard input
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Vertical");   // Forward/backward movement
        continuousActions[1] = Input.GetAxis("Horizontal"); // Turning left/right
    }
}
