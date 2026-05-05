using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// 关卡进度与检查点管理器（单例）
/// 管理：关卡解锁、检查点激活/选择、进度持久化
/// 文件：Application.persistentDataPath/progress.json
/// </summary>
public class ProgressManager : MonoBehaviour
{
    // ============================================================
    // 数据模型
    // ============================================================

    [Serializable]
    public class CheckpointData
    {
        public string id;          // e.g. "1-2" = 第1关第2个检查点
        public float posX, posY;   // 世界坐标
    }

    [Serializable]
    public class LevelData
    {
        public int levelIndex;     // 关卡编号（从 1 开始）
        public List<string> activatedCheckpoints = new List<string>();  // 已激活的 checkpoint ID 列表
        public List<CheckpointData> checkpointPositions = new List<CheckpointData>(); // 所有已知位置
    }

    [Serializable]
    public class GameProgress
    {
        public List<int> unlockedLevels = new List<int> { 1 };  // 已解锁的关卡（第一关默认解锁）
        public List<LevelData> levels = new List<LevelData>();
        public int currentLevel = 1;          // 当前关卡
        public string selectedCheckpointId = ""; // 当前选中的检查点 ID
        public string lastPlayTime;
        public bool isFirstTime = true;
    }

    // ============================================================
    // 单例
    // ============================================================

    private static ProgressManager _instance;
    public static ProgressManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ProgressManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject(nameof(ProgressManager));
                    _instance = go.AddComponent<ProgressManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_instance != null) return;
        GameObject go = new GameObject(nameof(ProgressManager));
        _instance = go.AddComponent<ProgressManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        _savePath = Path.Combine(Application.persistentDataPath, "progress.json");
        Load();

        if (_progress == null)
        {
            _progress = new GameProgress();
            Save();
        }
    }

    // ============================================================
    // 运行时
    // ============================================================

    private GameProgress _progress;
    private string _savePath;

    public GameProgress Progress => _progress;

    // ============================================================
    // 关卡解锁
    // ============================================================

    /// <summary>关卡是否已解锁</summary>
    public bool IsLevelUnlocked(int levelIndex)
    {
        return _progress.unlockedLevels.Contains(levelIndex);
    }

    /// <summary>解锁关卡（存文件）</summary>
    public void UnlockLevel(int levelIndex)
    {
        if (!_progress.unlockedLevels.Contains(levelIndex))
        {
            _progress.unlockedLevels.Add(levelIndex);
            Save();
        }
    }

    /// <summary>获取最新已解锁的关卡编号</summary>
    public int GetLatestUnlockedLevel()
    {
        return _progress.unlockedLevels.Max();
    }

    // ============================================================
    // 检查点管理
    // ============================================================

    /// <summary>激活检查点</summary>
    public void ActivateCheckpoint(int levelIndex, string checkpointId, Vector2 position)
    {
        // 更新当前关卡
        _progress.currentLevel = levelIndex;

        // 找到或创建该关卡的数据
        LevelData level = _progress.levels.Find(l => l.levelIndex == levelIndex);
        if (level == null)
        {
            level = new LevelData { levelIndex = levelIndex };
            _progress.levels.Add(level);
        }

        // 激活检查点
        if (!level.activatedCheckpoints.Contains(checkpointId))
        {
            level.activatedCheckpoints.Add(checkpointId);
        }

        // 记录位置
        CheckpointData existing = level.checkpointPositions.Find(c => c.id == checkpointId);
        if (existing != null)
        {
            existing.posX = position.x;
            existing.posY = position.y;
        }
        else
        {
            level.checkpointPositions.Add(new CheckpointData
            {
                id = checkpointId,
                posX = position.x,
                posY = position.y
            });
        }

        // 自动选中当前检查点
        _progress.selectedCheckpointId = checkpointId;

        Save();
    }

    /// <summary>检查点是否已激活</summary>
    public bool IsCheckpointActivated(int levelIndex, string checkpointId)
    {
        LevelData level = _progress.levels.Find(l => l.levelIndex == levelIndex);
        return level != null && level.activatedCheckpoints.Contains(checkpointId);
    }

    /// <summary>指定关卡是否已有激活的检查点</summary>
    public bool HasActivatedCheckpoints(int levelIndex)
    {
        LevelData level = _progress.levels.Find(l => l.levelIndex == levelIndex);
        return level != null && level.activatedCheckpoints.Count > 0;
    }

    /// <summary>自动激活第一个检查点（首次进关卡时用，无坐标）</summary>
    public void AutoActivateFirstCheckpoint(int levelIndex, string checkpointId)
    {
        // 不存坐标（玩家还没走过），只标记为已激活
        LevelData level = _progress.levels.Find(l => l.levelIndex == levelIndex);
        if (level == null)
        {
            level = new LevelData { levelIndex = levelIndex };
            _progress.levels.Add(level);
        }

        if (!level.activatedCheckpoints.Contains(checkpointId))
        {
            level.activatedCheckpoints.Add(checkpointId);
        }

        _progress.selectedCheckpointId = checkpointId;
        _progress.currentLevel = levelIndex;
        Save();
    }

    /// <summary>获取指定关卡中已激活的所有检查点 ID</summary>
    public List<string> GetActivatedCheckpoints(int levelIndex)
    {
        LevelData level = _progress.levels.Find(l => l.levelIndex == levelIndex);
        return level?.activatedCheckpoints ?? new List<string>();
    }

    /// <summary>获取指定检查点的世界坐标</summary>
    public Vector2? GetCheckpointPosition(int levelIndex, string checkpointId)
    {
        LevelData level = _progress.levels.Find(l => l.levelIndex == levelIndex);
        if (level == null) return null;

        CheckpointData cp = level.checkpointPositions.Find(c => c.id == checkpointId);
        if (cp == null) return null;

        return new Vector2(cp.posX, cp.posY);
    }

    /// <summary>获取当前选中的复活位置</summary>
    public Vector2? GetSelectedRespawnPosition()
    {
        return GetCheckpointPosition(_progress.currentLevel, _progress.selectedCheckpointId);
    }

    /// <summary>手动选中一个检查点</summary>
    public void SelectCheckpoint(int levelIndex, string checkpointId)
    {
        _progress.currentLevel = levelIndex;
        _progress.selectedCheckpointId = checkpointId;
        Save();
    }

    // ============================================================
    // 关卡完成
    // ============================================================

    /// <summary>完成当前关卡（解锁下一关）</summary>
    public void CompleteCurrentLevel()
    {
        int nextLevel = _progress.currentLevel + 1;
        UnlockLevel(nextLevel);
        _progress.currentLevel = nextLevel;
        _progress.selectedCheckpointId = "";
        Save();
    }

    // ============================================================
    // 文件读写
    // ============================================================

    public void Save()
    {
        _progress.lastPlayTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string json = JsonUtility.ToJson(_progress, true);
        File.WriteAllText(_savePath, json);
    }

    public void Load()
    {
        if (!File.Exists(_savePath)) return;

        try
        {
            string json = File.ReadAllText(_savePath);
            _progress = JsonUtility.FromJson<GameProgress>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"读取进度文件失败: {e.Message}");
            _progress = null;
        }
    }

    /// <summary>重置所有进度（慎用）</summary>
    public void ResetAllProgress()
    {
        _progress = new GameProgress();
        Save();
    }
}
