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

    void Start()
    {
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
        Ray ray = Camera.main.ScreenPointToRay(touchPosition);

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
        TrackableHit hit;
        if (planeFinder.HitTest(ray, out hit))
        {
            // Instanciar avatar en la posición del hit de Vuforia
            spawnedAvatar = Instantiate(avatarPrefab, hit.Position, Quaternion.identity);
            spawnedAvatar.SetActive(true);
            
            avatarPlaced = true;
            targetPosition = hit.Position;

            // Obtener el Animator si existe
            avatarAnimator = spawnedAvatar.GetComponent<Animator>();
            if (avatarAnimator == null)
            {
                avatarAnimator = spawnedAvatar.GetComponentInChildren<Animator>();
            }

            // Establecer estado inicial (idle)
            SetAnimationState(idle: true, walking: false);

            // Crear plano de referencia usando la normal del hit
            groundPlane = new Plane(hit.Normal, hit.Position);
            planeDetected = true;

            Debug.Log($"[GroundPlacementController] Avatar colocado en {hit.Position}");
        }
        else
        {
            // Fallback: usar Physics.Raycast si el HitTest de Vuforia no funciona
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo))
            {
                // Instanciar avatar en la posición del hit
                spawnedAvatar = Instantiate(avatarPrefab, hitInfo.point, Quaternion.identity);
                spawnedAvatar.SetActive(true);
                
                avatarPlaced = true;
                targetPosition = hitInfo.point;

                // Obtener el Animator si existe
                avatarAnimator = spawnedAvatar.GetComponent<Animator>();
                if (avatarAnimator == null)
                {
                    avatarAnimator = spawnedAvatar.GetComponentInChildren<Animator>();
                }

                // Establecer estado inicial (idle)
                SetAnimationState(idle: true, walking: false);

                // Crear plano de referencia
                if (!planeDetected)
                {
                    groundPlane = new Plane(hitInfo.normal, hitInfo.point);
                    planeDetected = true;
                }

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

        float enter;
        if (groundPlane.Raycast(ray, out enter))
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
                10f * Time.deltaTime
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
