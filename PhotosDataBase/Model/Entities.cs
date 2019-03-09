using System;
using System.Collections.Generic;
/*
database photosdatabase should be created from pgAdmin 
create user photodbuser with password 'Ph13#Zz89@001'; 
grant all privileges on database photosdatabase to photodbuser;
*/
namespace PhotosDataBase.Model
{
    public class ImportedDirectory
    {
        public int ImportedDirectoryId { get; set; }
        public string DirectoryPath { get; set; }
        public DateTime ImportStart { get; set; }
        public DateTime ImportFinish { get; set; }

        public List<ImportExceptionInfo> ImportExceptions { get; set; }
        public List<PhotoFileInfo> Photos { get; set; }
    }

    public class ImportExceptionInfo
    {
        public int ImportExceptionInfoId { get; set; }
        public string FileNameFull { get; set; }
        public string FileName { get; set; }
        public DateTime ExceptionDateTime { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }

        public int ImportedDirectoryId { get; set; }
        public ImportedDirectory Directory { get; set; }
    }

    public class PhotoFileInfo
    {
        public int PhotoFileInfoId { get; set; }
        public string FileNameFull { get; set; }
        public string FileName { get; set; }
        public string CameraModel { get; set; }
        public DateTime CreateDate { get; set; }
        public long FileSize { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] PhotoPreview { get; set; }
        public DateTime AddToBaseDate { get; set; }

        public int ImportedDirectoryId { get; set; }
        public ImportedDirectory Directory { get; set; }
    }
}
