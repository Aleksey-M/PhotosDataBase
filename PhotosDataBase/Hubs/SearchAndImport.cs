using ExifLibrary;
using Microsoft.AspNetCore.SignalR;
using PhotosDataBase.Model;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhotosDataBase.Hubs
{
    public class SearchAndImport : Hub
    {
        private readonly PhotosDbContext _context;

        public SearchAndImport(PhotosDbContext context)
        {
            _context = context;
        }

        private static CancellationTokenSource _cts = null;
        private static IDisposable _timer = null;
        private static int _filesToProcess = 0;
        private static int _processedFiles = 0;

        public void CancelLoading()
        {
            try
            {
                _cts?.Cancel();
                _timer?.Dispose();
            }
            finally
            {
                _cts = null;
                _timer = null;
                _filesToProcess = 0;
                _processedFiles = 0;
            }
        }

        public async Task LoadPhotos(string basePath)
        {
            try
            {
                if (_cts != null) throw new InvalidOperationException("The process is already underway");

                var photoFilesNames = Directory.GetFileSystemEntries(basePath, "*.jpg,*jpeg", SearchOption.AllDirectories);

                _timer = Observable.Interval(TimeSpan.FromMilliseconds(500))
                    .Subscribe(
                        l => Clients.All.SendAsync("ShowLoadingProcess", _filesToProcess, _processedFiles)
                    );

                _cts = new CancellationTokenSource();
                await LoadFilesProcess(photoFilesNames, _cts.Token);
            }
            catch (Exception exc)
            {
                await Clients.All.SendAsync("ShowServerError", exc.Message + Environment.NewLine + exc.StackTrace);
            }
        }

        public async Task LoadFilesProcess(IEnumerable<string> fileList, CancellationToken token)
        {
            try
            {
                var buffer = new List<PhotoFileInfo>();
                _filesToProcess = fileList.Count();

                foreach (var fn in fileList)
                {
                    try
                    {
                        token.ThrowIfCancellationRequested();

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

                        if (pFile.FileSize < 1024 * 1024 * 7)
                        {
                            var img = await File.ReadAllBytesAsync(pFile.FileNameFull);
                            pFile.PhotoPreview = ScaleImage(img, 250);
                        }

                        buffer.Add(pFile);
                        _processedFiles++;

                        if (buffer.Count >= 5)
                        {
                            await _context.PhotoFiles.AddRangeAsync(buffer);
                            await _context.SaveChangesAsync();
                            buffer.Clear();
                        }
                    }
                    catch (Exception exc)
                    {
                        await Clients.All.SendAsync("ShowServerError", "File: " + fn + Environment.NewLine + exc.Message + Environment.NewLine + exc.StackTrace);
                    }
                }
                await _context.PhotoFiles.AddRangeAsync(buffer);
                await _context.SaveChangesAsync();
                await Clients.All.SendAsync("ShowImportResult");
            }
            catch (Exception exc)
            {
                await Clients.All.SendAsync("ShowServerError", exc.Message + Environment.NewLine + exc.StackTrace);
            }
            finally
            {
                _timer?.Dispose();
                _cts = null;
                _timer = null;
                _filesToProcess = 0;
                _processedFiles = 0;
            }
        }

        public static byte[] ScaleImage(byte[] data, int maxSizePx)
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
