using UnityEngine;
using UnityEngine.EventSystems;

public class InputButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public bool Input => _input;
    private bool _input;

    public void OnPointerDown(PointerEventData eventData)
    {
        _input = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _input = false;
    }
}
