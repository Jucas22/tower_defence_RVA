using UnityEngine;
using System.Collections;

public class monster_controller : MonoBehaviour
{
    [Header("Búsqueda de torre")]
    [Tooltip("Tag que identifica a la torre (asegúrate de crear la tag 'Tower' y asignarla)")]
    public string towerTag = "Tower";

    [Header("Movimiento")]
    [Tooltip("Velocidad en metros/segundo")]
    public float speed = 1.2f;
    [Tooltip("Distancia a la torre para considerarse llegado")]
    public float stoppingDistance = 0.6f;

    [Header("Comportamiento")]
    [Tooltip("Cada cu�nto (s) reintenta buscar la torre si no existe")]
    public float searchInterval = 0.5f;

    [Header("Animador")]
    [Tooltip("Referencia al Animator del monstruo (arrastrar el componente Animator del prefab)")]
    public Animator animator;
    [Tooltip("Nombre del par�metro bool para 'stay' (idle)")]
    public string stayBoolName = "is_staying";
    [Tooltip("Nombre del par�metro bool para 'walk' (moverse)")]
    public string walkBoolName = "is_walking";

    Transform targetTower;
    Rigidbody rb;
    bool chasing = false;
    bool reached = false;
    Coroutine behaviorCoroutine;

    void Awake()
    {
        // Intentar obtener Rigidbody; si no existe, añadirlo para evitar MissingComponentException
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning($"{name}: Rigidbody faltante en prefab. Se añadirá uno en tiempo de ejecución. Recomendado añadirlo al prefab.");
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Usamos movimiento por transform; no queremos que la f�sica empuje el objeto.
        rb.isKinematic = true;

        if (animator == null)
            animator = GetComponent<Animator>();

        // Al aparecer, estar en estado Staying
        SetAnimatorState(staying: true, walking: false);
        Debug.Log($"{name} initialized and waiting for tower.");
    }

    void OnEnable()
    {
        // Suscribirse al evento para reaccionar inmediatamente cuando se instancie una torre
        img_target_tower.TowerSpawned += OnTowerSpawned;

        // Iniciar la l�gica principal
        behaviorCoroutine = StartCoroutine(BehaviorLoop());
    }

    void OnDisable()
    {
        // Desuscribirse y parar cualquier coroutine en curso
        img_target_tower.TowerSpawned -= OnTowerSpawned;

        if (behaviorCoroutine != null)
            StopCoroutine(behaviorCoroutine);
        behaviorCoroutine = null;
    }

    // Evento llamado por el spawner cuando aparece una torre en escena
    void OnTowerSpawned(Transform towerTransform)
    {
        if (towerTransform == null) return;

        targetTower = towerTransform;
        Debug.Log($"{name} detected tower spawn and is starting immediate chase.");
        chasing = true;
        reached = false;
        SetAnimatorState(staying: false, walking: true);

        // Detener el loop principal y empezar una persecuci�n inmediata
        if (behaviorCoroutine != null)
        {
            StopCoroutine(behaviorCoroutine);
        }
        behaviorCoroutine = StartCoroutine(ImmediateChase());
    }

    IEnumerator ImmediateChase()
    {
        while (chasing && targetTower != null && !reached)
        {
            Vector3 dir = targetTower.position - transform.position;
            float dist = dir.magnitude;

            if (dist <= stoppingDistance)
            {
                OnReachedTower();
                break;
            }

            Vector3 lookDir = dir;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 6f * Time.deltaTime);
            }

            transform.position = Vector3.MoveTowards(transform.position, targetTower.position, speed * Time.deltaTime);

            yield return null;
        }

        // Si no se lleg�, reiniciar el loop principal para continuar buscando
        if (!reached)
        {
            chasing = false;
            SetAnimatorState(staying: true, walking: false);
            // Esperar un poco antes de reiniciar el comportamiento principal
            yield return new WaitForSeconds(searchInterval);
            behaviorCoroutine = StartCoroutine(BehaviorLoop());
        }
        else
        {
            // Si se lleg�, dejamos el comportamiento detenido (igual que antes)
            behaviorCoroutine = null;
        }
    }

    void OnDestroy()
    {
        // Asegurar desuscripci�n
        img_target_tower.TowerSpawned -= OnTowerSpawned;
    }

    void OnDisableOld()
    {
        // mantenemos por compatibilidad si antes se usaba este m�todo
    }

    IEnumerator BehaviorLoop()
    {
        // Loop principal: si hay torre -> perseguir; si no -> stay y reintentar
        while (true)
        {
            // Buscar torre
            var towerGO = GameObject.FindWithTag(towerTag);
            if (towerGO == null)
            {
                // No hay torre: stay
                targetTower = null;
                chasing = false;
                reached = false;
                SetAnimatorState(staying: true, walking: false);
                // Esperar un poco antes de volver a buscar
                yield return new WaitForSeconds(searchInterval);
            }
            else
            {
                // Torre encontrada: empezar a perseguirla
                targetTower = towerGO.transform;
                chasing = true;
                SetAnimatorState(staying: false, walking: true);

                // Mientras exista la torre y no se haya llegado, moverse
                while (chasing && targetTower != null && !reached)
                {
                    Vector3 dir = targetTower.position - transform.position;
                    float dist = dir.magnitude;

                    if (dist <= stoppingDistance)
                    {
                        // Lleg� a la torre
                        OnReachedTower();
                        break;
                    }

                    // Rotaci�n suave hacia la torre (en el plano horizontal)
                    Vector3 lookDir = dir;
                    lookDir.y = 0f;
                    if (lookDir.sqrMagnitude > 0.0001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(lookDir);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 6f * Time.deltaTime);
                    }

                    // Movimiento hacia la torre
                    transform.position = Vector3.MoveTowards(transform.position, targetTower.position, speed * Time.deltaTime);

                    yield return null;
                }

                // Si la torre desapareci� o fue destruida sin haberse llegado, volver a buscar
                if (!reached)
                {
                    chasing = false;
                    SetAnimatorState(staying: true, walking: false);
                    // peque�a espera antes de reintentar
                    yield return new WaitForSeconds(searchInterval);
                }
                else
                {
                    // Si se lleg�, decidimos qu� hacer: por defecto quedar idle
                    SetAnimatorState(staying: true, walking: false);
                    // Puedes destruir el monstruo o reproducir animaci�n de ataque aqu�
                    yield break; // detener el loop si quieres que deje de comportarse
                }
            }
        }
    }

    void SetAnimatorState(bool staying, bool walking)
    {
        if (animator == null) return;
        if (!string.IsNullOrEmpty(stayBoolName))
            animator.SetBool(stayBoolName, staying);
        if (!string.IsNullOrEmpty(walkBoolName))
            animator.SetBool(walkBoolName, walking);
    }

    void OnReachedTower()
    {
        reached = true;
        chasing = false;
        Debug.Log($"{name} reached the tower.");
        // Aqu� puedes notificar a la torre:
        if (targetTower != null)
            targetTower.SendMessage("OnMonsterArrived", this, SendMessageOptions.DontRequireReceiver);

        // Ejemplo simple: destruir el monstruo al llegar
        // Destroy(gameObject);

        // O reproducir una animaci�n de ataque mediante Animator
        // animator.SetTrigger("attack");
    }
}