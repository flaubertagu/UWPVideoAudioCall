using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Media.Capture;
using System.Threading.Tasks;
using Windows.System.Display;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.Media.MediaProperties;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Storage.Streams;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace VideoSenderClientApp.Views
{
    public sealed partial class VideoChat : UserControl
    {
        MediaCapture videoCapture;
        bool isVideoPreviewing;
        DisplayRequest videoDisplayRequest = new DisplayRequest();
        public VideoChat()
        {
            this.InitializeComponent();
        }

        private bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;
        
        private async void MainGrid_Loaded(object sender, RoutedEventArgs e)
        {
          
            try
            {
                await StartPreviewAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
               
        private async void SwitchCamera_Click(object sender, RoutedEventArgs e)
        {
            if (SwitchCamera.IsChecked ?? false)
            {
                await StartPreviewAsync();
            }
        }

        public CaptureElement videocaptureElement = new CaptureElement();
        private async Task StartPreviewAsync()
        {
            try
            {
                videoCapture = new MediaCapture();
                await videoCapture.InitializeAsync(new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Video
                });                

                videoDisplayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                // This will be thrown if the user denied access to the camera in privacy settings
                // ShowMessageToUser("The app was denied access to the camera");
                return;
            }

            try
            {
                OwnCamera.Source = videoCapture;
                await videoCapture.StartPreviewAsync();
                isVideoPreviewing = true;
            }
            catch (FileLoadException)
            {
                videoCapture.CaptureDeviceExclusiveControlStatusChanged += _videoCapture_CaptureDeviceExclusiveControlStatusChanged;
            }

        }

        private async void _videoCapture_CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                // ShowMessageToUser("The camera preview can't be displayed because another app has exclusive access");
            }
            else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !isVideoPreviewing)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await StartPreviewAsync();
                });
            }
        }

        private async void ScreenShot_Click(object sender, RoutedEventArgs e)
        {
            var lowLagCapture = await videoCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));

            var capturedPhoto = await lowLagCapture.CaptureAsync();
            var softwareBitmap = capturedPhoto.Frame.SoftwareBitmap;

            await lowLagCapture.FinishAsync();
        }



        static bool isStreamingOut = false;
        static Queue<VideoFrame> VideoFramesQueue = new Queue<VideoFrame>();
        int delayMilliSeconds = 20;
        private void RecordVideo_Click(object sender, RoutedEventArgs e)
        {
            isStreamingOut = RecordVideo.IsChecked ?? false;

            if (isStreamingOut)
            {
                _ = SaveFrames();
                _ = StreamOutFrames();
            }
        }

        private async Task SaveFrames()
        {
            while (isStreamingOut)
            {
                try
                {
                    // Get information about the preview
                    var videoPreviewProperties = videoCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

                    // Create a video frame in the desired format for the preview frame
                    VideoFrame videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)videoPreviewProperties.Width, (int)videoPreviewProperties.Height);

                    VideoFramesQueue.Enqueue(await videoCapture.GetPreviewFrameAsync(videoFrame));
                    await Task.Delay(delayMilliSeconds);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
        } 
        private async Task StreamOutFrames()
        {
            try
            {
                while (isStreamingOut)
                {
                    VideoFramesQueue.TryDequeue(out VideoFrame frame);

                    if (frame == null)
                    {
                        await Task.Delay(delayMilliSeconds);
                        continue;
                    }

                    var memoryRandomAccessStream = new InMemoryRandomAccessStream();

                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, memoryRandomAccessStream);

                    // Set the software bitmap
                    encoder.SetSoftwareBitmap(frame.SoftwareBitmap);
                    //encoder.BitmapTransform.ScaledWidth = 320;
                    //encoder.BitmapTransform.ScaledHeight = 240;
                    //encoder.BitmapTransform.Rotation = Windows.Graphics.Imaging.BitmapRotation.Clockwise90Degrees;
                    //encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                    encoder.IsThumbnailGenerated = false;
                    await encoder.FlushAsync();


                    try
                    {
                        var videoArray = new byte[memoryRandomAccessStream.Size];
                        await memoryRandomAccessStream.ReadAsync(videoArray.AsBuffer(), (uint)memoryRandomAccessStream.Size, InputStreamOptions.None);

                        if (videoArray.Any())
                            await SignalRConn.connection.InvokeAsync("UploadStream", videoArray);

                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }

                    await Task.Delay(5);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            VideoFramesQueue.Clear();
        }        
    }
}
