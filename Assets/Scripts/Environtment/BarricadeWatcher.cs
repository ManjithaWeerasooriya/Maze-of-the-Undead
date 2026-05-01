using UnityEngine;

public class BarricadeWatcher : MonoBehaviour
{
    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        // Check if moved
        if (Vector3.Distance(transform.position, lastPosition) > 0.5f)
        {
            PathManager.Instance.RecalculatePath();
            lastPosition = transform.position;
        }
    }
}