using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 检查点选择界面
/// 选择一个已激活的检查点作为复活点，然后加载关卡
/// </summary>
public class CheckpointSelectUI : MonoBehaviour
{
    [Header("按钮模板")]
    [SerializeField] private GameObject checkpointButtonPrefab; // 检查点按钮预制体
    [SerializeField] private Transform buttonContainer;        // 按钮容器

    [Header("UI 文字（兼容 TMP 和旧版 Text）")]
    [SerializeField] private TextMeshProUGUI titleTmp;
    [SerializeField] private Text titleLegacy;

    [Header("场景名前缀")]
    [SerializeField] private string sceneNamePrefix = "Level_";

    [Header("返回按钮")]
    [SerializeField] private GameObject levelSelectPanel;      // 点击"返回"时显示这个

    private int _currentLevel;
    private GameObject _thisPanel; // 本面板 GameObject

    private void Awake()
    {
        _thisPanel = gameObject;
    }

    /// <summary>由 LevelSelectUI 调用，传入选中的关卡和所有检查点列表</summary>
    public void ShowForLevel(int levelIndex, string[] allCheckpointIds)
    {
        _currentLevel = levelIndex;
        _thisPanel.SetActive(true);

        // 标题（兼容 TMP 和旧版 Text）
        string titleStr = $"第{levelIndex}关 - 选择起点";
        if (titleTmp != null) titleTmp.text = titleStr;
        else if (titleLegacy != null) titleLegacy.text = titleStr;

        GenerateCheckpointButtons(allCheckpointIds);
    }

    private void GenerateCheckpointButtons(string[] allCheckpointIds)
    {
        // 清空旧按钮
        foreach (Transform child in buttonContainer)
        {
            if (child.gameObject == checkpointButtonPrefab) continue;
            Destroy(child.gameObject);
        }

        // 从 ProgressManager 获取已激活的检查点
        List<string> activated = ProgressManager.Instance != null
            ? ProgressManager.Instance.GetActivatedCheckpoints(_currentLevel)
            : new List<string>();

        // 如果有完整配置：显示所有检查点，未激活的灰色
        if (allCheckpointIds != null && allCheckpointIds.Length > 0)
        {
            foreach (string cpId in allCheckpointIds)
            {
                bool isActivated = activated.Contains(cpId);
                CreateCheckpointButton($"检查点 {cpId}", cpId, isActivated);
            }
        }
        // 没有配置时：只显示已激活的检查点（兜底）
        else
        {
            foreach (string cpId in activated)
            {
                CreateCheckpointButton($"检查点 {cpId}", cpId, true);
            }
        }
    }

    private void CreateCheckpointButton(string label, string checkpointId, bool activated)
    {
        if (checkpointButtonPrefab == null) return;

        GameObject btnObj = Instantiate(checkpointButtonPrefab, buttonContainer);
        btnObj.name = $"CP_{checkpointId}_Btn";
        btnObj.SetActive(true);

        // 按钮文字（兼容旧版 Text 和 TextMeshPro）
        Text legacyText = btnObj.GetComponentInChildren<Text>();
        TextMeshProUGUI tmpText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null) tmpText.text = label;
        else if (legacyText != null) legacyText.text = label;

        // 按钮交互 + 视觉
        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = activated;

            if (activated)
            {
                string capturedCpId = checkpointId;
                btn.onClick.AddListener(() => OnCheckpointClicked(capturedCpId));
            }
        }
    }

    /// <summary>点击了某个检查点</summary>
    private void OnCheckpointClicked(string checkpointId)
    {
        // 记录选中的检查点到 ProgressManager
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.SelectCheckpoint(_currentLevel, checkpointId);
        }

        // 加载关卡场景
        string sceneName = $"{sceneNamePrefix}{_currentLevel}";
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>返回到选关界面</summary>
    public void BackToLevelSelect()
    {
        _thisPanel.SetActive(false);
        if (levelSelectPanel != null)
            levelSelectPanel.SetActive(true);
    }
}
