using System.Text.Json;
using System.Text.Json.Serialization;

namespace G2Data.Collections.Json;

/// <summary>
/// JSON converter for PolymorphicGraph that handles polymorphic node serialization/deserialization
/// </summary>
/// <remarks>
/// Creates a converter with type discriminators for polymorphic node types
/// </remarks>
/// <param name="typeDiscriminators">Map of type names to actual types (e.g., "PersonNode" -> typeof(PersonNode))</param>
/// <param name="discriminatorPropertyName">Name of the property that indicates the node type (default: "$type")</param>
public class PolymorphicGraphJsonConverter<TNodeId>(
    Dictionary<string, Type> typeDiscriminators,
    string discriminatorPropertyName = "$type") 
    : JsonConverter<PolymorphicGraph<TNodeId>>
    where TNodeId : IEquatable<TNodeId>
{
    private readonly Dictionary<string, Type> typeDiscriminators = typeDiscriminators ?? throw new ArgumentNullException(nameof(typeDiscriminators));
    private readonly string discriminatorPropertyName = discriminatorPropertyName;

    public override PolymorphicGraph<TNodeId> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        var graph = new PolymorphicGraph<TNodeId>();
        var nodeMap = new Dictionary<TNodeId, GraphNode<TNodeId>>();
        var edgeList = new List<(TNodeId fromId, TNodeId toId)>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName token");
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "Nodes":
                    ReadNodes(ref reader, options, graph, nodeMap);
                    break;
                case "Edges":
                    PolymorphicGraphJsonConverter<TNodeId>.ReadEdges(ref reader, edgeList);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        // Add edges after all nodes are loaded
        foreach (var (fromId, toId) in edgeList)
        {
            graph.AddEdge(fromId, toId);
        }

        return graph;
    }

    private void ReadNodes(ref Utf8JsonReader reader, JsonSerializerOptions options, 
        PolymorphicGraph<TNodeId> graph, Dictionary<TNodeId, GraphNode<TNodeId>> nodeMap)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected StartArray token for Nodes");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                var node = ReadNode(ref reader, options);
                if (node != null)
                {
                    graph.AddNode(node);
                    nodeMap[node.Id] = node;
                }
            }
        }
    }

    private GraphNode<TNodeId>? ReadNode(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty(discriminatorPropertyName, out var typeProperty))
        {
            throw new JsonException($"Missing '{discriminatorPropertyName}' property in node");
        }

        var typeName = typeProperty.GetString();
        if (typeName == null || !typeDiscriminators.TryGetValue(typeName, out var nodeType))
        {
            throw new JsonException($"Unknown node type: {typeName}");
        }

        var json = root.GetRawText();
        var node = JsonSerializer.Deserialize(json, nodeType, options) as GraphNode<TNodeId>;

        return node;
    }

    private static void ReadEdges(ref Utf8JsonReader reader, List<(TNodeId fromId, TNodeId toId)> edgeList)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected StartArray token for Edges");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                TNodeId? fromId = default;
                TNodeId? toId = default;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }

                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        string? propertyName = reader.GetString();
                        reader.Read();

                        switch (propertyName)
                        {
                            case "From":
                                fromId = JsonSerializer.Deserialize<TNodeId>(ref reader);
                                break;
                            case "To":
                                toId = JsonSerializer.Deserialize<TNodeId>(ref reader);
                                break;
                        }
                    }
                }

                if (fromId != null && toId != null)
                {
                    edgeList.Add((fromId, toId));
                }
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, PolymorphicGraph<TNodeId> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Write nodes array
        writer.WritePropertyName("Nodes");
        writer.WriteStartArray();

        foreach (var node in value.GetAllNodes())
        {
            WriteNode(writer, node, options);
        }

        writer.WriteEndArray();

        // Write edges array
        writer.WritePropertyName("Edges");
        writer.WriteStartArray();

        foreach (var node in value.GetAllNodes())
        {
            foreach (var connection in node.Connections)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("From");
                JsonSerializer.Serialize(writer, node.Id, options);
                writer.WritePropertyName("To");
                JsonSerializer.Serialize(writer, connection.Id, options);
                writer.WriteEndObject();
            }
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    private void WriteNode(Utf8JsonWriter writer, GraphNode<TNodeId> node, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Write type discriminator
        var nodeType = node.GetType();
        var typeName = typeDiscriminators.FirstOrDefault(kvp => kvp.Value == nodeType).Key
            ?? nodeType.Name;

        writer.WriteString(discriminatorPropertyName, typeName);

        // Write node properties using reflection or serialize the whole object
        var nodeJson = JsonSerializer.SerializeToElement(node, nodeType, options);

        foreach (var property in nodeJson.EnumerateObject())
        {
            // Skip the Connections property as we handle edges separately
            if (property.Name == "Connections" || property.Name == discriminatorPropertyName)
            {
                continue;
            }

            property.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}

public class PolymorphicGraphJsonConverterFactory<TNodeId>
    where TNodeId : IEquatable<TNodeId>
{
    private readonly Dictionary<string, Type> typeDiscriminators = [];
    private string discriminatorPropertyName = "$type";

    public PolymorphicGraphJsonConverterFactory<TNodeId> RegisterNodeType<TNode>(string typeName)
        where TNode : GraphNode<TNodeId>
    {
        typeDiscriminators[typeName] = typeof(TNode);
        return this;
    }

    public PolymorphicGraphJsonConverterFactory<TNodeId> WithDiscriminatorProperty(string propertyName)
    {
        discriminatorPropertyName = propertyName;
        return this;
    }

    public PolymorphicGraphJsonConverter<TNodeId> Build()
    {
        return new PolymorphicGraphJsonConverter<TNodeId>(typeDiscriminators, discriminatorPropertyName);
    }
}
