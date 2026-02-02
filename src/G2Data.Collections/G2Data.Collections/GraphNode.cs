namespace G2Data.Collections;

public abstract class GraphNode<TNodeId>(TNodeId id)
{
    public TNodeId Id { get; set; } = id;

    public List<GraphNode<TNodeId>> Connections { get; set; } = [];

    public void AddConnection(GraphNode<TNodeId> node)
    {
        if (!Connections.Contains(node))
        {
            Connections.Add(node);
        }
    }
    internal bool WouldCreateCycle(GraphNode<TNodeId> targetNode)
    {
        if (targetNode == null || Equals(targetNode.Id, Id))
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

            foreach (var connection in current.Connections)
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
