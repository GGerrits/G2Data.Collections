# PolymorphicGraph JSON Converter

A custom `System.Text.Json` converter for serializing and deserializing `PolymorphicGraph<TNodeId>` instances with support for polymorphic node types.

## Features

✅ **Polymorphic Node Support** - Serialize and deserialize different node types in the same graph  
✅ **Type Safety** - Maintains type information through a discriminator property  
✅ **Fluent API** - Easy-to-use factory for configuring the converter  
✅ **Customizable** - Configure discriminator property names  
✅ **Efficient** - Separates nodes and edges for optimal serialization  
✅ **Generic** - Works with any `TNodeId` type that implements `IEquatable<TNodeId>`

## JSON Format

The converter serializes graphs into a structure with two main sections:

```json
{
  "Nodes": [
    {
      "$type": "TypeName",
      "Id": "node-id",
      "Property1": "value1",
      ...
    }
  ],
  "Edges": [
    {
      "From": "source-id",
      "To": "target-id"
    }
  ]
}
```

### Why This Format?

- **Nodes and Edges Separation**: Prevents circular reference issues and makes the structure more readable
- **Type Discriminator**: The `$type` property enables polymorphic deserialization
- **Connection Exclusion**: Node connections are stored as edges, not inline, avoiding duplication

## Usage

### Basic Setup

```csharp
// 1. Define your node types
public class PersonNode : GraphNode<string>
{
    public PersonNode(string id) : base(id) { }
    public string Name { get; set; }
    public int Age { get; set; }
}

public class CompanyNode : GraphNode<string>
{
    public CompanyNode(string id) : base(id) { }
    public string CompanyName { get; set; }
    public string Industry { get; set; }
}

// 2. Create converter with type registrations
var converter = new PolymorphicGraphJsonConverterFactory<string>()
    .RegisterNodeType<PersonNode>("Person")
    .RegisterNodeType<CompanyNode>("Company")
    .Build();

// 3. Configure JsonSerializerOptions
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters = { converter }
};

// 4. Serialize
string json = JsonSerializer.Serialize(graph, options);

// 5. Deserialize
var graph = JsonSerializer.Deserialize<PolymorphicGraph<string>>(json, options);
```

### Advanced Configuration

```csharp
// Custom discriminator property name
var converter = new PolymorphicGraphJsonConverterFactory<string>()
    .RegisterNodeType<PersonNode>("Person")
    .RegisterNodeType<CompanyNode>("Company")
    .WithDiscriminatorProperty("nodeType")  // Instead of default "$type"
    .Build();
```

### Multiple Node Types

```csharp
var converter = new PolymorphicGraphJsonConverterFactory<string>()
    .RegisterNodeType<PersonNode>("Person")
    .RegisterNodeType<CompanyNode>("Company")
    .RegisterNodeType<LocationNode>("Location")
    .RegisterNodeType<EventNode>("Event")
    .Build();
```

### Integer IDs

```csharp
public class TaskNode : GraphNode<int>
{
    public TaskNode(int id) : base(id) { }
    public string Title { get; set; }
    public string Status { get; set; }
}

var converter = new PolymorphicGraphJsonConverterFactory<int>()
    .RegisterNodeType<TaskNode>("Task")
    .Build();

var options = new JsonSerializerOptions
{
    Converters = { converter }
};

var graph = new PolymorphicGraph<int>();
// ... add nodes and edges
string json = JsonSerializer.Serialize(graph, options);
```

## API Reference

### PolymorphicGraphJsonConverter<TNodeId>

Main converter class for `PolymorphicGraph<TNodeId>`.

**Constructor:**
```csharp
public PolymorphicGraphJsonConverter(
    Dictionary<string, Type> typeDiscriminators,
    string discriminatorPropertyName = "$type")
```

**Parameters:**
- `typeDiscriminators` - Maps type names to actual `Type` objects
- `discriminatorPropertyName` - Property name for type discrimination (default: "$type")

### PolymorphicGraphJsonConverterFactory<TNodeId>

Fluent API for creating converters.

**Methods:**

