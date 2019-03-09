using ExifLibrary;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PhotosDataBase.Hubs;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhotosDataBase.Model
{
    //public interface IClientFunctions
    //{
    //    Task ShowLoadingProcess(string message, float percent);
    //    Task ShowServerError(string fileName, string message);
    //    Task ShowImportResult(string message);
    //}

    public class PhotosImportWorker : BackgroundService
    {        
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IHubContext<PhotosClientHub> _clientHub;

        public PhotosImportWorker(IServiceScopeFactory serviceScopeFactory, IHubContext<PhotosClientHub> clientHub)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _clientHub = clientHub;
        }

        private static readonly object _mutex = new object();
        private static string _importFolderPath = null;
        private static CancellationTokenSource _cts = null;
        private static IDisposable _timer = null;
        private static int _filesToProcess = 0;
        private static int _processedFiles = 0;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string path; 
            while (!stoppingToken.IsCancellationRequested)
            {
                lock (_mutex)
                {
                    path = _importFolderPath;
                }
                if(path != null)
                {
                    await StartWork(path);
                }
                else
                    await Task.Delay(500);
            }
        }        

        public void SetImportFolder(string path)
        {
            if (!IsWorking)
            {
                lock (_mutex)
                {
                    if (_importFolderPath == null) _importFolderPath = path;
                }
            }            
        }

        public void CancelWork()
        {
            lock (_mutex)
            {
                try
                {
                    _cts?.Cancel();
                    _timer?.Dispose();
                }
                finally
                {
                    FinishWork();
                }
            }
        }

        private static void FinishWork()
        {
            _cts = null;
            _timer = null;
            _filesToProcess = 0;
            _processedFiles = 0;
            _importFolderPath = null;
        }

        public bool IsWorking
        {
            get
            {
                lock (_mutex)
                {
                    return _cts != null;
                }
            }
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

        private async Task StartWork(string basePath)
        {
            try
            {
                //if (IsWorking)
                //{
                //    await ShowServerError(string.Empty, "The process is already underway");
                //    return;
                //}

                _timer = Observable.Interval(TimeSpan.FromMilliseconds(500))
                    .Subscribe(
                        async i => await ShowLoadingProcess()
                    );

                lock (_mutex)
                {
                    _cts = new CancellationTokenSource();
                }
                
                var photoFilesNames = Directory.GetFileSystemEntries(basePath, "*.jpg", SearchOption.AllDirectories);
                await WorkProcess(basePath, photoFilesNames, _cts.Token);
            }
            catch (Exception exc)
            {
                await ShowServerError(string.Empty, exc.Message);
            }
        }

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
                        token.ThrowIfCancellationRequested();

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
                                pFile.PhotoPreview = ScaleImage(img, 250);
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
                    _timer?.Dispose();
                    FinishWork();
                }

                importedDirectory.ImportFinish = DateTime.Now;
                await context.SaveChangesAsync();
            }
        }

        private static byte[] ScaleImage(byte[] data, int maxSizePx)
        {
            using (var bitmap = SKBitmap.Decode(data))
            {
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
                using (var thumbnail = bitmap.Resize(imageInfo, SKFilterQuality.Medium))
                using (var img = SKImage.FromBitmap(thumbnail))
                using (var jpeg = img.Encode(SKEncodedImageFormat.Jpeg, 90))
                using (var memoryStream = new MemoryStream())
                {
                    jpeg.AsStream().CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }       
    }
}
