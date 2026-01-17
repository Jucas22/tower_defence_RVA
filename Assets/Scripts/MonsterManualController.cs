using UnityEngine;
using UnityEngine.InputSystem; // si usas nuevo Input System

[RequireComponent(typeof(monster_controller))]
public class MonsterManualController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 1.5f;
    public float rotateSpeed = 6f;

    [Header("Joystick (opcional)")]
    public SimpleJoystick joystick;

    monster_controller autoController;

    void Awake()
    {
        autoController = GetComponent<monster_controller>();
    }

    void Update()
    {
        Vector2 inputDir = Vector2.zero;

        // 1) Joystick (móvil o PC) si está asignado
        if (joystick != null)
        {
            inputDir = joystick.Direction;
        }

        // 2) Teclado (solo en PC/editor) como apoyo
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Keyboard.current != null)
        {
            Vector2 keyboardInput = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) keyboardInput.y += 1;
            if (Keyboard.current.sKey.isPressed) keyboardInput.y -= 1;
            if (Keyboard.current.aKey.isPressed) keyboardInput.x -= 1;
            if (Keyboard.current.dKey.isPressed) keyboardInput.x += 1;

            if (keyboardInput != Vector2.zero)
                inputDir = keyboardInput.normalized;
        }
#endif

        // Si no hay input, no movemos
        if (inputDir == Vector2.zero)
        {
            // dejar que el monster_controller haga lo que quiera (stay/walk) o forzar idle
            return;
        }

        // Opcional: cuando controlas manualmente, desactiva la persecución auto
        if (autoController != null)
        {
            // Por simplicidad, puedes desactivar el script
            autoController.enabled = false;
        }

        // Convertir input (x,z) a movimiento en mundo desde la perspectiva local del monstruo
        Vector3 move = new Vector3(inputDir.x, 0f, inputDir.y); // x = izquierda/derecha, y = adelante/atrás

        // Rotar hacia la dirección de movimiento
        if (move.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        // Mover
        transform.position += move.normalized * moveSpeed * Time.deltaTime;
    }
}