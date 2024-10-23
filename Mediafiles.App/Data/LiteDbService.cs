using LiteDB;

namespace Mediafiles.App.Data;

public static class CollectionsNames
{
    public const string MEDIA = "media";
    public const string ERRORS = "errors";
}

public class LiteDbService : IDisposable
{
    private readonly LiteDatabase _liteDb;

    public LiteDbService(string fileName)
    {
        _liteDb = new LiteDatabase($@"Filename={fileName}; Connection=direct");
    }

    public void AddMedia(MediaFileInfo file)
    {
        var col = _liteDb.GetCollection<MediaFileInfo>(CollectionsNames.MEDIA);
        col.Insert(file);
    }

    public void AddError(ExceptionInfo exceptionInfo)
    {
        var col = _liteDb.GetCollection<ExceptionInfo>(CollectionsNames.ERRORS);
        col.Insert(exceptionInfo);
    }

    public void AddMedia(IEnumerable<MediaFileInfo> files)
    {
        var col = _liteDb.GetCollection<MediaFileInfo>(CollectionsNames.MEDIA);
        col.InsertBulk(files);
    }

    public void AddErrors(IEnumerable<ExceptionInfo> exceptionsInfos)
    {
        var col = _liteDb.GetCollection<ExceptionInfo>(CollectionsNames.ERRORS);
        col.InsertBulk(exceptionsInfos);
    }

    public List<MediaFileInfo> GetMedia(int skip, int take)
    {
        var col = _liteDb.GetCollection<MediaFileInfo>(CollectionsNames.MEDIA);
        col.EnsureIndex(i => i.AddToBaseDate);
        return col.FindAll().OrderByDescending(i => i.AddToBaseDate).Skip(skip).Take(take).ToList();
    }

    public int GetMediaCount()
    {
        var col = _liteDb.GetCollection<MediaFileInfo>(CollectionsNames.MEDIA);
        return col.Count();
    }

    public List<ExceptionInfo> GetErrors(int skip, int take)
    {
        var col = _liteDb.GetCollection<ExceptionInfo>(CollectionsNames.ERRORS);
        col.EnsureIndex(e => e.ExceptionDateTime);
        return col.FindAll().OrderByDescending(e => e.ExceptionDateTime).Skip(skip).Take(take).ToList();
    }

    public int GetErrorsCount()
    {
        var col = _liteDb.GetCollection<ExceptionInfo>(CollectionsNames.ERRORS);
        return col.Count();
    }

    public void DeleteErrors(IEnumerable<Guid> errorsIds)
    {
        var col = _liteDb.GetCollection<ExceptionInfo>(CollectionsNames.ERRORS);
        col.DeleteMany(e => errorsIds.Contains(e.Id));
    }

    public void DeleteMedia(Guid mediaId)
    {
        var col = _liteDb.GetCollection<MediaFileInfo>(CollectionsNames.MEDIA);
        col.EnsureIndex(e => e.Id);
        col.DeleteMany(i => i.Id == mediaId);
    }

    public void Dispose()
    {
        _liteDb?.Dispose();
    }
}
