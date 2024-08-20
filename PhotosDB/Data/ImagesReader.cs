using ExifLibrary;
using SkiaSharp;

namespace PhotosDB.Data;

public class ImagesReader : IDisposable
{
    private readonly LiteDbService _liteDbService;
    public ImagesReader(LiteDbService liteDbService)
    {
        _liteDbService = liteDbService;
    }

    private readonly object _syncObj = new();
    private CancellationTokenSource? _cts;

    public bool IsWorking
    {
        get
        {
            lock (_syncObj)
            {
                return _cts != null;
            }
        }
    }

    public async Task Process(
        string searchDirectoryPath,
        CancellationToken token,
        IProgress<(int allImages, int addedImages, int errors)> progress)
    {
        var fileExtensions = new string[] { "*.jpg", "*.jpeg", "*.jfif" };
        var imagesFilesNames = fileExtensions
            .SelectMany(ext => Directory.GetFileSystemEntries(searchDirectoryPath, ext, SearchOption.AllDirectories))
            .ToList();

        await Process(imagesFilesNames, token, progress).ConfigureAwait(false);
    }

    public async Task Process(
        List<string> filesNames,
        CancellationToken token,
        IProgress<(int allImages, int addedImages, int errors)> progress)
    {
        lock (_syncObj)
        {
            if (_cts != null)
            {
                throw new Exception("Import process already executing");
            }
        }

        _cts?.Dispose();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        var _combinedToken = _cts.Token;

        try
        {
            var fileExtensions = new string[] { "*.jpg", "*.jpeg", "*.jfif" };

            int imagesCount = filesNames.Count;
            int errorsCount = 0;
            int tmpCount = 0;
            int w, h;

            _ = Task.Run(async () =>
            {
                PeriodicTimer timer = new(TimeSpan.FromSeconds(1));

                while (await timer.WaitForNextTickAsync(_combinedToken))
                {
                    progress?.Report((allImages: imagesCount, addedImages: tmpCount, errors: errorsCount));
                }
            }, _combinedToken);


            await Task.Run(async () =>
            {
                var files = new ImageFileInfo?[filesNames.Count];
                var errors = new ImportExceptionInfo?[filesNames.Count];

                await Parallel.ForEachAsync(
                    filesNames.Select((fName, index) => (fName, index)),
                    _combinedToken,
                    async (t, token) =>
                {
                    if (token.IsCancellationRequested) return;

                    var (fName, index) = t;

                    try
                    {
                        var imgFile = new ImageFileInfo
                        {
                            ImageFileInfoId = Guid.NewGuid(),
                            FileNameFull = fName,
                            FileName = Path.GetFileName(fName),
                            FileExtension = Path.GetExtension(fName),
                            FileSize = new FileInfo(fName).Length,
                            FileCreatedDate = File.GetCreationTime(fName),
                            FileModifiedDate = File.GetLastWriteTime(fName)
                        };

                        ImageFile? exifImageFile = null;

                        try
                        {
                            exifImageFile = ImageFile.FromFile(fName);
                        }
                        catch
                        {
                            exifImageFile = null;
                        }

                        if (exifImageFile != null)
                        {
                            var tag = exifImageFile.Properties.FirstOrDefault(p => p.Tag == ExifTag.PixelXDimension);
                            imgFile.Height = tag == null ? null : (int?)Convert.ToInt32(tag.Value);

                            tag = exifImageFile.Properties.FirstOrDefault(p => p.Tag == ExifTag.PixelYDimension);
                            imgFile.Width = tag == null ? null : (int?)Convert.ToInt32(tag.Value);
                            imgFile.TakenDate = (DateTime?)exifImageFile.Properties.FirstOrDefault(p => p.Tag == ExifTag.DateTimeOriginal)?.Value;

                            var model = exifImageFile.Properties.FirstOrDefault(p => p.Tag == ExifTag.Model)?.Value?.ToString();
                            var make = exifImageFile.Properties.FirstOrDefault(p => p.Tag == ExifTag.Make)?.Value?.ToString() ?? string.Empty; ;

                            imgFile.CameraModel = (model, make, model?.Contains(make)) switch
                            {
                                (_, _, true) => model,
                                (_, _, false) => make + " " + model,
                                _ => null
                            };
                        }

                        imgFile.AddToBaseDate = DateTime.Now;

                        var img = await File.ReadAllBytesAsync(imgFile.FileNameFull, token);
                        (imgFile.PhotoPreview, w, h) = ScaleImage(img, maxSizePx: 300, jpegQuality: 30);
                        imgFile.Width ??= w;
                        imgFile.Height ??= h;

                        files[index] = imgFile;
                    }
                    catch (Exception exc)
                    {
                        var error = new ImportExceptionInfo
                        {
                            FileName = Path.GetFileName(fName),
                            FileNameFull = fName,
                            ExceptionDateTime = DateTime.Now,
                            ImportExceptionInfoId = Guid.NewGuid(),
                            Message = exc.Message,
                            StackTrace = exc.StackTrace ?? string.Empty
                        };

                        Interlocked.Increment(ref errorsCount);
                        errors[index] = error;
                    }

                    Interlocked.Increment(ref tmpCount);
                });

                // запись результатов в базу
                var readedFiles = files.Where(x => x != null).ToArray();
                if (readedFiles.Length != 0)
                {
                    _liteDbService.AddImages(readedFiles!);
                }

                var throwedErrors = errors.Where(x => x != null).ToArray();
                if (throwedErrors.Length != 0)
                {
                    _liteDbService.AddErrors(throwedErrors!);
                }

            }, _combinedToken).ConfigureAwait(false);
        }
        finally
        {
            lock (_syncObj)
            {
                _cts?.Dispose();
                _cts = null;
            }
        }
    }

    private static (byte[] thumbnailImg, int sourceWidth, int sourceHeight) ScaleImage(byte[] data, int maxSizePx, int jpegQuality)
    {
        using var bitmap = SKBitmap.Decode(data);
        int _width = bitmap.Width;
        int _height = bitmap.Height;

        if (bitmap.ColorType != SKImageInfo.PlatformColorType)
        {
            bitmap.CopyTo(bitmap, SKImageInfo.PlatformColorType);
        }

        int width, height;
        if (bitmap.Width >= bitmap.Height)
        {
            width = maxSizePx;
            height = Convert.ToInt32(bitmap.Height / (double)bitmap.Width * maxSizePx);
        }
        else
        {
            height = maxSizePx;
            width = Convert.ToInt32(bitmap.Width / (double)bitmap.Height * maxSizePx);
        }

        var imageInfo = new SKImageInfo(width, height);
        using var thumbnail = bitmap.Resize(imageInfo, SKFilterQuality.Medium);
        using var img = SKImage.FromBitmap(thumbnail);
        using var jpeg = img.Encode(SKEncodedImageFormat.Jpeg, jpegQuality);
        using var memoryStream = new MemoryStream();

        jpeg.AsStream().CopyTo(memoryStream);
        return (memoryStream.ToArray(), _width, _height);
    }

    public void Dispose()
    {
        lock (_syncObj)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }
    }
}
