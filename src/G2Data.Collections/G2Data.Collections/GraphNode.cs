namespace G2Data.Collections;

public abstract class GraphNode<TNodeId>(TNodeId id)
    where TNodeId : IEquatable<TNodeId>
{
    private readonly HashSet<GraphNode<TNodeId>> connections = [];
    private readonly object connectionLock = new();

    public TNodeId Id { get; init; } = id;

    public IReadOnlyCollection<GraphNode<TNodeId>> Connections
    {
        get
        {
            lock (connectionLock)
            {
                return connections.ToList().AsReadOnly();
            }
        }
    }

    internal IEnumerable<GraphNode<TNodeId>> GetConnections()
    {
        lock (connectionLock)
        {
            return connections.ToList();
        }
    }

    public void AddConnection(GraphNode<TNodeId> node)
    {
        ArgumentNullException.ThrowIfNull(node);

        lock (connectionLock)
        {
            connections.Add(node);
        }
    }

    internal bool RemoveConnection(TNodeId nodeId)
    {
        ArgumentNullException.ThrowIfNull(nodeId);

        lock (connectionLock)
        {
            return connections.RemoveWhere(n => n.Id.Equals(nodeId)) > 0;
        }
    }

    internal bool WouldCreateCycle(GraphNode<TNodeId> targetNode)
    {
        ArgumentNullException.ThrowIfNull(targetNode);

        if (Equals(targetNode.Id, Id))
            return true;

        return HasPathTo(targetNode, this);
    }

    private static bool HasPathTo(GraphNode<TNodeId> from, GraphNode<TNodeId> to)
    {
        if (from == null)
        {
            return false;
        }

        if (Equals(from.Id, to.Id))
        {
            return true;
        }

        var visited = new HashSet<TNodeId>();
        var stack = new Stack<GraphNode<TNodeId>>();

        stack.Push(from);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (!visited.Add(current.Id))
                continue;

            if (Equals(current.Id, to.Id))
                return true;

            foreach (var connection in current.GetConnections())
            {
                if (!visited.Contains(connection.Id))
                {
                    stack.Push(connection);
                }
            }
        }

        return false;
    }
}