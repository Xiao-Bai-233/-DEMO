using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 关卡选择界面
/// 挂在 Canvas 上，自动生成关卡按钮
/// </summary>
[System.Serializable]
public class LevelCheckpointConfig
{
    public int levelIndex;
    public string[] checkpointIds; // 该关卡的所有检查点 ID
}

public class LevelSelectUI : MonoBehaviour
{
    [Header("按钮模板")]
    [SerializeField] private GameObject levelButtonPrefab; // 拖入一个 Button 预制体
    [SerializeField] private Transform buttonContainer;    // 按钮放在哪个容器下

    [Header("关卡配置")]
    [SerializeField] private int maxLevels = 10;
    [SerializeField] private string sceneNamePrefix = "Level_"; // 场景名前缀：Level_1, Level_2 ...

    [Header("每个关卡的所有检查点（用于未激活时显示灰色）")]
    [SerializeField] private LevelCheckpointConfig[] levelCheckpointConfigs; // 拖入配置

    [Header("跳转面板")]
    [SerializeField] private GameObject checkpointSelectPanel;  // 选检查点的面板（隐藏/显示）
    [SerializeField] private GameObject levelSelectPanel;       // 本面板（用于隐藏）

    private void Start()
    {
        GenerateLevelButtons();
    }

    private void GenerateLevelButtons()
    {
        // 清空旧的按钮（保留模板）
        foreach (Transform child in buttonContainer)
        {
            if (child.gameObject == levelButtonPrefab) continue;
            Destroy(child.gameObject);
        }

        for (int i = 1; i <= maxLevels; i++)
        {
            bool unlocked = ProgressManager.Instance != null && 
                            ProgressManager.Instance.IsLevelUnlocked(i);

            CreateLevelButton(i, unlocked);
        }
    }

    private void CreateLevelButton(int levelIndex, bool unlocked)
    {
        if (levelButtonPrefab == null) return;

        GameObject btnObj = Instantiate(levelButtonPrefab, buttonContainer);
        btnObj.name = $"Level_{levelIndex}_Btn";
        btnObj.SetActive(true);

        // 按钮文字（兼容旧版 Text 和 TextMeshPro）
        Text legacyText = btnObj.GetComponentInChildren<Text>();
        TextMeshProUGUI tmpText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        string btnLabel = unlocked ? $"第{levelIndex}关" : $"第{levelIndex}关\n🔒";
        if (tmpText != null) tmpText.text = btnLabel;
        else if (legacyText != null) legacyText.text = btnLabel;

        // 按钮交互
        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = unlocked;

            if (unlocked)
            {
                int capturedIndex = levelIndex;
                btn.onClick.AddListener(() => OnLevelClicked(capturedIndex));
            }
        }
    }

    /// <summary>点击了某个关卡</summary>
    private void OnLevelClicked(int levelIndex)
    {
        // 隐藏选关面板，显示选检查点面板
        if (levelSelectPanel != null)
            levelSelectPanel.SetActive(false);

        if (checkpointSelectPanel != null)
        {
            checkpointSelectPanel.SetActive(true);

            // 通知检查点选择面板
            CheckpointSelectUI cpUI = checkpointSelectPanel.GetComponent<CheckpointSelectUI>();
            if (cpUI != null)
            {
                // 查找该关卡的所有检查点列表
                string[] allCps = null;
                foreach (var cfg in levelCheckpointConfigs)
                {
                    if (cfg != null && cfg.levelIndex == levelIndex)
                    {
                        allCps = cfg.checkpointIds;
                        break;
                    }
                }

                // 自动激活第一个检查点（如果该关卡还没有任何激活记录）
                if (allCps != null && allCps.Length > 0 &&
                    ProgressManager.Instance != null &&
                    !ProgressManager.Instance.HasActivatedCheckpoints(levelIndex))
                {
                    // 玩家还没碰过任何检查点，自动激活第一个
                    // 不记录坐标（还没走过），选它只是为了让玩家能出生
                    ProgressManager.Instance.AutoActivateFirstCheckpoint(levelIndex, allCps[0]);
                }

                cpUI.ShowForLevel(levelIndex, allCps);
            }
        }
        else
        {
            // 没有检查点面板，直接加载关卡
            LoadLevel(levelIndex, "");
        }
    }

    /// <summary>加载关卡（从关卡选择调用）</summary>
    public void LoadLevel(int levelIndex, string checkpointId)
    {
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.SelectCheckpoint(levelIndex, checkpointId);
        }

        string sceneName = $"{sceneNamePrefix}{levelIndex}";
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>回到主菜单（不重新加载场景，直接切换面板）</summary>
    public void BackToMainMenu()
    {
        // 如果在 Menu 场景中，直接用 MainMenuUI 切换面板
        MainMenuUI mainMenu = FindFirstObjectByType<MainMenuUI>();
        if (mainMenu != null)
        {
            mainMenu.BackToMainMenu();
            return;
        }

        // 兜底：加载 Menu 场景
        SceneManager.LoadScene("Menu");
    }
}
