using UnityEngine;
using System.Collections;
using Unity.MLAgents;

public class EnvironmentManager : MonoBehaviour
{
    public EscapeRoomAgent agentRed;  // Assign in Inspector
    public EscapeRoomAgent agentBlue; // Assign in Inspector

    private bool redAgentEscaped = false;
    private bool blueAgentEscaped = false;

    // Curriculum Variables
    private int maxPhases = 3;    // Total number of phases in the curriculum
    public GameObject[] lavaBlocks; // All potential lava blocks in the scene
    private int currentPhase = 0;  // Tracks the current phase

    private void Start()
    {
        // Get the initial difficulty level from Unity's environment parameters
        var envParams = Academy.Instance.EnvironmentParameters;
        currentPhase = Mathf.RoundToInt(envParams.GetWithDefault("difficulty", 0));

      //  Debug.Log($"Starting with Difficulty Level (Phase): {currentPhase}");

        AdjustEnvironmentForPhase(); // Apply the initial difficulty level
    }

    public void AgentReachedGoal(EscapeRoomAgent agent)
    {
        if (agent == agentRed)
        {
            if (!redAgentEscaped)
            {
                redAgentEscaped = true;
             //   Debug.Log("Agent Red has reached the goal.");
            }
        }
        else if (agent == agentBlue)
        {
            if (!blueAgentEscaped)
            {
                blueAgentEscaped = true;
             //   Debug.Log("Agent Blue has reached the goal.");
            }
        }

        if (redAgentEscaped && blueAgentEscaped)
        {
          //  Debug.Log("Both agents have escaped! Resetting environment.");
            IncrementPhase(); // Update the phase if needed
            ResetEnvironment();
        }
        else
        {
          //  Debug.Log($"Waiting for both agents. Red: {redAgentEscaped}, Blue: {blueAgentEscaped}");
        }
    }

    public bool AreAllAgentsAtGoal()
    {
        return redAgentEscaped && blueAgentEscaped;
    }

    public void ResetEnvironment()
    {
        StartCoroutine(DelayedReset());
    }

    public IEnumerator DelayedReset()
    {
        yield return new WaitForEndOfFrame();

        redAgentEscaped = false;
        blueAgentEscaped = false;

        AdjustEnvironmentForPhase(); // Apply phase-specific adjustments

        if (agentRed != null)
        {
            agentRed.gameObject.SetActive(true);
            agentRed.EnableAgent();
            agentRed.OnEpisodeBegin();
        }

        if (agentBlue != null)
        {
            agentBlue.gameObject.SetActive(true);
            agentBlue.EnableAgent();
            agentBlue.OnEpisodeBegin();
        }

      //  Debug.Log($"Environment reset for Phase {currentPhase}. Both agents are re-enabled.");
    }

    private void AdjustEnvironmentForPhase()
    {
        // Activate/deactivate lava blocks based on the current phase
        for (int i = 0; i < lavaBlocks.Length; i++)
        {
            // Adjust based on the number of blocks per difficulty
            if (currentPhase == 0) // Difficulty 0: 1 lava block
            {
                lavaBlocks[i].SetActive(i == 0);
            }
            else if (currentPhase == 1) // Difficulty 1: 2 lava blocks
            {
                lavaBlocks[i].SetActive(i < 2);
            }
            else if (currentPhase == 2) // Difficulty 2: 3 lava blocks
            {
                lavaBlocks[i].SetActive(i < 3);
            }
            else if (currentPhase == 3) // Difficulty 3: All 4 lava blocks
            {
                lavaBlocks[i].SetActive(i < 4);
            }
        }

        // Additional logs and setup for each phase
        switch (currentPhase)
        {
            case 0:
              //  Debug.Log("Phase 0: Easy setup, 1 lava block.");
                break;
            case 1:
              //  Debug.Log("Phase 1: Moderate setup, 2 lava blocks.");
                break;
            case 2:
              //  Debug.Log("Phase 2: Hard setup, 3 lava blocks.");
                break;
            case 3:
              //  Debug.Log("Phase 3: Maximum setup, all 4 lava blocks.");
                break;
            default:
             //   Debug.LogWarning("Unknown phase configuration.");
                break;
        }
    }

    private void IncrementPhase()
    {
        if (currentPhase < maxPhases - 1)
        {
            currentPhase++;
           // Debug.Log($"Phase incremented to {currentPhase}.");
        }
        else
        {
           // Debug.Log("Maximum phase reached. No further increments.");
        }
    }

}
