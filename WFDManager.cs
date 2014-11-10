using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.System.Threading;
using Windows.Devices.WiFiDirect;
using Windows.Devices.Enumeration;
using System.Diagnostics;


namespace Buffalo.WiFiDirect
{
    public class WFDManager
    {
        private WFDDeviceDiscoveredListener wfdDeviceDiscoveredListener;
        private WFDDeviceConnectedListener wfdDeviceConnectedListener;
        private WFDPairInfo.PairSocketConnectedListener wfdPairSocketConnectedListener;

        private readonly DependencyObject parent;

        public void setWFDDeviceDiscoveredListener(WFDDeviceDiscoveredListener wfdDeviceDiscoveredListener)
        {
            this.wfdDeviceDiscoveredListener = wfdDeviceDiscoveredListener;
        }

        public void setWFDDeviceConnectedListener(WFDDeviceConnectedListener wfdDeviceConnectedListener)
        {
            this.wfdDeviceConnectedListener = wfdDeviceConnectedListener;
        }


        public WFDManager(DependencyObject parent,
                          WFDDeviceDiscoveredListener wfdDeviceDiscoveredListener,
                          WFDDeviceConnectedListener wfdDeviceConnectedListener,
                          WFDPairInfo.PairSocketConnectedListener wfdPairSocketConnectedListener
            )
        {
            this.parent = parent;
            setWFDDeviceConnectedListener(wfdDeviceConnectedListener);
            setWFDDeviceDiscoveredListener(wfdDeviceDiscoveredListener);

        }

        //private delegate void WorkItemHandler(IAsyncAction operation);

        public void getDevicesAsync()
        {
            List<WFDDevice> wfdList = new List<WFDDevice>();

            IAsyncAction asyncAction = ThreadPool.RunAsync( async (workItem) =>
            {
                /*to Android*/
                string wfdSelector = Windows.Devices.WiFiDirect.WiFiDirectDevice.GetDeviceSelector();
                DeviceInformationCollection devInfoCollection = await DeviceInformation.FindAllAsync(wfdSelector);


                foreach (DeviceInformation devInfo in devInfoCollection)
                { /* to Android */
                    wfdList.Add(new WFDDevice(devInfo));
                }

                
                /*비동기 작업이 취소되면 wfdList를 clear한다*/
                if (workItem.Status == AsyncStatus.Canceled)
                {
                    wfdList.Clear();
                }


                await parent.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        //call callback
                        //WFDDeviceDiscoverdListner.onDevicesDiscovered를 통해 wfdList를 리턴한다
                        wfdDeviceDiscoveredListener.onDevicesDiscovered(wfdList);
                    });        
                
                /*CoreWindow.GetForCurrentThread().Dispatcher.RunAsync
                    (CoreDispatcherPriority.Normal, () =>
                    {
                        l.onDevicesDiscovered(wfdList);
                    });*/
            });

            //onDevicesDiscoverFailed() 추가해야함
        }


        public delegate void PairingAsyncDelegate(WFDDevice device);
        public void pairAsync(WFDDevice device)
        {
            PairingAsyncDelegate pairing = new PairingAsyncDelegate(pairAsyncRun);
            pairing(device);
        }

        //private WFDDeviceConnectedListener connectedListener = null;
        /*
         * @param device : 연결하고자 하는 WFDDevice
         */
        Windows.Devices.WiFiDirect.WiFiDirectDevice wfdDevice = null;
        public async void pairAsyncRun(WFDDevice device)
        {
            //IAsyncAction asyncAction = ThreadPool.RunAsync( async (workItem) =>
            //parent.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, async () =>
            //{
                DeviceInformation devInfo = null;
                
                if (device.IsDevice)
                { /*to Android*/
                    devInfo = (DeviceInformation)device.WFDDeviceInfo;

                    wfdDevice = null;
                    try
                    {
                        wfdDevice = await Windows.Devices.WiFiDirect.WiFiDirectDevice.FromIdAsync(devInfo.Id);

                        if (wfdDevice == null)
                        {
                            throw new Exception("wfd device is null");
                        }

                        wfdDevice.ConnectionStatusChanged += new TypedEventHandler<Windows.Devices.WiFiDirect.WiFiDirectDevice, object>(
                            async (Windows.Devices.WiFiDirect.WiFiDirectDevice sender, object arg)
                            => {
                                await parent.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                {
                                    wfdDeviceConnectedListener.onDeviceDisconnected();
                                });
                            });

                        var endpointPairCollection = wfdDevice.GetConnectionEndpointPairs();
                        EndpointPair endpointPair = endpointPairCollection[0];


                        wfdDeviceConnectedListener.onDeviceConnected(new WFDPairInfo(device, endpointPair, parent));
                        //onDeviceConnectFailed(int reasonCode)추가해야함
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message + "\n" + e.StackTrace);
                        parent.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            wfdDeviceConnectedListener.onDeviceConnectFailed(10);   // <- make reason code!!!
                        });
                        return;
                    }
                }
                else
                { /* to Windows
                   * 실제 Connection은 WFDDeviceConnectedListener에서 이루어지므로 필요한 정보(WFDPairInfo)만 리스터로 넘겨준다.
                   */
                    //PeerInformation peerInfo = (PeerInformation)device.WFDDeviceInfo;
                    //StreamSocket socket = await PeerFinder.ConnectAsync(peerInfo);

                    wfdDeviceConnectedListener.onDeviceConnected(new WFDPairInfo(device, parent));
                }
            //});
        }

        public void unpair(WFDPairInfo pair)
        {
            if (pair.getWFDDevice().IsDevice)
            {
                (pair.getWFDDevice().WFDDeviceInfo as Windows.Devices.WiFiDirect.WiFiDirectDevice).Dispose();
            }
            else
            {
                //PeerFinder.Stop();
                //add peerfinder
            }
        }
    }
}
