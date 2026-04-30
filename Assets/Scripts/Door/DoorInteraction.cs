using UnityEngine;

public class DoorInteraction : MonoBehaviour
{
    private static readonly int IsOpenParameter = Animator.StringToHash("isOpen");

    private Animator animator;
    private bool isOpen = false;

    [Header("Interaction Settings")]
    public float interactionDistance = 2.5f;
    public string interactKey = "e";

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void ToggleDoor()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            return;
        }

        isOpen = !isOpen;
        animator.SetBool(IsOpenParameter, isOpen);
    }

    public bool IsPlayerInRange(Transform playerTransform)
    {
        return Vector3.Distance(transform.position, playerTransform.position) <= interactionDistance;
    }
}
