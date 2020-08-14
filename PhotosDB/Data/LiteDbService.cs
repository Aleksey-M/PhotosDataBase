using LiteDB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PhotosDB.Data
{
    public class LiteDbService : IDisposable
    {
        private readonly LiteDatabase _liteDb;

        public LiteDbService(string fileName)
        {
            _liteDb = new LiteDatabase($@"Filename={fileName}; Connection=shared");
        }

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

        public List<ImageFileInfo> GetImages(int skip, int take)
        {
            var col = _liteDb.GetCollection<ImageFileInfo>("images");
            col.EnsureIndex(i => i.AddToBaseDate);
            return col.FindAll().OrderByDescending(i => i.AddToBaseDate).Skip(skip).Take(take).ToList();
        }

        public int GetImagesCount()
        {
            var col = _liteDb.GetCollection<ImageFileInfo>("images");
            return col.Count();
        }

        public List<ImportExceptionInfo> GetErrors(int skip, int take)
        {
            var col = _liteDb.GetCollection<ImportExceptionInfo>("errors");
            col.EnsureIndex(e => e.ExceptionDateTime);
            return col.FindAll().OrderByDescending(e => e.ExceptionDateTime).Skip(skip).Take(take).ToList();
        }

        public int GetErrorsCount()
        {
            var col = _liteDb.GetCollection<ImportExceptionInfo>("errors");
            return col.Count();
        }

        public void DeleteErrors(IEnumerable<Guid> errorsIds)
        {
            var col = _liteDb.GetCollection<ImportExceptionInfo>("errors");
            col.DeleteMany(e => errorsIds.Contains(e.ImportExceptionInfoId));
        }

        public void DeleteImage(Guid imageId)
        {
            var col = _liteDb.GetCollection<ImageFileInfo>("images");
            col.EnsureIndex(e => e.ImageFileInfoId);
            col.DeleteMany(i => i.ImageFileInfoId == imageId);
        }

        public void Dispose()
        {
            _liteDb?.Dispose();
        }
    }
}
