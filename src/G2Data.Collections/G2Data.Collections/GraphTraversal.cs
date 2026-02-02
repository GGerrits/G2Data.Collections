namespace G2Data.Collections;

public abstract class GraphTraversal
{
    public abstract IEnumerable<GraphNode<TNodeId>> Traverse<TNodeId>(GraphNode<TNodeId> startNode)
        where TNodeId : IEquatable<TNodeId>;
}
