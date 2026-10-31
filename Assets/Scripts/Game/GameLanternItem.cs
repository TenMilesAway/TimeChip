using System;
using UnityEngine;
using UnityEngine.UI;

public class GameLanternItem : MonoBehaviour
{
    [SerializeField] private Text _txtName;
    [SerializeField] private Button _btnLantern;
    [SerializeField] private GameObject _goName;
    [SerializeField] private GameObject _goPass;
    [SerializeField] private GameObject _goWrong;

    private int _slotIndex = -1;
    private Action<int> _onLanternClicked;

    private void Awake()
    {
        if (_btnLantern != null)
        {
            _btnLantern.onClick.AddListener(OnLanternButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (_btnLantern != null)
        {
            _btnLantern.onClick.RemoveListener(OnLanternButtonClicked);
        }
    }

    public void Configure(int slotIndex, Action<int> onLanternClicked)
    {
        _slotIndex = slotIndex;
        _onLanternClicked = onLanternClicked;
    }

    public void ApplyDisplay(string lanternName, AugustLanternRiddleAnswerState answerState)
    {
        if (_txtName != null)
        {
            _txtName.text = lanternName ?? string.Empty;
        }

        bool answered = answerState != AugustLanternRiddleAnswerState.Unanswered;
        if (_goName != null)
        {
            _goName.SetActive(!answered);
        }

        if (_goPass != null)
        {
            _goPass.SetActive(answerState == AugustLanternRiddleAnswerState.Correct);
        }

        if (_goWrong != null)
        {
            _goWrong.SetActive(answerState == AugustLanternRiddleAnswerState.Wrong);
        }

        if (_btnLantern != null)
        {
            _btnLantern.interactable = !answered;
        }
    }

    public void ShowAnswerResult(bool isCorrect)
    {
        if (_goName != null)
        {
            _goName.SetActive(false);
        }

        if (_goPass != null)
        {
            _goPass.SetActive(isCorrect);
        }

        if (_goWrong != null)
        {
            _goWrong.SetActive(!isCorrect);
        }

        if (_btnLantern != null)
        {
            _btnLantern.interactable = false;
        }
    }

    private void OnLanternButtonClicked()
    {
        if (_slotIndex < 0)
        {
            return;
        }

        _onLanternClicked?.Invoke(_slotIndex);
    }
}
