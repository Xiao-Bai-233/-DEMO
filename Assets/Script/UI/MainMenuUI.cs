using UnityEngine;

/// <summary>
/// 主菜单界面控制
/// 管理"开始游戏"按钮与选关面板的切换
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("面板")]
    [SerializeField] private GameObject mainMenuPanel;      // 主菜单（标题、开始游戏按钮等）
    [SerializeField] private GameObject levelSelectPanel;    // 选关面板

    private void Start()
    {
        // 初始状态：主菜单可见，选关面板隐藏
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
    }

    /// <summary>
    /// 点击"开始游戏"：隐藏主菜单，显示选关面板
    /// </summary>
    public void OnStartGameClicked()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
    }

    /// <summary>
    /// 从选关面板返回主菜单
    /// </summary>
    public void BackToMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
    }

    /// <summary>
    /// 退出游戏（挂到退出按钮上）
    /// </summary>
    public void OnQuitGame()
    {
        Application.Quit();
    }
}
