using System;

namespace PhotosDataBase.Model
{
    public class PhotoFileInfo
    {
        public int Id { get; set; }
        public string FileNameFull { get; set; }
        public string FileName { get; set; }
        public string CameraModel { get; set; }
        public DateTime CreateDate { get; set; }
        public long FileSize { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] PhotoPreview { get; set; }
        public DateTime AddToBaseDate { get; set; }
    }
}
