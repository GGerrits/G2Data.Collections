using System.Collections.Concurrent;

namespace G2Data.Collections;

public class PolymorphicGraph<TNodeId>
    where TNodeId : IEquatable<TNodeId>
{
    private readonly ConcurrentDictionary<TNodeId, GraphNode<TNodeId>> nodes = new();
    private readonly object lockObject = new();

    public void AddNode(GraphNode<TNodeId> node)
    {
        ArgumentNullException.ThrowIfNull(node);
        nodes.TryAdd(node.Id, node);
    }

    public bool RemoveNode(TNodeId id)
    {
        ArgumentNullException.ThrowIfNull(id);

        lock (lockObject)
        {
            if (!nodes.TryRemove(id, out _))
            {
                return false;
            }

            // Remove all edges pointing to this node
            foreach (var node in nodes.Values)
            {
                node.RemoveConnection(id);
            }

            return true;
        }
    }

    public void AddEdge(TNodeId fromId, TNodeId toId)
    {
        ArgumentNullException.ThrowIfNull(fromId);
        ArgumentNullException.ThrowIfNull(toId);

        lock (lockObject)
        {
            if (nodes.TryGetValue(fromId, out GraphNode<TNodeId>? fromNode)
                && nodes.TryGetValue(toId, out GraphNode<TNodeId>? toNode))
            {
                fromNode.AddConnection(toNode);
            }
        }
    }

    public bool AddEdgeSafe(TNodeId fromId, TNodeId toId, bool allowCycles = false)
    {
        ArgumentNullException.ThrowIfNull(fromId);
        ArgumentNullException.ThrowIfNull(toId);

        lock (lockObject)
        {
            if (!nodes.TryGetValue(fromId, out GraphNode<TNodeId>? fromNode))
            {
                return false;
            }

            if (!nodes.TryGetValue(toId, out GraphNode<TNodeId>? toNode))
            {
                return false;
            }

            if (!allowCycles && fromNode.WouldCreateCycle(toNode))
            {
                return false;
            }

            fromNode.AddConnection(toNode);
            return true;
        }
    }

    public bool RemoveEdge(TNodeId fromId, TNodeId toId)
    {
        ArgumentNullException.ThrowIfNull(fromId);
        ArgumentNullException.ThrowIfNull(toId);

        lock (lockObject)
        {
            if (nodes.TryGetValue(fromId, out GraphNode<TNodeId>? fromNode))
            {
                return fromNode.RemoveConnection(toId);
            }

            return false;
        }
    }

    public void Clear()
    {
        lock (lockObject)
        {
            nodes.Clear();
        }
    }

    public int NodeCount => nodes.Count;

    public bool HasCycles()
    {
        lock (lockObject)
        {
            var visited = new HashSet<TNodeId>();
            var recursionStack = new HashSet<TNodeId>();

            foreach (var node in nodes.Values)
            {
                if (!visited.Contains(node.Id))
                {
                    if (HasCycleUtil(node, visited, recursionStack))
                        return true;
                }
            }

            return false;
        }
    }

    private static bool HasCycleUtil(GraphNode<TNodeId> startNode, HashSet<TNodeId> visited, HashSet<TNodeId> recursionStack)
    {
        var stack = new Stack<(GraphNode<TNodeId> node, bool isReturning)>();
        stack.Push((startNode, false));

        while (stack.Count > 0)
        {
            var (currentNode, isReturning) = stack.Pop();

            if (isReturning)
            {
                recursionStack.Remove(currentNode.Id);
                continue;
            }

            if (recursionStack.Contains(currentNode.Id))
                return true;

            if (visited.Contains(currentNode.Id))
                continue;

            visited.Add(currentNode.Id);
            recursionStack.Add(currentNode.Id);

            stack.Push((currentNode, true));

            foreach (var connection in currentNode.GetConnections())
            {
                if (!visited.Contains(connection.Id))
                {
                    stack.Push((connection, false));
                }
                else if (recursionStack.Contains(connection.Id))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public GraphNode<TNodeId>? GetNode(TNodeId id)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (nodes.TryGetValue(id, out GraphNode<TNodeId>? node))
        {
            return node;
        }
        return null;
    }

    public bool ContainsNode(TNodeId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return nodes.ContainsKey(id);
    }

    public IEnumerable<GraphNode<TNodeId>> GetAllNodes()
    {
        foreach (var node in nodes.Values)
        {
            yield return node;
        }
    }

    public IEnumerable<GraphNode<TNodeId>> TraverseGraph(TNodeId startId, GraphTraversal strategy)
    {
        ArgumentNullException.ThrowIfNull(startId);
        ArgumentNullException.ThrowIfNull(strategy);

        if (!nodes.TryGetValue(startId, out GraphNode<TNodeId>? value))
        {
            yield break;
        }

        foreach (var node in strategy.Traverse(value))
        {
            yield return node;
        }
    }
}