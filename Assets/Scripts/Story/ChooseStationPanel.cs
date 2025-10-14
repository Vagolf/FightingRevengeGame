using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace Story
{
    public class ChooseStationPanel : MonoBehaviour
    {
        [Header("Panels (same scene)")]
        [SerializeField] private GameObject panelSaveStory;
        [SerializeField] private GameObject panelCreateSave;
        [SerializeField] private GameObject panelChooseStation;

        [Header("Header UI")]
        [SerializeField] private TMP_Text saveNameText;
        [SerializeField] private TMP_Text difficultyText;

        [Header("Stage Select")]
        [SerializeField] private Button stage1Button;
        [SerializeField] private Button stage2Button;
        [SerializeField] private Button stage3Button;
        [SerializeField] private Image mapPreview;
        [SerializeField] private Sprite stage1Sprite;
        [SerializeField] private Sprite stage2Sprite;
        [SerializeField] private Sprite stage3Sprite;
        [Header("Optional: Multiple Previews (Roman/Eva/Dusan)")]
        [Tooltip("Assign 3 Images to show per selection (1=Roman, 2=Eva, 3=Dusan). If set, these will be toggled active; otherwise fallback to single sprite swap above.")]
        [SerializeField] private Image[] mapPreviews; // size 3

        [Header("Start")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button backButton;

        [Header("Scene names (9 scenes)")]
        [SerializeField] private string easyStage1Scene;
        [SerializeField] private string easyStage2Scene;
        [SerializeField] private string easyStage3Scene;
        [SerializeField] private string normalStage1Scene;
        [SerializeField] private string normalStage2Scene;
        [SerializeField] private string normalStage3Scene;
        [SerializeField] private string hardStage1Scene;
        [SerializeField] private string hardStage2Scene;
        [SerializeField] private string hardStage3Scene;

        private int selectedStageIndex = 0; // 1..3

        private void Awake()
        {
            if (stage1Button) stage1Button.onClick.AddListener(() => SelectStage(1));
            if (stage2Button) stage2Button.onClick.AddListener(() => SelectStage(2));
            if (stage3Button) stage3Button.onClick.AddListener(() => SelectStage(3));
            if (startButton) startButton.onClick.AddListener(StartGame);
            if (backButton) backButton.onClick.AddListener(Back);
        }

        private void OnEnable()
        {
            // Ensure time is resumed when entering this panel (in case previous UI paused it)
            Time.timeScale = 1f;
            try { PauseGame.GameIsPause = false; } catch { }

            var save = SaveManager.GetCurrent();
            if (save == null) return;
            if (saveNameText) saveNameText.text = save.name;
            if (difficultyText) difficultyText.text = save.difficulty.ToString();

            // lock stages beyond progress+1
            var maxPlayable = Mathf.Clamp(save.selectedStage + 1, 1, 3);
            if (stage1Button) stage1Button.interactable = 1 <= maxPlayable;
            if (stage2Button) stage2Button.interactable = 2 <= maxPlayable;
            if (stage3Button) stage3Button.interactable = 3 <= maxPlayable;

            // auto select first available
            selectedStageIndex = Mathf.Clamp(maxPlayable, 1, 3);
            SelectStage(selectedStageIndex);
        }

        private void SelectStage(int index)
        {
            selectedStageIndex = index;
            // Prefer multi-image previews if assigned
            if (mapPreviews != null && mapPreviews.Length >= 3)
            {
                for (int i = 0; i < mapPreviews.Length; i++)
                {
                    if (mapPreviews[i] == null) continue;
                    mapPreviews[i].gameObject.SetActive((i + 1) == index);
                }
            }
            else if (mapPreview)
            {
                // Fallback to single sprite switch
                mapPreview.sprite = index == 1 ? stage1Sprite : index == 2 ? stage2Sprite : stage3Sprite;
            }
        }

        private void StartGame()
        {
            var save = SaveManager.GetCurrent();
            if (save == null) return;
            string scene = ResolveScene(save.difficulty, selectedStageIndex);
            if (!string.IsNullOrEmpty(scene))
            {
                // Ensure game is not paused when entering gameplay
                Time.timeScale = 1f;
                try { PauseGame.GameIsPause = false; } catch { }
                SceneManager.LoadScene(scene);
            }
        }

        private string ResolveScene(Difficulty diff, int stage)
        {
            switch (diff)
            {
                case Difficulty.Easy:
                    return stage == 1 ? easyStage1Scene : stage == 2 ? easyStage2Scene : easyStage3Scene;
                case Difficulty.Normal:
                    return stage == 1 ? normalStage1Scene : stage == 2 ? normalStage2Scene : normalStage3Scene;
                case Difficulty.Hard:
                    return stage == 1 ? hardStage1Scene : stage == 2 ? hardStage2Scene : hardStage3Scene;
            }
            return null;
        }

        private void Back()
        {
            if (panelChooseStation) panelChooseStation.SetActive(false);
            if (panelSaveStory) panelSaveStory.SetActive(true);
            if (panelCreateSave) panelCreateSave.SetActive(false);
        }
    }
}
