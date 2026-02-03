namespace G2Data.Collections.Bson;

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
