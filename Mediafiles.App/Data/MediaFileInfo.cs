namespace Mediafiles.App.Data;

public enum MediaType { Unknown, Photo, Video }

public class MediaFileInfo
{
    public Guid Id { get; set; }
    public MediaType MediaType { get; set; }
    public string FileNameFull { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string? CameraModel { get; set; }
    public DateTime? TakenDate { get; set; }
    public long FileSize { get; set; }
    public DateTime FileCreatedDate { get; set; }
    public DateTime FileModifiedDate { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public TimeSpan? Duration { get; set; }
    public byte[] Thumbnail { get; set; } = [];
    public DateTime AddToBaseDate { get; set; }
}
