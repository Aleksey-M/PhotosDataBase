namespace Mediafiles.App.Data;

public class ExceptionInfo
{
    public Guid Id { get; set; }
    public string FileNameFull { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime ExceptionDateTime { get; set; }
    public string Message { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
}
