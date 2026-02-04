using UnityEngine;
using System.Collections;

// Este script hace que la torreta apunte siempre al monster en la escena y active la animación al instanciarse

public class TurretAimer : MonoBehaviour
{
    [Header("Disparo")]
    [Tooltip("Prefab de la bala a disparar")]
    public GameObject bulletPrefab;
    [Tooltip("Punto de spawn de la bala (puede ser la boca del cañón)")]
    public Transform firePoint;
    [Tooltip("Tiempo entre disparos (segundos)")]
    public float fireCooldown = 0.5f; // Aumentado para reducir la cadencia
    private float fireTimer = 0f;

    [Tooltip("Tag del objetivo a seguir (Monster)")]
    public string targetTag = "Monster";

    [Tooltip("Transform de la parte rotatoria de la torreta (Bone_Padre/Bone_Rotacion_Base)")]
    public Transform rotatingPart;

    [Tooltip("Animator de la torreta")]
    public Animator animator;

    [Tooltip("Nombre del trigger para activar la animación de despliegue")]
    public string activateTrigger = "ActiveTurret";

    [Tooltip("Corrección fina en Y para ajustar la puntería (grados)")]
    public float yAxisCorrection = 0f;

    private Transform target;
    private bool animationPlayed = false;
    private bool readyToAim = false;
    private Vector3 initialLocalEulerAngles; // Guardamos los ángulos iniciales del prefab

    void Start()
    {
        // Guardamos la rotación inicial del prefab para mantenerla durante el tracking
        if (rotatingPart != null)
        {
            initialLocalEulerAngles = rotatingPart.localEulerAngles;
        }

        // Empezamos permitiendo la rotación para evitar bloqueos, la animación puede correr en paralelo
        readyToAim = true;

        if (animator != null && !animationPlayed)
            StartCoroutine(PlayActivationAnim());
    }

    IEnumerator PlayActivationAnim()
    {
        // Reducimos el tiempo de espera
        yield return new WaitForSeconds(0.5f);
        if (animator != null)
        {
            animator.SetTrigger(activateTrigger);
        }
        animationPlayed = true;
    }

    // Este método debe ser llamado por un Animation Event al final de la animación de despliegue
    public void OnDeployAnimationFinished()
    {
        if (readyToAim) return;
        readyToAim = true;
    }

    // Cambiado a LateUpdate para que la rotación se aplique después de que el Animator procese las animaciones
    void LateUpdate()
    {
        if (!readyToAim)
            return;

        if (target == null)
        {
            var monsterObj = GameObject.FindGameObjectWithTag(targetTag);
            if (monsterObj != null)
                target = monsterObj.transform;
            else
                return;
        }

        Vector3 turretPos = rotatingPart ? rotatingPart.position : transform.position;
        Vector3 targetPos = target.position;

        Debug.Log($"[TurretAimer] Monster en: {targetPos}");
        Debug.Log($"[TurretAimer] Torreta en: {turretPos}");

        if (rotatingPart)
        {
            // Calculamos dirección en espacio GLOBAL
            Vector3 lookDir = targetPos - turretPos;
            Debug.Log($"[TurretAimer] Dirección ANTES de bloquear Y: {lookDir}");
            lookDir.y = 0f; // CORREGIDO: Debe ser 0, no 0.5
            Debug.Log($"[TurretAimer] Dirección DESPUÉS de bloquear Y: {lookDir}");

            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                Vector3 targetEuler = targetRotation.eulerAngles;

                Debug.Log($"[TurretAimer] Ángulo Y calculado: {targetEuler.y}");
                Debug.Log($"[TurretAimer] yAxisCorrection: {yAxisCorrection}");
                Debug.Log($"[TurretAimer] initialLocalEulerAngles: {initialLocalEulerAngles}");

                // SIMPLIFICADO: Solo usamos el ángulo Y, forzamos X y Z a 0
                float finalY = targetEuler.y + yAxisCorrection;

                Debug.Log($"[TurretAimer] Ángulo Y FINAL (con corrección): {finalY}");

                // Probamos con rotación LOCAL para AR
                rotatingPart.localRotation = Quaternion.Euler(0f, finalY, 0f);

                // Debug: Hacia dónde apunta la torreta
                Vector3 turretForward = rotatingPart.forward;
                Vector3 expectedDir = lookDir.normalized;
                Debug.Log($"[TurretAimer] Dirección ESPERADA (normalizada): {expectedDir}");
                Debug.Log($"[TurretAimer] Dirección REAL (forward): {turretForward}");
                Debug.Log($"[TurretAimer] rotatingPart.localRotation: {rotatingPart.localEulerAngles}");
                Debug.Log($"[TurretAimer] rotatingPart.rotation (global): {rotatingPart.eulerAngles}");
                Debug.DrawRay(turretPos, expectedDir * 2f, Color.green, 0.1f); // Verde = donde debería apuntar
                Debug.DrawRay(turretPos, turretForward * 2f, Color.blue, 0.1f); // Azul = donde apunta realmente
            }
        }
        else
        {
            Vector3 lookDir = targetPos - turretPos;
            lookDir.y = 0.5f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion lookRot = Quaternion.LookRotation(lookDir);
                float finalY = lookRot.eulerAngles.y + yAxisCorrection;
                transform.rotation = Quaternion.Euler(0, finalY, 0);
            }
        }

        // Debug visual
        Debug.DrawLine(turretPos, targetPos, Color.red);

        // Disparo automático
        fireTimer -= Time.deltaTime;

        if (fireTimer <= 0f)
        {
            if (bulletPrefab == null) Debug.LogWarning("[TurretAimer] No hay bulletPrefab asignado.");
            if (target == null)
            {
                // Intentar buscar de nuevo si se perdió el target
                var monsterObj = GameObject.FindGameObjectWithTag(targetTag);
                if (monsterObj != null) target = monsterObj.transform;
            }

            if (bulletPrefab != null && target != null)
            {
                // Posición de disparo en el centro exacto de la torreta
                Vector3 spawnPos = rotatingPart != null ? rotatingPart.position : transform.position;

                // Forzamos Y a 0.1 para que las balas no aparezcan en negativo
                spawnPos.y = 0.1f;

                // Calculamos la dirección hacia el monstruo para las balas
                Vector3 shootDir = (target.position - spawnPos).normalized;
                shootDir.y = 0f; // Mantener horizontal

                // La bala debe apuntar hacia el monstruo, no usar la rotación de la torreta
                Quaternion bulletRot = Quaternion.LookRotation(shootDir);

                // Instanciamos la bala
                GameObject bullet = Instantiate(bulletPrefab, spawnPos, bulletRot, transform.parent);

                fireTimer = fireCooldown;
            }
        }
    }
}