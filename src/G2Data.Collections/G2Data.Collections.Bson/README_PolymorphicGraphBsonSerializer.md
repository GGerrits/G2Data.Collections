# PolymorphicGraph BSON Serializer

A custom BSON serializer for serializing and deserializing `PolymorphicGraph<TNodeId>` instances with support for polymorphic node types using MongoDB.Bson.

## Features

✅ **MongoDB Integration** - Fully compatible with MongoDB.Driver for database storage  
✅ **Polymorphic Node Support** - Serialize and deserialize different node types in the same graph  
✅ **Type Discrimination** - Uses BSON type discriminator (default: `_t`)  
✅ **Fluent API** - Easy-to-use factory for configuring the serializer  
✅ **Binary Efficiency** - BSON's binary format is more compact than JSON  
✅ **File Storage** - Save/load graphs to/from .bson files  
✅ **Generic Support** - Works with any `TNodeId` type that implements `IEquatable<TNodeId>`

## Installation

Install the MongoDB.Bson NuGet package:

```bash
dotnet add package MongoDB.Bson
dotnet add package MongoDB.Driver  # If using MongoDB
```

## BSON Format

The serializer uses a structure similar to the JSON converter:

```json
{
  "Nodes": [
    {
      "_t": "TypeName",
      "Id": "node-id",
      "property1": "value1",
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

### Key Differences from JSON Converter

- **Type Discriminator**: Uses `_t` by default (MongoDB convention) instead of `$type`
- **Binary Format**: BSON is a binary format, more efficient than JSON
- **MongoDB Native**: Seamlessly integrates with MongoDB collections
- **Type Safety**: BSON has native support for more data types (DateTime, ObjectId, etc.)

## Basic Usage

### 1. Define Your Node Types

```csharp
public class PersonNode : GraphNode<string>
{
    public PersonNode(string id) : base(id) { }

    [BsonElement("name")]
    public string Name { get; set; }

    [BsonElement("age")]
    public int Age { get; set; }
}

public class CompanyNode : GraphNode<string>
{
    public CompanyNode(string id) : base(id) { }

    [BsonElement("companyName")]
    public string CompanyName { get; set; }

    [BsonElement("industry")]
    public string Industry { get; set; }
}
```

### 2. Register the Serializer

```csharp
// Create and register the serializer
var serializer = new PolymorphicGraphBsonSerializerFactory<string>()
    .RegisterNodeType<PersonNode>("Person")
    .RegisterNodeType<CompanyNode>("Company")
    .Build();

PolymorphicGraphBsonSerializerRegistration.RegisterSerializer(serializer);
```

### 3. Serialize and Deserialize

```csharp
// Create a graph
var graph = new PolymorphicGraph<string>();
graph.AddNode(new PersonNode("alice") { Name = "Alice", Age = 30 });
graph.AddNode(new CompanyNode("techcorp") { CompanyName = "TechCorp" });
graph.AddEdge("alice", "techcorp");

// Serialize to BSON bytes
byte[] bson = graph.ToBson();

// Deserialize from BSON bytes
var deserializedGraph = BsonSerializer.Deserialize<PolymorphicGraph<string>>(bson);
```

## MongoDB Integration

### Storing Graphs in MongoDB

```csharp
// Register the serializer
var serializer = new PolymorphicGraphBsonSerializerFactory<string>()
    .RegisterNodeType<PersonNode>("Person")
    .RegisterNodeType<CompanyNode>("Company")
    .Build();

PolymorphicGraphBsonSerializerRegistration.RegisterSerializer(serializer);

// Connect to MongoDB
var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("graphdb");
var collection = database.GetCollection<BsonDocument>("graphs");

// Create a graph
var graph = new PolymorphicGraph<string>();
// ... add nodes and edges

// Store in MongoDB
var document = new BsonDocument
{
    { "_id", ObjectId.GenerateNewId() },
    { "name", "MyGraph" },
    { "createdAt", DateTime.UtcNow },
    { "graph", graph.ToBson() }
};

await collection.InsertOneAsync(document);
```

### Retrieving Graphs from MongoDB

```csharp
// Find the document
var filter = Builders<BsonDocument>.Filter.Eq("name", "MyGraph");
var document = await collection.Find(filter).FirstOrDefaultAsync();

// Extract and deserialize the graph
var graphBytes = document["graph"].AsBsonBinaryData.Bytes;
var graph = BsonSerializer.Deserialize<PolymorphicGraph<string>>(graphBytes);
```

### Using Strongly-Typed Collections

```csharp
// Define a wrapper class
public class GraphDocument
{
    [BsonId]
    public ObjectId Id { get; set; }
    
