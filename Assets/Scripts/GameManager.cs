using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI statusText;
    public GameObject messagePanel;
    public Slider monsterHealthBar;
    public Button spawnMonsterButton;

    [Header("Audio")]
    public AudioSource musicSource;       // Para la música de fondo
    public AudioSource sfxSource;         // Para efectos puntuales
    public AudioClip damageSound;
    public AudioClip victorySound;
    public AudioClip defeatSound;

    [Header("Game State")]
    public string monsterTag = "Monster";
    public string turretTag = "Turret";

    private bool monsterSpawned = false;
    private bool gameEnded = false;

    public static event Action OnMonsterDied;
    public static event Action OnTurretDestroyed;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (messagePanel != null) messagePanel.SetActive(true);
        if (monsterHealthBar != null) monsterHealthBar.gameObject.SetActive(false); // Ocultar al inicio
        UpdateStatusText("Tienes que instanciar el monster para empezar la partida.");
    }

    void Update()
    {
        if (gameEnded) return;

        // Verificar si el monstruo ha sido instanciado
        if (!monsterSpawned)
        {
            GameObject monster = GameObject.FindGameObjectWithTag(monsterTag);
            if (monster != null)
            {
                monsterSpawned = true;

                // Empezar música de fondo cuando el monstruo aparece
                if (musicSource != null && !musicSource.isPlaying)
                {
                    musicSource.Play();
                }

                if (spawnMonsterButton != null) spawnMonsterButton.interactable = false;

                UpdateStatusText("¡Partida empezada! Protege la torre.");
                // Opcional: ocultar el panel después de unos segundos
                Invoke(nameof(HideMessage), 3f);
            }
        }
    }

    void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        if (messagePanel != null) messagePanel.SetActive(true);
    }

    void HideMessage()
    {
        if (!gameEnded && messagePanel != null)
        {
            messagePanel.SetActive(false);
        }
    }

    public void TriggerVictory()
    {
        if (gameEnded) return;
        gameEnded = true;
        if (statusText != null) statusText.color = Color.green;
        UpdateStatusText("¡VICTORIA! El monstruo ha destruido la torre.");

        if (sfxSource != null && victorySound != null)
            sfxSource.PlayOneShot(victorySound);

        if (musicSource != null) musicSource.Stop(); // Parar música de fondo

        Time.timeScale = 0.5f;
    }

    public void TriggerDefeat()
    {
        if (gameEnded) return;
        gameEnded = true;
        if (statusText != null) statusText.color = Color.red;
        UpdateStatusText("¡DERROTA! El monstruo ha muerto.");

        if (sfxSource != null && defeatSound != null)
            sfxSource.PlayOneShot(defeatSound);

        if (musicSource != null) musicSource.Stop();

    }

    public void PlayDamageSound()
    {
        if (sfxSource != null && damageSound != null)
            sfxSource.PlayOneShot(damageSound);
    }

    public void UpdateMonsterHealth(float current, float max)
    {
        if (monsterHealthBar != null)
        {
            monsterHealthBar.maxValue = max;
            monsterHealthBar.value = current;

            // Opcional: Mostrar la barra solo cuando el monstruo está en escena
            monsterHealthBar.gameObject.SetActive(true);
        }
    }
}
