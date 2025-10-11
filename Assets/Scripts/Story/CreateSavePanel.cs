using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Story
{
    public class CreateSavePanel : MonoBehaviour
    {
        [Header("Panels (same scene)")]
        [SerializeField] private GameObject panelSaveStory;
        [SerializeField] private GameObject panelCreateSave;
        [SerializeField] private GameObject panelChooseStation;

        [Header("Inputs")]
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;
        [SerializeField] private Button createButton;
        [SerializeField] private Button backButton;

        [Header("Visual state")]
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color normalColor = Color.white;

        private Difficulty selected = Difficulty.Normal;
        private static readonly Regex NameRegex = new Regex("^[A-Za-z0-9 _-]{1,20}$");

        private void Awake()
        {
            if (easyButton) easyButton.onClick.AddListener(() => SetDifficulty(Difficulty.Easy));
            if (normalButton) normalButton.onClick.AddListener(() => SetDifficulty(Difficulty.Normal));
            if (hardButton) hardButton.onClick.AddListener(() => SetDifficulty(Difficulty.Hard));
            if (createButton) createButton.onClick.AddListener(CreateSave);
            if (backButton) backButton.onClick.AddListener(Back);
            SetDifficulty(Difficulty.Normal);
        }

        private void SetDifficulty(Difficulty diff)
        {
            selected = diff;
            // simple visual toggle by button image color
            if (easyButton) SetBtnColor(easyButton, diff == Difficulty.Easy);
            if (normalButton) SetBtnColor(normalButton, diff == Difficulty.Normal);
            if (hardButton) SetBtnColor(hardButton, diff == Difficulty.Hard);
        }

        private void SetBtnColor(Button btn, bool active)
        {
            var img = btn.GetComponent<Image>();
            if (img) img.color = active ? selectedColor : normalColor;
        }

        private void CreateSave()
        {
            var n = nameInput ? nameInput.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(n) || !NameRegex.IsMatch(n))
            {
                Debug.LogWarning("Invalid name. Use 1-20 English letters/numbers/space/_/-");
                return;
            }
            var save = SaveManager.Create(n, selected);
            if (panelCreateSave) panelCreateSave.SetActive(false);
            if (panelChooseStation) panelChooseStation.SetActive(true);
            if (panelSaveStory) panelSaveStory.SetActive(false);
        }

        private void Back()
        {
            if (panelCreateSave) panelCreateSave.SetActive(false);
            if (panelSaveStory) panelSaveStory.SetActive(true);
            if (panelChooseStation) panelChooseStation.SetActive(false);
        }
    }
}
