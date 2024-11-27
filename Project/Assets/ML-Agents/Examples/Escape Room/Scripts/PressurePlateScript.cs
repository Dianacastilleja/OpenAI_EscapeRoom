/// PressurePlateScript.cs
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    public Door door;                   // Reference to the Door script
    public Material activatedMaterial;  // Material to indicate the plate is activated
    public Material defaultMaterial;    // Default material
    public Renderer plateRenderer;      // The Renderer component for the pressure plate

    private bool isActivated = false;   // To prevent repeated activations

    public bool IsActivated
    {
        get { return isActivated; }
    }

    private void Start()
    {
        // Ensure the plate starts with its default material
        ResetPlate();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"PressurePlate: Triggered by {other.gameObject.name} with tag {other.tag}.");

        // Check if the object triggering is the Agent and the plate is not already activated
        if (!isActivated && other.CompareTag("Agent"))
        {
            ActivatePlate();

            if (door != null)
            {
                door.OpenDoor();
                Debug.Log("PressurePlate: Activated and door opened.");
            }
            else
            {
                Debug.LogWarning("PressurePlate: No Door reference found.");
            }
        }
        else if (!other.CompareTag("Agent"))
        {
            Debug.LogWarning("PressurePlate: Triggered by non-agent object.");
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
        else
        {
            Debug.LogWarning("PressurePlate: Attempted to activate an already activated plate.");
        }
    }

    public void ResetPlate()
    {
        if (isActivated) // Only log and reset if necessary
        {
            Debug.Log("PressurePlate: Resetting to default state.");
        }
        isActivated = false;

        if (plateRenderer != null && defaultMaterial != null)
        {
            plateRenderer.material = defaultMaterial;
        }
    }
}
