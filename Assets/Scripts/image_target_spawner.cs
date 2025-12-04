using System;
using UnityEngine;
using Vuforia;

public class image_target_spawner : MonoBehaviour
{
    [Tooltip("Prefab que se instanciará cuando se detecte este ImageTarget")]
    public GameObject prefabToSpawn;

    [Tooltip("Si true, se instanciará solo una vez")]
    public bool spawnOnce = false;

    [Tooltip("Opcional: offset local aplicado al prefab instanciado")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;
    public Vector3 scale = Vector3.one;

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
        // Use the runtime string name of the status so the code compiles regardless of exact enum type/name.
        var statusObj = targetStatus.Status;
        string statusName = statusObj.ToString().ToUpperInvariant();

        bool isFound = statusName == "DETECTED" ||
                       statusName == "TRACKED" ||
                       statusName == "EXTENDED_TRACKED";

        if (isFound)
            OnFound();

        // Helpful debug: prints the actual enum CLR type and value so you can confirm what your Vuforia exposes.
        Debug.Log($"[ImageTargetSpawner] Status type: {statusObj.GetType().FullName}, value: {statusObj} (isFound={isFound})");
    }

    void OnFound()
    {
        if (prefabToSpawn == null) return;
        if (spawnOnce && hasSpawned) return;

        // Instanciar en world space en la pose del ImageTarget
        spawnedInstance = Instantiate(prefabToSpawn, transform.position, transform.rotation);
        spawnedInstance.transform.position += spawnedInstance.transform.TransformDirection(positionOffset);
        spawnedInstance.transform.rotation = spawnedInstance.transform.rotation * Quaternion.Euler(rotationOffset);
        spawnedInstance.transform.localScale = Vector3.Scale(spawnedInstance.transform.localScale, scale);

        // Asegurar que NO sea child del ImageTarget para que Vuforia no lo habilite/deshabilite automáticamente
        spawnedInstance.transform.SetParent(null, true);

        // Eliminar handlers de Vuforia que puedan desactivar renderers al perder tracking
        RemoveVuforiaTrackableHandlers(spawnedInstance);

        // Intentar asegurar la tag "Tower" para que otros scripts la detecten mediante FindWithTag
        try
        {
            spawnedInstance.tag = "Tower";
        }
        catch (UnityException ex)
        {
            Debug.LogWarning("[ImageTargetSpawner] La tag 'Tower' no está definida en el proyecto. Por favor añádela en Unity Tags. " + ex.Message);
        }

        hasSpawned = true;

        Debug.Log($"[ImageTargetSpawner] Spawned tower instance: {spawnedInstance.name}");

        // Notificar mediante evento estático a interesados (p. ej. monsters) para que reaccionen inmediatamente
        TowerSpawned?.Invoke(spawnedInstance.transform);

        // No parentear al ImageTarget -> se quedará en world space aunque se pierda tracking
    }

    // Remove common Vuforia components from the spawned hierarchy that might auto-enable/disable renderers
    void RemoveVuforiaTrackableHandlers(GameObject root)
    {
        if (root == null) return;

        // Remove components whose type name indicates they are Vuforia handlers
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

        // Also ensure renderers are enabled so object remains visible
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
            r.enabled = true;

        var canvases = root.GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases)
            c.enabled = true;
    }
}
