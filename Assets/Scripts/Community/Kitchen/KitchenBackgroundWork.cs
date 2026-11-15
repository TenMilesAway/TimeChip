using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>厨房砧板/灶台状态与后台切菜、烹饪进度（关闭 KitchenView 后仍继续）。</summary>
public static class KitchenBackgroundWork
{
    public const float CutDurationSeconds = 10f;
    public const float CookDurationSeconds = 20f;
    public const int HearthMaterialSlotCount = 4;

    public static event Action WorkstationChanged;

    public static KitchenWorkstationState Workstation { get; } = new KitchenWorkstationState();

    public static bool IsCuttingInProgress => Runner != null && Runner.IsCuttingInProgress;

    public static bool IsCookingInProgress => Runner != null && Runner.IsCookingInProgress;

    public static float CuttingElapsedSeconds => Runner != null ? Runner.CuttingElapsedSeconds : 0f;

    public static float CookingElapsedSeconds => Runner != null ? Runner.CookingElapsedSeconds : 0f;

    private static KitchenBackgroundWorkRunner Runner => KitchenBackgroundWorkRunner.EnsureInstance();

    public static void NotifyChanged()
    {
        WorkstationChanged?.Invoke();
    }

    public static bool TryStartCutting(cfg.Kitchen recipe)
    {
        if (recipe == null || IsCuttingInProgress)
        {
            return false;
        }

        if (Workstation.CutMatLeftItemId <= 0 || Workstation.CutMatRightItemId > 0)
        {
            return false;
        }

        Runner.StartCutting(recipe);
        return true;
    }

    public static bool TryStartCooking(cfg.Kitchen recipe, List<int> cookMaterialInputs)
    {
        if (recipe == null || cookMaterialInputs == null || IsCookingInProgress)
        {
            return false;
        }

        if (Workstation.HearthDishItemId > 0)
        {
            return false;
        }

        Runner.StartCooking(recipe, cookMaterialInputs);
        return true;
    }

    public static void AbortCuttingForKitchenClose()
    {
        if (!IsCuttingInProgress)
        {
            return;
        }

        Runner.StopCutting();
    }

    public static void AbortCookingForKitchenClose()
    {
        if (!IsCookingInProgress)
        {
            return;
        }

        Runner.StopCooking();
    }

    internal static void RaiseChanged()
    {
        WorkstationChanged?.Invoke();
    }
}

[Serializable]
public sealed class KitchenWorkstationState
{
    public int CutMatLeftItemId;
    public int CutMatRightItemId;
    public readonly int[] HearthMatSlotItemIds = new int[KitchenBackgroundWork.HearthMaterialSlotCount];
    public int HearthDishItemId;
}

public sealed class KitchenBackgroundWorkRunner : MonoBehaviour
{
    private static KitchenBackgroundWorkRunner _instance;

    private Coroutine _cutCoroutine;
    private Coroutine _cookCoroutine;
    private Coroutine _dishSpoilCoroutine;

    public bool IsCuttingInProgress { get; private set; }
    public bool IsCookingInProgress { get; private set; }
    public float CuttingElapsedSeconds { get; private set; }
    public float CookingElapsedSeconds { get; private set; }

    public static KitchenBackgroundWorkRunner EnsureInstance()
    {
        if (_instance != null)
        {
            return _instance;
        }

        GameObject host = new GameObject(nameof(KitchenBackgroundWorkRunner));
        DontDestroyOnLoad(host);
        _instance = host.AddComponent<KitchenBackgroundWorkRunner>();
        return _instance;
    }

    public void StartCutting(cfg.Kitchen recipe)
    {
        StopCutting();
        CuttingElapsedSeconds = 0f;
        _cutCoroutine = StartCoroutine(RunCuttingRoutine(recipe));
    }

    public void StartCooking(cfg.Kitchen recipe, List<int> cookMaterialInputs)
    {
        StopCooking();
        CookingElapsedSeconds = 0f;
        _cookCoroutine = StartCoroutine(RunCookingRoutine(recipe, cookMaterialInputs));
    }

    public void StopCutting()
    {
        if (_cutCoroutine != null)
        {
            StopCoroutine(_cutCoroutine);
            _cutCoroutine = null;
        }

        IsCuttingInProgress = false;
        CuttingElapsedSeconds = 0f;
    }

