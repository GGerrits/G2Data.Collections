# PolymorphicGraphJsonConverter

A custom `System.Text.Json` converter for serializing and deserializing graphs with polymorphic node types. This converter enables you to work with graph structures where nodes can be of different types while maintaining type information during JSON serialization.

## Features

- ✅ **Polymorphic Node Support** - Serialize graphs with multiple node types
- ✅ **Type Discrimination** - Automatic type resolution using configurable discriminator properties
- ✅ **Separate Edge Management** - Edges are serialized independently from node data
- ✅ **Cycle Prevention** - Built-in support for detecting and preventing cycles
- ✅ **Fluent API** - Easy-to-use factory pattern for configuration
- ✅ **Generic Type Support** - Works with any `IEquatable<T>` node ID type

## Installation

Add the `PolymorphicGraph.cs` file to your project. The converter requires:
- .NET 6.0 or higher
- `System.Text.Json` namespace

## Quick Start

### 1. Define Your Node Types

```csharp
// Define concrete node types that inherit from GraphNode<TNodeId>
public class PersonNode : GraphNode<int>
{
    public PersonNode(int id) : base(id) { }
    
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class CompanyNode : GraphNode<int>
{
    public CompanyNode(int id) : base(id) { }
    
    public string CompanyName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
}
```

### 2. Create and Configure the Converter

```csharp
// Build the converter using the factory
var converter = new PolymorphicGraphJsonConverterFactory<int>()
    .RegisterNodeType<PersonNode>("person")
    .RegisterNodeType<CompanyNode>("company")
    .WithDiscriminatorProperty("$type")  // Optional: defaults to "$type"
    .Build();

// Add to JsonSerializerOptions
var options = new JsonSerializerOptions
{
    WriteIndented = true
};
options.Converters.Add(converter);
```

### 3. Build and Serialize Your Graph

```csharp
// Create a graph
var graph = new PolymorphicGraph<int>();

// Add nodes
var person1 = new PersonNode(1) { Name = "Alice", Age = 30 };
var person2 = new PersonNode(2) { Name = "Bob", Age = 25 };
var company = new CompanyNode(3) { CompanyName = "Tech Corp", Industry = "Software" };

graph.AddNode(person1);
graph.AddNode(person2);
graph.AddNode(company);

// Add edges (relationships)
graph.AddEdge(1, 3);  // Alice works at Tech Corp
graph.AddEdge(2, 3);  // Bob works at Tech Corp

// Serialize to JSON
string json = JsonSerializer.Serialize(graph, options);
Console.WriteLine(json);
```

### 4. Deserialize Back to Graph

```csharp
// Deserialize from JSON
var deserializedGraph = JsonSerializer.Deserialize<PolymorphicGraph<int>>(json, options);

// Access nodes
var node = deserializedGraph.GetNode(1);
if (node is PersonNode person)
{
    Console.WriteLine($"{person.Name} is {person.Age} years old");
}
```

## JSON Format

The converter uses a specific JSON structure that separates nodes and edges:

```json
{
  "Nodes": [
    {
      "$type": "person",
      "Id": 1,
      "Name": "Alice",
      "Age": 30
    },
    {
      "$type": "person",
      "Id": 2,
      "Name": "Bob",
      "Age": 25
    },
    {
      "$type": "company",
      "Id": 3,
      "CompanyName": "Tech Corp",
      "Industry": "Software"
    }
  ],
  "Edges": [
    {
      "From": 1,
      "To": 3
    },
    {
      "From": 2,
      "To": 3
    }
  ]
}
```

## Advanced Usage

### Custom Discriminator Property

By default, the converter uses `"$type"` as the discriminator property. You can customize this:

```csharp
var converter = new PolymorphicGraphJsonConverterFactory<string>()
    .RegisterNodeType<PersonNode>("person")
    .RegisterNodeType<CompanyNode>("company")
    .WithDiscriminatorProperty("nodeType")  // Custom property name
    .Build();
```

### Working with String IDs

The converter supports any type that implements `IEquatable<T>`:

```csharp
// Using string IDs instead of integers
public class PersonNode : GraphNode<string>
{
    public PersonNode(string id) : base(id) { }
    public string Name { get; set; } = string.Empty;
}

var converter = new PolymorphicGraphJsonConverterFactory<string>()
    .RegisterNodeType<PersonNode>("person")
    .Build();

var graph = new PolymorphicGraph<string>();
graph.AddNode(new PersonNode("person-001") { Name = "Alice" });
```

### Cycle Detection

Use the graph's built-in cycle detection before serialization:

```csharp
// Check for cycles
if (graph.HasCycles())
{
    Console.WriteLine("Warning: Graph contains cycles!");
}

// Add edge with cycle prevention
bool added = graph.AddEdgeSafe(fromId: 1, toId: 2, allowCycles: false);
if (!added)
{
    Console.WriteLine("Edge would create a cycle and was not added.");
}
```

### Graph Traversal

After deserialization, you can traverse the graph using built-in strategies:

```csharp
// Depth-First Search
foreach (var node in graph.TraverseGraph(startId: 1, new DFSTraversal()))
{
    Console.WriteLine($"Visiting node: {node.Id}");
}

// Breadth-First Search
foreach (var node in graph.TraverseGraph(startId: 1, new BFSTraversal()))
{
    Console.WriteLine($"Visiting node: {node.Id}");
}
```

## API Reference

### PolymorphicGraphJsonConverterFactory<TNodeId>

