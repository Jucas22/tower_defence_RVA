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
    public float fireCooldown = 2.0f; // Aumentado para reducir la cadencia
    private float fireTimer = 0f;

    [Tooltip("Tag del objetivo a seguir (Monster)")]
    public string targetTag = "Monster";

    [Tooltip("Transform de la parte rotatoria de la torreta (Bone_Padre/Bone_Rotacion_Base)")]
    public Transform rotatingPart;

    [Tooltip("Animator de la torreta")]
    public Animator animator;

    [Tooltip("Nombre del trigger para activar la animación de despliegue")]
    public string activateTrigger = "ActiveTurret";

    [Tooltip("Corrección de ángulo en X para modelos tumbados (grados)")]
    public float xAxisCorrection = 90f;

    [Tooltip("Corrección fina en Y para ajustar la puntería (grados)")]
    public float yAxisCorrection = -75f;

    private Transform target;
    private bool animationPlayed = false;
    private bool readyToAim = false;

    void Start()
    {
        Debug.Log($"[TurretAimer] Start. Animator: {(animator != null ? animator.name : "null")}, RotatingPart: {(rotatingPart != null ? rotatingPart.name : "null")}");
        if (animator != null && !animationPlayed)
            StartCoroutine(PlayActivationAnim());
    }

    IEnumerator PlayActivationAnim()
    {
        Debug.Log("[TurretAimer] Esperando 1s antes de activar animación...");
        yield return new WaitForSeconds(1f);
        if (animator != null)
        {
            Debug.Log($"[TurretAimer] Lanzando trigger '{activateTrigger}'");
            animator.SetTrigger(activateTrigger);
            // Si después de 2 segundos no se ha llamado al evento de animación, forzamos el inicio
            Invoke(nameof(OnDeployAnimationFinished), 2.0f);
        }
        else
        {
            readyToAim = true;
        }
        animationPlayed = true;
    }

    // Este método debe ser llamado por un Animation Event al final de la animación de despliegue
    public void OnDeployAnimationFinished()
    {
        if (readyToAim) return; // Evita ejecutarlo dos veces si el invoke y el evento coinciden
        readyToAim = true;
        Debug.Log("[TurretAimer] Turret lista para rotar.");
    }

    void Update()
    {
        if (!readyToAim)
        {
            Debug.Log("[TurretAimer] Esperando a que termine la animación de despliegue...");
            return;
        }
        if (target == null)
        {
            var monsterObj = GameObject.FindGameObjectWithTag(targetTag);
            if (monsterObj != null)
            {
                target = monsterObj.transform;
                Debug.Log($"[TurretAimer] Monster encontrado: {target.name}");
            }
            else
            {
                // Debug.Log("[TurretAimer] No se encontró ningún Monster en la escena.");
                return;
            }
        }

        Vector3 turretPos = rotatingPart ? rotatingPart.position : transform.position;
        Vector3 targetPos = target.position;
        Vector3 lookDir = targetPos - turretPos;
        lookDir.y = 0; // Forzamos que la torre solo rote horizontalmente

        // Debug visual y de consola
        Debug.DrawLine(turretPos, targetPos, Color.red);
        Debug.DrawRay(turretPos, (rotatingPart ? rotatingPart.forward : transform.forward) * 2f, Color.green);

        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(lookDir);
            if (rotatingPart)
            {
                // Aplicamos la rotación Y del cálculo y mantenemos X y Z controlados
                float targetY = lookRot.eulerAngles.y + yAxisCorrection;
                rotatingPart.rotation = Quaternion.Euler(xAxisCorrection, targetY, 0);
            }
            else
            {
                float targetY = lookRot.eulerAngles.y + yAxisCorrection;
                transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, targetY, 0);
            }
        }
        else
        {
            // Debug.Log("[TurretAimer] lookDir demasiado pequeño, no se rota.");
        }

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
                Vector3 spawnPos = rotatingPart ? rotatingPart.position : transform.position;
                spawnPos.y = 0.01f;

                Vector3 shootDir = (target.position - spawnPos);
                shootDir.y = 0;
                Vector3 dirNormalized = shootDir.normalized;

                Quaternion bulletRot = Quaternion.LookRotation(dirNormalized);

                Debug.Log($"[TurretAimer] INSTANCIANDO BALA en {spawnPos} hacia {target.position}");
                GameObject bullet = Instantiate(bulletPrefab, spawnPos, bulletRot);
                fireTimer = fireCooldown;
            }
        }
    }
}