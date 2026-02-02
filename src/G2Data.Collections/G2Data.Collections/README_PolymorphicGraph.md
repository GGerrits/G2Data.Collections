# Polymorphic Graph Library

A flexible, generic graph data structure implementation in C# with support for custom node types, multiple traversal strategies, and cycle detection.

## Features

- **Generic Node IDs**: Support for any type that implements `IEquatable<T>`
- **Polymorphic Nodes**: Extend `GraphNode<TNodeId>` to create custom node types
- **Multiple Traversal Strategies**: Built-in DFS and BFS traversal using the Strategy pattern
- **Cycle Detection**: Both preventive (during edge addition) and detective (graph analysis)
- **Memory Efficient**: Iterative implementations avoid stack overflow on large graphs

## Quick Start

### Basic Usage

```csharp
// Create a graph with integer node IDs
var graph = new PolymorphicGraph<int>();

// Define custom node types
public class PersonNode : GraphNode<int>
{
    public string Name { get; set; }
    public PersonNode(int id, string name) : base(id)
    {
        Name = name;
    }
}

// Add nodes to the graph
var alice = new PersonNode(1, "Alice");
var bob = new PersonNode(2, "Bob");
var charlie = new PersonNode(3, "Charlie");

graph.AddNode(alice);
graph.AddNode(bob);
graph.AddNode(charlie);

// Add edges (connections)
graph.AddEdge(1, 2);  // Alice -> Bob
graph.AddEdge(2, 3);  // Bob -> Charlie
```

### Safe Edge Addition with Cycle Detection

```csharp
// Prevent cycles when adding edges
bool success = graph.AddEdgeSafe(3, 1, allowCycles: false);
// Output: "Warning: Adding edge 3 -> 1 would create a cycle. Edge not added."
// Returns: false

// Allow cycles if needed
graph.AddEdgeSafe(3, 1, allowCycles: true);
// Output: "Successfully added edge: 3 -> 1"
// Returns: true
```

### Graph Traversal

```csharp
// Depth-First Search
var dfsTraversal = new DFSTraversal();
foreach (var node in graph.TraverseGraph(1, dfsTraversal))
{
    Console.WriteLine($"Visited node: {node.Id}");
}

// Breadth-First Search
var bfsTraversal = new BFSTraversal();
foreach (var node in graph.TraverseGraph(1, bfsTraversal))
{
    Console.WriteLine($"Visited node: {node.Id}");
}
```

### Cycle Detection

```csharp
// Check if the entire graph contains cycles
bool hasCycles = graph.HasCycles();
Console.WriteLine($"Graph has cycles: {hasCycles}");
```

## API Reference

### `PolymorphicGraph<TNodeId>`

The main graph container class.

#### Methods

| Method | Description |
|--------|-------------|
| `AddNode(GraphNode<TNodeId> node)` | Adds a node to the graph (no duplicates) |
| `AddEdge(TNodeId fromId, TNodeId toId)` | Adds a directed edge between two nodes |
| `AddEdgeSafe(TNodeId fromId, TNodeId toId, bool allowCycles)` | Safely adds an edge with validation and optional cycle prevention |
| `HasCycles()` | Checks if the graph contains any cycles |
| `GetNode(TNodeId id)` | Retrieves a node by its ID (returns null if not found) |
| `GetAllNodes()` | Returns all nodes in the graph |
| `TraverseGraph(TNodeId startId, GraphTraversal strategy)` | Traverses the graph using the specified strategy |

### `GraphNode<TNodeId>`

Abstract base class for creating custom node types.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `TNodeId` | Unique identifier for the node |
| `Connections` | `List<GraphNode<TNodeId>>` | List of connected nodes |

#### Methods

| Method | Description |
|--------|-------------|
| `AddConnection(GraphNode<TNodeId> node)` | Adds a connection to another node |

### Traversal Strategies

#### `DFSTraversal`

Implements depth-first search traversal.

