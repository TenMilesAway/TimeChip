using RedSaw.MissionSystem;
using UnityEngine;

/// <summary>要求玩家当前持有指定数量模拟币的任务条件。</summary>
public class MissionRequireSimulationCoinBalance : MissionRequire<MissionMessage>
{
    private readonly int count;

    public MissionRequireSimulationCoinBalance(int count)
    {
        this.count = count;
    }

    public class Handle : MissionRequireHandle<MissionMessage>, IMissionProgressHandle
    {
        private readonly MissionRequireSimulationCoinBalance require;
        private int count;

        public Handle(MissionRequireSimulationCoinBalance require) : base(require)
        {
            this.require = require;
        }

        public int CurrentCount { get { return count; } }
        public int TargetCount { get { return require.count; } }

        public void RestoreProgress(int progress)
        {
            count = Mathf.Max(0, progress);
        }

        protected override bool UseMessage(MissionMessage message)
        {
            count = Mathf.Max(0, message.amount);
            return count >= require.count;
        }
    }

    public override bool CheckMessage(MissionMessage message)
    {
        return message.type == MissionEventType.SimulationCoinBalance;
    }
}
