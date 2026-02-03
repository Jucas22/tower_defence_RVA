using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f; // Aumentado significativamente para ser visible
    public int damage = 10;
    public float lifeTime = 3f;

    void Start()
    {
        Debug.Log("[Bullet] ¡He nacido! Inicializando...");
        // Forzamos que el Collider sea Trigger para que no choquen físicamente entre ellas
        Collider col = GetComponentInChildren<Collider>();
        if (col != null) col.isTrigger = true;

        // Buscamos el Rigidbody en este objeto o en sus hijos por si se quedó en el modelo
        Rigidbody rb = GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Movimiento simple directo al forward
        transform.position += transform.forward * speed * Time.deltaTime;

        // Debug para ver si la bala realmente tiene velocidad y se mueve
        if (Time.frameCount % 100 == 0)
        {
            Debug.Log($"[Bullet] Pos: {transform.position}, Speed: {speed}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Si toca al monstruo, buscamos el componente de salud (buscamos también en padres por si el collider está en un hijo)
        if (other.CompareTag("Monster"))
        {
            MonsterHealth monster = other.GetComponent<MonsterHealth>();
            if (monster == null) monster = other.GetComponentInParent<MonsterHealth>();

            if (monster != null)
            {
                monster.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
        // Ignoramos colisiones con otras balas, con la propia torreta o si no tiene tag (suelo)
        else if (!other.CompareTag("Bullet") && !other.CompareTag("Tower") && other.tag != "Untagged")
        {
            Destroy(gameObject);
        }
    }
}
