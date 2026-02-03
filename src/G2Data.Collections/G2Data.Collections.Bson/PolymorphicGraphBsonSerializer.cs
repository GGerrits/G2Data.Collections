using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System.Text.Json;

namespace G2Data.Collections.Bson;

/// <summary>
/// BSON serializer for PolymorphicGraph that supports polymorphic node types
/// </summary>
/// <typeparam name="TNodeId">The type of node identifiers</typeparam>
public class PolymorphicGraphBsonSerializer<TNodeId> : SerializerBase<PolymorphicGraph<TNodeId>>
    where TNodeId : IEquatable<TNodeId>
{
    private readonly Dictionary<string, Type> _typeDiscriminators;
    private readonly Dictionary<Type, string> reverseTypeMap;
    private readonly string _discriminatorElementName;
    private readonly IBsonSerializer<TNodeId> nodeIdSerializer;

    /// <summary>
    /// Creates a BSON serializer with type discriminators for polymorphic node types
    /// </summary>
    /// <param name="typeDiscriminators">Map of type names to actual types (e.g., "PersonNode" -> typeof(PersonNode))</param>
    /// <param name="discriminatorElementName">Name of the element that indicates the node type (default: "_t")</param>
    public PolymorphicGraphBsonSerializer(
        Dictionary<string, Type> typeDiscriminators,
        string discriminatorElementName = "_t")
    {
        _typeDiscriminators = typeDiscriminators ?? throw new ArgumentNullException(nameof(typeDiscriminators));
        _discriminatorElementName = discriminatorElementName ?? throw new ArgumentNullException(nameof(discriminatorElementName));

        if (typeDiscriminators.Count == 0)
        {
            throw new ArgumentException("At least one type discriminator must be provided", nameof(typeDiscriminators));
        }

        // Validate that all types inherit from GraphNode<TNodeId>
        var baseType = typeof(GraphNode<TNodeId>);
        foreach (var kvp in typeDiscriminators)
        {
            if (!baseType.IsAssignableFrom(kvp.Value))
            {
                throw new ArgumentException(
                    $"Type {kvp.Value.Name} must inherit from GraphNode<{typeof(TNodeId).Name}>",
                    nameof(typeDiscriminators));
            }
        }

        // Create reverse map for serialization
        reverseTypeMap = typeDiscriminators.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        // Get the serializer for TNodeId
        nodeIdSerializer = BsonSerializer.LookupSerializer<TNodeId>();
    }

    public override PolymorphicGraph<TNodeId> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        ArgumentNullException.ThrowIfNull(context);

        var reader = context.Reader;
        var graph = new PolymorphicGraph<TNodeId>();
        var edgeList = new List<(TNodeId fromId, TNodeId toId)>();

        reader.ReadStartDocument();

        while (reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var elementName = reader.ReadName();

            switch (elementName)
            {
                case "Nodes":
                    DeserializeNodes(context, graph);
                    break;
                case "Edges":
                    DeserializeEdges(context, edgeList);
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndDocument();

        // Add edges after all nodes are loaded
        foreach (var (fromId, toId) in edgeList)
        {
            if (graph.ContainsNode(fromId) && graph.ContainsNode(toId))
            {
                graph.AddEdge(fromId, toId);
            }
            else
            {
                throw new BsonSerializationException(
                    $"Cannot create edge from {fromId} to {toId}: one or both nodes do not exist");
            }
        }

        return graph;
    }

    private void DeserializeNodes(BsonDeserializationContext context, PolymorphicGraph<TNodeId> graph)
    {
        var reader = context.Reader;
        reader.ReadStartArray();

        while (reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var node = DeserializeNode(context);
            if (node != null)
            {
                graph.AddNode(node);
            }
        }

        reader.ReadEndArray();
    }

    private GraphNode<TNodeId>? DeserializeNode(BsonDeserializationContext context)
    {
        var reader = context.Reader;

        // Read the entire node as a BsonDocument first
        var nodeDocument = BsonDocumentSerializer.Instance.Deserialize(context);

        // Get the type discriminator
        if (!nodeDocument.TryGetValue(_discriminatorElementName, out var discriminatorValue))
        {
            throw new BsonSerializationException(
                $"Missing type discriminator '{_discriminatorElementName}' in node document");
        }

        var typeName = discriminatorValue.AsString;
        if (!_typeDiscriminators.TryGetValue(typeName, out var nodeType))
        {
            throw new BsonSerializationException(
                $"Unknown node type '{typeName}'. Known types: {string.Join(", ", _typeDiscriminators.Keys)}");
        }

        // Remove the Connections field if present (will be reconstructed from edges)
        nodeDocument.Remove("Connections");

        // Deserialize using the specific type's serializer
        var serializer = BsonSerializer.LookupSerializer(nodeType);
        using var documentReader = new BsonDocumentReader(nodeDocument);
        var documentContext = BsonDeserializationContext.CreateRoot(documentReader);

        var node = serializer.Deserialize(documentContext) as GraphNode<TNodeId>;

        if (node == null)
        {
            throw new BsonSerializationException(
                $"Failed to deserialize node of type '{typeName}' as GraphNode<{typeof(TNodeId).Name}>");
        }

        return node;
    }

    private void DeserializeEdges(BsonDeserializationContext context, List<(TNodeId fromId, TNodeId toId)> edgeList)
    {
        var reader = context.Reader;
        reader.ReadStartArray();

        while (reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            reader.ReadStartDocument();

            TNodeId? fromId = default;
            TNodeId? toId = default;
            bool hasFrom = false;
            bool hasTo = false;

            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var elementName = reader.ReadName();

                switch (elementName)
                {
                    case "From":
                        fromId = nodeIdSerializer.Deserialize(context);
                        hasFrom = true;
                        break;
                    case "To":
                        toId = nodeIdSerializer.Deserialize(context);
                        hasTo = true;
                        break;
                    default:
                        reader.SkipValue();
                        break;
                }
            }

            reader.ReadEndDocument();

            if (!hasFrom || !hasTo || fromId == null || toId == null)
            {
                throw new BsonSerializationException("Edge is missing 'From' or 'To' field");
            }

            edgeList.Add((fromId, toId));
        }

        reader.ReadEndArray();
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, PolymorphicGraph<TNodeId> value)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(value);

        var writer = context.Writer;

        writer.WriteStartDocument();

        // Write nodes array
        writer.WriteName("Nodes");
        writer.WriteStartArray();

        foreach (var node in value.GetAllNodes())
        {
            SerializeNode(context, node);
        }

        writer.WriteEndArray();

        // Write edges array
        writer.WriteName("Edges");
        writer.WriteStartArray();

        var serializedEdges = new HashSet<(TNodeId, TNodeId)>();

        foreach (var node in value.GetAllNodes())
        {
            foreach (var connection in node.Connections)
            {
                var edgeKey = (node.Id, connection.Id);

                // Avoid duplicate edges
                if (!serializedEdges.Contains(edgeKey))
                {
                    serializedEdges.Add(edgeKey);

                    writer.WriteStartDocument();

                    writer.WriteName("From");
                    nodeIdSerializer.Serialize(context, node.Id);

                    writer.WriteName("To");
                    nodeIdSerializer.Serialize(context, connection.Id);

                    writer.WriteEndDocument();
                }
            }
        }

        writer.WriteEndArray();

        writer.WriteEndDocument();
    }

    private void SerializeNode(BsonSerializationContext context, GraphNode<TNodeId> node)
    {
        var writer = context.Writer;
        var nodeType = node.GetType();

        // Get type name
        if (!reverseTypeMap.TryGetValue(nodeType, out var typeName))
        {
            typeName = nodeType.Name;
        }

        // Serialize the node to a BsonDocument first
        var serializer = BsonSerializer.LookupSerializer(nodeType);
        var tempDocument = new BsonDocument();

        using (var documentWriter = new BsonDocumentWriter(tempDocument))
        {
            var documentContext = BsonSerializationContext.CreateRoot(documentWriter);
            serializer.Serialize(documentContext, node);
        }

        // Remove Connections if present (edges are stored separately)
        tempDocument.Remove("Connections");

        // Add or replace type discriminator
        tempDocument[_discriminatorElementName] = typeName;

        // Serialize the document
        BsonDocumentSerializer.Instance.Serialize(context, tempDocument);
    }
}

