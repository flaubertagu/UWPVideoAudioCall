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
using Windows.Media.Core;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
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
        public AudioRecord()
        {
            this.InitializeComponent();
            SettingsGrid.Visibility = Visibility.Collapsed;
            RefreshGrid.Visibility = Visibility.Collapsed;
            RecordAudio.IsEnabled = false;
        }

        //--GRAPH--
        private AudioGraph graphRecord { get; set; }

        //--NODE--
        private AudioFileOutputNode fileOutputNodeRecord { get; set; }
        private AudioDeviceOutputNode deviceOutputNodeRecord { get; set; }
        private AudioDeviceInputNode deviceInputNodeRecord { get; set; }

        //-- INPUT AND OUTPUT DEVICES LIST
        private DeviceInformationCollection outputDevices { get; set; }
        private DeviceInformationCollection inputDevices { get; set; }

        //-- SELECTED INPUT AND OUTPUT DEVICE
        private DeviceInformation selectedOutputDevice { get; set; }
        private DeviceInformation selectedInputDevice { get; set; }

        //-- FILE RECORD PARAM
        private StorageFile fileRecord { get; set; }
        private StorageFolder storageFolder { get; set; } = ApplicationData.Current.LocalFolder;

        private static string audioRecordFileName { get; set; } = "audioRecordFile.mp3";
        private static string SpeakerName { get; set; } = string.Empty;
        private static string MicrophoneName { get; set; } = string.Empty;
        private static bool audioCanBeRecord { get; set; } = false;
        private static bool CanRecord { get; set; } = false;
        private static bool GraphSettingChanged { get; set; } = false;
        private static bool AudioFileRecordCreated { get; set; } = false;

        // ------  Automatically loaded when the page is launched  ------
        private async void AudioGrid_Loaded(object sender, RoutedEventArgs e)
        {
            await PopulateOutputDeviceList();
            await PopulateInputDeviceList();

            CanRecord = await CheckRecordSettings();
            if (CanRecord)
            {
                await AudioRecordSettings();
            }
        }

        private void AudioGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!isStreamingOut)
            {
                try
                {
                    isStreamingOut = false;
                }
                catch (Exception)
                {
                }
            }
        }

        #region Settings
        private async Task<bool> CheckRecordSettings()
        {
            bool canRecord = true;
            bool hasMicrophoneSetting = Check_MicrophoneSetting();
            if (hasMicrophoneSetting)
            {
                RecordAudio.IsEnabled = true;
            }
            else if (!hasMicrophoneSetting)
            {
                RecordAudio.IsEnabled = false;
                SettingsGrid.Visibility = Visibility.Visible;
                canRecord = false;
                MessageDialog errMsg = new MessageDialog($"Choose input first");
                await errMsg.ShowAsync();
            }
            return canRecord;
        }

        private bool Check_SpearkerSetting()
        {
            bool hasSpeakerSetting = true;
            SpeakerName = AppSettings.LoadAppSettings(AppSettings.SpeakerNameSettings);
            if (string.IsNullOrEmpty(SpeakerName))
            {
                hasSpeakerSetting = false;
            }
            else
            {
                Set_SpeakerSetting();
            }
            return hasSpeakerSetting;
        }

        private bool Check_MicrophoneSetting()
        {
            bool hasMicrophoneSetting = true;
            MicrophoneName = AppSettings.LoadAppSettings(AppSettings.MicrophoneNameSettings);
            if (string.IsNullOrEmpty(MicrophoneName))
            {
                hasMicrophoneSetting = false;
            }
            else
            {
                Set_MicrophoneSetting();
            }
            return hasMicrophoneSetting;
        }

        private async void Settings_Click(object sender, RoutedEventArgs e)
        {
            CanRecord = await CheckRecordSettings();
            if (CanRecord)
            {
                if (SettingsGrid.Visibility == Visibility.Visible)
                {
                    SettingsGrid.Visibility = Visibility.Collapsed;
                    RefreshGrid.Visibility = Visibility.Collapsed;
                }
                else if (SettingsGrid.Visibility == Visibility.Collapsed)
                {
                    SettingsGrid.Visibility = Visibility.Visible;
                    RefreshGrid.Visibility = Visibility.Visible;
                }
                await AudioRecordSettings();
            }
        }

        private async void RefreshAllDevices_Click(object sender, RoutedEventArgs e)
        {
            await PopulateOutputDeviceList();
            await PopulateInputDeviceList();
        }
        #endregion

        #region Speaker selection
        private async Task PopulateOutputDeviceList()
        {
            outputDevicesListBox.Items.Clear();
            outputDevices = await DeviceInformation.FindAllAsync(MediaDevice.GetAudioRenderSelector());
            outputDevicesListBox.Items.Add("-- Pick output device --");
            foreach (var device in outputDevices)
            {
                outputDevicesListBox.Items.Add(device.Name);
            }
        }

        private async void outputDevicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (outputDevicesListBox.SelectedIndex == 0)
            {
                // Destroy graphRecord
                ResetRecordGraph();
            }
            else
            {
                //To make sure the stream will not crash while changing settings
                CanRecord = false;
                AudioFileRecordCreated = false;

                SpeakerName = outputDevicesListBox.SelectedItem.ToString();
                AppSettings.SaveAppSettings(AppSettings.SpeakerNameSettings, SpeakerName);
                GraphSettingChanged = Set_SpeakerSetting();
                if (GraphSettingChanged)
                {
                    await AudioRecordSettings();
                    GraphSettingChanged = false;
                }
            }
        }

        private bool Set_SpeakerSetting()
        {
            bool changed = false;
            int count = 0;
            foreach (DeviceInformation device in outputDevices)
            {
                if (device.Name == SpeakerName)
                {
                    selectedOutputDevice = device;
                    count++;
                }
            }
            if (count > 0)
            {
                changed = true;
            }
            return changed;
        }
        #endregion

        #region Microphone selection
        private async Task PopulateInputDeviceList()
        {
            inputDevicesListBox.Items.Clear();
            inputDevices = await DeviceInformation.FindAllAsync(MediaDevice.GetAudioCaptureSelector());
            inputDevicesListBox.Items.Add("-- Pick input device --");
            foreach (var device in inputDevices)
            {
                inputDevicesListBox.Items.Add(device.Name);
            }
        }

        private async void inputDevicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (inputDevicesListBox.SelectedIndex == 0)
            {
                // Destroy graphRecord
                ResetRecordGraph();
            }
            else
            {
                //To make sure the stream will not crash while changing settings
                CanRecord = false;
                AudioFileRecordCreated = false;

                MicrophoneName = inputDevicesListBox.SelectedItem.ToString();
                AppSettings.SaveAppSettings(AppSettings.MicrophoneNameSettings, MicrophoneName);
                GraphSettingChanged = Set_MicrophoneSetting();
                if (GraphSettingChanged)
                {
                    await AudioRecordSettings();
                    GraphSettingChanged = false;
                }
            }
        }

        private bool Set_MicrophoneSetting()
        {
            bool changed = false;
            int count = 0;
            foreach (DeviceInformation device in inputDevices)
            {
                if (device.Name == MicrophoneName)
                {
                    selectedInputDevice = device;
                    count++;
                }
            }
            if (count > 0)
            {
                changed = true;
            }
            return changed;
        }
        #endregion

        #region GRAPH RECORD SET-UP OR REFRESH
        private async Task AudioRecordSettings()
        {
            ResetRecordGraph();
            bool recordGraphReady = await AudioRecordGraphCreation();
            if (recordGraphReady)
            {
                AudioFileRecordCreated = recordGraphReady;
            }
        }

        private void ResetRecordGraph()
        {
            if (graphRecord != null)
            {
                graphRecord.Dispose();
                graphRecord = null;
            }
        }
        #endregion

        #region Create Record Graph
        private async Task<bool> AudioRecordGraphCreation()
        {
            bool audioGraphReady = true;
            try
            {
                AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.Media);
                settings.QuantumSizeSelectionMode = QuantumSizeSelectionMode.LowestLatency;
                settings.PrimaryRenderDevice = selectedOutputDevice;

                CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

                if (result.Status != AudioGraphCreationStatus.Success)
                {
                    // Cannot create graphRecord
                    MessageDialog errMsg = new MessageDialog($"AudioGraph Creation Error");
                    await errMsg.ShowAsync();
                    audioGraphReady = false;
                    return audioGraphReady;
                }

                graphRecord = result.Graph;

                /*
                 * --- THIS PART OF THE CODE HAS BEEN COMMENT BECAUSE IT IS NO USE.
                 * --- THE PURPOSE OF THIS PART IS TO LISTEN TO WHAT YOU SAY WHILE RECORDING
                 * --- USELESS WHEN IT COMES TO CALLS
                //// Create a device output node
                //CreateAudioDeviceOutputNodeResult deviceOutputNodeResult = await graphRecord.CreateDeviceOutputNodeAsync();
                //if (deviceOutputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
                //{
                //    // Cannot create device output node
                //    MessageDialog errMsg = new MessageDialog($"Audio Device Output unavailable");
                //    await errMsg.ShowAsync();
                //    audioGraphReady = false;
                //    return audioGraphReady;
                //}

                //deviceOutputNodeRecord = deviceOutputNodeResult.DeviceOutputNode;
                */

                // Create a device input node using the default audio input device

                CreateAudioDeviceInputNodeResult deviceInputNodeResult = 
                    await graphRecord.CreateDeviceInputNodeAsync(MediaCategory.Other, graphRecord.EncodingProperties, selectedInputDevice);

                if (deviceInputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
                {
                    // Cannot create device input node
                    MessageDialog errMsg = new MessageDialog($"Audio Device Input unavailable");
                    await errMsg.ShowAsync();
                    audioGraphReady = false;
                    return audioGraphReady;
                }

                deviceInputNodeRecord = deviceInputNodeResult.DeviceInputNode;

                // Because we are using lowest latency setting, we need to handle device disconnection errors
                audioCanBeRecord = true;
                graphRecord.UnrecoverableErrorOccurred += AudioRecordGraph_UnrecoverableErrorOccurred;

                if (audioCanBeRecord)
                {
                    audioGraphReady = true;
                    return audioGraphReady;
                }
                else
                {
                    MessageDialog errMsg = new MessageDialog($"Audio graphRecord unrecoverable error");
                    await errMsg.ShowAsync();
                    audioGraphReady = false;
                    return audioGraphReady;
                }
            }
            catch (Exception ex)
            {
                MessageDialog errMsg = new MessageDialog($"{ex.Message}");
                await errMsg.ShowAsync();
                audioGraphReady = false;
                return audioGraphReady;
            }
        }

        private async void AudioRecordGraph_UnrecoverableErrorOccurred(AudioGraph sender, AudioGraphUnrecoverableErrorOccurredEventArgs args)
        {
            // Recreate the graphRecord and all nodes when this happens
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                sender.Dispose();
                // Re-query for devices
                await PopulateOutputDeviceList();
                await PopulateInputDeviceList();
                // Reset UI
                audioCanBeRecord = false;
            });
        }
        #endregion

        #region File OutPut
        private async Task<bool> CreateOutputFile()
        {
            bool fileCanBeUsed = true;
            try
            {
                // Create sample file; replace if exists.
                fileRecord = await storageFolder.CreateFileAsync(audioRecordFileName, CreationCollisionOption.ReplaceExisting);

                bool fileExist = await IsFilePresent(audioRecordFileName);
                if (!fileExist)
                {
                    MessageDialog errMsg = new MessageDialog($"{audioRecordFileName} failed to be created");
                    await errMsg.ShowAsync();
                    fileCanBeUsed = false;
                    return fileCanBeUsed;
                }

                MediaEncodingProfile fileProfile = CreateTheMediaEncodingProfile(fileRecord);

                // Operate node at the graphRecord format, but save file at the specified format
                CreateAudioFileOutputNodeResult fileOutputNodeResult = await graphRecord.CreateFileOutputNodeAsync(fileRecord, fileProfile);

                if (fileOutputNodeResult.Status != AudioFileNodeCreationStatus.Success)
                {
                    MessageDialog errMsg = new MessageDialog($"{audioRecordFileName} failed to create output file");
                    await errMsg.ShowAsync();
                    fileCanBeUsed = false;
                    return fileCanBeUsed;
                }

                fileOutputNodeRecord = fileOutputNodeResult.FileOutputNode;

                //// Connect the input node to both output nodes
                deviceInputNodeRecord.AddOutgoingConnection(fileOutputNodeRecord);
                //deviceInputNodeRecord.AddOutgoingConnection(deviceOutputNodeRecord);
                return fileCanBeUsed;
            }
            catch (Exception ex)
            {
                MessageDialog errMsg = new MessageDialog($"{ex.Message}");
                await errMsg.ShowAsync();
                fileCanBeUsed = false;
                return fileCanBeUsed;
            }
        }

        public async Task<bool> IsFilePresent(string fileName)
        {
            var item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(fileName);
            return item != null;
        }

        private MediaEncodingProfile CreateTheMediaEncodingProfile(StorageFile file)
        {
            switch (file.FileType.ToString().ToLowerInvariant())
            {
                case ".wma":
                    return MediaEncodingProfile.CreateWma(AudioEncodingQuality.High);
                case ".mp3":
                    return MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High);
                case ".wav":
                    return MediaEncodingProfile.CreateWav(AudioEncodingQuality.High);
                default:
                    throw new ArgumentException();
            }
        }
        #endregion

        #region Record
        public static bool isStreamingOut = false;
        public static bool isRecording = false;
        static Queue<InMemoryRandomAccessStream> AudioFramesQueue = new Queue<InMemoryRandomAccessStream>();
        static int delayMilliSeconds = 800; //The best preference to not have a bad soung quality and less gap between video and image
        static int waitMilliSeconds = 5;
        public int OutstreamPosition { get; set; } = 0;
        private async void RecordAudio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (audioCanBeRecord)
                {
                    isStreamingOut = RecordAudio.IsChecked ?? false;

                    if (isStreamingOut)
                    {
                        _ = RecordStreamOutAudio();
                    }
                    else
                    {
                        await Task.Delay(delayMilliSeconds);
                        await StopRecord();
                    }
                }
                else
                {
                    isStreamingOut = RecordAudio.IsChecked ?? true;

                    MessageDialog errMsg = new MessageDialog($"Audio input configuration failed");
                    await errMsg.ShowAsync();
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private static bool fileCanBeUsed { get; set; } = false;
        private async Task RecordStreamOutAudio()
        {
            while (isStreamingOut)
            {
                if (CanRecord && AudioFileRecordCreated)
                {
                    fileCanBeUsed = await CreateOutputFile();
                    if (fileCanBeUsed)
                    {
                        StartRecord();
                        await Task.Delay(delayMilliSeconds);
                        await StopRecord();
                    }
                }
                else
                {
                    CanRecord = await CheckRecordSettings();
                    await AudioRecordSettings();
                }
            }
        }

        private void StartRecord()
        {
            graphRecord.Start();
        }

        private async Task StopRecord()
        {
            try
            {
                // Good idea to stop the graphRecord to avoid data loss
                graphRecord.Stop();

                TranscodeFailureReason finalizeResult = await fileOutputNodeRecord.FinalizeAsync();
                if (finalizeResult != TranscodeFailureReason.None)
                {
                    MessageDialog errMsg = new MessageDialog($"Record error - finalization of file failed");
                    await errMsg.ShowAsync();
                    return;
                }
                else
                {
                    byte[] audioStream = await GetBytesFromFile(fileRecord);
                    if (audioStream != null)
                    {
                        await SignalRConn.connection.InvokeAsync("UploadAudioStream", audioStream);
                    }
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
    }
}