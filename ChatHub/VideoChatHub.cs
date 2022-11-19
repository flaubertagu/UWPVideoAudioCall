using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace ChatHub
{
    public class VideoChatHub : Hub<IVideoChatClient> 
    {

        public async Task UploadStream(byte[] stream)
        {
            await Clients.All.DownloadVideoStream(stream);
            Console.WriteLine($"{stream.Length}");
        }

        public async Task UploadAudioStream(byte[] stream)
        {
            //Console.WriteLine($"------\nInit Updload");
            //ConsoleDateTime();
            await Clients.All.DownloadAudioStream(stream);
            //Console.WriteLine($"------\nExecute download");
            Console.WriteLine($"{stream.Length}");
            //ConsoleDateTime();
        }

        private void ConsoleDateTime()
        {
#if DEBUG
            Console.WriteLine($"DateTime: {DateTime.Now.ToString("yyyy:MM:ss")}\n------");
#endif
        }
    }
}
