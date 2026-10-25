using System;
using System.Collections.Generic;

/// <summary>
/// grow2 天赋前置关系的有向无环图（DAG），用于校验配置与判断可解锁状态。
/// </summary>
public sealed class Grow2TalentGraph
{
    private readonly Dictionary<int, cfg.Grow2> _nodes = new Dictionary<int, cfg.Grow2>();
    private readonly Dictionary<int, List<int>> _children = new Dictionary<int, List<int>>();

    public static Grow2TalentGraph Build(cfg.Grow2Table table)
    {
        if (table == null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        Grow2TalentGraph graph = new Grow2TalentGraph();
        IReadOnlyList<cfg.Grow2> dataList = table.DataList;
        for (int index = 0; index < dataList.Count; index++)
        {
            cfg.Grow2 node = dataList[index];
            if (node.Id <= 0)
            {
                throw new InvalidOperationException($"grow2 存在非法 ID：{node.Id}");
            }

            if (!graph._nodes.TryAdd(node.Id, node))
            {
                throw new InvalidOperationException($"grow2 存在重复 ID：{node.Id}");
            }
        }

        for (int index = 0; index < dataList.Count; index++)
        {
            cfg.Grow2 node = dataList[index];
            int prerequisiteId = node.PrerequisiteId;
            if (prerequisiteId <= 0)
            {
                continue;
            }

            if (!graph._nodes.ContainsKey(prerequisiteId))
            {
                throw new InvalidOperationException(
                    $"天赋 {node.Id} 的前置 {prerequisiteId} 不存在");
            }

            if (!graph._children.TryGetValue(prerequisiteId, out List<int> childIds))
            {
                childIds = new List<int>();
                graph._children[prerequisiteId] = childIds;
            }

            childIds.Add(node.Id);
        }

        graph.EnsureAcyclic();
        return graph;
    }

    public bool TryGetNode(int grow2Id, out cfg.Grow2 node)
    {
        return _nodes.TryGetValue(grow2Id, out node);
    }

    /// <summary>枚举 DAG 边：前置天赋 → 当前天赋。</summary>
    public void CollectEdges(List<(int fromId, int toId)> edges)
    {
        if (edges == null)
        {
            throw new ArgumentNullException(nameof(edges));
        }

        edges.Clear();
        foreach (KeyValuePair<int, cfg.Grow2> pair in _nodes)
        {
            int prerequisiteId = pair.Value.PrerequisiteId;
            if (prerequisiteId <= 0)
            {
                continue;
            }

            edges.Add((prerequisiteId, pair.Key));
        }
    }

    public bool IsPrerequisiteSatisfied(int grow2Id, HashSet<int> unlockedIds)
    {
        if (!_nodes.TryGetValue(grow2Id, out cfg.Grow2 node))
        {
            return false;
        }

        if (node.PrerequisiteId <= 0)
        {
            return true;
        }

        return unlockedIds != null && unlockedIds.Contains(node.PrerequisiteId);
    }

    public bool CanUnlock(int grow2Id, HashSet<int> unlockedIds)
    {
        if (unlockedIds != null && unlockedIds.Contains(grow2Id))
        {
            return false;
        }

        return IsPrerequisiteSatisfied(grow2Id, unlockedIds);
    }

    private void EnsureAcyclic()
    {
        HashSet<int> visiting = new HashSet<int>();
        HashSet<int> visited = new HashSet<int>();
        foreach (KeyValuePair<int, cfg.Grow2> pair in _nodes)
        {
            if (visited.Contains(pair.Key))
            {
                continue;
            }

            VisitAcyclic(pair.Key, visiting, visited);
        }
    }

    private void VisitAcyclic(int nodeId, HashSet<int> visiting, HashSet<int> visited)
    {
        if (visited.Contains(nodeId))
        {
            return;
        }

        if (!visiting.Add(nodeId))
        {
            throw new InvalidOperationException($"grow2 天赋图存在环，节点：{nodeId}");
        }

        if (_nodes.TryGetValue(nodeId, out cfg.Grow2 node) && node.PrerequisiteId > 0)
        {
            VisitAcyclic(node.PrerequisiteId, visiting, visited);
        }

        visiting.Remove(nodeId);
        visited.Add(nodeId);
    }
}
