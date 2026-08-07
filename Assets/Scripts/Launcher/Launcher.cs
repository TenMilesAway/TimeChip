using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Launcher : MonoBehaviour
{
    /// <summary>
    /// 开始游戏状态机
    /// </summary>
    private enum State
    {
        None,

        InitBegin,
        InitIng,
        InitEnd,

        SwitchSceneBegin,
        SwitchSceneIng,
        SwitchSceneEnd,
    }
}
