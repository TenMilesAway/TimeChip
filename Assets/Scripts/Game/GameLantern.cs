using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameLantern : UIBasePanel
{
    [SerializeField] private Text _txtQuestTitle;
    [SerializeField] private Text _txtAnswerA;
    [SerializeField] private Text _txtAnswerB;
    [SerializeField] private Text _txtAnswerC;
    [SerializeField] private Text _txtAnswerD;
    [SerializeField] private Button _btnAnswerA;
    [SerializeField] private Button _btnAnswerB;
    [SerializeField] private Button _btnAnswerC;
    [SerializeField] private Button _btnAnswerD;
    [SerializeField] private Button _btnClose;
    [SerializeField] private GameObject _goQuestMain;

    [SerializeField] private GameLanternItem[] _lanterns;

    private int _activeSlotIndex = -1;
    private int _correctAnswerButtonIndex = -1;
    private bool _isAwaitingAnswer;

    private readonly struct AnswerButtonBinding
    {
        public AnswerButtonBinding(Button button, Text label)
        {
            Button = button;
            Label = label;
        }

        public Button Button { get; }
        public Text Label { get; }
    }

    private AnswerButtonBinding[] _answerBindings;

    private void Awake()
    {
        _answerBindings = new[]
        {
            new AnswerButtonBinding(_btnAnswerA, _txtAnswerA),
            new AnswerButtonBinding(_btnAnswerB, _txtAnswerB),
            new AnswerButtonBinding(_btnAnswerC, _txtAnswerC),
            new AnswerButtonBinding(_btnAnswerD, _txtAnswerD),
        };

        if (_btnAnswerA != null)
        {
            _btnAnswerA.onClick.AddListener(() => OnAnswerButtonClicked(0));
        }

        if (_btnAnswerB != null)
        {
            _btnAnswerB.onClick.AddListener(() => OnAnswerButtonClicked(1));
        }

        if (_btnAnswerC != null)
        {
            _btnAnswerC.onClick.AddListener(() => OnAnswerButtonClicked(2));
        }

        if (_btnAnswerD != null)
        {
            _btnAnswerD.onClick.AddListener(() => OnAnswerButtonClicked(3));
        }

        if (_btnClose != null)
        {
            _btnClose.onClick.AddListener(OnCloseButtonClicked);
        }

        if (_lanterns != null)
        {
            for (int i = 0; i < _lanterns.Length; i++)
            {
                GameLanternItem item = _lanterns[i];
                if (item == null)
                {
                    continue;
                }

                int slotIndex = i;
                item.Configure(slotIndex, OnLanternItemClicked);
            }
        }
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);
        PlayerInfoManager.GetInstance().EnsureAugustLanternRiddlesInitialized();
        ResetQuestPanel();
        RefreshLanternItems();
    }

    protected override void ShowHandle()
    {
        base.ShowHandle();
        ResetQuestPanel();
        RefreshLanternItems();
    }

    protected override void OnDestroy()
    {
        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveListener(OnCloseButtonClicked);
        }

        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.GameLantern;
    }

    private void ResetQuestPanel()
    {
        _activeSlotIndex = -1;
        _correctAnswerButtonIndex = -1;
        _isAwaitingAnswer = false;
        if (_goQuestMain != null)
        {
            _goQuestMain.SetActive(false);
        }

        SetAnswerButtonsInteractable(false);
    }

    private void RefreshLanternItems()
    {
        if (_lanterns == null)
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        for (int i = 0; i < _lanterns.Length && i < PlayerInfoManager.AugustLanternRiddleSlotCount; i++)
        {
            GameLanternItem item = _lanterns[i];
            if (item == null)
            {
                continue;
            }

            if (!playerInfoManager.TryGetAugustLanternRiddleIdAt(i, out int riddleId) ||
                tables?.LanternTable == null)
            {
                item.ApplyDisplay(string.Empty, AugustLanternRiddleAnswerState.Unanswered);
                continue;
            }

            cfg.Lantern riddle = tables.LanternTable.GetOrDefault(riddleId);
            if (riddle == null)
            {
                item.ApplyDisplay(string.Empty, AugustLanternRiddleAnswerState.Unanswered);
                continue;
            }

            AugustLanternRiddleAnswerState state =
                playerInfoManager.GetAugustLanternRiddleAnswerStateAt(i);
            item.ApplyDisplay(riddle.Name, state);
        }
    }

    private void OnLanternItemClicked(int slotIndex)
    {
        if (_isAwaitingAnswer && slotIndex == _activeSlotIndex)
        {
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (playerInfoManager.GetAugustLanternRiddleAnswerStateAt(slotIndex) !=
            AugustLanternRiddleAnswerState.Unanswered)
        {
            return;
        }

        if (!playerInfoManager.TryGetAugustLanternRiddleIdAt(slotIndex, out int riddleId))
        {
            return;
        }

        cfg.Lantern riddle = DataTableMananger.GetInstance().Tables.LanternTable.GetOrDefault(riddleId);
        if (riddle == null)
        {
            Debug.LogError($"灯谜配置不存在: [{riddleId}]", this);
            return;
        }

        UiButtonAudio.PlayClick();
        _activeSlotIndex = slotIndex;
        _isAwaitingAnswer = true;
        if (_txtQuestTitle != null)
        {
            _txtQuestTitle.text = riddle.Clue;
        }

        ApplyShuffledAnswers(riddle, slotIndex);
        if (_goQuestMain != null)
        {
            _goQuestMain.SetActive(true);
        }

        SetAnswerButtonsInteractable(true);
    }

    private void ApplyShuffledAnswers(cfg.Lantern riddle, int slotIndex)
    {
        string[] options =
        {
            riddle.Option1,
            riddle.Option2,
            riddle.Option3,
            riddle.Option4,
        };

        int correctOptionIndex = Mathf.Clamp(riddle.CorrectIndex, 1, options.Length) - 1;

        List<int> order = new List<int> { 0, 1, 2, 3 };
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        for (int i = order.Count - 1; i > 0; i--)
        {
            int swapIndex = TurnRandom.Range(
                $"AugustLantern.OptionShuffle.{playerInfoManager.CurrentAge}." +
                $"{playerInfoManager.CurrentMonth}.{slotIndex}.{i}",
                0,
                i + 1);
            int temp = order[i];
            order[i] = order[swapIndex];
            order[swapIndex] = temp;
        }

        _correctAnswerButtonIndex = -1;
        for (int buttonIndex = 0; buttonIndex < _answerBindings.Length; buttonIndex++)
        {
            int optionIndex = order[buttonIndex];
            if (_answerBindings[buttonIndex].Label != null)
            {
                _answerBindings[buttonIndex].Label.text = options[optionIndex];
            }

            if (optionIndex == correctOptionIndex)
            {
                _correctAnswerButtonIndex = buttonIndex;
            }
        }
    }

    private void OnAnswerButtonClicked(int buttonIndex)
    {
        if (!_isAwaitingAnswer || _activeSlotIndex < 0)
        {
            return;
        }

        UiButtonAudio.PlayClick();
        bool isCorrect = buttonIndex == _correctAnswerButtonIndex;
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (!playerInfoManager.TryRecordAugustLanternRiddleAnswer(_activeSlotIndex, isCorrect))
        {
            ResetQuestPanel();
            return;
        }

        if (isCorrect)
        {
            GameLanternRewardService.TryGrantCorrectAnswerSimulationCoins(_activeSlotIndex);
        }

        if (_goQuestMain != null)
        {
            _goQuestMain.SetActive(false);
        }

        SetAnswerButtonsInteractable(false);
        _isAwaitingAnswer = false;

        if (_lanterns != null &&
            _activeSlotIndex >= 0 &&
            _activeSlotIndex < _lanterns.Length &&
            _lanterns[_activeSlotIndex] != null)
        {
            _lanterns[_activeSlotIndex].ShowAnswerResult(isCorrect);
        }

        _activeSlotIndex = -1;
        _correctAnswerButtonIndex = -1;

        if (playerInfoManager.AreAllAugustLanternRiddlesAnswered())
        {
            GameLanternRewardService.TryGrantCompletionLotteryReward();
        }
    }

    private void SetAnswerButtonsInteractable(bool interactable)
    {
        if (_answerBindings == null)
        {
            return;
        }

        for (int i = 0; i < _answerBindings.Length; i++)
        {
            if (_answerBindings[i].Button != null)
            {
                _answerBindings[i].Button.interactable = interactable;
            }
        }
    }

    private void OnCloseButtonClicked()
    {
        UiButtonAudio.PlayClick();
        ClosePanel();
    }

    private void ClosePanel()
    {
        UIManager.GetInstance().ClosePanel(GetPanelName());
    }
}
