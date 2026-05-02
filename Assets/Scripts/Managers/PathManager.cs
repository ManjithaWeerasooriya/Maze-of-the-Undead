using UnityEngine;

public class PathManager : MonoBehaviour
{
    public static PathManager Instance;
    public static event System.Action OnPathChanged;

    void Awake()
    {
        Instance = this;
    }

    public void RecalculatePath()
    {
        OnPathChanged?.Invoke();
    }
}