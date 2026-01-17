using UnityEngine;
using Vuforia;

public class GroundAnchorDebugger : MonoBehaviour
{
    AnchorBehaviour anchor;

    void Awake()
    {
        anchor = GetComponent<AnchorBehaviour>();
    }

    void Update()
    {
        if (anchor != null && anchor.enabled)
        {
            // El anchor ya está colocado en una superficie detectada
            Debug.DrawRay(transform.position, Vector3.up * 0.2f, Color.green);
        }
    }
}