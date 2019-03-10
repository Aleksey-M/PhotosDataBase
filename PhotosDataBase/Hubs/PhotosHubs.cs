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
  
        public void StartLoadingPhotos(string dir)
        {
            AppCurrentState.SetPath(dir);                    
        }

        public void CancelLoadingPhotos()
        {
            AppCurrentState.StopWorking();         
        }
    }
}
