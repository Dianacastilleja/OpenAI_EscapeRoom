using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    public Door door;                   // Reference to the Door script
    public Material activatedMaterial;  // Material to indicate the plate is activated
    public Material defaultMaterial;    // Default material
    public Renderer plateRenderer;      // The Renderer component for the pressure plate

    private bool isActivated = false;   // Tracks whether this plate is activated
    private int agentsOnPlate = 0;      // Tracks the number of agents currently on the plate

    public bool IsActivated
    {
        get { return isActivated; }
    }

    private void Start()
    {
        // Ensure the plate starts with its default material
        ResetPlate();
        Debug.Log($"PressurePlate: Initialized at position {transform.localPosition}.");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"PressurePlate: Triggered by {other.gameObject.name} with tag {other.tag}.");

        // Check if the object triggering is an agent
        if (other.CompareTag("Agent"))
        {
            agentsOnPlate++;
            ActivatePlate();

            // Check if all plates are activated
            if (AllPlatesActivated())
            {
                door?.OpenDoor();
                Debug.Log("PressurePlate: All plates activated, door opened.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"PressurePlate: Exit triggered by {other.gameObject.name} with tag {other.tag}.");

        // Check if the object exiting is an agent
        if (other.CompareTag("Agent"))
        {
            agentsOnPlate--;

            // Deactivate plate if no agents are on it
            if (agentsOnPlate <= 0)
            {
                ResetPlate();
                Debug.Log("PressurePlate: Deactivated as no agents are on it.");
            }
        }
    }

    public void ActivatePlate()
    {
        if (!isActivated)
        {
            isActivated = true;

            if (plateRenderer != null && activatedMaterial != null)
            {
                plateRenderer.material = activatedMaterial;
            }

            Debug.Log("PressurePlate: Activated successfully.");
        }
    }

    public void ResetPlate()
    {
        isActivated = false;

        if (plateRenderer != null && defaultMaterial != null)
        {
            plateRenderer.material = defaultMaterial;
        }

        Debug.Log("PressurePlate: Reset to default state.");
    }

    private bool AllPlatesActivated()
    {
        // Check if all pressure plates in the scene are activated
        PressurePlate[] allPlates = FindObjectsOfType<PressurePlate>();
        foreach (PressurePlate plate in allPlates)
        {
            if (!plate.IsActivated)
            {
                return false; // If any plate is not activated, return false
            }
        }
        return true; // All plates are activated
    }
}
