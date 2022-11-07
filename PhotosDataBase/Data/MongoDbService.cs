using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace PhotosDataBase.Data;

internal sealed class MongoDbService
{
    private readonly IMongoCollection<ImageFileInfo> _imagesCollection;
    private readonly IMongoCollection<ImportExceptionInfo> _exceptionsCollection;

    public MongoDbService(IOptions<MongoDbConfig> config)
    {
        var client = new MongoClient(config.Value.ConnectionString);

        var database = client.GetDatabase("imagesDatabase");

        _imagesCollection = database.GetCollection<ImageFileInfo>("images");
        _exceptionsCollection = database.GetCollection<ImportExceptionInfo>("errors");
    }

    public async Task AddImage(ImageFileInfo image) =>
        await _imagesCollection.InsertOneAsync(image);

    public async Task AddError(ImportExceptionInfo exceptionInfo) =>
        await _exceptionsCollection.InsertOneAsync(exceptionInfo);

    public async Task<List<ImageFileInfo>> GetImages(int skip, int take) =>
        await _imagesCollection
            .Find(_ => true)
            .SortByDescending(x => x.AddToBaseDate)
            .Skip(skip)
            .Limit(take)
            .ToListAsync();

    public async Task<long> GetImagesCount() =>
        await _imagesCollection
            .CountDocumentsAsync(_ => true);

    public async Task<List<ImportExceptionInfo>> GetErrors() =>
        await _exceptionsCollection
            .Find(_ => true)
            .ToListAsync();

    public async Task<long> GetErrorsCount() =>
        await _exceptionsCollection
            .CountDocumentsAsync(_ => true);

    public async Task DeleteErrors(IEnumerable<string> errorsIds) =>
        await _exceptionsCollection
            .DeleteManyAsync(x => errorsIds.Contains(x.Id));

    public async Task DeleteImage(string imageId) =>
        await _imagesCollection
            .DeleteOneAsync(x => x.Id == imageId);

    public async Task<ImageFileInfo?> GetImage(string id) =>
        await _imagesCollection
        .Find(x => x.Id == id)
        .FirstOrDefaultAsync();
}
