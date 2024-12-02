using UnityEngine;

public class EndGoal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"EndGoal: OnTriggerEnter called with {other.gameObject.name}.");

        // Check if the collided object has the "Agent" tag
        if (other.CompareTag("Agent"))
        {
            // Try to get the EscapeRoomAgent component
            EscapeRoomAgent agent = other.GetComponent<EscapeRoomAgent>();

            if (agent != null)
            {
                // Call the ReachedEndGoal method on the agent
                agent.ReachedEndGoal();
                Debug.Log($"EndGoal: Agent {other.gameObject.name} has reached the end goal.");
            }
            else
            {
                Debug.LogError($"EndGoal: No EscapeRoomAgent component found on {other.gameObject.name}!");
            }
        }
        else
        {
            Debug.LogWarning($"EndGoal: Collided object {other.gameObject.name} does not have the 'Agent' tag.");
        }
    }
}
