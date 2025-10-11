using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Story
{
    public class SaveStoryPanel : MonoBehaviour
    {
        [Header("Panels (same scene)")]
        [SerializeField] private GameObject panelSaveStory;
        [SerializeField] private GameObject panelCreateSave;
        [SerializeField] private GameObject panelChooseStation;

        [Header("Save list UI")]
        [SerializeField] private Transform content;
        [SerializeField] private GameObject saveItemPrefab; // Button + TMP_Text
        [SerializeField] private Button createNewButton;
        [SerializeField] private Button backButton;

        private void Awake()
        {
            if (createNewButton) createNewButton.onClick.AddListener(OpenCreateSave);
            if (backButton) backButton.onClick.AddListener(BackToMenu);
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (!content || !saveItemPrefab) return;
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);

            List<SaveData> saves = SaveManager.GetAll();
            foreach (var s in saves)
            {
                var go = Instantiate(saveItemPrefab, content);
                var btn = go.GetComponentInChildren<Button>();
                var txt = go.GetComponentInChildren<TMP_Text>();
                if (txt) txt.text = $"{s.name} ({s.difficulty})";
                if (btn) btn.onClick.AddListener(() => SelectSave(s.id));
            }
        }

        private void SelectSave(string id)
        {
            SaveManager.SetCurrentId(id);
            SaveManager.Touch(id);
            // switch panel to ChooseStation
            if (panelSaveStory) panelSaveStory.SetActive(false);
            if (panelChooseStation) panelChooseStation.SetActive(true);
            if (panelCreateSave) panelCreateSave.SetActive(false);
        }

        private void OpenCreateSave()
        {
            if (panelSaveStory) panelSaveStory.SetActive(false);
            if (panelCreateSave) panelCreateSave.SetActive(true);
            if (panelChooseStation) panelChooseStation.SetActive(false);
        }

        private void BackToMenu()
        {
            // just deactivate this panel to reveal parent/menu panel in same scene
            if (panelSaveStory) panelSaveStory.SetActive(false);
        }
    }
}
