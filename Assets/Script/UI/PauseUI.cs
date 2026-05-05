using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 暂停设置界面管理器
/// 
/// 【设计原理】
/// 自动附加到 GameManager（DontDestroyOnLoad），独立于场景 UI 的生命周期。
/// Canvas 初始为 inactive（隐藏状态），暂停时 PauseUI 激活它，恢复时隐藏。
/// 按钮全部通过代码自动绑定，用户不需要手动拖拽引用。
/// 
/// 【用户需要做的 - 在 Unity 编辑器中】
/// 1. 把 "Canvas" 重命名为 "PauseCanvas"（便于自动查找）
/// 2. 在 Pause 子物体上删除 "Pause_true (Missing Script)"
/// 3. 其余什么都不用做，PauseUI 自动完成
/// </summary>
public class PauseUI : MonoBehaviour
{
    [Header("场景中的 Canvas（运行时自动查找）")]
    [SerializeField] private GameObject pauseCanvas; // 可以手动拖入，也可以自动查找

    [Header("快捷键")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Tab;

    // 用于自动查找的名称约定
    private const string CANVAS_NAME = "PauseCanvas";
    private const string PANEL_NAME = "Pause";
    private const string RESUME_BTN_NAME = "Button (1)";
    private const string MENU_BTN_NAME = "Button";
    private const string SLIDER_NAME = "Slider";
    private const string VOLUME_TEXT_NAME = "Text (TMP)";

    private void Awake()
    {
        // 初始隐藏面板
        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGamePaused += ShowPauseUI;
            GameManager.Instance.OnGameResumed += HidePauseUI;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGamePaused -= ShowPauseUI;
            GameManager.Instance.OnGameResumed -= HidePauseUI;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (IsMenuScene()) return;
            GameManager.Instance.TogglePause();
        }
    }

    // ============================================================
    // 场景切换
    // ============================================================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 切场景后恢复时间（防卡死）
        if (GameManager.Instance.IsPaused)
            GameManager.Instance.ResumeGame();

        // 在新场景中查找 PauseCanvas
        FindPauseCanvas();

        // 如果找到，代码自动绑定按钮事件
        if (pauseCanvas != null)
        {
            HookUpButtonsAndSlider();
            pauseCanvas.SetActive(false); // 初始隐藏
        }
    }

    // ============================================================
    // 查找 Canvas（即使它是 inactive 也能找到）
    // ============================================================

    private void FindPauseCanvas()
    {
        if (pauseCanvas != null) return; // 已经有了

        // GetRootGameObjects 能找到 inactive 的对象
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == CANVAS_NAME)
            {
                pauseCanvas = root;
                Debug.Log($"PauseUI: 找到暂停 Canvas ({CANVAS_NAME})");
                return;
            }
        }

        Debug.LogWarning($"PauseUI: 场景中未找到名为 {CANVAS_NAME} 的 Canvas（请将暂停 Canvas 重命名为 {CANVAS_NAME}）");
    }

    // ============================================================
    // 代码自动绑定按钮和滑块（不需要在 Inspector 拖拽）
    // ============================================================

    private void HookUpButtonsAndSlider()
    {
        if (pauseCanvas == null) return;

        // PauseCanvas → Pause → 各子物体
        Transform pausePanel = pauseCanvas.transform.Find(PANEL_NAME);
        if (pausePanel == null)
        {
            Debug.LogWarning($"PauseUI: 未找到 {PANEL_NAME} 子物体");
            return;
        }

        // --- "继续游戏" 按钮 ---
        Transform resumeBtnTransform = pausePanel.Find(RESUME_BTN_NAME);
        if (resumeBtnTransform != null)
        {
            Button resumeBtn = resumeBtnTransform.GetComponent<Button>();
            if (resumeBtn != null)
            {
                resumeBtn.onClick.RemoveAllListeners();
                resumeBtn.onClick.AddListener(OnResumeClicked);
                Debug.Log("PauseUI: 自动绑定 '继续游戏' 按钮");
            }
        }

        // --- "返回主菜单" 按钮 ---
        Transform menuBtnTransform = pausePanel.Find(MENU_BTN_NAME);
        if (menuBtnTransform != null)
        {
            Button menuBtn = menuBtnTransform.GetComponent<Button>();
            if (menuBtn != null)
            {
                menuBtn.onClick.RemoveAllListeners();
                menuBtn.onClick.AddListener(OnReturnToMenuClicked);
                Debug.Log("PauseUI: 自动绑定 '返回主菜单' 按钮");
            }
        }

        // --- 音量滑块 ---
        Transform sliderTransform = pausePanel.Find(SLIDER_NAME);
        if (sliderTransform != null)
        {
            Slider volumeSlider = sliderTransform.GetComponent<Slider>();
            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.RemoveAllListeners();
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

                // 初始化滑块值 = 当前音量
                volumeSlider.value = AudioListener.volume;
                Debug.Log("PauseUI: 自动绑定音量滑块");
            }
        }
    }

    // ============================================================
    // 暂停/恢复事件
    // ============================================================

    private void ShowPauseUI()
    {
        if (pauseCanvas != null)
            pauseCanvas.SetActive(true);
    }

    private void HidePauseUI()
    {
        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);
    }

    // ============================================================
    // 按钮回调
    // ============================================================

    public void OnResumeClicked()
    {
        GameManager.Instance.ResumeGame();
    }

    public void OnReturnToMenuClicked()
    {
        GameManager.Instance.ResumeGame();
        SceneManager.LoadScene("Menu");
    }

    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
    }

    // ============================================================
    // 工具
    // ============================================================

    private bool IsMenuScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.name == "Menu" || activeScene.buildIndex == 0;
    }
}
