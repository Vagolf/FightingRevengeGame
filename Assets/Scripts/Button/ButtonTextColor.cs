using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonTextColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color selectedColor = Color.cyan; // Added selected color
    public TextMeshProUGUI buttonText;

    public ButtonGroupColorManager groupManager;
    private bool isSelected = false;

    private void Start()
    {
        buttonText.color = normalColor;
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        buttonText.color = selectedColor; // Use selected color
        groupManager?.OnButtonSelected(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelected)
            buttonText.color = highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected)
            buttonText.color = normalColor;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        buttonText.color = normalColor;
    }

    public void SetToNormal()
    {
        isSelected = false;
        buttonText.color = normalColor;
    }
}