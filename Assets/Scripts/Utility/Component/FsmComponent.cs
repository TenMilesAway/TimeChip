using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 用于流程性的有限状态机 (例如: 地图切换), 区别于控制角色的有限状态机
/// </summary>
public class FsmComponent : BaseComponent
{
    private FsmStatemachine _statemachine = new FsmStatemachine();
    private string _currentFsmStateName;

    protected override void Awake()
    {
        base.Awake();

        string[] stateNames = GetStateNames(typeof(IFsmState));

        foreach (string stateName in stateNames)
        {
            Type type = Type.GetType(stateName);
            IFsmState state = Activator.CreateInstance(type) as IFsmState;
            _statemachine.CreateFsmState(stateName, state);
        }
    }

    private void Update()
    {
        _statemachine.OnUpdate();
    }

    #region 主要方法
    public void StartFsmState(string stateName)
    {
        _statemachine.StartFsmState(stateName);
        _currentFsmStateName = stateName;
    }

    public string GetCurrentFsmStateName()
    {
        return _currentFsmStateName;
    }
    #endregion

    #region 辅助方法
    private string[] GetStateNames(Type typeBase)
    {
        List<string> stateNames = new List<string>();

        Type[] types = Assembly.GetExecutingAssembly().GetTypes();

        foreach (Type type in types)
        {
            if (type.IsClass && !type.IsAbstract && typeBase.IsAssignableFrom(type))
            {
                stateNames.Add(type.FullName);
            }
        }

        return stateNames.ToArray();
    }
    #endregion
}

public class FsmStatemachine
{
    private readonly Dictionary<string, IFsmState> _fsmStates;
    private IFsmState _currentState = null;

    public FsmStatemachine()
    {
        _fsmStates = new Dictionary<string, IFsmState>();
    }

    public bool CreateFsmState(string stateName, IFsmState fsmState)
    {
        // 已创建
        if (_fsmStates.ContainsKey(stateName)) return false;

        _fsmStates.Add(stateName, fsmState);
        return true;
    }

    public bool StartFsmState(string stateName)
    {
        // 无该状态
        if (!_fsmStates.ContainsKey(stateName)) return false;

        // 切换状态
        if (_currentState != null) _currentState.OnLeave();

        _currentState = _fsmStates[stateName];
        _currentState.OnEnterAsync();
        return true;
    }

    public void OnUpdate()
    {
        if (_currentState != null) _currentState.OnUpdate();
    }
}

/// <summary>
/// 状态接口
/// </summary>
public interface IFsmState
{
    public void OnEnterAsync();

    public void OnUpdate();

    public void OnLeave();
}
