using UnityEngine;

public class EndGoal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("EndGoal: OnTriggerEnter called with " + other.gameObject.name);
        if (other.CompareTag("Agent"))
        {
            // Notify the agent that the end goal has been reached
            EscapeRoomAgent agent = other.GetComponent<EscapeRoomAgent>();
            if (agent != null)
            {
                agent.ReachedEndGoal();
            }
        }
    }
}
