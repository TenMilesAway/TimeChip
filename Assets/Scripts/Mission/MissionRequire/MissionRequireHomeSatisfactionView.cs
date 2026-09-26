using RedSaw.MissionSystem;
using UnityEngine;

public class MissionRequireHomeSatisfactionView : MissionRequire<MissionMessage>
{
    private readonly int _targetCount;

    public MissionRequireHomeSatisfactionView(int targetCount)
    {
        _targetCount = targetCount;
    }

    public class Handle : MissionRequireHandle<MissionMessage>, IMissionProgressHandle
    {
        private readonly MissionRequireHomeSatisfactionView _require;
        private int _currentCount;

        public Handle(MissionRequireHomeSatisfactionView require) : base(require)
        {
            _require = require;
        }

        public int CurrentCount { get { return _currentCount; } }
        public int TargetCount { get { return _require._targetCount; } }

        public void RestoreProgress(int progress)
        {
            _currentCount = Mathf.Max(0, progress);
        }

        protected override bool UseMessage(MissionMessage message)
        {
            _currentCount += message.amount;
            return _currentCount >= _require._targetCount;
        }
    }

    public override bool CheckMessage(MissionMessage message)
    {
        return message.type == MissionEventType.HomeSatisfactionView;
    }
}
