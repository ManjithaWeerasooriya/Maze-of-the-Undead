using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private Image healthFill;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TextMeshProUGUI interactionText;

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
}