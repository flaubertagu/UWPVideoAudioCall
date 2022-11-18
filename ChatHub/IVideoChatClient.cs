using System.Threading.Tasks;

namespace ChatHub
{
    public interface IVideoChatClient
    {
        Task DownloadVideoStream(byte[] stream);
        Task DownloadAudioStream(byte[] stream);
    }
}