    public string Name { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public PolymorphicGraph<string> Graph { get; set; }
}

// Use strongly-typed collection
var collection = database.GetCollection<GraphDocument>("graphs");

var doc = new GraphDocument
{
    Name = "MyGraph",
    CreatedAt = DateTime.UtcNow,
    Graph = graph
};

await collection.InsertOneAsync(doc);
```

## File Storage

### Save to File

```csharp
string filename = "graph.bson";

using (var fileStream = new FileStream(filename, FileMode.Create))
using (var writer = new BsonBinaryWriter(fileStream))
{
    var context = BsonSerializationContext.CreateRoot(writer);
    serializer.Serialize(context, new BsonSerializationArgs(), graph);
}
```

### Load from File

```csharp
PolymorphicGraph<string> graph;

using (var fileStream = new FileStream(filename, FileMode.Open))
using (var reader = new BsonBinaryReader(fileStream))
{
    var context = BsonDeserializationContext.CreateRoot(reader);
    graph = serializer.Deserialize(context, new BsonDeserializationArgs());
}
```

## Advanced Configuration

### Custom Type Discriminator

```csharp
var serializer = new PolymorphicGraphBsonSerializerFactory<string>()
    .RegisterNodeType<PersonNode>("Person")
    .RegisterNodeType<CompanyNode>("Company")
    .WithDiscriminatorElement("nodeType")  // Custom discriminator
    .Build();
```

### Custom Node Serializers

```csharp
// Define a custom serializer for a specific node type
public class LocationNodeSerializer : SerializerBase<LocationNode>
{
    public override LocationNode Deserialize(
        BsonDeserializationContext context, 
        BsonDeserializationArgs args)
    {
        // Custom deserialization logic
    }

    public override void Serialize(
        BsonSerializationContext context, 
        BsonSerializationArgs args, 
        LocationNode value)
    {
        // Custom serialization logic
    }
}

// Register the custom serializer
BsonSerializer.RegisterSerializer(typeof(LocationNode), new LocationNodeSerializer());

// Then register the graph serializer as usual
var serializer = new PolymorphicGraphBsonSerializerFactory<string>()
    .RegisterNodeType<LocationNode>("Location")
    .Build();
```

### Working with Different ID Types

```csharp
// Integer IDs
var intSerializer = new PolymorphicGraphBsonSerializerFactory<int>()
    .RegisterNodeType<TaskNode>("Task")
    .Build();

PolymorphicGraphBsonSerializerRegistration.RegisterSerializer(intSerializer);

// GUID IDs
var guidSerializer = new PolymorphicGraphBsonSerializerFactory<Guid>()
    .RegisterNodeType<EntityNode>("Entity")
    .Build();

PolymorphicGraphBsonSerializerRegistration.RegisterSerializer(guidSerializer);

// ObjectId IDs
var objectIdSerializer = new PolymorphicGraphBsonSerializerFactory<ObjectId>()
    .RegisterNodeType<DocumentNode>("Document")
    .Build();

PolymorphicGraphBsonSerializerRegistration.RegisterSerializer(objectIdSerializer);
```

## API Reference

### PolymorphicGraphBsonSerializer<TNodeId>

Main serializer class for `PolymorphicGraph<TNodeId>`.

**Constructor:**
```csharp
public PolymorphicGraphBsonSerializer(
    Dictionary<string, Type> typeDiscriminators,
    string discriminatorElementName = "_t")
```

**Methods:**
```csharp
public override PolymorphicGraph<TNodeId> Deserialize(
    BsonDeserializationContext context, 
    BsonDeserializationArgs args)

public override void Serialize(
    BsonSerializationContext context, 
    BsonSerializationArgs args, 
    PolymorphicGraph<TNodeId> value)
```

### PolymorphicGraphBsonSerializerFactory<TNodeId>

Fluent API for creating serializers.

**Methods:**

```csharp
// Register a node type
RegisterNodeType<TNode>(string typeName)

// Set custom discriminator element name
WithDiscriminatorElement(string elementName)

// Build the serializer
Build()
```

### PolymorphicGraphBsonSerializerRegistration

Helper class for registration.

**Methods:**

```csharp
// Register the graph serializer globally
RegisterSerializer<TNodeId>(PolymorphicGraphBsonSerializer<TNodeId> serializer)

// Register custom node type serializer
RegisterNodeTypeSerializer<TNode, TNodeId>(IBsonSerializer<TNode> serializer)
```

## BSON Attributes

You can use MongoDB.Bson attributes to control serialization:

```csharp
public class PersonNode : GraphNode<string>
{
    public PersonNode(string id) : base(id) { }

    [BsonElement("name")]  // Custom element name
    public string Name { get; set; }

    [BsonIgnore]  // Skip this property
    public string TemporaryData { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]  // DateTime options
    public DateTime BirthDate { get; set; }

    [BsonRepresentation(BsonType.String)]  // Store as string
    public int SocialSecurityNumber { get; set; }