```csharp
var dfs = new DFSTraversal();
var nodes = graph.TraverseGraph(startNodeId, dfs);
```

#### `BFSTraversal`

Implements breadth-first search traversal.

```csharp
var bfs = new BFSTraversal();
var nodes = graph.TraverseGraph(startNodeId, bfs);
```

## Advanced Examples

### Custom Node Type with Business Logic

```csharp
public class TaskNode : GraphNode<string>
{
    public string Description { get; set; }
    public int Priority { get; set; }
    public bool IsCompleted { get; set; }

    public TaskNode(string id, string description, int priority) : base(id)
    {
        Description = description;
        Priority = priority;
        IsCompleted = false;
    }

    public void Complete()
    {
        IsCompleted = true;
        Console.WriteLine($"Task '{Description}' completed!");
    }
}

// Usage
var graph = new PolymorphicGraph<string>();
var task1 = new TaskNode("T1", "Design system", 1);
var task2 = new TaskNode("T2", "Implement features", 2);
var task3 = new TaskNode("T3", "Write tests", 3);

graph.AddNode(task1);
graph.AddNode(task2);
graph.AddNode(task3);

graph.AddEdge("T1", "T2");  // Design must happen before implementation
graph.AddEdge("T2", "T3");  // Implementation before testing
```

### Custom Traversal Strategy

```csharp
public class PriorityTraversal : GraphTraversal
{
    public override IEnumerable<GraphNode<TNodeId>> Traverse<TNodeId>(GraphNode<TNodeId> startNode)
    {
        // Implement custom traversal logic
        // For example: priority-based traversal for TaskNodes
        // ...
    }
}
```

### Finding All Paths

```csharp
public IEnumerable<List<GraphNode<TNodeId>>> FindAllPaths(TNodeId startId, TNodeId endId)
{
    var startNode = GetNode(startId);
    var endNode = GetNode(endId);
    
    if (startNode == null || endNode == null)
        yield break;
    
    // Implement path-finding logic using DFS with backtracking
    // ...
}
```

## Design Patterns Used

- **Strategy Pattern**: Pluggable traversal algorithms (`GraphTraversal` and its implementations)
- **Template Method**: Abstract `GraphNode` allows subclasses to define custom behavior
- **Iterator Pattern**: Traversal methods use `yield return` for lazy evaluation

## Known Limitations

1. **Node ID Mutability**: The `Id` property has a setter, which could cause issues if modified after adding to the graph
2. **Reference Equality**: Connection checking uses reference equality, not ID-based equality
3. **No Thread Safety**: Not safe for concurrent access without external synchronization
4. **No Edge Removal**: Currently no method to remove edges or nodes
5. **Memory Usage**: Stores full node references in connections, which may be memory-intensive for large graphs

## Best Practices

1. **Don't modify node IDs** after adding them to the graph
2. **Use `AddEdgeSafe`** when you need validation and cycle prevention
3. **Choose appropriate node ID types**: Use immutable types like `int`, `string`, or `Guid`
4. **Validate input**: Always check return values from `AddEdgeSafe` and null checks from `GetNode`
5. **Consider graph size**: For very large graphs (millions of nodes), consider specialized graph databases

## Performance Characteristics

| Operation | Time Complexity | Space Complexity |
|-----------|----------------|------------------|
| Add Node | O(1) average | O(1) |
| Add Edge | O(1) average | O(1) |
| Find Node | O(1) average | - |
| DFS Traversal | O(V + E) | O(V) |
| BFS Traversal | O(V + E) | O(V) |
| Cycle Detection | O(V + E) | O(V) |

*V = number of vertices (nodes), E = number of edges*

## Contributing

When extending this library:

1. Ensure all node ID types implement `IEquatable<T>`
2. Maintain the iterative (non-recursive) approach for traversals
3. Add appropriate null checks and validation
4. Consider cycle implications when adding new edge operations
5. Write unit tests for new functionality

## License

This code is provided as-is for educational and commercial use.
