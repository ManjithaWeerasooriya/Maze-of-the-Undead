using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    public Image healthBarFill; // drag from UI

    public GameObject gameOverPanel; // your GameOver UI

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();

        gameOverPanel.SetActive(false);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateHealthBar()
    {
        healthBarFill.fillAmount = currentHealth / maxHealth;
    }

    void Die()
    {
        gameOverPanel.SetActive(true);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}