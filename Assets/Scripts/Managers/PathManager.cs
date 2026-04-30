using UnityEngine;

public class PathManager : MonoBehaviour
{
    public static PathManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public void RecalculatePath()
    {
        Debug.Log("Recalculating path...");
    }
}