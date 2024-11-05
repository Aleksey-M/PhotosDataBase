namespace Mediafiles.Domain.Entities;

internal class MediaFileInfo
{
    public Guid FileId { get; set; }
    public string CollectionName { get; set; } = string.Empty;
    public string FileNameFull { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime FileCreatedDate { get; set; }
    public DateTime FileModifiedDate { get; set; }
    public string TypeDescr { get; set; } = string.Empty;
    public Metadata[] Metadata { get; set; } = [];
    public string? ThumbnailBase64 { get; set; }
    public string? Exception { get; set; }
    public DateTime AddToBaseDate { get; set; }
    public DateTime? DeletedDate { get; set; }
}

public class Metadata
{
    public string Name { get; set; } = string.Empty;
    public Tag[] Tags { get; set; } = [];
}

public class Tag
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
