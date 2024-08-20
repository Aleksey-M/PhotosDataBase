namespace PhotosDB.Data;

public class ImportExceptionInfo
{
    public Guid ImportExceptionInfoId { get; set; }
    public string FileNameFull { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime ExceptionDateTime { get; set; }
    public string Message { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
}

public class ImageFileInfo
{
    public Guid ImageFileInfoId { get; set; }
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
    public byte[] PhotoPreview { get; set; } = [];
    public DateTime AddToBaseDate { get; set; }
}
