using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 存档系统核心管理器（单例）
/// 支持多槽位、自动存档、独立于槽位的跨周目全局记忆
/// </summary>
public class SaveManager : MonoBehaviour
{
    // ============================================================
    // 单例
    // ============================================================

    private static SaveManager _instance;
    public static SaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SaveManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject(nameof(SaveManager));
                    _instance = go.AddComponent<SaveManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        if (_instance != null) return;
        GameObject go = new GameObject(nameof(SaveManager));
        _instance = go.AddComponent<SaveManager>();
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

        EnsureDirectoryExists();
    }

    // ============================================================
    // 配置
    // ============================================================

    [Header("存档配置")]
    [SerializeField] private int maxSlots = 5;
    [SerializeField] private int currentSlot = 0;

    public int CurrentSlot => currentSlot;
    public int MaxSlots => maxSlots;

    // ============================================================
    // 路径
    // ============================================================

    private string SaveFilePath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, $"save_{slot}.json");
    }

    private string GlobalFilePath
    {
        get { return Path.Combine(Application.persistentDataPath, "global_meta.json"); }
    }

    private void EnsureDirectoryExists()
    {
        string dir = Application.persistentDataPath;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    // ============================================================
    // 存档（写入）
    // ============================================================

    /// <summary>保存到指定槽位</summary>
    public void SaveToSlot(int slotIndex)
    {
        currentSlot = Mathf.Clamp(slotIndex, 0, maxSlots - 1);

        SaveData data = new SaveData();
        data.slotIndex = currentSlot;
        data.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        data.sceneIndex = SceneManager.GetActiveScene().buildIndex;

        // 保存玩家位置
        SavePlayerPosition(data);

        // 遍历场景中所有 ISaveable 组件
        MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var script in allScripts)
        {
            if (script is ISaveable saveable)
            {
                object state = saveable.CaptureState();
                SaveEntry entry = new SaveEntry();
                entry.id = GetIdentifier(script.gameObject);
                entry.componentTypeName = script.GetType().FullName;
                entry.stateTypeName = state?.GetType().FullName ?? "";
                entry.jsonData = JsonUtility.ToJson(state);
                data.entries.Add(entry);
            }
        }

        // 写入文件
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(SaveFilePath(currentSlot), json);
        Debug.Log($"存档已保存到槽位 {currentSlot}: {SaveFilePath(currentSlot)}");
    }

    /// <summary>自动存档（覆盖当前槽位）</summary>
    public void AutoSave()
    {
        SaveToSlot(currentSlot);
    }

    // ============================================================
    // 读档（读取）
    // ============================================================

    /// <summary>从指定槽位读取存档</summary>
    public bool LoadFromSlot(int slotIndex)
    {
        string path = SaveFilePath(slotIndex);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"槽位 {slotIndex} 没有存档文件");
            return false;
        }

        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null)
        {
            Debug.LogError($"槽位 {slotIndex} 的存档文件损坏");
            return false;
        }

        currentSlot = slotIndex;

        // 恢复玩家位置
        RestorePlayerPosition(data);

        // 恢复场景中所有 ISaveable 组件
        Dictionary<string, ISaveable> saveableMap = new Dictionary<string, ISaveable>();
        MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var script in allScripts)
        {
            if (script is ISaveable saveable)
            {
                string id = GetIdentifier(script.gameObject);
                if (!saveableMap.ContainsKey(id))
                    saveableMap[id] = saveable;
                else
                    Debug.LogWarning($"重复的存档标识符: {id}");
            }
        }

        foreach (var entry in data.entries)
        {
            if (saveableMap.TryGetValue(entry.id, out ISaveable saveable))
            {
                try
                {
                    // 根据 stateTypeName 反序列化状态对象
                    Type stateType = !string.IsNullOrEmpty(entry.stateTypeName)
                        ? Type.GetType(entry.stateTypeName)
                        : null;

                    object state;
                    if (stateType != null)
                        state = JsonUtility.FromJson(entry.jsonData, stateType);
                    else
                        state = JsonUtility.FromJson(entry.jsonData, typeof(object));

                    saveable.RestoreState(state);
                }
                catch (Exception e)
                {
                    Debug.LogError($"恢复 {entry.id} 失败: {e.Message}");
                }
            }
        }

        Debug.Log($"已读取槽位 {slotIndex} 的存档");
        return true;
    }

    /// <summary>加载上次存档（当前槽位）</summary>
    public bool LoadLastSave()
    {
        return LoadFromSlot(currentSlot);
    }

    /// <summary>检查指定槽位是否有存档</summary>
    public bool HasSave(int slotIndex)
    {
        return File.Exists(SaveFilePath(slotIndex));
    }

    // ============================================================
    // 删除存档
    // ============================================================

    /// <summary>删除指定槽位的存档文件（保留 global_meta.json）</summary>
    public void DeleteSlot(int slotIndex)
    {
        string path = SaveFilePath(slotIndex);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"已删除槽位 {slotIndex} 的存档文件");
        }
    }

    /// <summary>删除所有槽位的存档（保留 global_meta.json）</summary>
    public void DeleteAllSlots()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            DeleteSlot(i);
        }
        Debug.Log("已删除所有槽位的存档文件");
    }

    // ============================================================
    // 跨周目全局记忆
    // ============================================================

    public void SaveGlobalData(GlobalData data)
    {
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(GlobalFilePath, json);
    }

    public GlobalData LoadGlobalData()
    {
        if (!File.Exists(GlobalFilePath))
            return new GlobalData();

        string json = File.ReadAllText(GlobalFilePath);
        return JsonUtility.FromJson<GlobalData>(json) ?? new GlobalData();
    }

    // ============================================================
    // 玩家位置辅助
    // ============================================================

    private void SavePlayerPosition(SaveData data)
    {
        // 从 GameManager 获取玩家位置
        if (GameManager.Instance != null && GameManager.Instance.Player != null)
        {
            Vector3 pos = GameManager.Instance.Player.transform.position;
            data.playerPosX = pos.x;
            data.playerPosY = pos.y;
            data.playerPosZ = pos.z;
        }

        // 存档点位置
        if (GameManager.Instance != null)
        {
            Vector2 cp = GameManager.Instance.LastCheckpoint;
            data.checkpointX = cp.x;
            data.checkpointY = cp.y;
            data.hasCheckpoint = true;
        }
    }

    private void RestorePlayerPosition(SaveData data)
    {
        // 恢复玩家位置到存档点或保存位置
        if (GameManager.Instance != null && GameManager.Instance.Player != null)
        {
            Vector3 pos;
            if (data.hasCheckpoint)
            {
                pos = new Vector3(data.checkpointX, data.checkpointY, 0f);
            }
            else
            {
                pos = new Vector3(data.playerPosX, data.playerPosY, data.playerPosZ);
            }

            GameManager.Instance.Player.transform.position = pos;
            GameManager.Instance.SetCheckpoint(pos);
        }
    }

    // ============================================================
    // 标识符获取
    // ============================================================

    private string GetIdentifier(GameObject obj)
    {
        SaveableIdentifier si = obj.GetComponent<SaveableIdentifier>();
        if (si != null)
            return si.GetId();

        // 没有挂 SaveableIdentifier 时自动生成
        return obj.scene.name + "/" + obj.name;
    }
}
