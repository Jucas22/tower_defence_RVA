using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]

public class monster_controller : MonoBehaviour
{
    [Header("Búsqueda de torre")]
    [Tooltip("Tag que identifica a la torre (asegúrate de crear la tag 'Tower' y asignarla)")]
    public string towerTag = "Tower";

    [Header("Movimiento")]
    [Tooltip("Velocidad en metros/segundo")]
    public float speed =1.2f;
    [Tooltip("Distancia a la torre para considerarse llegado")]
    public float stoppingDistance =0.6f;

    [Header("Comportamiento")]
    [Tooltip("Cada cuánto (s) reintenta buscar la torre si no existe")]
    public float searchInterval =0.5f;

    [Header("Animador")]
    [Tooltip("Referencia al Animator del monstruo (arrastrar el componente Animator del prefab)")]
    public Animator animator;
    [Tooltip("Nombre del parámetro bool para 'stay' (idle)")]
    public string stayBoolName = "isStaying";
    [Tooltip("Nombre del parámetro bool para 'walk' (moverse)")]
    public string walkBoolName = "isWalking";

    Transform targetTower;
    Rigidbody rb;
    bool chasing = false;
    bool reached = false;
    Coroutine behaviorCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Usamos movimiento por transform; no queremos que la física empuje el objeto.
        rb.isKinematic = true;
        if (animator == null)
            animator = GetComponent<Animator>();

        // Al aparecer, estar en estado Staying
        SetAnimatorState(staying: true, walking: false);
    }

    void OnEnable()
    {
        // Suscribirse al evento para reaccionar inmediatamente cuando se instancie una torre
        image_target_spawner.TowerSpawned += OnTowerSpawned;

        // Iniciar la lógica principal
        behaviorCoroutine = StartCoroutine(BehaviorLoop());
    }

    void OnDisable()
    {
        // Desuscribirse y parar cualquier coroutine en curso
        image_target_spawner.TowerSpawned -= OnTowerSpawned;

        if (behaviorCoroutine != null)
            StopCoroutine(behaviorCoroutine);
        behaviorCoroutine = null;
    }

    // Evento llamado por el spawner cuando aparece una torre en escena
    void OnTowerSpawned(Transform towerTransform)
    {
        if (towerTransform == null) return;

        targetTower = towerTransform;
        chasing = true;
        reached = false;
        SetAnimatorState(staying: false, walking: true);

        // Detener el loop principal y empezar una persecución inmediata
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
            lookDir.y =0f;
            if (lookDir.sqrMagnitude >0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,6f * Time.deltaTime);
            }

            transform.position = Vector3.MoveTowards(transform.position, targetTower.position, speed * Time.deltaTime);

            yield return null;
        }

        // Si no se llegó, reiniciar el loop principal para continuar buscando
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
            // Si se llegó, dejamos el comportamiento detenido (igual que antes)
            behaviorCoroutine = null;
        }
    }

    void OnDestroy()
    {
        // Asegurar desuscripción
        image_target_spawner.TowerSpawned -= OnTowerSpawned;
    }

    void OnDisableOld()
    {
        // mantenemos por compatibilidad si antes se usaba este método
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
                        // Llegó a la torre
                        OnReachedTower();
                        break;
                    }

                    // Rotación suave hacia la torre (en el plano horizontal)
                    Vector3 lookDir = dir;
                    lookDir.y =0f;
                    if (lookDir.sqrMagnitude >0.0001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(lookDir);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,6f * Time.deltaTime);
                    }

                    // Movimiento hacia la torre
                    transform.position = Vector3.MoveTowards(transform.position, targetTower.position, speed * Time.deltaTime);

                    yield return null;
                }

                // Si la torre desapareció o fue destruida sin haberse llegado, volver a buscar
                if (!reached)
                {
                    chasing = false;
                    SetAnimatorState(staying: true, walking: false);
                    // pequeña espera antes de reintentar
                    yield return new WaitForSeconds(searchInterval);
                }
                else
                {
                    // Si se llegó, decidimos qué hacer: por defecto quedar idle
                    SetAnimatorState(staying: true, walking: false);
                    // Puedes destruir el monstruo o reproducir animación de ataque aquí
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
        // Aquí puedes notificar a la torre:
        if (targetTower != null)
            targetTower.SendMessage("OnMonsterArrived", this, SendMessageOptions.DontRequireReceiver);

        // Ejemplo simple: destruir el monstruo al llegar
        // Destroy(gameObject);

        // O reproducir una animación de ataque mediante Animator
        // animator.SetTrigger("attack");
    }
}