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