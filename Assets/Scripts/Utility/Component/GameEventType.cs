/// <summary>
/// 该枚举类主要管理事件号
/// </summary>
public enum GameEventType
{
    /** 网络事件 **/
    PacketIdBegin = 0,

    /** 玩家输入 **/

    /** 业务事件 **/
    OneSecondEvent,             // 每秒触发事件
    PlayAudio,                  // 播放音频事件
}
