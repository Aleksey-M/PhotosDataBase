using LiteDB;
using System;

namespace PhotosDB.Data
{
    public class LiteDbService : IDisposable
    {
        private readonly LiteDatabase _liteDb;

        public LiteDbService(string fileName)
        {
            _liteDb = new LiteDatabase($@"Filename={fileName}; Connection=shared");
        }

        //public LiteDatabase Database => _liteDb;

        public void AddImage(ImageFileInfo image)
        {
            var col = _liteDb.GetCollection<ImageFileInfo>("images");
            col.Insert(image);
        }

        public void AddError(ImportExceptionInfo exceptionInfo)
        {
            var col = _liteDb.GetCollection<ImportExceptionInfo>("errors");
            col.Insert(exceptionInfo);
        }

        public void Dispose()
        {
            _liteDb?.Dispose();
        }
    }
}
