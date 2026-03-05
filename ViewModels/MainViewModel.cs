using WinIsland.Core;
using WinIsland.Services.Media;

namespace WinIsland.ViewModels
{
    public class MainViewModel : ObservableObject, IDisposable
    {
        public MediaViewModel Media { get; }

        public MainViewModel()
        {
            // Initialize Services
            var mediaService = new MediaService();
            
            // Initialize ViewModels
            Media = new MediaViewModel(mediaService);
            
            // Start listening
            Media.Initialize();
        }

        public void Dispose()
        {
            Media.Dispose();
        }
    }
}
