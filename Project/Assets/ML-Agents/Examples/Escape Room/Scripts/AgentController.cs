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
    public float spawnRange = 8f;
    public float stuckThreshold = 0.1f;
    public int stuckCheckFrequency = 100;
    public int stuckGracePeriod = 5;
    public float proximityPenaltyThreshold = 3.0f;
    public float distanceRewardMultiplier = 1.0f;

    private bool platePressed = false;
    private bool goalReached = false;
    private bool doorIsOpen = false;
    private float totalReward = 0f;
    private Vector3 lastPosition;
    private int stepsSinceLastMoveCheck = 0;
    private int stuckChecks = 0;
    private float proximityTimer = 0f;
    private float lastDistanceToGoal = Mathf.Infinity;
    private float lastDistanceToPlate = Mathf.Infinity;
    private float lastHeading = 0f;

    public enum TrainingStage { PlatePressing, ReachingGoal }
    public TrainingStage currentStage = TrainingStage.PlatePressing;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        lastPosition = transform.localPosition;
    }

    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        platePressed = false;
        goalReached = false;
        doorIsOpen = false;
        totalReward = 0f;
        proximityTimer = 0f;
        stuckChecks = 0;
        lastDistanceToGoal = Mathf.Infinity;
        lastDistanceToPlate = Mathf.Infinity;
        lastHeading = transform.eulerAngles.y;

        pressurePlate.GetComponent<PressurePlate>().ResetPlate();
        door.GetComponent<Door>().CloseDoor();

        if (currentStage == TrainingStage.PlatePressing)
        {
            RandomizePositions();
        }
        else if (currentStage == TrainingStage.ReachingGoal)
        {
            platePressed = true;
            door.GetComponent<Door>().OpenDoor();
            doorIsOpen = true;
            RandomizePositions();
        }
    }

    private void RandomizePositions()
    {
        float randomX = Random.Range(-4f, 14.0f);
        float randomZ = Random.Range(-4f, 14.0f);
        transform.localPosition = new Vector3(randomX, -0.75f, randomZ);

        float plateRandomX = Random.Range(-6f, 13.0f);
        float plateRandomZ = Random.Range(-18f, 1.0f);
        pressurePlate.transform.localPosition = new Vector3(plateRandomX, 0f, plateRandomZ);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float maxRange = 20f;
        sensor.AddObservation(transform.localPosition / maxRange);

        float normalizedYaw = Mathf.Repeat(transform.eulerAngles.y, 360f) / 180f - 1f;
        sensor.AddObservation(normalizedYaw);

        sensor.AddObservation((pressurePlate.transform.localPosition - transform.localPosition) / maxRange);
        sensor.AddObservation((endGoal.transform.localPosition - transform.localPosition) / maxRange);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        HandleMovement(actions);
        HandlePenalties();
        HandleRewards();

        AddReward(-0.001f);  // Small time penalty

        if (StepCount >= MaxStep && !goalReached)
        {
            AddReward(-0.5f);
            EndEpisode();
        }
    }

    private void HandleMovement(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float turn = actions.ContinuousActions[1];
        float moveSpeed = 5f;
        float rotationSpeed = 100f;

        if (Mathf.Abs(moveX) > 0.01f)
        {
            Vector3 movement = transform.forward * moveX * Time.fixedDeltaTime * moveSpeed;
            rb.MovePosition(rb.position + movement);
        }

        if (Mathf.Abs(turn) > 0.01f)
        {
            Quaternion rotation = Quaternion.Euler(new Vector3(0f, turn * Time.fixedDeltaTime * rotationSpeed, 0f));
            rb.MoveRotation(rb.rotation * rotation);
        }
    }

    private void HandlePenalties()
    {
        CheckForStuckAgent();
        CheckProximityToDoor();
        CheckIncorrectSequence();
        CheckForBacktracking();
    }

    private void CheckProximityToDoor()
    {
        if (Vector3.Distance(transform.localPosition, door.transform.localPosition) < 1.5f && !doorIsOpen)
        {
            proximityTimer += Time.deltaTime;
            if (proximityTimer > proximityPenaltyThreshold)
            {
                AddReward(-0.1f);
                proximityTimer = 0f;
            }
        }
        else
        {
            proximityTimer = 0f;
        }
    }

    private void CheckForStuckAgent()
    {
        stepsSinceLastMoveCheck++;
        if (stepsSinceLastMoveCheck >= stuckCheckFrequency)
        {
            float distanceMoved = Vector3.Distance(transform.localPosition, lastPosition);
            if (distanceMoved < stuckThreshold)
            {
                stuckChecks++;
                if (stuckChecks > stuckGracePeriod)
                {
                    AddReward(-0.1f);
                    stuckChecks = 0;
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

    private void CheckForBacktracking()
    {
        float currentHeading = transform.eulerAngles.y;
        float headingChange = Mathf.Abs(currentHeading - lastHeading);

        if (headingChange > 90f)
        {
            AddReward(-0.05f);
        }
        lastHeading = currentHeading;
    }

    private void CheckIncorrectSequence()
    {
        if (!platePressed && Vector3.Distance(transform.localPosition, door.transform.localPosition) < 1.5f)
        {
            AddReward(-0.5f);
        }
    }

    private void HandleRewards()
    {
        if (currentStage == TrainingStage.PlatePressing && !platePressed)
        {
            float distanceToPlate = Vector3.Distance(transform.localPosition, pressurePlate.transform.localPosition);
            if (distanceToPlate < lastDistanceToPlate)
            {
                AddReward(0.01f);
            }
            lastDistanceToPlate = distanceToPlate;

            RewardForAlignment(pressurePlate);

            if (distanceToPlate < 1f)
            {
                platePressed = true;
                AddReward(1.0f);
                pressurePlate.GetComponent<PressurePlate>().ActivatePlate();
                door.GetComponent<Door>().OpenDoor();
                doorIsOpen = true;
                currentStage = TrainingStage.ReachingGoal;
            }
        }
        else if (currentStage == TrainingStage.ReachingGoal && doorIsOpen && !goalReached)
        {
            float distanceToGoal = Vector3.Distance(transform.localPosition, endGoal.transform.localPosition);
            if (distanceToGoal < lastDistanceToGoal)
            {
                AddReward(0.01f);
            }
            lastDistanceToGoal = distanceToGoal;

            RewardForAlignment(endGoal);

            if (distanceToGoal < 1f)
            {
                ReachedEndGoal();
            }
        }
    }

    private void RewardForAlignment(GameObject target)
    {
        Vector3 directionToTarget = (target.transform.localPosition - transform.localPosition).normalized;
        float alignment = Vector3.Dot(transform.forward, directionToTarget);
        AddReward(alignment * 0.01f);
    }

    public void ReachedEndGoal()
    {
        if (!goalReached)
        {
            goalReached = true;
            float cumulativeReward = 2.0f + (totalReward >= 0 ? 1.0f : 0.0f);
            AddReward(cumulativeReward);
            EndEpisode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Wall"))
        {
            AddReward(-0.2f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Vertical");
        continuousActions[1] = Input.GetAxis("Horizontal");
    }
}