Factory class for creating configured converter instances.

#### Methods

- `RegisterNodeType<TNode>(string typeName)` - Register a node type with its discriminator name
- `WithDiscriminatorProperty(string propertyName)` - Set custom discriminator property name (default: "$type")
- `Build()` - Create the configured converter instance

### PolymorphicGraphJsonConverter<TNodeId>

The actual JSON converter implementation.

#### Constructor Parameters

- `typeDiscriminators` - Dictionary mapping type names to node types
- `discriminatorPropertyName` - Property name for type discrimination (default: "$type")

## Implementation Details

### How It Works

1. **Serialization Process:**
   - Nodes are serialized to a "Nodes" array with type discriminators
   - Node connections are extracted and serialized separately to an "Edges" array
   - The `Connections` property is excluded from node serialization to avoid duplication

2. **Deserialization Process:**
   - Nodes are parsed first and added to the graph
   - Edges are collected during parsing
   - After all nodes are loaded, edges are recreated by connecting the nodes

3. **Recursion Prevention:**
   - The converter creates a new `JsonSerializerOptions` instance without itself when serializing/deserializing individual nodes
   - This prevents infinite recursion when handling complex node hierarchies

## Best Practices

1. **Always register all node types** before serialization/deserialization
2. **Use consistent discriminator names** across your application
3. **Validate graph structure** after deserialization for data integrity
4. **Consider cycle detection** for directed graphs that shouldn't contain cycles
5. **Use meaningful node IDs** that won't collide across different node types

## Error Handling

The converter throws `JsonException` in these scenarios:

- Missing or invalid `$type` discriminator property
- Unknown node type (not registered with the factory)
- Malformed JSON structure
- Invalid token types during parsing

```csharp
try
{
    var graph = JsonSerializer.Deserialize<PolymorphicGraph<int>>(json, options);
}
catch (JsonException ex)
{
    Console.WriteLine($"Deserialization failed: {ex.Message}");
}
```

## Limitations

- The `Connections` property must be named exactly "Connections" in derived node classes
- Node types must have a parameterless constructor or a constructor compatible with JSON deserialization
- The converter doesn't support circular references during the serialization process itself (connections are handled separately)

## Performance Considerations

- Large graphs may require significant memory during serialization/deserialization
- Consider streaming for very large graphs (not currently supported)
- Cycle detection runs in O(V + E) time complexity

## Examples

### Example 1: Social Network Graph

```csharp
public class UserNode : GraphNode<string>
{
    public UserNode(string id) : base(id) { }
    public string Username { get; set; } = string.Empty;
    public List<string> Interests { get; set; } = new();
}

public class GroupNode : GraphNode<string>
{
    public GroupNode(string id) : base(id) { }
    public string GroupName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
}

// Build converter
var converter = new PolymorphicGraphJsonConverterFactory<string>()
    .RegisterNodeType<UserNode>("user")
    .RegisterNodeType<GroupNode>("group")
    .Build();

var options = new JsonSerializerOptions { WriteIndented = true };
options.Converters.Add(converter);

// Create social network
var graph = new PolymorphicGraph<string>();
var alice = new UserNode("user-1") { Username = "alice", Interests = new() { "coding", "music" } };
var bob = new UserNode("user-2") { Username = "bob", Interests = new() { "gaming" } };
var devGroup = new GroupNode("group-1") { GroupName = "Developers", MemberCount = 150 };

graph.AddNode(alice);
graph.AddNode(bob);
graph.AddNode(devGroup);
graph.AddEdge("user-1", "group-1");  // Alice is in Developers group
graph.AddEdge("user-2", "group-1");  // Bob is in Developers group

// Serialize and save
string json = JsonSerializer.Serialize(graph, options);
File.WriteAllText("social-network.json", json);
```

### Example 2: Organizational Hierarchy

```csharp
public class EmployeeNode : GraphNode<int>
{
    public EmployeeNode(int id) : base(id) { }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal Salary { get; set; }
}

public class DepartmentNode : GraphNode<int>
{
    public DepartmentNode(int id) : base(id) { }
    public string DepartmentName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

var converter = new PolymorphicGraphJsonConverterFactory<int>()
    .RegisterNodeType<EmployeeNode>("employee")
    .RegisterNodeType<DepartmentNode>("department")
    .Build();

var options = new JsonSerializerOptions();
options.Converters.Add(converter);

var graph = new PolymorphicGraph<int>();
var ceo = new EmployeeNode(1) { Name = "Jane Smith", Position = "CEO", Salary = 250000 };
var engineering = new DepartmentNode(100) { DepartmentName = "Engineering", Location = "Building A" };
var engineer = new EmployeeNode(2) { Name = "John Doe", Position = "Software Engineer", Salary = 95000 };

graph.AddNode(ceo);
graph.AddNode(engineering);
graph.AddNode(engineer);
graph.AddEdge(1, 100);    // CEO oversees Engineering
graph.AddEdge(2, 100);    // Engineer belongs to Engineering

string json = JsonSerializer.Serialize(graph, options);
```

## Contributing

When contributing to this converter, please ensure:
- All node types inherit from `GraphNode<TNodeId>`
- The `TNodeId` type implements `IEquatable<TNodeId>`
- Unit tests cover serialization/deserialization round trips
- Cycle detection is tested for complex graph structures

## License

This code is provided as-is for use in your projects.

## Support

For issues, questions, or contributions, please refer to the project repository or contact the maintainers.