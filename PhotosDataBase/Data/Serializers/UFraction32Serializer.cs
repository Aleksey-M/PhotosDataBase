using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using static ExifLibrary.MathEx;

namespace PhotosDataBase.Data.Serializers;

internal sealed class UFraction32Serializer : IBsonSerializer<UFraction32>
{
    public Type ValueType => typeof(UFraction32);

    public static UFraction32Serializer Instance { get; } = new UFraction32Serializer();

    private UFraction32Serializer()
    {
    }

    public UFraction32 Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonType = context.Reader.GetCurrentBsonType();

        if (bsonType != BsonType.Document)
        {
            throw new InvalidOperationException($"Cannot deserialize UFraction32 from BsonType {bsonType}.");
        }

        context.Reader.ReadStartDocument();
        long numerator = context.Reader.ReadInt64("Numerator");
        long denominator = context.Reader.ReadInt64("Denominator");
        context.Reader.ReadEndDocument();

        return new UFraction32(numerator: (uint)numerator, denominator: (uint)denominator);
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, UFraction32 value)
    {
        context.Writer.WriteStartDocument();
        context.Writer.WriteInt64("Numerator", value.Numerator);
        context.Writer.WriteInt64("Denominator", value.Denominator);
        context.Writer.WriteEndDocument();
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var uf32 = (UFraction32)value;

        Serialize(context, args, uf32);
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return Deserialize(context, args);
    }
}
