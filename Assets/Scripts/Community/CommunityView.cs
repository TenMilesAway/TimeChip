using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommunityView : UIBasePanel
{
    private const float TrashCanSpawnChance = 0.1f;
    private const int MinimumTrashCanHealthCost = 3;
    private const int MaximumTrashCanHealthCost = 5;
    private static readonly int[] TrashCanLotteryPoolIds = { 5, 6, 7 };

    [SerializeField] private Button _btnWork;             // 零工中心
    [SerializeField] private Button _btnHomeStore;        // 家具店
    [SerializeField] private Button _btnConvenienceStore; // 便利店
    [SerializeField] private Button _btnCilinic;          // 医务室
    [SerializeField] private Button _btnCommunityCentre;  // 社区中心
    [SerializeField] private Button _btnSubway;           // 地铁

    [Space(10)]
    [SerializeField] private GameObject[] _goTrashCans;   // 垃圾桶点位

    private void Awake()
    {
        BindTrashCanButtons();

        if (_btnConvenienceStore == null)
        {
            Debug.LogError("CommunityView 未绑定便利店按钮", this);
            return;
        }

        _btnConvenienceStore.onClick.AddListener(OnClickConvenienceStore);
        if (_btnSubway == null)
        {
            Debug.LogError("CommunityView 未绑定地铁按钮", this);
            return;
        }

        _btnSubway.onClick.AddListener(OnClickSubway);
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);
    }

    protected override void ShowHandle()
    {
        base.ShowHandle();

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.TurnAdvanced -= RefreshTrashCanSpawns;
        playerInfoManager.TurnAdvanced += RefreshTrashCanSpawns;
        RefreshTrashCanSpawns();
    }

    protected override void HideHandle()
    {
        PlayerInfoManager.GetInstance().TurnAdvanced -= RefreshTrashCanSpawns;
        base.HideHandle();
    }

    protected override void CloseHandle()
    {
        base.CloseHandle();
    }

    protected override void OnDestroy()
    {
        PlayerInfoManager.GetInstance().TurnAdvanced -= RefreshTrashCanSpawns;

        if (_btnConvenienceStore != null)
        {
            _btnConvenienceStore.onClick.RemoveListener(OnClickConvenienceStore);
        }

        if (_btnSubway != null)
        {
            _btnSubway.onClick.RemoveListener(OnClickSubway);
        }

        base.OnDestroy();
    }

    public void OnClickWork()
    {
        UIManager.GetInstance().OpenPanel(GlobalDefine.WorkView);
    }

    public void OnClickHomeStore()
    {
        UIManager.GetInstance().OpenPanel(GlobalDefine.HomeStoreView);
    }

    public void OnClickConvenienceStore()
    {
        UIManager.GetInstance().OpenPanel(GlobalDefine.ConvenienceStoreView);
    }

    public void OnClickClinic()
    {
        UIManager.GetInstance().OpenPanel(GlobalDefine.ClinicView);
    }

    public void OnClickCommunityCentre()
    {
        UIManager.GetInstance().OpenPanel(GlobalDefine.CommunityCentreView);
    }

    public void OnClickSubway()
    {
        UIManager.GetInstance().ClosePanel(GetPanelName());
        UIManager.GetInstance().OpenPanel(GlobalDefine.SubwayView);
    }

    private void BindTrashCanButtons()
    {
        if (_goTrashCans == null || _goTrashCans.Length != 6)
        {
            Debug.LogError("CommunityView 必须配置 6 个垃圾桶点位。", this);
            return;
        }

        for (int pointIndex = 0; pointIndex < _goTrashCans.Length; pointIndex++)
        {
            GameObject point = _goTrashCans[pointIndex];
            if (point == null)
            {
                Debug.LogError($"CommunityView 的第 {pointIndex + 1} 个垃圾桶点位无效。", this);
                continue;
            }

            for (int childIndex = 0; childIndex < point.transform.childCount; childIndex++)
            {
                GameObject trashCan = point.transform.GetChild(childIndex).gameObject;
                Button button = trashCan.GetComponent<Button>();
                if (button == null)
                {
                    button = trashCan.AddComponent<Button>();
                    button.targetGraphic = trashCan.GetComponent<Graphic>();
                }

                int capturedPointIndex = pointIndex;
                int capturedChildIndex = childIndex;
                button.onClick.AddListener(() =>
                    TrySearchTrashCan(capturedPointIndex, capturedChildIndex, trashCan));
                trashCan.SetActive(false);
            }
        }
    }

    private void RefreshTrashCanSpawns()
    {
        if (_goTrashCans == null || _goTrashCans.Length != 6)
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (!playerInfoManager.TryGetCurrentTurnTrashCanChildIndices(
                _goTrashCans.Length,
                out List<int> childIndices))
        {
            childIndices = CreateTrashCanChildIndices();
            playerInfoManager.SetCurrentTurnTrashCanChildIndices(childIndices);
        }

        for (int pointIndex = 0; pointIndex < _goTrashCans.Length; pointIndex++)
        {
            SetTrashCanPointVisibility(_goTrashCans[pointIndex], childIndices[pointIndex]);
        }
    }

    private List<int> CreateTrashCanChildIndices()
    {
        List<int> childIndices = new List<int>(_goTrashCans.Length);
        for (int pointIndex = 0; pointIndex < _goTrashCans.Length; pointIndex++)
        {
            GameObject point = _goTrashCans[pointIndex];
            int childCount = point == null ? 0 : point.transform.childCount;
            bool shouldSpawn = childCount > 0 && UnityEngine.Random.value < TrashCanSpawnChance;
            childIndices.Add(shouldSpawn ? UnityEngine.Random.Range(0, childCount) : -1);
        }

        return childIndices;
    }

    private static void SetTrashCanPointVisibility(GameObject point, int visibleChildIndex)
    {
        if (point == null)
        {
            return;
        }

        for (int childIndex = 0; childIndex < point.transform.childCount; childIndex++)
        {
            point.transform.GetChild(childIndex).gameObject.SetActive(childIndex == visibleChildIndex);
        }
    }

    private void TrySearchTrashCan(int pointIndex, int childIndex, GameObject trashCan)
    {
        int healthCost = UnityEngine.Random.Range(
            MinimumTrashCanHealthCost,
            MaximumTrashCanHealthCost + 1);
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (playerInfoManager.Health < healthCost)
        {
            CommonTipView.Show($"健康值不足，需要至少 {healthCost} 点健康值");
            return;
        }

        int poolId = TrashCanLotteryPoolIds[
            UnityEngine.Random.Range(0, TrashCanLotteryPoolIds.Length)];
        if (!LotteryView.TryDrawReward(poolId, out CommonRewardItemData reward))
        {
            Debug.LogError($"垃圾桶奖池配置无效: [{poolId}]", this);
            CommonTipView.Show("垃圾桶里什么也没有");
            return;
        }

        if (!playerInfoManager.TryConsumeCurrentTurnTrashCan(pointIndex, childIndex))
        {
            return;
        }

        playerInfoManager.ChangeHealth(-healthCost);
        trashCan.SetActive(false);
        LotteryView.GrantAndPresentReward(reward);
    }

    public bool TryGetWorkButton(out Button workButton)
    {
        if (_btnWork != null)
        {
            workButton = _btnWork;
            return true;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            for (int listenerIndex = 0; listenerIndex < button.onClick.GetPersistentEventCount(); listenerIndex++)
            {
                if (button.onClick.GetPersistentMethodName(listenerIndex) == nameof(OnClickWork))
                {
                    workButton = button;
                    return true;
                }
            }
        }

        workButton = null;
        Debug.LogError("CommunityView 未绑定零工中心按钮", this);
        return false;
    }

    public bool TryGetHomeStoreButton(out Button homeStoreButton)
    {
        if (_btnHomeStore != null && _btnHomeStore.gameObject.activeInHierarchy)
        {
            homeStoreButton = _btnHomeStore;
            return true;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            for (int listenerIndex = 0; listenerIndex < button.onClick.GetPersistentEventCount(); listenerIndex++)
            {
                if (button.onClick.GetPersistentMethodName(listenerIndex) == nameof(OnClickHomeStore))
                {
                    homeStoreButton = button;
                    return true;
                }
            }
        }

        homeStoreButton = null;
        Debug.LogError("CommunityView 未绑定家具店按钮", this);
        return false;
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommunityView;
    }
}
