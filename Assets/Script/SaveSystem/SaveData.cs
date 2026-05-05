using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
// 存档槽位数据
// ============================================================

[Serializable]
public class SaveData
{
    public int slotIndex;
    public string timestamp;
    public int sceneIndex;
    public float playerPosX, playerPosY, playerPosZ;
    public float checkpointX, checkpointY;
    public bool hasCheckpoint;
    public List<SaveEntry> entries = new List<SaveEntry>();
}

// ============================================================
// 单个 ISaveable 组件的存档条目
// ============================================================

[Serializable]
public class SaveEntry
{
    public string id;               // 唯一标识符
    public string componentTypeName; // 组件类型全名
    public string stateTypeName;     // 状态对象类型全名
    public string jsonData;         // 该组件的状态（JsonUtility 序列化）
}

// ============================================================
// 跨周目全局记忆数据（独立于槽位）
// ============================================================

[Serializable]
public class GlobalData
{
    public int totalPlaythroughs;
    public List<string> discoveredSecrets = new List<string>();
    public string lastPlayTime;
    public List<string> globalFlags = new List<string>();
}

// ============================================================
// 存档标识符组件：挂载在需要存档的 GameObject 上
// ============================================================

/// <summary>
/// 给 ISaveable 组件提供唯一标识。
/// 如果不指定 ID，会自动生成基于场景路径的 ID。
/// </summary>
public class SaveableIdentifier : MonoBehaviour
{
    [SerializeField] private string customId;

    public string GetId()
    {
        if (!string.IsNullOrEmpty(customId))
            return customId;

        // 自动生成：场景名/对象层级路径
        string path = gameObject.scene.name + "/" + GetScenePath(gameObject.transform);
        return path;
    }

    private static string GetScenePath(Transform t)
    {
        if (t.parent == null) return t.name;
        return GetScenePath(t.parent) + "/" + t.name;
    }

    private void Reset()
    {
        // 在编辑器中添加组件时，自动填上一个友好的 ID
        if (string.IsNullOrEmpty(customId))
            customId = gameObject.name + "_" + GetType().Name;
    }
}
