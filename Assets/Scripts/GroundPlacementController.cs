using UnityEngine;
using Vuforia;

/// <summary>
/// Controlador para detectar un plano en el suelo usando Vuforia PlaneFinderBehaviour,
/// colocar un avatar mediante toque en pantalla (HitTest) y moverlo tocando otros puntos del suelo.
/// Maneja animaciones básicas (caminar/idle) si el avatar tiene Animator.
/// </summary>
public class GroundPlacementController : MonoBehaviour
{
    [Header("Configuración de Avatar")]
    [Tooltip("Prefab del avatar que se instanciará al tocar el suelo")]
    public GameObject avatarPrefab;

    [Header("Configuración de PlaneFinderBehaviour")]
    [Tooltip("Referencia al PlaneFinderBehaviour (si no se asigna, se buscará automáticamente en la escena)")]
    public PlaneFinderBehaviour planeFinder;

    [Header("Configuración de Movimiento")]
    [Tooltip("Velocidad de movimiento del avatar (metros/segundo)")]
    public float moveSpeed = 2.0f;
    [Tooltip("Velocidad de rotación del avatar al girar hacia un destino")]
    public float rotationSpeed = 10.0f;
    [Tooltip("Distancia mínima al destino para considerarse llegado")]
    public float stoppingDistance = 0.1f;

    [Header("Configuración de Animaciones")]
    [Tooltip("Nombre del parámetro bool del Animator para idle/staying")]
    public string idleBoolName = "isIdle";
    [Tooltip("Nombre del parámetro bool del Animator para caminar")]
    public string walkBoolName = "isWalking";

    // Estado interno
    private GameObject spawnedAvatar;
    private Animator avatarAnimator;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool avatarPlaced = false;
    private Plane groundPlane;
    private bool planeDetected = false;
    private Camera arCamera;

    void Start()
    {
        // Obtener referencia a la cámara principal
        arCamera = Camera.main;
        if (arCamera == null)
        {
            Debug.LogError("[GroundPlacementController] No se encontró cámara principal. Asegúrate de tener una cámara con tag 'MainCamera'.");
        }

        // Buscar PlaneFinderBehaviour si no está asignado
        if (planeFinder == null)
        {
            planeFinder = FindObjectOfType<PlaneFinderBehaviour>();
            if (planeFinder == null)
            {
                Debug.LogWarning("[GroundPlacementController] No se encontró PlaneFinderBehaviour en la escena. " +
                    "Asegúrate de tener un Plane Finder configurado en Vuforia.");
            }
            else
            {
                Debug.Log("[GroundPlacementController] PlaneFinderBehaviour encontrado automáticamente.");
            }
        }

        // Suscribirse a eventos del PlaneFinderBehaviour
        if (planeFinder != null)
        {
            var contentPositioningBehaviour = planeFinder.GetComponent<ContentPositioningBehaviour>();
            if (contentPositioningBehaviour != null)
            {
                contentPositioningBehaviour.OnContentPlaced.AddListener(OnContentPlaced);
            }
        }
    }

    void OnDestroy()
    {
        // Desuscribirse de eventos
        if (planeFinder != null)
        {
            var contentPositioningBehaviour = planeFinder.GetComponent<ContentPositioningBehaviour>();
            if (contentPositioningBehaviour != null)
            {
                contentPositioningBehaviour.OnContentPlaced.RemoveListener(OnContentPlaced);
            }
        }
    }

    void Update()
    {
        // Detectar toque en pantalla
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            HandleTouch(Input.GetTouch(0).position);
        }
        // Soporte para mouse en editor
        else if (Input.GetMouseButtonDown(0))
        {
            HandleTouch(Input.mousePosition);
        }

