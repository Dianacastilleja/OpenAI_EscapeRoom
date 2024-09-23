using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    public Door door;  // Reference to the Door object to control
    public Material activatedMaterial;  // Material to switch to when the plate is activated
    public Material defaultMaterial;  // Original material of the pressure plate
    public Renderer plateRenderer;  // The Renderer component for the pressure plate

    private bool isActivated = false;  // To prevent repeated activations

    private void Start()
    {
        // Make sure the plate starts with its default material
        if (plateRenderer != null && defaultMaterial != null)
        {
            plateRenderer.material = defaultMaterial;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Agent") && !isActivated)
        {
            isActivated = true;

            // Change the material to green (activated)
            if (plateRenderer != null && activatedMaterial != null)
            {
                plateRenderer.material = activatedMaterial;
            }

            // Open the door
            door.OpenDoor();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Agent") && isActivated)
        {
            isActivated = false;

            // Change the material back to the default material
            if (plateRenderer != null && defaultMaterial != null)
            {
                plateRenderer.material = defaultMaterial;
            }

            // Close the door if the agent steps off the plate (optional)
            door.CloseDoor();
        }
    }
}
