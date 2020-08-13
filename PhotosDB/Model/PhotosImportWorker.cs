using ExifLibrary;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhotosDB.Model
{
    public static class AppCurrentState
    {
        // !!! Is not thread safe
        private static string _path = null;
        private static CancellationTokenSource _workCancellation = null;

        public static bool IsWorking => _workCancellation != null;

        public static void StopWorking()
        {
            if (IsWorking)
            {
                _workCancellation.Cancel();
                _workCancellation = null;
                _path = null;
            }
        }

        public static void SetPath(string path)
        {
            if (_path == null)
                _path = path;
        }

        public static (string path, CancellationToken token) GetPathForWork()
        {
            if (!IsWorking)
            {
                _workCancellation = new CancellationTokenSource();
                return (_path, _workCancellation.Token);
            }
            else
                return (null, CancellationToken.None);
        }

        public static bool IsReadyForStart => _path != null && !IsWorking;

        public static void WorkIsDone()
        {
            _workCancellation = null;
            _path = null;
        }
    }

    public class PhotosImportWorker : IHostedService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IHubContext<PhotosClientHub> _clientHub;

        public PhotosImportWorker(IServiceScopeFactory serviceScopeFactory, IHubContext<PhotosClientHub> clientHub)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _clientHub = clientHub;
        }

        //private CancellationTokenSource _cts = null;
        private CancellationToken? _token = null;
        private CancellationTokenSource _cts = null;
        private IDisposable _timer = null;
        private int _filesToProcess = 0;
        private int _processedFiles = 0;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            string path;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (AppCurrentState.IsReadyForStart)
                {
                    (path, _token) = AppCurrentState.GetPathForWork();
                    try
                    {
                        _timer = Observable.Interval(TimeSpan.FromMilliseconds(500))
                            .Subscribe(
                                i => Task.WaitAll(ShowLoadingProcess())
                            );

                        _cts = CancellationTokenSource.CreateLinkedTokenSource(_token.Value);

                        var photoFilesNames = Directory.GetFileSystemEntries(path, "*.jpg", SearchOption.AllDirectories);
                        await WorkProcess(path, photoFilesNames, _cts.Token);
                    }
                    catch (Exception exc)
                    {
                        await ShowServerError(string.Empty, exc.Message);
                    }
                    finally
                    {
                        FinishWork();
                    }
                }
                else
                    await Task.Delay(500);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();
            FinishWork();
            return Task.CompletedTask;
        }

        private void FinishWork()
        {
            AppCurrentState.WorkIsDone();
            _cts?.Dispose();
            _timer?.Dispose();

            _token = null;
            _cts = null;
            _timer = null;
            _filesToProcess = 0;
            _processedFiles = 0;
        }

        private Task ShowLoadingProcess()
        {
            float percent = Convert.ToSingle(Math.Round((float)_processedFiles / _filesToProcess * 100, 2, MidpointRounding.AwayFromZero));
            if (float.IsNaN(percent)) percent = 0;
            var msg = $"Loading photos: {_processedFiles} / {_filesToProcess} ({percent.ToString("N2")}%)";
            return _clientHub.Clients.All.SendAsync("ShowLoadingProcess", msg, percent);
        }

        private Task ShowServerError(string fileName, string message) =>
            _clientHub.Clients.All.SendAsync("ShowServerError", fileName, message);

        private Task ShowImportResult() =>
            _clientHub.Clients.All.SendAsync("ShowImportResult", "Photos search is finished");

        private async Task WorkProcess(string path, IEnumerable<string> fileList, CancellationToken token)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<PhotosDbContext>();

                var importedDirectory = new ImportedDirectory
                {
                    DirectoryPath = path,
                    ImportStart = DateTime.Now
                };

                context.Add(importedDirectory);
                await context.SaveChangesAsync();

                try
                {
                    var buffer = new List<PhotoFileInfo>();
                    _filesToProcess = fileList.Count();

                    foreach (var fn in fileList)
                    {
                        if (token.IsCancellationRequested) break;

                        try
                        {
                            var pFile = new PhotoFileInfo
                            {
                                FileNameFull = fn,
                                FileName = Path.GetFileName(fn),
                                FileSize = new FileInfo(fn).Length
                            };

                            var file = ImageFile.FromFile(fn);

                            pFile.Height = Convert.ToInt32(file.Properties.First(p => p.Tag == ExifTag.PixelXDimension).Value);
                            pFile.Width = Convert.ToInt32(file.Properties.First(p => p.Tag == ExifTag.PixelYDimension).Value);
                            pFile.CreateDate = (DateTime)file.Properties.First(p => p.Tag == ExifTag.DateTimeOriginal).Value;

                            var model = file.Properties.First(p => p.Tag == ExifTag.Model).Value.ToString();
                            var make = file.Properties.First(p => p.Tag == ExifTag.Make).Value.ToString();
                            pFile.CameraModel = model.Contains(make) ? model : make + " " + model;

                            pFile.AddToBaseDate = DateTime.Now;
                            pFile.ImportedDirectoryId = importedDirectory.ImportedDirectoryId;

                            if (pFile.FileSize < 1024 * 1024 * 7)
                            {
                                var img = await File.ReadAllBytesAsync(pFile.FileNameFull);
                                pFile.PhotoPreview = ScaleImage(img, 350);
                            }

                            buffer.Add(pFile);
                            _processedFiles++;

                            if (buffer.Count >= 5)
                            {
                                await context.PhotoFiles.AddRangeAsync(buffer);
                                await context.SaveChangesAsync();
                                buffer.Clear();
                            }
                        }
                        catch (Exception exc)
                        {
                            var err = new ImportExceptionInfo
                            {
                                ExceptionDateTime = DateTime.Now,
                                FileName = Path.GetFileName(fn),
                                FileNameFull = fn,
                                ImportedDirectoryId = importedDirectory.ImportedDirectoryId,
                                Message = exc.Message,
                                StackTrace = exc.StackTrace
                            };

                            context.Add(err);
                            await context.SaveChangesAsync();

                            await ShowServerError(fn, exc.Message);
                            _processedFiles++;
                        }
                    }

                    await context.PhotoFiles.AddRangeAsync(buffer);
                    await context.SaveChangesAsync();
                    await ShowImportResult();
                }
                catch (Exception exc)
                {
                    await ShowServerError(string.Empty, exc.Message);
                }
                finally
                {
                    FinishWork();
                }

                importedDirectory.ImportFinish = DateTime.Now;
                await context.SaveChangesAsync();
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
    }
}
