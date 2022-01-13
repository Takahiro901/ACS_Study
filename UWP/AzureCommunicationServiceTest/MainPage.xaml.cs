using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Azure.WinRT.Communication;
using Azure.Communication.Calling;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace AzureCommunicationServiceTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        CallAgent callAgent;
        Call call;
        DeviceManager deviceManager;
        LocalVideoStream[] localVideoStream;

        public MainPage()
        {
            this.InitializeComponent();
            this.InitCallAgentAndDeviceManager();
        }

        private async void InitCallAgentAndDeviceManager()
        {
            CallClient callClient = new CallClient();
            deviceManager = await callClient.GetDeviceManager();

            CommunicationTokenCredential token_credential = new CommunicationTokenCredential(Constants.TokenCredential);

            CallAgentOptions callAgentOptions = new CallAgentOptions()
            {
                DisplayName = "TAS"
            };
            callAgent = await callClient.CreateCallAgent(token_credential, callAgentOptions);
            callAgent.OnCallsUpdated += Agent_OnCallsUpdated;
            callAgent.OnIncomingCall += Agent_OnIncomingCall;
        }

        private async void CallButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            Debug.Assert(deviceManager.Microphones.Count > 0);
            Debug.Assert(deviceManager.Speakers.Count > 0);
            Debug.Assert(deviceManager.Cameras.Count > 0);

            if (deviceManager.Cameras.Count > 0)
            {
                VideoDeviceInfo videoDeviceInfo = deviceManager.Cameras[0];
                localVideoStream = new LocalVideoStream[1];
                localVideoStream[0] = new LocalVideoStream(videoDeviceInfo);

                Uri localUri = await localVideoStream[0].CreateBindingAsync();

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    LocalVideo.Source = localUri;
                    LocalVideo.Play();
                });

            }

            StartCallOptions startCallOptions = new StartCallOptions();
            startCallOptions.VideoOptions = new VideoOptions(localVideoStream);
            ICommunicationIdentifier[] callees = new ICommunicationIdentifier[1]
            {
                new CommunicationUserIdentifier(CalleeTextBox.Text)
            };

            call = await callAgent.StartCallAsync(callees, startCallOptions);
        }

        private async void Agent_OnIncomingCall(object sender, IncomingCall incomingcall)
        {
            Debug.Assert(deviceManager.Microphones.Count > 0);
            Debug.Assert(deviceManager.Speakers.Count > 0);
            Debug.Assert(deviceManager.Cameras.Count > 0);

            if (deviceManager.Cameras.Count > 0)
            {
                VideoDeviceInfo videoDeviceInfo = deviceManager.Cameras[0];
                localVideoStream = new LocalVideoStream[1];
                localVideoStream[0] = new LocalVideoStream(videoDeviceInfo);

                Uri localUri = await localVideoStream[0].CreateBindingAsync();

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    LocalVideo.Source = localUri;
                    LocalVideo.Play();
                });

            }
            AcceptCallOptions acceptCallOptions = new AcceptCallOptions();
            acceptCallOptions.VideoOptions = new VideoOptions(localVideoStream);

            call = await incomingcall.AcceptAsync(acceptCallOptions);
        }

        private async void Agent_OnCallsUpdated(object sender, CallsUpdatedEventArgs args)
        {
            foreach (var call in args.AddedCalls)
            {
                foreach (var remoteParticipant in call.RemoteParticipants)
                {
                    await AddVideoStreams(remoteParticipant.VideoStreams);
                    remoteParticipant.OnVideoStreamsUpdated += async (s, a) => await AddVideoStreams(a.AddedRemoteVideoStreams);
                }
                call.OnRemoteParticipantsUpdated += Call_OnRemoteParticipantsUpdated;
                call.OnStateChanged += Call_OnStateChanged;
            }
        }

        private async void HangupButton_Click(object sender, RoutedEventArgs e)
        {
            var hangUpOptions = new HangUpOptions();
            await call.HangUpAsync(hangUpOptions);
        }

        private async void Call_OnRemoteParticipantsUpdated(object sender, ParticipantsUpdatedEventArgs args)
        {
            foreach (var remoteParticipant in args.AddedParticipants)
            {
                await AddVideoStreams(remoteParticipant.VideoStreams);
                remoteParticipant.OnVideoStreamsUpdated += async (s, a) => await AddVideoStreams(a.AddedRemoteVideoStreams);
            }
        }

        private async Task AddVideoStreams(IReadOnlyList<RemoteVideoStream> streams)
        {

            foreach (var remoteVideoStream in streams)
            {
                var remoteUri = await remoteVideoStream.CreateBindingAsync();

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    RemoteVideo.Source = remoteUri;
                    RemoteVideo.Play();
                });
                remoteVideoStream.Start();
            }
        }

        private async void Call_OnStateChanged(object sender, PropertyChangedEventArgs args)
        {
            switch (((Call)sender).State)
            {
                case CallState.Disconnected:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        LocalVideo.Source = null;
                        RemoteVideo = null;
                    });
                    break;
                default:
                    Debug.WriteLine(((Call)sender).State);
                    break;
            }
        }
    }
}
