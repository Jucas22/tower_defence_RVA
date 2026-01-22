using System;
using UnityEngine;
using Vuforia;

// Specific spawner for the Tower image target.
// Instantiates the configured tower prefab in world space when the ImageTarget is detected,
// forces its tag to "Tower", removes Vuforia trackable handlers so it stays visible,
// and notifies listeners via the static TowerSpawned event.
public class img_target_tower : MonoBehaviour
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
    [Tooltip("Padre raíz para la torre (plano de test o GroundAnchorStage)")]
    public Transform rootParent;
    [Tooltip("Si true, busca el padre por nombre en la escena (útil para cambiar entre test y AR)")]
    public bool findParentByName = false;
    [Tooltip("Nombre del objeto raíz a buscar si findParentByName es true")]
    public string parentObjectName = "TestPlane";

    // Evento global que notifica a otros scripts cuando se ha instanciado una torre
    public static Action<Transform> TowerSpawned;

    ObserverBehaviour mObserver;
    GameObject spawnedInstance;
    bool hasSpawned = false;

    void Awake()
    {
        mObserver = GetComponent<ObserverBehaviour>();
        if (mObserver != null)
        {
            // Newer Vuforia exposes an event for status changes
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

        bool isFound = statusName == "DETECTED" ||
                       statusName == "TRACKED" ||
                       statusName == "EXTENDED_TRACKED";

        if (isFound)
            OnFound();

        Debug.Log($"[img_target_tower] Status type: {statusObj.GetType().FullName}, value: {statusObj} (isFound={isFound})");
    }

    void OnFound()
    {
        if (prefabToSpawn == null) return;
        if (spawnOnce && hasSpawned) return;

        // Calculate world pose using the ImageTarget transform and the provided local offsets
        Vector3 worldPos = transform.TransformPoint(positionOffset);
        Quaternion worldRot = transform.rotation * Quaternion.Euler(rotationOffset);

        // Determinar el padre adecuado para la torre
        Transform parentToUse = rootParent;
        if (findParentByName && !string.IsNullOrEmpty(parentObjectName))
        {
            GameObject found = GameObject.Find(parentObjectName);
            if (found != null)
                parentToUse = found.transform;
            else
                Debug.LogWarning($"[img_target_tower] No se encontró el objeto raíz '{parentObjectName}' en la escena.");
        }

        spawnedInstance = Instantiate(prefabToSpawn, worldPos, worldRot, parentToUse);
        if (scale != Vector3.one)
            spawnedInstance.transform.localScale = scale;
        spawnedInstance.SetActive(true);

        // Force tag "Tower"
        try
        {
            spawnedInstance.tag = "Tower";
        }
        catch (UnityException ex)
        {
            Debug.LogWarning($"[img_target_tower] La tag 'Tower' no esta definida en el proyecto. " + ex.Message);
        }

        hasSpawned = true;

        Debug.Log($"[img_target_tower] Spawned tower: {spawnedInstance.name} pos={spawnedInstance.transform.position} scale={spawnedInstance.transform.localScale}");

        // Notify listeners
        TowerSpawned?.Invoke(spawnedInstance.transform);
    }

    // Remove common Vuforia components from the spawned hierarchy that might auto-enable/disable renderers
    void RemoveVuforiaTrackableHandlers(GameObject root)
    {
        if (root == null) return;

        var monoBehaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mb in monoBehaviours)
        {
            if (mb == null) continue;
            var typeName = mb.GetType().Name;
            if (typeName.Contains("Trackable") || typeName.Contains("Observer") || typeName.Contains("DefaultObserverEventHandler") || typeName.Contains("DefaultTrackableEventHandler"))
            {
                try
                {
                    Destroy(mb);
                }
                catch { }
            }
        }

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
            r.enabled = true;

        var canvases = root.GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases)
            c.enabled = true;
    }
}
