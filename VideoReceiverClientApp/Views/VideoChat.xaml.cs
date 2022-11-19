using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using Windows.UI.Core;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using System.IO;
using Windows.Media.Playback;
using Windows.Media.Core;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace VideoReceiverClientApp
{
    public sealed partial class VideoChat : UserControl
    {
        public VideoChat()
        {
            this.InitializeComponent();
        }

        // FOR AUDIO STREAMING
        private StorageFile filePlay { get; set; }
        private StorageFolder storageFolder { get; set; } = ApplicationData.Current.LocalFolder;
        private static string audioPlayFileName { get; set; } = "audioPlayFile.mp3";
        private MediaPlayer mediaPlayer { get; set; } = new MediaPlayer();

        // FOR VIDEO STREAMING
        static bool isStreamingIn { get; set; } = false;
        static Queue<byte[]> StreamedArraysQueue { get; set; } = new Queue<byte[]>();

        private async void StreamVideo_Click(object sender, RoutedEventArgs e)
        {
            isStreamingIn = StreamVideo.IsChecked ?? false;

            if (isStreamingIn)
            {
                InitAudioStream();
                InitVideoStream();

                if (SignalRConn.connection.State == HubConnectionState.Disconnected)
                    await SignalRConn.connection.StartAsync();
            }
            else
                await SignalRConn.connection.StopAsync();
        }

        #region Play audio stream
        private void InitAudioStream()
        {
            SignalRConn.connection.On<byte[]>("DownloadAudioStream", async (stream) =>
            {
                try
                {
                    if (stream != null)
                    {
                        await PlayAudioStream(stream);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            });
        }

        private async Task PlayAudioStream(byte[] audioStream)
        {
            try
            {
                byte[] audioToPlay = await GetBytesFromFile(filePlay);
                if (audioToPlay != null)
                {
                    await filePlay.DeleteAsync();
                }

                if (audioStream != null)
                {
                    filePlay = await storageFolder.CreateFileAsync(audioPlayFileName, CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteBytesAsync(filePlay, audioStream);

                    mediaPlayer = new MediaPlayer();
                    mediaPlayer.Source = MediaSource.CreateFromStorageFile(filePlay);
                    mediaPlayer.Play();
                }
            }
            catch (Exception)
            {
            }
        }

        public async Task<byte[]> GetBytesFromFile(StorageFile file)
        {
            byte[] bytes = null;
            try
            {
                var stream = await file.OpenStreamForReadAsync();
                bytes = new byte[stream.Length];
                await stream.ReadAsync(bytes, 0, bytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                return bytes;
            }
            catch (Exception)
            {
                return bytes;
            }
        }
        #endregion

        #region Play video stream
        private void InitVideoStream()
        {
            SignalRConn.connection.On<byte[]>("DownloadVideoStream", (stream) =>
            {
                _ = this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (isStreamingIn)
                        StreamedArraysQueue.Enqueue(stream);
                    _ = BuildImageFrames();
                });
            });
        }

        private async Task BuildImageFrames()
        {

            while (isStreamingIn)
            {
                await Task.Delay(5);

                StreamedArraysQueue.TryDequeue(out byte[] buffer);

                if (!(buffer?.Any() ?? false))
                    continue;

                try
                {


                    var randomAccessStream = new InMemoryRandomAccessStream();
                    await randomAccessStream.WriteAsync(buffer.AsBuffer());
                    randomAccessStream.Seek(0);
                    await randomAccessStream.FlushAsync();

                    var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);

                    var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                    var imageSource = await ConvertToSoftwareBitmapSource(softwareBitmap);

                    ImageVideo.Source = imageSource;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
        }

        public async Task<SoftwareBitmapSource> ConvertToSoftwareBitmapSource(SoftwareBitmap softwareBitmap)
        {
            var displayableImage = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            SoftwareBitmapSource bitmapSource = new SoftwareBitmapSource();
            await bitmapSource.SetBitmapAsync(displayableImage);

            return bitmapSource;
        }
        #endregion
    }
}
