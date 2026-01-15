using System;
using UnityEngine;
using Vuforia;

// This script spawns a monster prefab when its ImageTarget is detected.
// The spawned monster will be placed in world space, assigned the tag configured
// (default "Monster") and, if it has an Animator, will be set to the "staying" state.
public class img_target_monster : MonoBehaviour
{
    [Tooltip("Prefab que se instanciará cuando se detecte este ImageTarget")]
    public GameObject prefabToSpawn;

    [Tooltip("Si true, se instanciará solo una vez")]
    public bool spawnOnce = false;

    [Tooltip("Opcional: offset local aplicado al prefab instanciado")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;
    public Vector3 scale = Vector3.one;

    [Header("Configuración de Spawn")]
    [Tooltip("Tag que se asignará al objeto instanciado (por defecto 'Monster')")]
    public string spawnTag = "Monster";

    // Observador Vuforia
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

        bool isFound = statusName == "DETECTED" ||
                       statusName == "TRACKED" ||
                       statusName == "EXTENDED_TRACKED";

        if (isFound)
            OnFound();

        Debug.Log($"[img_target_monster] Status type: {statusObj.GetType().FullName}, value: {statusObj} (isFound={isFound})");
    }

    void OnFound()
    {
        if (prefabToSpawn == null) return;
        if (spawnOnce && hasSpawned) return;

        // Calculate world pose using the ImageTarget transform and the provided local offsets
        Vector3 worldPos = transform.TransformPoint(positionOffset);
        Quaternion worldRot = transform.rotation * Quaternion.Euler(rotationOffset);

        // Instantiate at computed world pose
        spawnedInstance = Instantiate(prefabToSpawn, worldPos, worldRot);

        // If user provided an explicit scale, apply it directly instead of multiplying
        if (scale != Vector3.one)
            spawnedInstance.transform.localScale = scale;

        // Ensure it's active and not parented to target so Vuforia won't toggle it
        spawnedInstance.SetActive(true);
        spawnedInstance.transform.SetParent(null, true);

        // Remove handlers of Vuforia that could hide it when other targets are tracked
        RemoveVuforiaTrackableHandlers(spawnedInstance);

        // Assign tag if configured
        if (!string.IsNullOrEmpty(spawnTag))
        {
            try
            {
                spawnedInstance.tag = spawnTag;
            }
            catch (UnityException ex)
            {
                Debug.LogWarning($"[img_target_monster] La tag '{spawnTag}' no está definida en el proyecto. " + ex.Message);
            }
        }

        // If the prefab has an Animator, try to force staying state
        var anim = spawnedInstance.GetComponentInChildren<Animator>(true);
        if (anim != null)
        {
            if (HasAnimatorBool(anim, "isStaying"))
                anim.SetBool("isStaying", true);
            if (HasAnimatorBool(anim, "isWalking"))
                anim.SetBool("isWalking", false);

            // Fallback: try to play state named "Staying"
            try
            {
                anim.Play("Staying");
            }
            catch { }
        }

        hasSpawned = true;

        Debug.Log($"[img_target_monster] Spawned monster instance: {spawnedInstance.name} pos={spawnedInstance.transform.position} scale={spawnedInstance.transform.localScale}");
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

    bool HasAnimatorBool(Animator animator, string paramName)
    {
        foreach (var p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Bool && p.name == paramName)
                return true;
        }
        return false;
    }
}
