using RedSaw.MissionSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionRequireHealth : MissionRequire<MissionMessage>
{
    [SerializeField] private GameEventType type;
    [SerializeField] private string args;
    [SerializeField] private int count;

    public MissionRequireHealth(int count)
    {
        this.count = count;
    }

    public class Handle : MissionRequireHandle<MissionMessage>, IMissionProgressHandle
    {
        private readonly MissionRequireHealth require;
        private int count;

        public Handle(MissionRequireHealth require) : base(require)
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
            count = message.amount;
            return count >= require.count;
        }
    }

    public override bool CheckMessage(MissionMessage message)
    {
        if (message.type == MissionEventType.Health)
        {
            return true;
        }
        return false;
    }
}
