using RedSaw.MissionSystem;
using UnityEngine;

public class MissionRequireHomePurchase : MissionRequire<MissionMessage>
{
    private readonly int _homeId;
    private readonly int _targetCount;

    public MissionRequireHomePurchase(int homeId, int targetCount)
    {
        _homeId = homeId;
        _targetCount = targetCount;
    }

    public class Handle : MissionRequireHandle<MissionMessage>, IMissionProgressHandle
    {
        private readonly MissionRequireHomePurchase _require;
        private int _currentCount;

        public Handle(MissionRequireHomePurchase require) : base(require)
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
        return message.type == MissionEventType.HomePurchase &&
            int.TryParse(message.args, out int homeId) &&
            homeId == _homeId;
    }
}
