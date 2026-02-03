using MongoDB.Bson.Serialization;

namespace G2Data.Collections.Bson;

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

