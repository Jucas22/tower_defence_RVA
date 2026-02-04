using UnityEngine;
using UnityEngine.InputSystem; // Nuevo Input System

public class MonsterManualController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 0.01f;

    [Header("Joystick (opcional)")]
    public SimpleJoystick joystick;

    [Header("Animación")]
    [SerializeField] Animator animator;
    [SerializeField] string walkBoolName = "is_walking"; // cámbialo si tu parámetro se llama distinto


    Rigidbody rb;
    Vector2 _inputDir;
    bool _warnedNoJoystick;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"{name}: No tiene Rigidbody. Añadiendo uno automáticamente.");
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        if (joystick == null)
            joystick = FindFirstObjectByType<SimpleJoystick>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        _inputDir = Vector2.zero;

        // 1) Joystick
        if (joystick != null)
        {
            _inputDir = joystick.Direction;
        }
        else if (!_warnedNoJoystick)
        {
            Debug.LogWarning("[MonsterManualController] Joystick es NULL!");
            _warnedNoJoystick = true;
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        // 2) Teclado (solo editor/standalone)
        if (Keyboard.current != null)
        {
            Vector2 keyboardInput = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) keyboardInput.y += 1;
            if (Keyboard.current.sKey.isPressed) keyboardInput.y -= 1;
            if (Keyboard.current.aKey.isPressed) keyboardInput.x -= 1;
            if (Keyboard.current.dKey.isPressed) keyboardInput.x += 1;

            if (keyboardInput != Vector2.zero)
                _inputDir = keyboardInput.normalized;
        }
#endif
    }

    void FixedUpdate()
    {
        if (_inputDir == Vector2.zero || rb == null)
        {
            SetWalking(false);
            return;
        }

        // Invertimos los controles: negamos ambos ejes
        Vector3 move = new Vector3(-_inputDir.y, 0f, _inputDir.x);

        // Rotación hacia la dirección de movimiento
        if (move.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(move);
        }

        // Movimiento: forzamos posición en Transform y también por Rigidbody
        Vector3 before = transform.position;
        Vector3 newPos = before + move.normalized * moveSpeed * Time.fixedDeltaTime * 0.1f;

        // Si hay colisiones, usa MovePosition; si algún padre lo resetea, el Transform se fuerza igualmente
        rb.MovePosition(newPos);
        transform.position = newPos; // fuerza la posición aunque otro script/parent la resetease

        Vector3 delta = transform.position - before;
        // Debug.Log($"[MonsterManualController] Parent={transform.parent?.name ?? "<null>"} | Pos delta: {delta} | from {before} to {transform.position}");

        SetWalking(true);
    }

    void SetWalking(bool walking)
    {
        if (animator != null && !string.IsNullOrEmpty(walkBoolName))
            animator.SetBool(walkBoolName, walking);
    }

    void OnTriggerEnter(Collider collision)
    {
        // Verifica si el objeto que tocó es una torre
        if (collision.CompareTag("Tower"))
        {
            Debug.Log($"[monster_controller] ¡El monstruo ha alcanzado la torre!");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerVictory();
            }

            Destroy(collision.gameObject); // destruye la torre
        }
    }
}