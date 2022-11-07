using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace PhotosDataBase.Data;

public sealed class ImportExceptionInfo
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public Guid FileId { get; set; }

    public string FileNameFull { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public DateTime ExceptionDateTime { get; set; }

    public string Message { get; set; } = string.Empty;

    public string StackTrace { get; set; } = string.Empty;
}
