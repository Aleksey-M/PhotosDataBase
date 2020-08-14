using System;

namespace PhotosDB.Data
{
    public class ImportExceptionInfo
    {
        public Guid ImportExceptionInfoId { get; set; }
        public string FileNameFull { get; set; }
        public string FileName { get; set; }
        public DateTime ExceptionDateTime { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }

    public class ImageFileInfo
    {
        public Guid ImageFileInfoId { get; set; }
        public string FileNameFull { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string CameraModel { get; set; }
        public DateTime? TakenDate { get; set; }
        public long FileSize { get; set; }
        public DateTime FileCreatedDate { get; set; }
        public DateTime FileModifiedDate { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public byte[] PhotoPreview { get; set; }
        public DateTime AddToBaseDate { get; set; }
    }
}
