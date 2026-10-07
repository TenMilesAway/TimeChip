using RedSaw.MissionSystem;
using UnityEngine;

public class MissionRequireItem : MissionRequire<MissionMessage>
{
    private readonly int _itemId;
    private readonly int _targetCount;
    private readonly bool _trackItemAcquisition;

    public MissionRequireItem(
        int itemId,
        int targetCount,
        bool trackItemAcquisition = true)
    {
        _itemId = itemId;
        _targetCount = targetCount;
        _trackItemAcquisition = trackItemAcquisition;
    }

    public class Handle : MissionRequireHandle<MissionMessage>, IMissionProgressHandle
    {
        private readonly MissionRequireItem _require;
        private int _currentCount;

        public Handle(MissionRequireItem require) : base(require)
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
        return _trackItemAcquisition &&
            message.type == MissionEventType.Item &&
            int.TryParse(message.args, out int itemId) &&
            itemId == _itemId;
    }
}
