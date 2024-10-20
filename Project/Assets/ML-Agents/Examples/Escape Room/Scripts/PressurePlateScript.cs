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
        // Ensure the plate starts with its default material
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

            // Change the material to indicate the plate is activated
            if (plateRenderer != null && activatedMaterial != null)
            {
                plateRenderer.material = activatedMaterial;
            }

            // Open the door and keep it open for the rest of the episode
            door.OpenDoor();
        }
    }

    // We no longer need OnTriggerExit, as the door should stay open once activated
    // Remove the exit condition for keeping the door open throughout the episode

    // Method to reset the pressure plate and door to their default state
    public void ResetPlate()
    {
        isActivated = false;

        // Reset the material of the plate to the default one
        if (plateRenderer != null && defaultMaterial != null)
        {
            plateRenderer.material = defaultMaterial;
        }

        // Reset the door to its closed position
        door.CloseDoor();
    }
}
