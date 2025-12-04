using UnityEngine;
using UnityEngine.InputSystem;

public class mover : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        { transform.Translate(new Vector3(-1.0f * 2 * Time.deltaTime, 0.0f, 0.0f)); }
        if (Input.GetKey(KeyCode.RightArrow))
        { transform.Translate(new Vector3(1.0f * 2 * Time.deltaTime, 0.0f, 0.0f)); }
    }
}
