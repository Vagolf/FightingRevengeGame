using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonTextColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerClickHandler
{
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color selectedColor = Color.cyan; // Added selected color
    public TextMeshProUGUI buttonText;

    public ButtonGroupColorManager groupManager;
    [Tooltip("Buttons with the same groupKey belong to the same selection set. Leave empty to auto-group by parent container.")]
    public string groupKey = string.Empty;
    [Tooltip("Optional explicit grouping root. Buttons referencing the same container are in one set.")]
    public Transform groupContainer;
    private bool isSelected = false;

    private void Start()
    {
        buttonText.color = normalColor;
        if (groupManager == null)
        {
            groupManager = GetComponentInParent<ButtonGroupColorManager>();
            if (groupManager != null)
            {
                groupManager.AddButton(this);
            }
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        buttonText.color = selectedColor; // Use selected color
        groupManager?.OnButtonChosen(this);
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
        // Ignore global deselect; manager handles resetting within group
        // This allows multiple groups to keep their own selected state concurrently
        if (groupManager != null && groupManager.IsCurrentSelected(this))
        {
            isSelected = true;
            buttonText.color = selectedColor;
            return;
        }
    }

    public void SetToNormal()
    {
        isSelected = false;
        buttonText.color = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isSelected = true;
        buttonText.color = selectedColor;
        groupManager?.OnButtonChosen(this);
    }
}