using ExifLibrary;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhotosDB.Data
{
    public class ImportImages : IDisposable
    {
        private readonly LiteDbService _liteDbService;
        public ImportImages(LiteDbService liteDbService)
        {
            _liteDbService = liteDbService;
        }

        private readonly object _syncObj = new object();
        private CancellationTokenSource _cts;

        public async Task Process(string searchPath, CancellationToken token, IProgress<(int allImages, int addedImages, int errors)> progress)
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
                var imagesFilesNames = Directory.GetFileSystemEntries(searchPath, "*.jpg,*.jpeg", SearchOption.AllDirectories);
                int imagesCount = imagesFilesNames.Length;
                int errorsCount = 0;

                await Task.Run(async () =>
                {
                    int tmpCount = 0;

                    foreach (var fName in imagesFilesNames)
                    {
                        lock (_syncObj)
                        {
                            if (_combinedToken.IsCancellationRequested) break;
                        }

                        try
                        {
                            var imgFile = new ImageFileInfo
                            {
                                ImageFileInfoId = Guid.NewGuid(),
                                FileNameFull = fName,
                                FileName = Path.GetFileName(fName),
                                FileSize = new FileInfo(fName).Length,
                                FileCreatedDate = File.GetCreationTime(fName),
                                FileModifiedDate = File.GetLastWriteTime(fName)
                            };

                            var file = ImageFile.FromFile(fName);

                            imgFile.Height = Convert.ToInt32(file.Properties.First(p => p.Tag == ExifTag.PixelXDimension).Value);
                            imgFile.Width = Convert.ToInt32(file.Properties.First(p => p.Tag == ExifTag.PixelYDimension).Value);
                            imgFile.TakenDate = (DateTime)file.Properties.First(p => p.Tag == ExifTag.DateTimeOriginal).Value;

                            var model = file.Properties.First(p => p.Tag == ExifTag.Model).Value.ToString();
                            var make = file.Properties.First(p => p.Tag == ExifTag.Make).Value.ToString();
                            imgFile.CameraModel = model.Contains(make) ? model : make + " " + model;

                            imgFile.AddToBaseDate = DateTime.Now;

                            var img = await File.ReadAllBytesAsync(imgFile.FileNameFull);
                            imgFile.PhotoPreview = ScaleImage(img, 350);

                            _liteDbService.AddImage(imgFile);
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
                                StackTrace = exc.StackTrace
                            };

                            errorsCount++;
                            _liteDbService.AddError(error);
                        }

                        tmpCount++;
                        if (progress != null)
                        {
                            progress.Report((allImages: imagesCount, addedImages: tmpCount, errors: errorsCount));
                        }
                    }
                });
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

        private static byte[] ScaleImage(byte[] data, int maxSizePx)
        {
            using var bitmap = SKBitmap.Decode(data);
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
            using var jpeg = img.Encode(SKEncodedImageFormat.Jpeg, 90);
            using var memoryStream = new MemoryStream();

            jpeg.AsStream().CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        public void Dispose()
        {
            lock (_syncObj)
            {
                if(_cts != null)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                    _cts = null;
                }
            }
        }
    }
}
