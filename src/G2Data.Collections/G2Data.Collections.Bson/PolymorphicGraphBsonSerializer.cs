using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace G2Data.Collections.Bson;

public class PolymorphicGraphBsonSerializer<TNodeId> : SerializerBase<PolymorphicGraph<TNodeId>>
    where TNodeId : IEquatable<TNodeId>
{
    private readonly Dictionary<string, Type> typeDiscriminators;
    private readonly Dictionary<Type, string> reverseTypeMap;
    private readonly string discriminatorElementName;
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
        this.typeDiscriminators = typeDiscriminators ?? throw new ArgumentNullException(nameof(typeDiscriminators));
        this.discriminatorElementName = discriminatorElementName;

        // Create reverse map for serialization
        reverseTypeMap = typeDiscriminators.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        // Get the serializer for TNodeId
        nodeIdSerializer = BsonSerializer.LookupSerializer<TNodeId>();
    }

    public override PolymorphicGraph<TNodeId> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        var graph = new PolymorphicGraph<TNodeId>();
        var nodeMap = new Dictionary<TNodeId, GraphNode<TNodeId>>();
        var edgeList = new List<(TNodeId fromId, TNodeId toId)>();

        reader.ReadStartDocument();

        while (reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var elementName = reader.ReadName();

            switch (elementName)
            {
                case "Nodes":
                    DeserializeNodes(context, graph, nodeMap);
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
            graph.AddEdge(fromId, toId);
        }

        return graph;
    }

    private void DeserializeNodes(BsonDeserializationContext context,
        PolymorphicGraph<TNodeId> graph, Dictionary<TNodeId, GraphNode<TNodeId>> nodeMap)
    {
        var reader = context.Reader;
        reader.ReadStartArray();

        while (reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var node = DeserializeNode(context);
            if (node != null)
            {
                graph.AddNode(node);
                nodeMap[node.Id] = node;
            }
        }

        reader.ReadEndArray();
    }

    private GraphNode<TNodeId>? DeserializeNode(BsonDeserializationContext context)
    {
        var reader = context.Reader;
        reader.ReadStartDocument();

        string? typeName = null;
        Type? nodeType = null;
        BsonDocument? nodeDocument = null;

        // First pass: find the type discriminator
        while (reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var elementName = reader.ReadName();

            if (elementName == discriminatorElementName)
            {
                typeName = reader.ReadString();
                if (!typeDiscriminators.TryGetValue(typeName, out nodeType))
                {
                    throw new BsonSerializationException($"Unknown node type: {typeName}");
                }
            }
            else
            {
                reader.SkipValue();
            }
        }

        reader.ReadEndDocument();

        if (nodeType == null)
        {
            throw new BsonSerializationException($"Missing type discriminator '{discriminatorElementName}'");
        }

        // Second pass: deserialize the full document
        // We need to re-read the document, so we'll use BsonDocument as intermediate
        var bookmark = context.Reader.GetBookmark();
        context.Reader.ReturnToBookmark(bookmark);

        reader.ReadStartDocument();
        nodeDocument = new BsonDocument();

        while (reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var elementName = reader.ReadName();

            if (elementName == "Connections")
            {
                // Skip connections as they will be reconstructed from edges
                reader.SkipValue();
            }
            else
            {
                var value = BsonValueSerializer.Instance.Deserialize(context);
                nodeDocument.Add(elementName, value);
            }
        }

        reader.ReadEndDocument();

        // Deserialize the node using the specific type's serializer
        var serializer = BsonSerializer.LookupSerializer(nodeType);
        var documentReader = new BsonDocumentReader(nodeDocument);
        var documentContext = BsonDeserializationContext.CreateRoot(documentReader);

        var node = serializer.Deserialize(documentContext) as GraphNode<TNodeId>;
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

            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var elementName = reader.ReadName();

                switch (elementName)
                {
                    case "From":
                        fromId = nodeIdSerializer.Deserialize(context);
                        break;
                    case "To":
                        toId = nodeIdSerializer.Deserialize(context);
                        break;
                    default:
                        reader.SkipValue();
                        break;
                }
            }

            reader.ReadEndDocument();

            if (fromId != null && toId != null)
            {
                edgeList.Add((fromId, toId));
            }
        }

        reader.ReadEndArray();
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, PolymorphicGraph<TNodeId> value)
    {
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

        foreach (var node in value.GetAllNodes())
        {
            foreach (var connection in node.Connections)
            {
                writer.WriteStartDocument();

                writer.WriteName("From");
                nodeIdSerializer.Serialize(context, node.Id);

                writer.WriteName("To");
                nodeIdSerializer.Serialize(context, connection.Id);

                writer.WriteEndDocument();
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

        writer.WriteStartDocument();

        // Write type discriminator first
        writer.WriteName(discriminatorElementName);
        writer.WriteString(typeName);

        // Serialize the node to a BsonDocument first to filter out Connections
        var serializer = BsonSerializer.LookupSerializer(nodeType);
        var tempDocument = new BsonDocument();
        var documentWriter = new BsonDocumentWriter(tempDocument);
        var documentContext = BsonSerializationContext.CreateRoot(documentWriter);

        serializer.Serialize(documentContext, node);

        // Write all elements except Connections and the discriminator (if it was already in the document)
        foreach (var element in tempDocument.Elements)
        {
            if (element.Name != "Connections" && element.Name != discriminatorElementName)
            {
                writer.WriteName(element.Name);
                BsonValueSerializer.Instance.Serialize(context, element.Value);
            }
        }

        writer.WriteEndDocument();
    }
}

/// <summary>
/// Factory for creating PolymorphicGraphBsonSerializer with a fluent API
/// </summary>
public class PolymorphicGraphBsonSerializerFactory<TNodeId>
    where TNodeId : IEquatable<TNodeId>
{
    private readonly Dictionary<string, Type> typeDiscriminators = new();
    private string discriminatorElementName = "_t";

    public PolymorphicGraphBsonSerializerFactory<TNodeId> RegisterNodeType<TNode>(string typeName)
        where TNode : GraphNode<TNodeId>
    {
        typeDiscriminators[typeName] = typeof(TNode);
        return this;
    }

    public PolymorphicGraphBsonSerializerFactory<TNodeId> WithDiscriminatorElement(string elementName)
    {
        discriminatorElementName = elementName;
        return this;
    }

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
    public static void RegisterSerializer<TNodeId>(PolymorphicGraphBsonSerializer<TNodeId> serializer)
        where TNodeId : IEquatable<TNodeId>
    {
        BsonSerializer.RegisterSerializer(typeof(PolymorphicGraph<TNodeId>), serializer);
    }

    /// <summary>
    /// Register node types with custom serializers if needed
    /// </summary>
    public static void RegisterNodeTypeSerializer<TNode, TNodeId>(IBsonSerializer<TNode> serializer)
        where TNode : GraphNode<TNodeId>
        where TNodeId : IEquatable<TNodeId>
    {
        BsonSerializer.RegisterSerializer(typeof(TNode), serializer);
    }
}
