using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Vuforia;

/// <summary>
/// Gestiona el modo "colocar monstruo con tap" usando el plano detectado por Vuforia.
/// </summary>
public class ARMonsterPlacer : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Prefab del monstruo que quieres instanciar")]
    public GameObject monsterPrefab;

    [Tooltip("ARCamera de Vuforia")]
    public Camera arCamera;

    [Tooltip("Plane Finder de Vuforia (para asegurarnos de que hay plano detectado)")]
    public PlaneFinderBehaviour planeFinder;

    [Header("Opciones")]
    [Tooltip("Si true, solo se puede tener un monstruo a la vez")]
    public bool singleMonster = true;

    // Estado interno
    bool placingMode = false;          // si estamos en modo "colocar monstruo"
    GameObject currentMonster;         // referencia al monstruo ya instanciado

    void Awake()
    {
        if (arCamera == null)
        {
            // Intentar encontrar la c�mara por tag si no se asign�
            arCamera = Camera.main;
        }
    }

    /// <summary>
    /// Activa o desactiva el modo de colocar monstruo (llamado desde la UI).
    /// Si se desactiva, destruye el monstruo actual.
    /// </summary>
    public void SetPlacingMode()
    {
        if (placingMode)
            placingMode = false;
        else
            placingMode = true;

        Debug.Log($"[ARMonsterPlacer] Modo de colocación {(placingMode ? "activado" : "desactivado")}.");
        if (!placingMode && currentMonster != null)
        {
            Destroy(currentMonster);
            currentMonster = null;
        }
    }
    public void SetPlacingModeToggle(bool activado)
    {
        placingMode = activado;

        Debug.Log($"[ARMonsterPlacer] Modo de colocación {(placingMode ? "activado" : "desactivado")}.");
        if (!placingMode && currentMonster != null)
        {
            Destroy(currentMonster);
            currentMonster = null;
        }
    }

    void Update()
    {
        // Evitar tap si el dedo est� sobre un elemento UI
        //if (EventSystem.current != null &&
        //    EventSystem.current.IsPointerOverGameObject(0))
        //    return;


        //// Solo reaccionamos al primer toque
        //if (Input.touchCount == 0) return;

        //Touch touch = Input.GetTouch(0);
        //Debug.Log("Entrando en el update del plano");
        //Debug.Log(touch);
        //if (touch.phase != TouchPhase.Began) return;
        //TryPlaceMonsterAtTouch(touch.position);

        if (!placingMode) return;

        // Posici�n y �click� seg�n dispositivo
        bool clicked = false;
        Vector2 screenPos = Vector2.zero;

        // 1) Rat�n
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            clicked = true;
            screenPos = Mouse.current.position.ReadValue();

            // Evitar UI (versi�n nuevo Input System)
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
                return;
        }
        // 2) Pantalla t�ctil
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            clicked = true;
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();

            // Si tienes EventSystem configurado con UI, en m�vil normalmente tambi�n pasa por aqu�
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(0))
                return;
        }

        if (!clicked) return;

        TryPlaceMonsterAtTouch(screenPos);

        //if (Input.GetMouseButtonDown(0))
        //{
        //    // Evitar UI
        //    if (EventSystem.current != null &&
        //        EventSystem.current.IsPointerOverGameObject())
        //        return;

        //    TryPlaceMonsterAtTouch(Input.mousePosition);
        //}

    }

    void TryPlaceMonsterAtTouch(Vector2 screenPosition)
    {
        if (arCamera == null)
        {
            Debug.LogWarning("[ARMonsterPlacer] ARCamera no asignada.");
            return;
        }

        if (monsterPrefab == null)
        {
            Debug.LogWarning("[ARMonsterPlacer] monsterPrefab no asignado.");
            return;
        }

        // Lanzar un rayo desde la c�mara a trav�s del punto de pantalla
        Ray ray = arCamera.ScreenPointToRay(screenPosition);

        // Usaremos un simple Raycast de f�sica. Aseg�rate de que el plano/anchor tenga collider,
        // o que coloques un 'planeCollider' invisble donde quieras que sea "suelo".
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 50f))
        {
            Vector3 spawnPos = hitInfo.point;
            Quaternion spawnRot = Quaternion.identity;

            // Opcional: orientar el monstruo mirando hacia la c�mara pero sin inclinarse
            Vector3 lookDir = arCamera.transform.position - spawnPos;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                spawnRot = Quaternion.LookRotation(-lookDir); // que mire en sentido opuesto a la c�mara
            }

            if (singleMonster && currentMonster != null)
            {
                Destroy(currentMonster);
            }

            currentMonster = Instantiate(monsterPrefab, spawnPos, spawnRot);
            currentMonster.transform.SetParent(null, true); // salir de cualquier anchor/trackable
            currentMonster.SetActive(true);

            Debug.Log($"[ARMonsterPlacer] Monstruo colocado en {spawnPos}");

            // Al colocar el monstruo, desactivar el modo de colocación para que no se pueda mover
            placingMode = false;
            Debug.Log($"[ARMonsterPlacer] Modo de colocación desactivado automáticamente tras instanciar monstruo.");
        }
        else
        {
            Debug.Log("[ARMonsterPlacer] Raycast no ha tocado ning�n collider.");
        }
    }
}