        // Mover avatar hacia el destino si está en movimiento
        if (isMoving && spawnedAvatar != null)
        {
            MoveAvatarToTarget();
        }
    }

    /// <summary>
    /// Evento llamado cuando se detecta un plano con Vuforia
    /// </summary>
    void OnContentPlaced(ContentPositioningBehaviour behaviour)
    {
        planeDetected = true;
        
        // Crear un plano matemático para futuras intersecciones con raycast
        // Usamos la posición y normal del objeto contenido (Anchor)
        if (behaviour.AnchorStage != null)
        {
            Vector3 planePosition = behaviour.AnchorStage.position;
            Vector3 planeNormal = behaviour.AnchorStage.up;
            groundPlane = new Plane(planeNormal, planePosition);
            
            Debug.Log($"[GroundPlacementController] Plano detectado en posición {planePosition}");
        }
    }

    /// <summary>
    /// Maneja el toque en pantalla para colocar o mover el avatar
    /// </summary>
    void HandleTouch(Vector2 touchPosition)
    {
        if (arCamera == null)
            return;

        Ray ray = arCamera.ScreenPointToRay(touchPosition);

        if (!avatarPlaced)
        {
            // Primer toque: colocar avatar usando HitTest de Vuforia
            PlaceAvatarWithHitTest(ray);
        }
        else
        {
            // Avatar ya colocado: mover usando raycast sobre el plano matemático
            MoveAvatarWithRaycast(ray);
        }
    }

    /// <summary>
    /// Coloca el avatar en el punto detectado mediante HitTest de Vuforia
    /// </summary>
    void PlaceAvatarWithHitTest(Ray ray)
    {
        if (avatarPrefab == null)
        {
            Debug.LogWarning("[GroundPlacementController] No hay Avatar Prefab asignado.");
            return;
        }

        if (planeFinder == null)
        {
            Debug.LogWarning("[GroundPlacementController] No hay PlaneFinderBehaviour disponible.");
            return;
        }

        // Intentar usar el sistema de hit test de Vuforia
        if (planeFinder.HitTest(ray, out var hit))
        {
            // Instanciar y configurar avatar usando HitTest de Vuforia
            InstantiateAndSetupAvatar(hit.Position, hit.Normal);
            Debug.Log($"[GroundPlacementController] Avatar colocado en {hit.Position}");
        }
        else
        {
            // Fallback: usar Physics.Raycast si el HitTest de Vuforia no funciona
            if (Physics.Raycast(ray, out var hitInfo))
            {
                // Instanciar y configurar avatar usando Physics
                InstantiateAndSetupAvatar(hitInfo.point, hitInfo.normal);
                Debug.Log($"[GroundPlacementController] Avatar colocado en {hitInfo.point} (fallback Physics)");
            }
        }
    }

    /// <summary>
    /// Mueve el avatar a una nueva posición usando raycast sobre el plano matemático
    /// </summary>
    void MoveAvatarWithRaycast(Ray ray)
    {
        if (spawnedAvatar == null || !planeDetected)
            return;

        if (groundPlane.Raycast(ray, out var enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            
            // Establecer nuevo destino
            targetPosition = hitPoint;
            isMoving = true;

            // Activar animación de caminar
            SetAnimationState(idle: false, walking: true);

            Debug.Log($"[GroundPlacementController] Moviendo avatar hacia {hitPoint}");
        }
    }

    /// <summary>
    /// Mueve el avatar hacia el punto objetivo
    /// </summary>
    void MoveAvatarToTarget()
    {
        Vector3 currentPos = spawnedAvatar.transform.position;
        Vector3 direction = targetPosition - currentPos;
        
        // Mantener la altura Y del avatar constante
        direction.y = 0;
        float distance = direction.magnitude;

        if (distance <= stoppingDistance)
        {
            // Llegó al destino
            isMoving = false;
            SetAnimationState(idle: true, walking: false);
            Debug.Log("[GroundPlacementController] Avatar llegó al destino");
            return;
        }

        // Rotar hacia el destino
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            spawnedAvatar.transform.rotation = Quaternion.Slerp(
                spawnedAvatar.transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // Mover hacia el destino
        Vector3 newPosition = Vector3.MoveTowards(
            currentPos,
            targetPosition,
            moveSpeed * Time.deltaTime
        );
        
        // Mantener la misma altura Y
        newPosition.y = currentPos.y;
        spawnedAvatar.transform.position = newPosition;
    }

    /// <summary>
    /// Establece el estado de animación del avatar
    /// </summary>
    void SetAnimationState(bool idle, bool walking)
    {
        if (avatarAnimator == null)
            return;

        // Intentar establecer parámetros bool si existen
        if (HasAnimatorBool(avatarAnimator, idleBoolName))
            avatarAnimator.SetBool(idleBoolName, idle);

        if (HasAnimatorBool(avatarAnimator, walkBoolName))
            avatarAnimator.SetBool(walkBoolName, walking);
    }

    /// <summary>
    /// Instancia y configura el avatar en la posición especificada
    /// </summary>
    private void InstantiateAndSetupAvatar(Vector3 position, Vector3 normal)
    {
        // Instanciar avatar
        spawnedAvatar = Instantiate(avatarPrefab, position, Quaternion.identity);
        spawnedAvatar.SetActive(true);
        
        avatarPlaced = true;
        targetPosition = position;

        // Obtener el Animator si existe
        GetAvatarAnimator(spawnedAvatar);

        // Establecer estado inicial (idle)
        SetAnimationState(idle: true, walking: false);

        // Crear plano de referencia usando la normal
        if (!planeDetected)
        {
            groundPlane = new Plane(normal, position);
            planeDetected = true;
        }
    }

    /// <summary>
    /// Obtiene el Animator del avatar instanciado
    /// </summary>
    private void GetAvatarAnimator(GameObject avatar)
    {
        avatarAnimator = avatar.GetComponent<Animator>();
        if (avatarAnimator == null)
        {
            avatarAnimator = avatar.GetComponentInChildren<Animator>();
        }
    }

    /// <summary>
    /// Verifica si el Animator tiene un parámetro bool específico
    /// </summary>
    bool HasAnimatorBool(Animator animator, string paramName)
    {
        if (animator == null || string.IsNullOrEmpty(paramName))
            return false;

        foreach (var param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Bool && param.name == paramName)
                return true;
        }
        return false;
    }
}