    public void StopCooking()
    {
        if (_cookCoroutine != null)
        {
            StopCoroutine(_cookCoroutine);
            _cookCoroutine = null;
        }

        IsCookingInProgress = false;
        CookingElapsedSeconds = 0f;
    }

    private IEnumerator RunCuttingRoutine(cfg.Kitchen recipe)
    {
        IsCuttingInProgress = true;
        KitchenBackgroundWork.RaiseChanged();

        while (CuttingElapsedSeconds < KitchenBackgroundWork.CutDurationSeconds)
        {
            CuttingElapsedSeconds += Time.deltaTime;
            KitchenBackgroundWork.RaiseChanged();
            yield return null;
        }

        KitchenWorkstationState state = KitchenBackgroundWork.Workstation;
        int inputItemId = state.CutMatLeftItemId;
        List<int> inputs = new List<int>(1) { inputItemId };
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        int outputItemId = KitchenRecipeResolver.ResolveOutputItemId(
            recipe,
            inputs,
            $"Kitchen.Cut.{playerInfoManager.CurrentAge}.{playerInfoManager.CurrentMonth}.{inputItemId}");

        state.CutMatLeftItemId = 0;
        state.CutMatRightItemId = outputItemId;
        IsCuttingInProgress = false;
        CuttingElapsedSeconds = 0f;
        _cutCoroutine = null;
        KitchenBackgroundWork.RaiseChanged();
        GameManager.Audio.Play(AudioDefine.SFXBuy);
    }

    private IEnumerator RunCookingRoutine(cfg.Kitchen recipe, List<int> cookMaterialInputs)
    {
        IsCookingInProgress = true;
        KitchenBackgroundWork.RaiseChanged();

        while (CookingElapsedSeconds < KitchenBackgroundWork.CookDurationSeconds)
        {
            CookingElapsedSeconds += Time.deltaTime;
            KitchenBackgroundWork.RaiseChanged();
            yield return null;
        }

        KitchenWorkstationState state = KitchenBackgroundWork.Workstation;
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        int outputItemId = KitchenRecipeResolver.ResolveOutputItemId(
            recipe,
            cookMaterialInputs,
            $"Kitchen.Cook.{playerInfoManager.CurrentAge}.{playerInfoManager.CurrentMonth}.{recipe.Id}");

        for (int i = 0; i < state.HearthMatSlotItemIds.Length; i++)
        {
            state.HearthMatSlotItemIds[i] = 0;
        }

        state.HearthDishItemId = outputItemId;
        IsCookingInProgress = false;
        CookingElapsedSeconds = 0f;
        _cookCoroutine = null;
        KitchenBackgroundWork.RaiseChanged();
        GameManager.Audio.Play(AudioDefine.SFXBuy);
        StartDishSpoilTimerIfNeeded();
    }

    private void StartDishSpoilTimerIfNeeded()
    {
        StopDishSpoilTimer();
        KitchenWorkstationState state = KitchenBackgroundWork.Workstation;
        if (!KitchenDishFreshnessRules.CanSpoilOnHearth(state.HearthDishItemId))
        {
            return;
        }

        _dishSpoilCoroutine = StartCoroutine(RunDishSpoilRoutine());
    }

    private void StopDishSpoilTimer()
    {
        if (_dishSpoilCoroutine != null)
        {
            StopCoroutine(_dishSpoilCoroutine);
            _dishSpoilCoroutine = null;
        }
    }

    private IEnumerator RunDishSpoilRoutine()
    {
        float elapsed = 0f;
        while (elapsed < KitchenDishFreshnessRules.SpoilDelaySeconds)
        {
            KitchenWorkstationState state = KitchenBackgroundWork.Workstation;
            int dishId = state.HearthDishItemId;
            if (dishId <= 0 || !KitchenDishFreshnessRules.CanSpoilOnHearth(dishId))
            {
                _dishSpoilCoroutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        KitchenWorkstationState ws = KitchenBackgroundWork.Workstation;
        if (KitchenDishFreshnessRules.CanSpoilOnHearth(ws.HearthDishItemId))
        {
            ws.HearthDishItemId = KitchenDishFreshnessRules.StaleDishItemId;
            KitchenBackgroundWork.RaiseChanged();
        }

        _dishSpoilCoroutine = null;
    }
}