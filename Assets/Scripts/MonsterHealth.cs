using UnityEngine;

public class MonsterHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        // Inicializar la barra al aparecer
        UpdateUI();
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"[MonsterHealth] {gameObject.name} recibe {amount} de daño. Vida restante: {currentHealth}");

        UpdateUI();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayDamageSound();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateUI()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateMonsterHealth(currentHealth, maxHealth);
        }
    }

    void Die()
    {
        Debug.Log($"[MonsterHealth] {gameObject.name} ha muerto. Eliminando de la escena.");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerDefeat();
        }
        Destroy(gameObject);
    }
}
