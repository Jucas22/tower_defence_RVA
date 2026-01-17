using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SimpleJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public Image background;
    public Image handle;

    // Vector normalizado (-1..1) con la dirección del joystick
    public Vector2 Direction { get; private set; }

    Vector2 _center;

    void Start()
    {
        if (background == null)
            background = GetComponent<Image>();

        if (handle == null && transform.childCount > 0)
            handle = transform.GetChild(0).GetComponent<Image>();

        _center = background.rectTransform.position;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background.rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out pos);

        // radio máximo = mitad del tamaño
        pos = Vector2.ClampMagnitude(pos, background.rectTransform.sizeDelta.x * 0.5f);

        // mover el handle
        handle.rectTransform.anchoredPosition = pos;

        // dirección normalizada (-1,1)
        var normalized = pos / (background.rectTransform.sizeDelta.x * 0.5f);
        Direction = normalized;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        handle.rectTransform.anchoredPosition = Vector2.zero;
        Direction = Vector2.zero;
    }
}