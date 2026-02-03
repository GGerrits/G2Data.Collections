# PolymorphicGraph

A high-performance, thread-safe polymorphic graph data structure for .NET with built-in cycle detection and flexible traversal strategies.

[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-blue)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

## 🌟 Features

- **Thread-Safe**: Full concurrent operation support using `ConcurrentDictionary` and fine-grained locking
- **Polymorphic Design**: Abstract base class allows custom node types with domain-specific data
- **Cycle Detection**: Built-in algorithms to detect and prevent cycles
- **Flexible Traversal**: Strategy pattern implementation with DFS and BFS out of the box
- **Type-Safe**: Generic implementation with `IEquatable<TNodeId>` constraint
- **High Performance**: O(1) node/edge operations using HashSet-based connections
- **Complete CRUD**: Full support for adding, removing, and querying nodes and edges
- **Read-Only Collections**: Immutable access to connections prevents unauthorized modifications

## 📋 Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
- [API Reference](#api-reference)
- [Usage Examples](#usage-examples)
- [Performance](#performance)
- [Thread Safety](#thread-safety)
- [Contributing](#contributing)
- [License](#license)

## 🚀 Installation

### Option 1: Add to your project

Copy the `PolymorphicGraph.cs` file to your project:

```bash
# Navigate to your project directory
cd YourProject

# Copy the file
cp path/to/PolymorphicGraph.cs ./Collections/
```

### Option 2: Include in your solution

Add the file to your `.csproj`:

```xml
<ItemGroup>
  <Compile Include="Collections\PolymorphicGraph.cs" />
</ItemGroup>
```

## 🎯 Quick Start

### 1. Define Your Node Type

```csharp
using G2Data.Collections;

// Create a custom node type
public class PersonNode : GraphNode<int>
{
    public string Name { get; set; }
    public int Age { get; set; }

    public PersonNode(int id, string name, int age) : base(id)
    {
        Name = name;
        Age = age;
    }
}
```

### 2. Create and Use a Graph

```csharp
// Create a graph
var socialNetwork = new PolymorphicGraph<int>();

// Add nodes
var alice = new PersonNode(1, "Alice", 30);
var bob = new PersonNode(2, "Bob", 25);
var charlie = new PersonNode(3, "Charlie", 35);

socialNetwork.AddNode(alice);
socialNetwork.AddNode(bob);
socialNetwork.AddNode(charlie);

// Create relationships (edges)
socialNetwork.AddEdge(1, 2); // Alice knows Bob
socialNetwork.AddEdge(2, 3); // Bob knows Charlie
socialNetwork.AddEdge(1, 3); // Alice knows Charlie

// Traverse the network
foreach (var person in socialNetwork.TraverseGraph(1, new BFSTraversal()))
{
    Console.WriteLine($"{person.Name} (Age: {person.Age})");
}
```

Output:
```
Alice (Age: 30)
Bob (Age: 25)
Charlie (Age: 35)
```

## 🧩 Core Concepts

### Graph Structure

The `PolymorphicGraph` represents a **directed graph** where:
- **Nodes** are identified by a unique ID of type `TNodeId`
- **Edges** connect nodes in a specific direction
- Each node can have multiple outgoing connections
- Cycles can be detected and prevented

### Node Types

All nodes must inherit from `GraphNode<TNodeId>`:

```csharp
public abstract class GraphNode<TNodeId>(TNodeId id)
    where TNodeId : IEquatable<TNodeId>
{
    public TNodeId Id { get; init; }
    public IReadOnlyCollection<GraphNode<TNodeId>> Connections { get; }
    // ...
}
```

### Traversal Strategies

Two traversal strategies are provided:

- **DFS (Depth-First Search)**: Explores as far as possible along each branch
- **BFS (Breadth-First Search)**: Explores all neighbors before moving to next level

You can implement custom strategies by extending `GraphTraversal`.

## 📚 API Reference

### PolymorphicGraph Methods

#### Node Operations

| Method | Description | Returns | Complexity |
|--------|-------------|---------|------------|
| `AddNode(GraphNode<TNodeId> node)` | Add a node to the graph | `void` | O(1) |
| `RemoveNode(TNodeId id)` | Remove a node and all its edges | `bool` | O(V) |
| `GetNode(TNodeId id)` | Retrieve a node by ID | `GraphNode<TNodeId>?` | O(1) |
| `ContainsNode(TNodeId id)` | Check if node exists | `bool` | O(1) |
| `GetAllNodes()` | Get all nodes in the graph | `IEnumerable<GraphNode<TNodeId>>` | O(V) |
| `Clear()` | Remove all nodes | `void` | O(1) |

#### Edge Operations

| Method | Description | Returns | Complexity |
|--------|-------------|---------|------------|
| `AddEdge(TNodeId from, TNodeId to)` | Create an edge between nodes | `void` | O(1) |
| `AddEdgeSafe(TNodeId from, TNodeId to, bool allowCycles)` | Add edge with cycle check | `bool` | O(V+E) |
| `RemoveEdge(TNodeId from, TNodeId to)` | Remove a specific edge | `bool` | O(1) |

#### Graph Analysis

| Method | Description | Returns | Complexity |
|--------|-------------|---------|------------|
| `HasCycles()` | Detect if graph contains cycles | `bool` | O(V+E) |
| `TraverseGraph(TNodeId startId, GraphTraversal strategy)` | Traverse graph with strategy | `IEnumerable<GraphNode<TNodeId>>` | O(V+E) |

#### Properties

| Property | Description | Type |
|----------|-------------|------|
| `NodeCount` | Number of nodes in the graph | `int` |

### GraphNode Methods

| Method | Description | Returns |
|--------|-------------|---------|
| `AddConnection(GraphNode<TNodeId> node)` | Add a connection to another node | `void` |

### Traversal Strategies

| Class | Description |
|-------|-------------|
| `DFSTraversal` | Depth-first search traversal |
| `BFSTraversal` | Breadth-first search traversal |

## 💡 Usage Examples

### Example 1: Task Dependency Graph

```csharp
public class TaskNode : GraphNode<string>
{
    public string Description { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
    public bool IsCompleted { get; set; }

    public TaskNode(string id, string description, TimeSpan duration) : base(id)
    {
        Description = description;
        EstimatedDuration = duration;
        IsCompleted = false;
    }
}

// Create a task dependency graph
var projectTasks = new PolymorphicGraph<string>();

var design = new TaskNode("design", "Design system", TimeSpan.FromHours(8));
var implement = new TaskNode("implement", "Implement features", TimeSpan.FromHours(40));
var test = new TaskNode("test", "Test system", TimeSpan.FromHours(16));
var deploy = new TaskNode("deploy", "Deploy to production", TimeSpan.FromHours(4));

projectTasks.AddNode(design);
projectTasks.AddNode(implement);
projectTasks.AddNode(test);
projectTasks.AddNode(deploy);

// Define dependencies
projectTasks.AddEdge("design", "implement");
projectTasks.AddEdge("implement", "test");
projectTasks.AddEdge("test", "deploy");

// Verify no circular dependencies
if (projectTasks.HasCycles())
{
    Console.WriteLine("Warning: Circular task dependencies detected!");
}

// Get tasks in execution order (BFS from start)
Console.WriteLine("Task execution order:");
foreach (var task in projectTasks.TraverseGraph("design", new BFSTraversal()))
{
    Console.WriteLine($"- {task.Description} ({task.EstimatedDuration.TotalHours}h)");
}
```

### Example 2: File System Dependencies

```csharp
public class FileNode : GraphNode<string>
{
    public string FilePath { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }

    public FileNode(string id, string path, long size) : base(id)
    {
        FilePath = path;
        Size = size;
        LastModified = DateTime.Now;
    }
}

var fileGraph = new PolymorphicGraph<string>();

// Add files
var main = new FileNode("main.cs", "/src/main.cs", 1024);
var utils = new FileNode("utils.cs", "/src/utils.cs", 512);
var config = new FileNode("config.json", "/config.json", 256);

fileGraph.AddNode(main);
fileGraph.AddNode(utils);
fileGraph.AddNode(config);

// Define dependencies
fileGraph.AddEdge("main.cs", "utils.cs");
fileGraph.AddEdge("main.cs", "config.json");

// Prevent circular dependencies
bool added = fileGraph.AddEdgeSafe("utils.cs", "main.cs", allowCycles: false);
if (!added)
{
    Console.WriteLine("Cannot add edge: would create circular dependency");
}
```

### Example 3: Social Network

```csharp
public class UserNode : GraphNode<Guid>
{
    public string Username { get; set; }
    public string Email { get; set; }
    public List<string> Interests { get; set; }

    public UserNode(Guid id, string username, string email) : base(id)
    {
        Username = username;
        Email = email;
        Interests = new List<string>();
    }
}

var network = new PolymorphicGraph<Guid>();

var user1 = new UserNode(Guid.NewGuid(), "alice", "alice@example.com");
var user2 = new UserNode(Guid.NewGuid(), "bob", "bob@example.com");
var user3 = new UserNode(Guid.NewGuid(), "charlie", "charlie@example.com");

network.AddNode(user1);
network.AddNode(user2);
network.AddNode(user3);

// Create friendships (directed)
network.AddEdge(user1.Id, user2.Id); // Alice follows Bob
network.AddEdge(user2.Id, user1.Id); // Bob follows Alice (mutual)
network.AddEdge(user1.Id, user3.Id); // Alice follows Charlie

// Find all users Alice follows (direct connections)
var aliceNode = network.GetNode(user1.Id);
if (aliceNode != null)
{
    Console.WriteLine($"{aliceNode.Username} follows:");
    foreach (var connection in aliceNode.Connections)
    {
        Console.WriteLine($"  - {connection.Username}");
    }
}

// Find all users in Alice's network (BFS traversal)
Console.WriteLine($"\n{user1.Username}'s extended network:");
foreach (var user in network.TraverseGraph(user1.Id, new BFSTraversal()))
{
    Console.WriteLine($"  - {user.Username}");
}
```

### Example 4: Custom Traversal Strategy

```csharp
// Implement a custom traversal that limits depth
public class LimitedDepthTraversal : GraphTraversal
{
    private readonly int maxDepth;

    public LimitedDepthTraversal(int maxDepth)
    {
        this.maxDepth = maxDepth;
    }

    public override IEnumerable<GraphNode<TNodeId>> Traverse<TNodeId>(GraphNode<TNodeId> startNode)
    {
        ArgumentNullException.ThrowIfNull(startNode);

        var visited = new HashSet<TNodeId>();
        var queue = new Queue<(GraphNode<TNodeId> node, int depth)>();

        queue.Enqueue((startNode, 0));
        visited.Add(startNode.Id);

        while (queue.Count > 0)
        {
            var (node, depth) = queue.Dequeue();

            yield return node;

            if (depth < maxDepth)
            {
                foreach (var connection in node.GetConnections())
                {
                    if (!visited.Contains(connection.Id))
                    {
                        visited.Add(connection.Id);
                        queue.Enqueue((connection, depth + 1));
                    }
                }
            }
        }
    }
}

// Usage
var graph = new PolymorphicGraph<int>();
// ... add nodes and edges ...

// Only traverse 2 levels deep
foreach (var node in graph.TraverseGraph(1, new LimitedDepthTraversal(2)))
{
    Console.WriteLine(node.Id);
}
```

### Example 5: Concurrent Operations

```csharp
var graph = new PolymorphicGraph<int>();

// Thread-safe concurrent node addition
var tasks = new List<Task>();
for (int i = 0; i < 1000; i++)
{
    int nodeId = i;
    tasks.Add(Task.Run(() =>
    {
        var node = new TaskNode($"task_{nodeId}", $"Task {nodeId}", TimeSpan.FromMinutes(nodeId));
        graph.AddNode(node);
    }));
}

await Task.WhenAll(tasks);

Console.WriteLine($"Successfully added {graph.NodeCount} nodes concurrently");

// Thread-safe concurrent edge addition
tasks.Clear();
for (int i = 0; i < 999; i++)
{
    int fromId = i;
    tasks.Add(Task.Run(() =>
    {
        graph.AddEdgeSafe($"task_{fromId}", $"task_{fromId + 1}");
    }));
}

await Task.WhenAll(tasks);
```

## ⚡ Performance

### Time Complexity

| Operation | Average Case | Worst Case |
|-----------|--------------|------------|
| Add Node | O(1) | O(1) |
| Remove Node | O(V) | O(V) |
| Add Edge | O(1) | O(1) |
| Add Edge Safe | O(V + E) | O(V + E) |
| Remove Edge | O(1) | O(1) |
| Get Node | O(1) | O(1) |
| Has Cycles | O(V + E) | O(V + E) |
| DFS/BFS Traversal | O(V + E) | O(V + E) |

*Where V = number of vertices (nodes), E = number of edges*

### Space Complexity

- **Graph Storage**: O(V + E)
- **Node Connections**: O(E) using HashSet
- **Traversal Operations**: O(V) for visited tracking

### Performance Optimizations

1. **HashSet for Connections**: O(1) add/remove/contains operations
2. **ConcurrentDictionary**: Thread-safe with minimal locking
3. **Lazy Enumeration**: Traversal methods use `yield return` for memory efficiency
4. **Fine-grained Locking**: Separate locks for graph and node operations

## 🔒 Thread Safety

The implementation is **fully thread-safe**:

### Safe Operations
- ✅ Concurrent node additions from multiple threads
- ✅ Concurrent edge additions/removals
- ✅ Concurrent graph queries (GetNode, ContainsNode, etc.)
- ✅ Concurrent traversals
- ✅ Mixed read/write operations

### Locking Strategy
- **Graph Level**: Uses `lockObject` for structural changes
- **Node Level**: Uses `connectionLock` for connection modifications
- **ConcurrentDictionary**: Provides lock-free reads for node lookups

### Example: Thread-Safe Usage
```csharp
var graph = new PolymorphicGraph<int>();

// Multiple threads can safely add nodes
Parallel.For(0, 100, i =>
{
    graph.AddNode(new MyNode(i, $"Node {i}"));
});

// Multiple threads can safely add edges
Parallel.For(0, 99, i =>
{
    graph.AddEdge(i, i + 1);
});

// Safe to query while modifications are happening
var nodeCount = graph.NodeCount; // Thread-safe
var hasNode = graph.ContainsNode(50); // Thread-safe
```

## 🧪 Testing

The project includes comprehensive unit tests covering:

- ✅ Basic CRUD operations
- ✅ Null argument validation
- ✅ Cycle detection algorithms
- ✅ Traversal strategies (DFS, BFS)
- ✅ Thread safety and concurrent operations
- ✅ Edge cases and error conditions
- ✅ Read-only collection enforcement
- ✅ Complex multi-operation scenarios

Run tests:
```bash
dotnet test
```

## 📖 Additional Documentation

- **[IMPROVEMENTS.md](IMPROVEMENTS.md)** - Detailed explanation of all improvements and design decisions
- **[PolymorphicGraphTests.cs](PolymorphicGraphTests.cs)** - Complete test suite with examples

## 🤝 Contributing

Contributions are welcome! Here's how you can help:

1. **Report Bugs**: Open an issue describing the bug and how to reproduce it
2. **Suggest Features**: Open an issue with your feature request
3. **Submit Pull Requests**: Fork the repo, make your changes, and submit a PR

### Development Guidelines
- Follow C# coding conventions
- Add unit tests for new features
- Update documentation as needed
- Ensure all tests pass before submitting

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with modern C# 12 and .NET 8 features
- Inspired by graph theory and data structure best practices
- Designed for real-world production use

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/polymorphic-graph/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/polymorphic-graph/discussions)

---

**Made with ❤️ for the .NET community**