/// <summary>
/// Factory for creating PolymorphicGraphBsonSerializer with a fluent API
/// </summary>
/// <typeparam name="TNodeId">The type of node identifiers</typeparam>
public class PolymorphicGraphBsonSerializerFactory<TNodeId>
    where TNodeId : IEquatable<TNodeId>
{
    private readonly Dictionary<string, Type> typeDiscriminators = new();
    private string discriminatorElementName = "_t";

    /// <summary>
    /// Register a node type with its type name discriminator
    /// </summary>
    /// <typeparam name="TNode">The node type to register</typeparam>
    /// <param name="typeName">The type name to use in serialized documents</param>
    /// <returns>The factory for method chaining</returns>
    public PolymorphicGraphBsonSerializerFactory<TNodeId> RegisterNodeType<TNode>(string typeName)
        where TNode : GraphNode<TNodeId>
    {
        ArgumentNullException.ThrowIfNull(typeName);

        if (typeDiscriminators.ContainsKey(typeName))
        {
            throw new ArgumentException($"Type name '{typeName}' is already registered", nameof(typeName));
        }

        typeDiscriminators[typeName] = typeof(TNode);
        return this;
    }

    /// <summary>
    /// Set the name of the discriminator element (default is "_t")
    /// </summary>
    /// <param name="elementName">The element name to use</param>
    /// <returns>The factory for method chaining</returns>
    public PolymorphicGraphBsonSerializerFactory<TNodeId> WithDiscriminatorElement(string elementName)
    {
        ArgumentNullException.ThrowIfNull(elementName);
        discriminatorElementName = elementName;
        return this;
    }

    /// <summary>
    /// Build the BSON serializer
    /// </summary>
    /// <returns>A configured PolymorphicGraphBsonSerializer</returns>
    public PolymorphicGraphBsonSerializer<TNodeId> Build()
    {
        return new PolymorphicGraphBsonSerializer<TNodeId>(typeDiscriminators, discriminatorElementName);
    }
}