    [BsonDefaultValue(0)]  // Default value if missing
    public int Age { get; set; }
}
```

## Performance Considerations

### BSON vs JSON

| Aspect | BSON | JSON |
|--------|------|------|
| Size | More compact for binary data | More compact for text |
| Speed | Faster to parse | Slower to parse |
| Type Safety | Native type support | String-based |
| Readability | Requires tools to view | Human-readable |
| MongoDB | Native format | Requires conversion |

### Best Practices

1. **Use BSON for:**
   - MongoDB storage
   - High-performance scenarios
   - Binary data (images, files)
   - Internal data transfer

2. **Use JSON for:**
   - API responses
   - Configuration files
   - Human-readable exports
   - Web applications

3. **Size Optimization:**
   - Use shorter element names with `[BsonElement]`
   - Consider compression for large graphs
   - Store edges efficiently

## Common Scenarios

### Scenario 1: Social Network Graph in MongoDB

```csharp
// Register serializer
var serializer = new PolymorphicGraphBsonSerializerFactory<string>()
    .RegisterNodeType<UserNode>("User")
    .RegisterNodeType<GroupNode>("Group")
    .RegisterNodeType<PostNode>("Post")
    .Build();

PolymorphicGraphBsonSerializerRegistration.RegisterSerializer(serializer);

// Connect to MongoDB
var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("socialnetwork");
var collection = database.GetCollection<BsonDocument>("graphs");

// Create and store graph
var graph = new PolymorphicGraph<string>();
// ... build social network graph

var doc = new BsonDocument
{
    { "userId", "user123" },
    { "graphType", "friends" },
    { "lastUpdated", DateTime.UtcNow },
    { "graph", graph.ToBson() }
};

await collection.InsertOneAsync(doc);
```

### Scenario 2: Caching Graphs in Redis

```csharp
using StackExchange.Redis;

// Serialize graph to BSON
byte[] bson = graph.ToBson();

// Store in Redis
var redis = ConnectionMultiplexer.Connect("localhost");
var db = redis.GetDatabase();
await db.StringSetAsync("graph:social:user123", bson);

// Retrieve from Redis
byte[] cachedBson = await db.StringGetAsync("graph:social:user123");
var cachedGraph = BsonSerializer.Deserialize<PolymorphicGraph<string>>(cachedBson);
```

### Scenario 3: Workflow Engine

```csharp
public class WorkflowStepNode : GraphNode<Guid>
{
    public WorkflowStepNode(Guid id) : base(id) { }
    
    [BsonElement("step")]
    public string StepName { get; set; }
    
    [BsonElement("action")]
    public string ActionType { get; set; }
    
    [BsonElement("config")]
    public BsonDocument Configuration { get; set; }
}

// Store workflow definitions
var serializer = new PolymorphicGraphBsonSerializerFactory<Guid>()
    .RegisterNodeType<WorkflowStepNode>("Step")
    .Build();

var graph = new PolymorphicGraph<Guid>();
// ... build workflow graph

// Store in MongoDB for execution engine
var collection = database.GetCollection<BsonDocument>("workflows");
await collection.InsertOneAsync(new BsonDocument
{
    { "workflowId", workflowId },
    { "definition", graph.ToBson() }
});
```

## Error Handling

```csharp
try
{
    var graph = BsonSerializer.Deserialize<PolymorphicGraph<string>>(bson);
}
catch (BsonSerializationException ex)
{
    // Handle serialization errors (missing types, invalid format, etc.)
    Console.WriteLine($"Serialization error: {ex.Message}");
}
catch (FormatException ex)
{
    // Handle invalid BSON format
    Console.WriteLine($"Invalid BSON format: {ex.Message}");
}
```

## Migration from JSON to BSON

If you're migrating from the JSON converter:

```csharp
// Read JSON
string json = File.ReadAllText("graph.json");
var graph = JsonSerializer.Deserialize<PolymorphicGraph<string>>(json, jsonOptions);

// Write BSON
byte[] bson = graph.ToBson();
File.WriteAllBytes("graph.bson", bson);
```

## Troubleshooting

### Issue: "Type not registered"

**Solution:** Ensure all node types are registered before deserialization:

```csharp
var serializer = new PolymorphicGraphBsonSerializerFactory<string>()
    .RegisterNodeType<NodeType1>("Type1")
    .RegisterNodeType<NodeType2>("Type2")
    // Register ALL types
    .Build();

PolymorphicGraphBsonSerializerRegistration.RegisterSerializer(serializer);
```

### Issue: "Circular reference detected"

**Solution:** The serializer handles this by separating nodes and edges. If you still encounter this, ensure you're not trying to serialize the graph connections inline.

### Issue: "Cannot deserialize ObjectId"

**Solution:** Register the ObjectId serializer or use string IDs:

```csharp
// Use ObjectId as node ID
var serializer = new PolymorphicGraphBsonSerializerFactory<ObjectId>()
    .RegisterNodeType<MyNode>("MyNode")
    .Build();
```

## Comparison with JSON Converter

| Feature | BSON Serializer | JSON Converter |
|---------|----------------|----------------|
| MongoDB Integration | ✅ Native | ❌ Requires conversion |
| File Size | Smaller for binary | Smaller for text |
| Human Readable | ❌ No | ✅ Yes |
| Performance | Faster | Slower |
| Type Safety | Better | Good |
| Default Discriminator | `_t` | `$type` |
| Use Case | Databases, performance | APIs, configuration |

## License

This serializer is provided as-is for use with the PolymorphicGraph implementation.
