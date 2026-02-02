namespace G2Data.Collections;

/// <summary>
/// Breadth-First Search traversal
/// </summary>
public class BFSTraversal : GraphTraversal
{
    public override IEnumerable<GraphNode<TNodeId>> Traverse<TNodeId>(GraphNode<TNodeId> startNode)
    {
        var visited = new HashSet<TNodeId>();
        var queue = new Queue<GraphNode<TNodeId>>();

        queue.Enqueue(startNode);
        visited.Add(startNode.Id);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();

            yield return node;

            foreach (var connection in node.Connections)
            {
                if (!visited.Contains(connection.Id))
                {
                    visited.Add(connection.Id);
                    queue.Enqueue(connection);
                }
            }
        }
    }
}
