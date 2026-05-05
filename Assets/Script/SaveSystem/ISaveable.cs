/// <summary>
/// 存档接口：任何需要存档的脚本实现此接口
/// </summary>
public interface ISaveable
{
    /// <summary>返回当前状态（会被 SaveManager 序列化为 JSON）</summary>
    object CaptureState();

    /// <summary>从保存的状态中恢复</summary>
    void RestoreState(object state);
}
