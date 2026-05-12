using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private Image healthFill;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TextMeshProUGUI interactionText;
    [SerializeField] private TextMeshProUGUI debugModeIndicator;

    private PathfindingDebugVisualizer debugVisualizer;

    private void Start()
    {
        // Find the debug visualizer in the scene
        debugVisualizer = FindObjectOfType<PathfindingDebugVisualizer>();
        
        if (debugModeIndicator != null)
        {
            debugModeIndicator.text = "";
        }
    }

    private void Update()
    {
        // Update debug mode indicator
        if (debugModeIndicator != null && debugVisualizer != null)
        {
            if (debugVisualizer.IsDebugModeEnabled)
            {
                debugModeIndicator.text = $"[DEBUG] {debugVisualizer.CurrentAlgorithm}";
                debugModeIndicator.color = Color.yellow;
            }
            else
            {
                debugModeIndicator.text = "";
            }
        }
    }

    public void SetHealth(float current, float max)
    {
        if (max <= 0) return;
        healthFill.fillAmount = current / max;
    }

    public void ShowPrompt(string message)
    {
        interactionText.text = message;
        interactionPrompt.SetActive(true);
    }

    public void HidePrompt()
    {
        interactionPrompt.SetActive(false);
    }

    public PathfindingDebugVisualizer GetDebugVisualizer()
    {
        return debugVisualizer;
    }
}