using UnityEngine;

public class Dropdown : MonoBehaviour
{
    public GameObject dropdownContainer;

    public void ToggleDropdown()
    {
        dropdownContainer.SetActive(!dropdownContainer.activeSelf);
    }
}
