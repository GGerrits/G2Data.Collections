namespace G2Data.Collections;

/// <summary>
/// Depth-First Search traversal
/// </summary>
public class DFSTraversal : GraphTraversal
{
    public override IEnumerable<GraphNode<TNodeId>> Traverse<TNodeId>(GraphNode<TNodeId> startNode)
    {
        ArgumentNullException.ThrowIfNull(startNode);

        var visited = new HashSet<TNodeId>();
        var stack = new Stack<GraphNode<TNodeId>>();

        stack.Push(startNode);

        while (stack.Count > 0)
        {
            var node = stack.Pop();

            if (visited.Contains(node.Id))
                continue;

            visited.Add(node.Id);

            yield return node;

            var connections = node.GetConnections().ToList();
            for (int i = connections.Count - 1; i >= 0; i--)
            {
                if (!visited.Contains(connections[i].Id))
                {
                    stack.Push(connections[i]);
                }
            }
        }
    }
}

