//using Microsoft.AspNetCore.SignalR;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using PhotosDataBase.Model;
//using RiseDiary.Model;
//using RiseDiary.WebUI.Data;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reactive.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//namespace PhotosDataBase.Hubs
//{
//    public class PhotosImportHub : Hub
//    {
//        private readonly PhotosDbContext _context;
//        private readonly IConfiguration _configuration;
//        private static string _diarySQLConnection;

//        public PhotosImportHub(PhotosDbContext context, IConfiguration configuration)
//        {
//            _context = context;
//            _configuration = configuration;
//        }

//        private DiaryDbContext CreateDiaryContext()
//        {
//            var optionsBuilder = new DbContextOptionsBuilder<DiaryDbContext>();
//            if (_diarySQLConnection == null)
//            {
//                _diarySQLConnection = _configuration.GetConnectionString("DiaryConnectionString");
//            }
//            optionsBuilder.UseNpgsql(_diarySQLConnection);
//            return new DiaryDbContext(optionsBuilder.Options);
//        }

//        private static CancellationTokenSource _cts = null;
//        private static IDisposable _timer = null;
//        private static int _recordsToProcess = 0;
//        private static int _processedRecords = 0;
//        private static int _photosToProcess = 0;
//        private static int _processedPhotos = 0;

//        public async Task ImportPhotosToDiary()
//        {
//            try
//            {
//                if (_cts != null) throw new InvalidOperationException("The import is already underway");

//                _timer = Observable.Interval(TimeSpan.FromMilliseconds(500))
//                    .Subscribe(
//                        async l => await Clients.All.SendAsync("ShowImportProcess", _recordsToProcess, _processedRecords, _photosToProcess, _processedPhotos)
//                    );

//                _cts = new CancellationTokenSource();
//                await ImportPhotosProcess(_cts.Token);
//            }
//            catch (Exception exc)
//            {
//                await Clients.All.SendAsync("ShowServerError", exc.Message);
//            }
//            finally
//            {
//                _timer?.Dispose();
//                _timer = null;
//                _cts = null;
//                _recordsToProcess = 0;
//                _processedRecords = 0;
//                _photosToProcess = 0;
//                _processedPhotos = 0;
//            }
//        }

//        public void CancelImporting()
//        {
//            try
//            {
//                _cts?.Cancel();
//                _timer?.Dispose();
//            }
//            finally
//            {
//                _cts = null;
//                _timer = null;
//                _recordsToProcess = 0;
//                _processedRecords = 0;
//                _photosToProcess = 0;
//                _processedPhotos = 0;
//            }
//        }

//        public async Task ImportPhotosProcess(CancellationToken token)
//        {
//            _photosToProcess = await _context.PhotoFiles.CountAsync();

//            var byDate = _context.PhotoFiles
//                .AsNoTracking()
//                .ToLookup(p => p.CreateDate.Date);

//            _recordsToProcess = byDate.Count();

//            Parallel.ForEach(byDate, new ParallelOptions { CancellationToken = token }, async d =>
//            {
//                try
//                {
//                    var images = d.Where(i => i.PhotoPreview != null).Select(i => CreateLinkedImageEntities(i));
//                    long allSize = d.Sum(i => i.FileSize);

//                    var rec = new DiaryRecord
//                    {
//                        Date = d.Key,
//                        Name = $"{images.Count()} photos ({FileSize.ToString(allSize)})",
//                        CreateDate = DateTime.Now,
//                        ModifyDate = DateTime.Now,
//                        ImagesRefs = images.Select(i => new DiaryRecordImage { DiaryImage = i }).ToList()
//                    };

//                    var dContext = CreateDiaryContext();
//                    await dContext.AddAsync(rec);
//                    await dContext.SaveChangesAsync();

//                    Interlocked.Increment(ref _processedRecords);
//                    Interlocked.Add(ref _processedPhotos, d.Count());
//                }
//                catch (Exception exc)
//                {
//                    await Clients.All.SendAsync("ShowServerError", exc.Message);
//                }
//            });

//            _context.PhotoFiles.RemoveRange(_context.PhotoFiles);
//            await _context.SaveChangesAsync();

//            await Clients.All.SendAsync("ShowImportResult");
//        }

//        private DiaryImage CreateLinkedImageEntities(PhotoFileInfo info)
//        {
//            var fullImage = new DiaryImageFull
//            {
//                Data = info.PhotoPreview
//            };

//            return new DiaryImage
//            {
//                CreateDate = info.CreateDate,
//                FullImage = fullImage,
//                Height = info.Height,
//                Width = info.Width,
//                ModifyDate = info.AddToBaseDate,
//                Name = info.FileName,
//                SizeByte = (int)info.FileSize,
//                Thumbnail = ImageHelper.ScaleImage(info.PhotoPreview)
//            };
//        }
//    }
//}
