using System;
using UnityEngine;
using Vuforia;

// Spawner específico para la torreta AR usando Image Target
// Instancia el prefab de la torreta y notifica a listeners globales
public class img_target_turret : MonoBehaviour
{
    [Tooltip("Prefab que se instanciara cuando se detecte este ImageTarget")]
    public GameObject prefabToSpawn;

    [Tooltip("Si true, se instanciara solo una vez")]
    public bool spawnOnce = true;

    [Tooltip("Opcional: offset local aplicado al prefab instanciado")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;
    public Vector3 scale = Vector3.one;

    [Header("AR/Plano raíz para parenting dinámico")]
    [Tooltip("Padre raíz para la torreta (plano de test o GroundAnchorStage)")]
    public Transform rootParent;
    [Tooltip("Si true, busca el padre por nombre en la escena (útil para cambiar entre test y AR)")]
    public bool findParentByName = false;
    [Tooltip("Nombre del objeto raíz a buscar si findParentByName es true")]
    public string parentObjectName = "TestPlane";

    // Evento global para notificar cuando se instancia la torreta
    public static Action<Transform> TurretSpawned;

    ObserverBehaviour mObserver;
    GameObject spawnedInstance;
    bool hasSpawned = false;

    void Awake()
    {
        mObserver = GetComponent<ObserverBehaviour>();
        if (mObserver != null)
        {
            mObserver.OnTargetStatusChanged += OnObserverStatusChanged;
        }
    }

    void OnDestroy()
    {
        if (mObserver != null)
            mObserver.OnTargetStatusChanged -= OnObserverStatusChanged;
    }

    void OnObserverStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        var statusObj = targetStatus.Status;
        string statusName = statusObj.ToString().ToUpperInvariant();
        bool isFound = statusName == "DETECTED" || statusName == "TRACKED" || statusName == "EXTENDED_TRACKED";
        if (isFound)
            OnFound();
        Debug.Log($"[img_target_turret] Status type: {statusObj.GetType().FullName}, value: {statusObj} (isFound={isFound})");
    }

    void OnFound()
    {
        if (prefabToSpawn == null) return;
        if (spawnOnce && hasSpawned) return;
        Vector3 worldPos = transform.TransformPoint(positionOffset);
        Quaternion worldRot = transform.rotation * Quaternion.Euler(rotationOffset);
        // Determinar el padre adecuado para la torreta
        Transform parentToUse = rootParent;
        if (findParentByName && !string.IsNullOrEmpty(parentObjectName))
        {
            GameObject found = GameObject.Find(parentObjectName);
            if (found != null)
                parentToUse = found.transform;
            else
                Debug.LogWarning($"[img_target_turret] No se encontró el objeto raíz '{parentObjectName}' en la escena.");
        }

        spawnedInstance = Instantiate(prefabToSpawn, worldPos, worldRot, parentToUse);
        if (scale != Vector3.one)
            spawnedInstance.transform.localScale = scale;
        spawnedInstance.SetActive(true);
        try
        {
            spawnedInstance.tag = "Turret";
        }
        catch (UnityException ex)
        {
            Debug.LogWarning($"[img_target_turret] La tag 'Turret' no esta definida en el proyecto. " + ex.Message);
        }
        hasSpawned = true;
        Debug.Log($"[img_target_turret] Spawned turret: {spawnedInstance.name} pos={spawnedInstance.transform.position} scale={spawnedInstance.transform.localScale}");
        TurretSpawned?.Invoke(spawnedInstance.transform);
    }
}