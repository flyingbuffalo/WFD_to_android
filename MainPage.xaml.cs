using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.Foundation.Collections;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using System.Threading;
using Windows.UI.Core;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.System.Threading;
using System.Diagnostics;

using System.Collections.Generic;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;
using Buffalo.WiFiDirect;

// 빈 페이지 항목 템플릿에 대한 설명은 http://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace ShareWith
{
    /// <summary>
    /// 자체에서 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        DeviceInformationCollection devInfoCollection = null;
        Windows.Devices.WiFiDirect.WiFiDirectDevice wfdDevice;

        public MainPage()
        {
            this.InitializeComponent();

        }

        private async void GetDevices(object sender, RoutedEventArgs e)
        {
            //manager.getDevicesAsync();
            TextMessage.Text = "Finding Devices...";

            devInfoCollection = null;

            ComboDevicesList.Items.Clear();

            String deviceSelector = Windows.Devices.WiFiDirect.WiFiDirectDevice.GetDeviceSelector();
            devInfoCollection = await DeviceInformation.FindAllAsync(deviceSelector);

            if (devInfoCollection.Count == 0) TextMessage.Text = "Not Found.";
            else
            {
                foreach (var devInfo in devInfoCollection)
                {
                    ComboDevicesList.Items.Add(devInfo.Name);
                }
                ComboDevicesList.SelectedIndex = 0;

                TextMessage.Text = "Found " + devInfoCollection.Count;
            }
        }

        private async void Connect(object sender, RoutedEventArgs e)
        {
            /*selectedDevice = devList[ComboDevicesList.SelectedIndex];

            TextMessage.Text = "Connect to " + selectedDevice.Name;
            manager.pairAsync(selectedDevice);*/

            DeviceInformation chosenDevInfo = null;
            EndpointPair endpointPair = null;
            try
            {
                // If nothing is selected, return
                chosenDevInfo = devInfoCollection[ComboDevicesList.SelectedIndex];

                TextMessage.Text = "Connect to " + chosenDevInfo.Name;

                // Connect to the selected WiFiDirect device
                wfdDevice = await Windows.Devices.WiFiDirect.WiFiDirectDevice.FromIdAsync(chosenDevInfo.Id);

                if (wfdDevice == null)
                {
                    TextMessage.Text = "Connect Fail";
                    return;
                }

                // Register for Connection status change notification
                wfdDevice.ConnectionStatusChanged += new TypedEventHandler<Windows.Devices.WiFiDirect.WiFiDirectDevice, object>(DisconnectNotification);

                // Get the EndpointPair collection
                var EndpointPairCollection = wfdDevice.GetConnectionEndpointPairs();
                if (EndpointPairCollection.Count > 0)
                {
                    endpointPair = EndpointPairCollection[0];
                }
                else
                {
                    TextMessage.Text = "Connection to " + chosenDevInfo.Name + " failed.";
                    return;
                }

                TextMessage.Text = "Connection Succeeded with " + endpointPair.RemoteHostName.ToString();

                //Server Socket open
                StreamSocketListener listener = new StreamSocketListener();
                listener.ConnectionReceived += OnConnection;

                // Start listen operation.
                try
                {
                    await listener.BindServiceNameAsync("9190");
                    TextMessage.Text = "Listening";

                }
                catch (Exception exception)
                {
                    // If this is an unknown status it means that the error is fatal and retry will likely fail.
                    if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                    {
                        throw;
                    }

                    TextMessage.Text = 
                        "Start listening failed with error: " + exception.Message;
                }

            }
            catch (Exception err)
            {
                TextMessage.Text = "Connection to " + chosenDevInfo.Name + " failed: " + err.Message;
            }

        }

        private async void OnConnection(StreamSocketListener sender,
                StreamSocketListenerConnectionReceivedEventArgs args)
        {
            
        }

        private void DisconnectNotification(object sender, object arg)
        {

        }


    }
}
