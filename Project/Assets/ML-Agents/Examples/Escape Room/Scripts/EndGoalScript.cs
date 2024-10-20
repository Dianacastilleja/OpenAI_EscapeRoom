using UnityEngine;

public class EndGoal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is the agent
        if (other.CompareTag("Agent"))
        {
            // Print to the console
            Debug.Log("EndGoal Reached!");

            // Optionally, if you want to directly give a reward here (though it's handled in the agent script):
            EscapeRoomAgent agent = other.GetComponent<EscapeRoomAgent>();
            if (agent != null)
            {
                agent.SetReward(10.0f); // Big reward for reaching the goal
                agent.EndEpisode();    // End the episode
            }
        }
    }
}
