using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class InteractiveObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public UnityEvent onPointerEnter = new UnityEvent();
    public UnityEvent onPointerExit = new UnityEvent();
    public UnityEvent onPointerClick = new UnityEvent();

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"Pointer Enter: {gameObject.name}");
        onPointerEnter.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"Pointer Exit: {gameObject.name}");
        onPointerExit.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"Pointer Click: {gameObject.name}");
        onPointerClick.Invoke();
    }
}