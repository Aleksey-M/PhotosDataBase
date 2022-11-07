using ExifLibrary;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace PhotosDataBase.Data.Serializers;

internal sealed class JFIFThumbnailSerializer : IBsonSerializer<JFIFThumbnail>
{
    public Type ValueType => typeof(JFIFThumbnail);

    public static JFIFThumbnailSerializer Instance { get; } = new JFIFThumbnailSerializer();

    private JFIFThumbnailSerializer()
    {
    }

    public JFIFThumbnail Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonType = context.Reader.GetCurrentBsonType();

        if (bsonType != BsonType.Document)
        {
            throw new InvalidOperationException($"Cannot deserialize JFIFThumbnail from BsonType {bsonType}.");
        }

        context.Reader.ReadStartDocument();

        var format = (JFIFThumbnail.ImageFormat)context.Reader.ReadInt32("ImageFormat");

        byte[]? palette = null;
        if (format != JFIFThumbnail.ImageFormat.JPEG)
        {
            palette = context.Reader.ReadBytes("Palette");
        }

        var pixelData = context.Reader.ReadBytes("PixelData");

        context.Reader.ReadEndDocument();

        return format != JFIFThumbnail.ImageFormat.JPEG
            ? new JFIFThumbnail(palette, pixelData)
            : new JFIFThumbnail(format, pixelData);
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JFIFThumbnail value)
    {
        context.Writer.WriteStartDocument();

        context.Writer.WriteInt32("ImageFormat", (int)value.Format);

        if (value.Format != JFIFThumbnail.ImageFormat.JPEG)
        {
            context.Writer.WriteBytes("Palette", value.Palette);
        }
        context.Writer.WriteBytes("PixelData", value.PixelData);

        context.Writer.WriteEndDocument();
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var uf32 = (JFIFThumbnail)value;

        Serialize(context, args, uf32);
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return Deserialize(context, args);
    }
}
