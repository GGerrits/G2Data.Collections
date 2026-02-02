namespace G2Data.Collections;

public class PolymorphicGraph<TNodeId>
    where TNodeId : IEquatable<TNodeId>
{
    private readonly Dictionary<TNodeId, GraphNode<TNodeId>> nodes = [];

    public void AddNode(GraphNode<TNodeId> node)
    {
        if (!nodes.ContainsKey(node.Id))
        {
            nodes[node.Id] = node;
        }
    }

    public void AddEdge(TNodeId fromId, TNodeId toId)
    {
        if (nodes.TryGetValue(fromId, out GraphNode<TNodeId>? fromNode) 
            && nodes.TryGetValue(toId, out GraphNode<TNodeId>? toNode))
        {
            fromNode.AddConnection(toNode);
        }
    }

    public bool AddEdgeSafe(TNodeId fromId, TNodeId toId, bool allowCycles = false)
    {
        if (!nodes.TryGetValue(fromId, out GraphNode<TNodeId>? fromNode))
        {
            Console.WriteLine($"Error: Node {fromId} not found");
            return false;
        }

        if (!nodes.TryGetValue(toId, out GraphNode<TNodeId>? toNode))
        {
            Console.WriteLine($"Error: Node {toId} not found");
            return false;
        }

        if (!allowCycles && fromNode.WouldCreateCycle(toNode))
        {
            Console.WriteLine($"Warning: Adding edge {fromId} -> {toId} would create a cycle. Edge not added.");
            return false;
        }

        fromNode.AddConnection(toNode);
        Console.WriteLine($"Successfully added edge: {fromId} -> {toId}");
        return true;
    }

    public bool HasCycles()
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

            foreach (var connection in currentNode.Connections)
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
        if (nodes.TryGetValue(id, out GraphNode<TNodeId>? node))
        {
            return node;
        }
        return null;
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
        if (!nodes.TryGetValue(startId, out GraphNode<TNodeId>? value))
        {
            Console.WriteLine($"Node {startId} not found!");
            yield break;
        }

        foreach(var node in strategy.Traverse(value))
        {
            yield return node;
        };
    }
}
