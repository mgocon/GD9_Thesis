using UnityEngine;
using UnityEngine.UI;

public class DropdownHover : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public GameObject dropdown;  // Assign DropdownLevels in Inspector

    int inside = 0;

    void Awake()
    {
        if (dropdown != null)
            dropdown.SetActive(false);
    }

    // Called by EventTrigger -> PointerEnter
    public void OnPointerEnter()
    {
        inside++;
        Open();
    }

    // Called by EventTrigger -> PointerExit
    public void OnPointerExit()
    {
        inside = Mathf.Max(0, inside - 1);
        if (inside == 0)
            Close();
    }

    void Open()
    {
        if (!dropdown.activeSelf)
        {
            dropdown.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        }
    }

    void Close()
    {
        if (dropdown.activeSelf)
        {
            dropdown.SetActive(false);
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        }
    }

    // Optional: toggle by click (for keyboard/controller users)
    public void ToggleByClick()
    {
        bool next = !dropdown.activeSelf;
        dropdown.SetActive(next);
        inside = next ? 1 : 0;
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }
}
