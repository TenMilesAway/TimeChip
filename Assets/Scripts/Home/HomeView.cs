using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class HomeFurnitureBinding
{
    [Tooltip("home.xlsx 中家具的 id")]
    public int homeId;

    [Tooltip("购买该家具后显示的场景对象")]
    public GameObject furniture;
}

public class HomeView : UIBasePanel
{
    [SerializeField] private HomeFurnitureBinding[] _furnitureBindings;

    private readonly Dictionary<int, GameObject> _furnitureByHomeId =
        new Dictionary<int, GameObject>();

    private void Awake()
    {
        BuildFurnitureLookup();
        PlayerInfoManager.GetInstance().PlayerInfoChanged += OnPlayerInfoChanged;
    }

    private void OnEnable()
    {
        RefreshFurnitureVisibility();
    }

    protected override void OnDestroy()
    {
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= OnPlayerInfoChanged;
        base.OnDestroy();
    }

    private void OnPlayerInfoChanged(PlayerInfoManager playerInfoManager)
    {
        RefreshFurnitureVisibility();
    }

    /// <summary>
    /// 按玩家已购买的 home.xlsx 家具 ID 同步场景家具的显示状态
    /// </summary>
    private void RefreshFurnitureVisibility()
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        foreach (KeyValuePair<int, GameObject> binding in _furnitureByHomeId)
        {
            binding.Value.SetActive(playerInfoManager.IsHomeUnlocked(binding.Key));
        }
    }

    /// <summary>
    /// 构建并校验 Inspector 中的家具 ID 与场景对象映射
    /// </summary>
    private void BuildFurnitureLookup()
    {
        _furnitureByHomeId.Clear();

        if (_furnitureBindings == null || _furnitureBindings.Length == 0)
        {
            Debug.LogError("HomeView 未配置家具绑定。请为 home.xlsx 中的每个家具 ID 配置场景对象。");
            return;
        }

        for (int i = 0; i < _furnitureBindings.Length; i++)
        {
            HomeFurnitureBinding binding = _furnitureBindings[i];
            if (binding == null || binding.homeId <= 0 || binding.furniture == null)
            {
                Debug.LogError($"HomeView 的第 {i + 1} 个家具绑定无效。");
                continue;
            }

            if (_furnitureByHomeId.ContainsKey(binding.homeId))
            {
                Debug.LogError($"HomeView 存在重复的家具 ID: {binding.homeId}。");
                continue;
            }

            _furnitureByHomeId.Add(binding.homeId, binding.furniture);
        }
    }

    public void OpenHomeDetailView()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        UIManager.GetInstance().OpenPanel(GlobalDefine.HomeDetailView);
    }

    public override string GetPanelName()
    {
        return GlobalDefine.HomeView;
    }
}
