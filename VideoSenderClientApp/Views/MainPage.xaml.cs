using Windows.UI.Xaml.Controls;

// COPYRIGHT NOTIFICATION
// THIS PROJECT HAS BEEN MADE BASED ON THE PROJECT CREATED BY Guille1878
// THE ORIGINAL PROJECT IS POSTED ON https://github.com/Guille1878/VideoChat
// ADDITIONNAL CODE FOR THIS PROJECT ARE ONLY RELATED TO AUDIORECORD
// NEW FEATURES =>
// (1) AUDIO RECORD
// (2) STREAM OUT AUDIO RECORDED TO SIGNALR
// (3) PLAY AUDIO RECORD FROM Byte[] FROM SIGNALR (not fully developped)
// ANY HELP ON PLAYING THE AUDIO RECORD WILL Be HIGHLY APPRECIATED

namespace VideoSenderClientApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        { 
            this.InitializeComponent();
            TextBoxUserName.Text = UserService.User;
        }

        private void TextBoxUserName_TextChanged(object sender, TextChangedEventArgs e) => UserService.User = TextBoxUserName.Text;

        private async void Grid_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            SignalRConn.init();
            await SignalRConn.connection.StartAsync();
        }
    }
}
