using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RedSaw.MissionSystem;

public class MissionRequireCoin : MissionRequire<MissionMessage>
{
    [SerializeField] private GameEventType type;
    [SerializeField] private string args;
    [SerializeField] private int count;

    public MissionRequireCoin(int count)
    {
        this.count = count;
    }

    public class Handle : MissionRequireHandle<MissionMessage>, IMissionProgressHandle
    {
        private readonly MissionRequireCoin require;
        private int count;

        public Handle(MissionRequireCoin require) : base(require)
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
            count += message.amount;
            if (count >= require.count)
            {
                Debug.Log("任务完成, 达到任务目标: " + require.count);
                return true;
            }
            Debug.LogFormat("任务更新, {0}/{1}", count, require.count);
            return false;
        }
    }

    public override bool CheckMessage(MissionMessage message)
    {
        if (message.type == MissionEventType.Coin)
        {
            return true;
        }
        return false;
    }

}
