using UnityEngine;

public class EndGoal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("EndGoal: OnTriggerEnter called with " + other.gameObject.name);

        // Check if the collided object has the "Agent" tag
        if (other.CompareTag("Agent"))
        {
            // Try to get the EscapeRoomAgent component
            EscapeRoomAgent agent = other.GetComponent<EscapeRoomAgent>();

            if (agent != null)
            {
                // Call the ReachedEndGoal method on the agent
                agent.ReachedEndGoal();
            }
            else
            {
                Debug.LogError("EndGoal: No EscapeRoomAgent component found on the Agent!");
            }
        }
        else
        {
            Debug.LogWarning("EndGoal: Collided object does not have the 'Agent' tag.");
        }
    }
}