/// <summary>
/// Helper class for registering the BSON serializer with MongoDB
/// </summary>
public static class PolymorphicGraphBsonSerializerRegistration
{
    /// <summary>
    /// Register the serializer globally with the BsonSerializer
    /// </summary>
    /// <typeparam name="TNodeId">The type of node identifiers</typeparam>
    /// <param name="serializer">The serializer to register</param>
    public static void RegisterSerializer<TNodeId>(PolymorphicGraphBsonSerializer<TNodeId> serializer)
        where TNodeId : IEquatable<TNodeId>
    {
        ArgumentNullException.ThrowIfNull(serializer);
        BsonSerializer.RegisterSerializer(typeof(PolymorphicGraph<TNodeId>), serializer);
    }

    /// <summary>
    /// Register node types with custom serializers if needed
    /// </summary>
    /// <typeparam name="TNode">The node type</typeparam>
    /// <typeparam name="TNodeId">The type of node identifiers</typeparam>
    /// <param name="serializer">The custom serializer for the node type</param>
    public static void RegisterNodeTypeSerializer<TNode, TNodeId>(IBsonSerializer<TNode> serializer)
        where TNode : GraphNode<TNodeId>
        where TNodeId : IEquatable<TNodeId>
    {
        ArgumentNullException.ThrowIfNull(serializer);
        BsonSerializer.RegisterSerializer(typeof(TNode), serializer);
    }

    /// <summary>
    /// Create and register a serializer using the fluent factory API
    /// </summary>
    /// <typeparam name="TNodeId">The type of node identifiers</typeparam>
    /// <param name="configure">Configuration action</param>
    public static void RegisterSerializer<TNodeId>(
        Action<PolymorphicGraphBsonSerializerFactory<TNodeId>> configure)
        where TNodeId : IEquatable<TNodeId>
    {
        ArgumentNullException.ThrowIfNull(configure);

        var factory = new PolymorphicGraphBsonSerializerFactory<TNodeId>();
        configure(factory);
        var serializer = factory.Build();
        RegisterSerializer(serializer);
    }
}
