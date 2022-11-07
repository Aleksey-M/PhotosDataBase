using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace PhotosDataBase.Data;

public sealed class ImageFileInfo
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public Guid FileId { get; set; }

    public string FileNameFull { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string FileExtension { get; set; } = string.Empty;

    public string CameraModel { get; set; } = string.Empty;

    public DateTime? TakenDate { get; set; }

    public long FileSize { get; set; }

    public DateTime FileCreatedDate { get; set; }

    public DateTime FileModifiedDate { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public byte[] PhotoPreview { get; set; } = Array.Empty<byte>();

    public DateTime AddToBaseDate { get; set; }

    public List<ExifPropertyInfo> ExifProperties { get; set; } = new();
}

public sealed class ExifPropertyInfo
{
    public string IfdName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public object? Value { get; set; }
}
