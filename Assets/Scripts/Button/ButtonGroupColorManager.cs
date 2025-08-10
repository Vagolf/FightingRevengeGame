using UnityEngine;
using System.Collections.Generic;

public class ButtonGroupColorManager : MonoBehaviour
{
    public List<ButtonTextColor> buttons;

    public void OnButtonSelected(ButtonTextColor selected)
    {
        foreach (var btn in buttons)
        {
            if (btn != selected)
                btn.SetToNormal();
        }
    }
}