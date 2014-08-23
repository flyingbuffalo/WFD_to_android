//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//////
//*********************************************************

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using SDKTemplate;
using System;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;

namespace WiFiDirectDeviceScenario
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WiFiDirectDeviceScenario : SDKTemplate.Common.LayoutAwarePage
    {
        // A pointer back to the main page.  This is needed if you want to call methods in MainPage such
        // as rootPage.NotifyUser()
        MainPage rootPage = MainPage.Current;        

        DeviceInformationCollection devInfoCollection;
        Windows.Devices.WiFiDirect.WiFiDirectDevice wfdDevice;

        public WiFiDirectDeviceScenario()
        {
            this.InitializeComponent();

            GetDevicesButton.Visibility = Visibility.Visible; 
        }

        // This gets called when we receive a disconnect notification
        private void DisconnectNotification(object sender, object arg)
        {
            rootPage.NotifyUser("WiFiDirect device disconnected", NotifyType.ErrorMessage);

            var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                GetDevicesButton.Visibility = Visibility.Visible;

                PCIpAddress.Visibility = Visibility.Collapsed;
                DeviceIpAddress.Visibility = Visibility.Collapsed;
                SendTextButton.Visibility = Visibility.Collapsed;
                DisconnectButton.Visibility = Visibility.Collapsed;

                // Clear the FoundDevicesList
                FoundDevicesList.Visibility = Visibility.Collapsed;
                FoundDevicesList.Items.Clear();
            });

            devInfoCollection = null;
        }

        async void Connect(object sender, RoutedEventArgs e)
        {
            rootPage.NotifyUser("", NotifyType.ErrorMessage);
            DeviceInformation chosenDevInfo = null;
            EndpointPair endpointPair = null;
            try
            {
                // If nothing is selected, return
                if (FoundDevicesList.SelectedIndex == -1)
                {
                    rootPage.NotifyUser("Please select a device", NotifyType.StatusMessage);
                    return;
                }
                else
                {
                    chosenDevInfo = devInfoCollection[FoundDevicesList.SelectedIndex];
                }

                rootPage.NotifyUser("Connecting to " + chosenDevInfo.Name + "....", NotifyType.StatusMessage);


                // Connect to the selected WiFiDirect device
                wfdDevice = await Windows.Devices.WiFiDirect.WiFiDirectDevice.FromIdAsync(chosenDevInfo.Id);

                if (wfdDevice == null)
                {
                    rootPage.NotifyUser("Connection to " + chosenDevInfo.Name + " failed.", NotifyType.StatusMessage);
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
                    rootPage.NotifyUser("Connection to " + chosenDevInfo.Name + " failed.", NotifyType.StatusMessage);
                    return;
                }

                PCIpAddress.Text = "PC's IP Address: " + endpointPair.LocalHostName.ToString();
                DeviceIpAddress.Text =  "Device's IP Address: " + endpointPair.RemoteHostName.ToString();
                PCIpAddress.Visibility = Visibility.Visible;
                DeviceIpAddress.Visibility = Visibility.Visible;
                //SendTextButton.Visibility = Visibility.Visible;
                DisconnectButton.Visibility = Visibility.Visible;
                ConnectButton.Visibility = Visibility.Collapsed;
                FoundDevicesList.Visibility = Visibility.Collapsed;
                GetDevicesButton.Visibility = Visibility.Collapsed;

                rootPage.NotifyUser("Connection succeeded", NotifyType.StatusMessage);

                //Server Socket open
                StreamSocketListener listener = new StreamSocketListener();
                listener.ConnectionReceived += OnConnection;

                // Start listen operation.
                try
                {   
                    // bind to any
                    // Don't limit traffic to an address or an adapter.
                    await listener.BindServiceNameAsync("9190");
                    rootPage.NotifyUser("Listening", NotifyType.StatusMessage);
                    
                    // bind to specific address
                    // Try to bind to a specific address.
                    //BindEndpointAsync(Target IPAddr, Port Number)
                    //HostName DeviceIPHostName = new HostName(endpointPair.RemoteHostName.ToString());
                    //await listener.BindEndpointAsync(DeviceIPHostName, "9190");
                    //rootPage.NotifyUser(
                     //   "Listening on address " + DeviceIPHostName.ToString(), NotifyType.StatusMessage);

                }
                catch (Exception exception)
                {
                    // If this is an unknown status it means that the error is fatal and retry will likely fail.
                    if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                    {
                        throw;
                    }

                    rootPage.NotifyUser(
                        "Start listening failed with error: " + exception.Message, NotifyType.ErrorMessage);
                }
                
            }
            catch (Exception err)
            {
                rootPage.NotifyUser("Connection to " + chosenDevInfo.Name + " failed: " + err.Message, NotifyType.ErrorMessage);
            }
        }

        /// <summary>
        /// Invoked once a connection is accepted by StreamSocketListener.
        /// </summary>
        /// <param name="sender">The listener that accepted the connection.</param>
        /// <param name="args">Parameters associated with the accepted connection.</param>
        private async void OnConnection(StreamSocketListener sender,
                StreamSocketListenerConnectionReceivedEventArgs args)
        {
            //소켓연결됬을때 함수
            //ConnectButton.Visibility = Visibility.Visible;
            SendTextSocket = args.Socket;
        
        }

        StreamSocket SendTextSocket = null;

        async void SendText(object sender, RoutedEventArgs e)
        {
            rootPage.NotifyUser("Send Text to connected device.", NotifyType.StatusMessage);

            if (SendTextSocket != null)
            {
                //send text
                DataWriter dataWriter = new DataWriter(SendTextSocket.OutputStream);

                try
                {
                    // Write first the length of the string as UINT32 value followed up by the string. 
                    // Writing data to the writer will just store data in memory.
                    dataWriter.WriteString("Hello World\n");

                    // Write the locally buffered data to the network.
                    await dataWriter.StoreAsync();
                    await dataWriter.FlushAsync();
                    rootPage.NotifyUser("Send To Device Msg - Hello World", NotifyType.StatusMessage);
                }
                catch (Exception exception)
                {
                    // If this is an unknown status it means that the error is fatal and retry will likely fail.
                    if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                    {
                        throw;
                    }

                    rootPage.NotifyUser("Write stream failed with error: " + exception.Message,
                        NotifyType.ErrorMessage);
                }
            }
            else
            {
                rootPage.NotifyUser("Socket is NULL!",  NotifyType.ErrorMessage);
            }

        }

        void Disconnect(object sender, RoutedEventArgs e)
        {
            rootPage.NotifyUser("WiFiDirect device disconnected.", NotifyType.StatusMessage);

            if (SendTextSocket != null)
            {
                SendTextSocket.Dispose();
                SendTextSocket = null;
            }

            PCIpAddress.Visibility = Visibility.Collapsed;
            DeviceIpAddress.Visibility = Visibility.Collapsed;
            SendTextButton.Visibility = Visibility.Collapsed;
            DisconnectButton.Visibility = Visibility.Collapsed;
            ConnectButton.Visibility = Visibility.Collapsed;
            FoundDevicesList.Visibility = Visibility.Collapsed;
            GetDevicesButton.Visibility = Visibility.Visible;

            wfdDevice.Dispose();
        }

        async void GetDevices(object sender, RoutedEventArgs e)
        {                   
            try
            {
                rootPage.NotifyUser("Enumerating WiFiDirect devices...", NotifyType.StatusMessage);
                devInfoCollection = null;

                PCIpAddress.Visibility = Visibility.Collapsed;
                DeviceIpAddress.Visibility = Visibility.Collapsed;
                SendTextButton.Visibility = Visibility.Collapsed;
                DisconnectButton.Visibility = Visibility.Collapsed;
                ConnectButton.Visibility = Visibility.Visible;
                FoundDevicesList.Visibility = Visibility.Collapsed;

                SendTextButton.Visibility = Visibility.Visible;

                FoundDevicesList.Items.Clear();

                String deviceSelector = Windows.Devices.WiFiDirect.WiFiDirectDevice.GetDeviceSelector();
                devInfoCollection = await DeviceInformation.FindAllAsync(deviceSelector);

                if (devInfoCollection.Count == 0)
                {
                    rootPage.NotifyUser("No WiFiDirect devices found.", NotifyType.StatusMessage);
                }
                else
                {
                    foreach (var devInfo in devInfoCollection)
                    {
                        FoundDevicesList.Items.Add(devInfo.Name);
                    }
                    FoundDevicesList.SelectedIndex = 0;
                    FoundDevicesList.Visibility = Visibility.Visible;
                    
                    rootPage.NotifyUser("Enumerating WiFiDirect devices completed successfully.", NotifyType.StatusMessage);
                }
            }
            catch (Exception err)
            {
                rootPage.NotifyUser("Enumeration failed: " + err.Message, NotifyType.ErrorMessage);
            }

        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
                GetDevicesButton.Visibility = Visibility.Visible;
                ConnectButton.Visibility = Visibility.Collapsed;
                FoundDevicesList.Visibility = Visibility.Collapsed;
                PCIpAddress.Visibility = Visibility.Collapsed;
                DeviceIpAddress.Visibility = Visibility.Collapsed;
                SendTextButton.Visibility = Visibility.Collapsed;
                DisconnectButton.Visibility = Visibility.Collapsed;
        }

        // Invoked when the main page navigates to a different scenario
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
        }
    }
}
