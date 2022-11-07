using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using static ExifLibrary.MathEx;
using MongoDB.Bson.IO;

namespace PhotosDataBase.Data.Serializers;

internal sealed class Fraction32Serializer : IBsonSerializer<Fraction32>
{
    public Type ValueType => typeof(Fraction32);

    public static Fraction32Serializer Instance { get; } = new Fraction32Serializer();

    private Fraction32Serializer()
    {
    }

    public Fraction32 Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonType = context.Reader.GetCurrentBsonType();

        if (bsonType != BsonType.Document)
        {
            throw new InvalidOperationException($"Cannot deserialize UFraction32 from BsonType {bsonType}.");
        }

        context.Reader.ReadStartDocument();
        int numerator = context.Reader.ReadInt32("Numerator");
        int denominator = context.Reader.ReadInt32("Denominator");
        bool isNegative = context.Reader.ReadBoolean("IsNegative");
        context.Reader.ReadEndDocument();

        var value = new Fraction32(numerator: numerator, denominator: denominator)
        {
            IsNegative = isNegative
        };

        return value;
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Fraction32 value)
    {
        context.Writer.WriteStartDocument();
        context.Writer.WriteInt32("Numerator", value.Numerator);
        context.Writer.WriteInt32("Denominator", value.Denominator);
        context.Writer.WriteBoolean("IsNegative", value.IsNegative);
        context.Writer.WriteEndDocument();
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var uf32 = (Fraction32)value;

        Serialize(context, args, uf32);
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return Deserialize(context, args);
    }
}
