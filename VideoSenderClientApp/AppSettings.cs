using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;
using Windows.Storage;

namespace VideoSenderClientApp
{
    public static class AppSettings
    {
        public static DeviceInformationCollection outputDevices;
        public static DeviceInformationCollection inputDevices;

        public static DeviceInformation selectedOutputDevice;
        public static DeviceInformation selectedInputDevice;

        public static string SpeakerName { get; set; } = string.Empty;
        public static string MicrophoneName { get; set; } = string.Empty;
        public static bool audioRecordStatus { get; set; } = false;
        public static bool audioPlayStatus { get; set; } = false;
        public static bool CanRecord { get; set; } = false;
        public static bool AudioFileRecordCreated { get; set; } = false;
        public static bool AudioFilePlayCreated { get; set; } = false;

        private static ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        public static string SpeakerNameSettings { get; set; } = "SpeakerName";
        public static string MicrophoneNameSettings { get; set; } = "MicrophoneName";

        public static int delayMilliSeconds = 20;

        public static void SaveAppSettings(string settings, string value)
        {
            localSettings.Values[settings] = value;
        }
        
        public static string LoadAppSettings(string settings)
        {
            string value = localSettings.Values[settings] as string;
            return value;
        }
    }
}
