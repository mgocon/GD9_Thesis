using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundController : MonoBehaviour
{
    public bool isSwitched = false;
    public Image backgroundTutorial;
    public Image backgroundInterview;
    public Animator animator;

    public void SwitchImage(Sprite sprite)
    {
        if (!isSwitched)
        {
            backgroundInterview.sprite = sprite;
            animator.SetTrigger("SwitchTutorial");
        }
        else
        {
            backgroundTutorial.sprite = sprite;
            animator.SetTrigger("SwitchInterview");
        }
        isSwitched = !isSwitched;
    }

    public void SetImage(Sprite sprite)
    {
        if (!isSwitched)
        {
            backgroundTutorial.sprite = sprite;
        }
        else
        {
            backgroundInterview.sprite = sprite;
        }
    }
}
