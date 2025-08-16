using UnityEngine;
using UnityEngine.EventSystems;

public class HoverMenu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject dropdown;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (dropdown != null)
            dropdown.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (dropdown != null)
            dropdown.SetActive(false);
    }
}
