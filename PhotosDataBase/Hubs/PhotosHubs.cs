using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using PhotosDataBase.Model;
using System;
using System.Threading.Tasks;

namespace PhotosDataBase.Hubs
{
    public class PhotosClientHub : Hub
    {
    }

    public class PhotosServerHub : Hub
    {
        private readonly IServiceProvider _serviceProvider;
        public PhotosServerHub(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
               
        public void StartLoadingPhotos(string dir)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var worker = scope.ServiceProvider.GetRequiredService<PhotosImportWorker>();
                worker.SetImportFolder(dir);
            }                         
        }

        public void CancelLoadingPhotos()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var worker = scope.ServiceProvider.GetRequiredService<PhotosImportWorker>();
                worker.CancelWork();                
            }            
        }
    }
}
