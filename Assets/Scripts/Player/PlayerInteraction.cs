using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private PlayerHUD hud;

    private DoorController currentDoor;

    void Update()
    {
        // 🔴 Safety check for camera
        if (Camera.main == null)
        {
            Debug.LogError("Main Camera not found! Make sure it is tagged as 'MainCamera'");
            return;
        }

        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        // 🔍 Raycast check
        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            Debug.Log("Looking at: " + hit.collider.name);

            DoorController door = hit.collider.GetComponentInParent<DoorController>();

            if (door != null)
            {
                currentDoor = door;

                // 🔴 Safety check for HUD
                if (hud != null)
                {
                    hud.ShowPrompt("Press E to open door");
                }
                else
                {
                    Debug.LogError("HUD is not assigned in PlayerInteraction!");
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    Debug.Log("E pressed → toggling door");
                    door.ToggleDoor();
                }

                return;
            }
        }

        // If nothing detected
        currentDoor = null;

        if (hud != null)
        {
            hud.HidePrompt();
        }
    }
}