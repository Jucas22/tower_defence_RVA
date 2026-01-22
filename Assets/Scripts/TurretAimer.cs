using UnityEngine;
using System.Collections;

// Este script hace que la torreta apunte siempre al monster en la escena y active la animación al instanciarse
public class TurretAimer : MonoBehaviour
{
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
    public float yAxisCorrection = 0f;

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
        Debug.Log($"[TurretAimer] Lanzando trigger '{activateTrigger}'");
        animator.SetTrigger(activateTrigger);
        animationPlayed = true;
        // Ahora la animación notificará al script mediante Animation Event
    }

    // Este método debe ser llamado por un Animation Event al final de la animación de despliegue
    public void OnDeployAnimationFinished()
    {
        readyToAim = true;
        Debug.Log("[TurretAimer] Animation Event recibido: listo para rotar.");
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
                Debug.Log("[TurretAimer] No se encontró ningún Monster en la escena.");
                return;
            }
        }

        Vector3 turretPos = rotatingPart ? rotatingPart.position : transform.position;
        Vector3 targetPos = target.position;
        Vector3 lookDir = targetPos - turretPos;
        lookDir.y = 0; // Solo rotar en el eje Y (horizontal)

        // Debug visual y de consola
        Debug.DrawLine(turretPos, targetPos, Color.red); // Línea entre torreta y monster
        Debug.DrawRay(turretPos, (rotatingPart ? rotatingPart.forward : transform.forward) * 2f, Color.green); // Dirección actual de la torreta
        // Debug.Log($"[TurretAimer] TurretPos: {turretPos}, TargetPos: {targetPos}, lookDir: {lookDir}, sqrMagnitude: {lookDir.sqrMagnitude}, Forward: {(rotatingPart ? rotatingPart.forward : transform.forward)}");

        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(lookDir);
            if (rotatingPart)
            {
                Vector3 euler = rotatingPart.rotation.eulerAngles;
                float targetY = lookRot.eulerAngles.y + yAxisCorrection;
                // Aplica corrección en X para modelos tumbados y en Y para ajuste fino
                rotatingPart.rotation = Quaternion.Euler(xAxisCorrection, targetY, euler.z);
                Debug.Log($"[TurretAimer] Rotando rotatingPart '{rotatingPart.name}' a X={xAxisCorrection}, Y={targetY}");
            }
            else
            {
                Vector3 euler = transform.rotation.eulerAngles;
                float targetY = lookRot.eulerAngles.y + yAxisCorrection;
                transform.rotation = Quaternion.Euler(euler.x, targetY, euler.z);
                // Debug.Log($"[TurretAimer] Rotando objeto principal a Y={targetY}");
            }
        }
        else
        {
            Debug.Log("[TurretAimer] lookDir demasiado pequeño, no se rota.");
        }
    }
}