```csharp
// Register a node type with a type name
RegisterNodeType<TNode>(string typeName)

// Set custom discriminator property name
WithDiscriminatorProperty(string propertyName)

// Build the converter
Build()
```

## How It Works

### Serialization Process

1. **Write Nodes Array**: Iterates through all nodes in the graph
2. **Add Type Discriminator**: Adds `$type` property to each node
3. **Exclude Connections**: Skips the `Connections` property to avoid duplication
4. **Write Edges Array**: Creates separate edge objects with `From` and `To` IDs
5. **Output JSON**: Produces clean, readable JSON structure

### Deserialization Process

1. **Parse Nodes**: Reads nodes array and creates node instances based on type discriminator
2. **Store Node Map**: Keeps track of nodes by ID for edge reconstruction
3. **Parse Edges**: Reads edges array into a list
4. **Reconstruct Graph**: Adds all nodes to graph, then adds all edges

## Common Scenarios

### Scenario 1: Social Network

```csharp
var graph = new PolymorphicGraph<string>();

var person1 = new PersonNode("p1") { Name = "Alice" };
var person2 = new PersonNode("p2") { Name = "Bob" };
var group1 = new GroupNode("g1") { GroupName = "Developers" };

graph.AddNode(person1);
graph.AddNode(person2);
graph.AddNode(group1);

graph.AddEdge("p1", "p2");  // Alice knows Bob
graph.AddEdge("p1", "g1");  // Alice is in Developers
graph.AddEdge("p2", "g1");  // Bob is in Developers
```

### Scenario 2: Task Dependencies

```csharp
var graph = new PolymorphicGraph<int>();

var task1 = new TaskNode(1) { Title = "Design" };
var task2 = new TaskNode(2) { Title = "Implement" };
var task3 = new TaskNode(3) { Title = "Test" };

graph.AddNode(task1);
graph.AddNode(task2);
graph.AddNode(task3);

graph.AddEdge(1, 2);  // Implement depends on Design
graph.AddEdge(2, 3);  // Test depends on Implement
```

### Scenario 3: Knowledge Graph

```csharp
var graph = new PolymorphicGraph<string>();

var concept1 = new ConceptNode("c1") { Name = "OOP" };
var concept2 = new ConceptNode("c2") { Name = "Inheritance" };
var example1 = new ExampleNode("e1") { Code = "class Dog : Animal" };

graph.AddNode(concept1);
graph.AddNode(concept2);
graph.AddNode(example1);

graph.AddEdge("c1", "c2");  // OOP contains Inheritance
graph.AddEdge("c2", "e1");  // Inheritance has Example
```

## Error Handling

The converter throws `JsonException` in these cases:

- Missing or invalid type discriminator property
- Unknown node type (not registered)
- Invalid JSON structure
- Missing required properties

**Example:**
```csharp
try
{
    var graph = JsonSerializer.Deserialize<PolymorphicGraph<string>>(json, options);
}
catch (JsonException ex)
{
    Console.WriteLine($"Deserialization failed: {ex.Message}");
}
```

## Best Practices

1. **Register All Node Types** - Ensure all polymorphic node types are registered before deserialization
2. **Consistent Type Names** - Use clear, consistent naming for type discriminators
3. **Immutable IDs** - Don't modify node IDs after adding to graph (this is a limitation of the base `GraphNode` class)
4. **Validate After Deserialization** - Check for cycles or validate graph structure after loading
5. **Use Meaningful IDs** - Choose ID types that make sense for your domain (strings for names, ints for sequences, GUIDs for uniqueness)

## Limitations

1. **No Cycle Preservation During Serialization** - If your graph has cycles, they will be preserved, but you may want to validate after deserialization
2. **Node ID Mutability** - The base `GraphNode<TNodeId>` class has a mutable `Id` property which could cause issues
3. **Requires Type Registration** - All node types must be registered before deserialization
4. **No Edge Metadata** - Edges are simple From->To relationships without additional properties

## Future Enhancements

Potential improvements to consider:

- Support for edge weights or metadata
- Automatic type discovery via reflection
- Support for bidirectional edges
- Graph validation during deserialization
- Support for multiple edge types
- Schema validation

## License

This converter is provided as-is for use with the PolymorphicGraph implementation.
