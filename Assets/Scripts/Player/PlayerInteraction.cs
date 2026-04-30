using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float raycastDistance = 3f;
    public LayerMask interactableLayer;

    [Header("UI")]
    public GameObject interactPromptUI; // optional "Press E" UI element

    private DoorInteraction currentDoor;

    void Update()
    {
        DetectDoor();

        if (currentDoor != null && IsInteractPressed())
        {
            currentDoor.ToggleDoor();
        }
    }

    void DetectDoor()
    {
        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            ClearCurrentDoor();
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, raycastDistance, interactableLayer))
        {
            DoorInteraction door = hit.collider.GetComponentInParent<DoorInteraction>();
            if (door != null && door.IsPlayerInRange(transform))
            {
                currentDoor = door;
                if (interactPromptUI) interactPromptUI.SetActive(true);
                return;
            }
        }

        ClearCurrentDoor();
    }

    void ClearCurrentDoor()
    {
        currentDoor = null;
        if (interactPromptUI) interactPromptUI.SetActive(false);
    }

    bool IsInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E);
#else
        return false;
#endif
    }
}
