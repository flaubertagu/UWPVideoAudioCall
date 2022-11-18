using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Pour en savoir plus sur le modèle d'élément Contrôle utilisateur, consultez la page https://go.microsoft.com/fwlink/?LinkId=234236

namespace VideoSenderClientApp.Views
{
    public sealed partial class AudioRecord : UserControl
    {
        public static MediaCapture audioCapture;
        MediaEncodingProfile profile;
        private InMemoryRandomAccessStream _memoryBuffer = new InMemoryRandomAccessStream();
        public AudioRecord()
        {
            this.InitializeComponent();
        }

        private void AudioGrid_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                InitAudioStream();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
        private async void AudioGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!isStreamingOut)
            {
                try
                {
                    await audioCapture.StopRecordAsync();
                    isStreamingOut = false;
                }
                catch (Exception)
                {
                }
            }
        }

        #region Play stream
        private void InitAudioStream()
        {
            SignalRConn.connection.On<byte[]>("DownloadAudioStream", async (stream) =>
            {
                try
                {
                    if (stream != null)
                    {
                        InMemoryRandomAccessStream memoryBuffer = await ConvertTo(stream);
                        Play(memoryBuffer);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            });
        }

        internal static async Task<InMemoryRandomAccessStream> ConvertTo(byte[] arr)
        {
            InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream();
            await randomAccessStream.WriteAsync(arr.AsBuffer());
            randomAccessStream.Seek(0); // Just to be sure.
                                        // I don't think you need to flush here, but if it doesn't work, give it a try.
            return randomAccessStream;
        }
        public void Play(InMemoryRandomAccessStream memoryBuffer)
        {
            /*
             * THIS CODE DOES NOT WORKS
             * THE ISSUE IDENTIFY IS THE FOLLOWING
             * THE AUDIO IS NOT PLAYED ON ANY TYPE OF SPEAKERS
             */
            MediaElement playbackMediaElement = new MediaElement();
            playbackMediaElement.SetSource(memoryBuffer, "MP3");
            playbackMediaElement.Play();
        }
        #endregion

        #region Record and streamout
        public static async Task StopRecording()
        {
            try
            {
                await audioCapture.StopRecordAsync();
                isStreamingOut = false;
            }
            catch (Exception)
            {
            }
        }

        public static bool isStreamingOut = false;
        static Queue<InMemoryRandomAccessStream> AudioFramesQueue = new Queue<InMemoryRandomAccessStream>();
        int delayMilliSeconds = 20;
        public int OutstreamPosition { get; set; } = 0;
        private async void RecordAudio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                isStreamingOut = RecordAudio.IsChecked ?? false;
                await Record();
                if (isStreamingOut)
                {
                    _ = StreamOutAudio();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        public async Task Record()
        {
            if (!isStreamingOut)
            {
                await StopRecording();
            }
            else
            {
                isStreamingOut = true;
                try
                {

                    MediaCaptureInitializationSettings settings =
                      new MediaCaptureInitializationSettings
                      {
                          StreamingCaptureMode = StreamingCaptureMode.Audio,
                          AudioProcessing = AudioProcessing.Raw,
                      };
                    audioCapture = new MediaCapture();
                    string deviceId = settings.AudioDeviceId;
                    await audioCapture.InitializeAsync(settings);
                    await audioCapture.StartRecordToStreamAsync(MediaEncodingProfile.CreateMp3(AudioEncodingQuality.Auto), _memoryBuffer);
                }
                catch (Exception ex)
                {
                    await StopRecording();
                    if (ex.InnerException != null && ex.InnerException.GetType() == typeof(UnauthorizedAccessException))
                    {
                        MessageDialog messageDialog = new MessageDialog($"Message:{ex.Message}\n\nInnerException:{ex.InnerException}");
                        await messageDialog.ShowAsync();
                    }
                    else
                    {
                        MessageDialog messageDialog = new MessageDialog("No microphone detected");
                        await messageDialog.ShowAsync();
                    }
                    throw;
                }
            }
        }
        private async Task StreamOutAudio()
        {
            while (isStreamingOut)
            {
                try
                {
                    byte[] finalArray = null;
                    var audioMemoryRandomAccessStream = _memoryBuffer;
                    var audioArray = new byte[audioMemoryRandomAccessStream.Size];
                    if (OutstreamPosition == 0)
                    {
                        finalArray = audioArray;
                    }
                    else if (OutstreamPosition > 0)
                    {
                        finalArray = audioArray.Skip(OutstreamPosition).ToArray();
                    }
                    if (finalArray.Any())
                    {
                        await SignalRConn.connection.InvokeAsync("UploadAudioStream", finalArray);
                    }
                    OutstreamPosition = audioArray.Length;
                    await Task.Delay(delayMilliSeconds);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
        }
        #endregion
    }
}