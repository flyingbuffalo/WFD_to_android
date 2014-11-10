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

using Windows.Networking.Proximity;

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


        private event EventHandler<ConnectionRequestedEventArgs> ConnectionRequested;
        public WFDManager(DependencyObject parent,
                          WFDDeviceDiscoveredListener wfdDeviceDiscoveredListener,
                          WFDDeviceConnectedListener wfdDeviceConnectedListener,
                          WFDPairInfo.PairSocketConnectedListener wfdPairSocketConnectedListener
            )
        {
            this.parent = parent;
            setWFDDeviceConnectedListener(wfdDeviceConnectedListener);
            setWFDDeviceDiscoveredListener(wfdDeviceDiscoveredListener);

            //PeerFinder.Start();가 getDevicesAsync로 가야하는건가..
            /*peer Application을 찾는 프로세스를 시작하고 Application을 원격 피어에서 검색할 수 있게 만듦*/
            PeerFinder.Start();

            /* 상대 peer에서 connection요청이 왔을 경우 처리할 함수*/
            /// public event EventHandler<ConnectionRequestedEventArgs> ConnectionRequested;
           
            PeerFinder.ConnectionRequested += async (object sender, ConnectionRequestedEventArgs args) =>
            {
                Debug.WriteLine("ConnectionReceived in manager");

                //StreamSocket s = await PeerFinder.ConnectAsync(args.PeerInformation);
                if (ConnectionRequested != null)
                {
                    ConnectionRequested(this, args);
                }

                WFDPairInfo pInfo = new WFDPairInfo(new WFDDevice(args.PeerInformation), parent);
                pInfo.receivedSocketAsync(wfdPairSocketConnectedListener);
                

                /*await parent.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Debug.WriteLine("Call onSocketConnected");
                });*/
            };
        }

        //private delegate void WorkItemHandler(IAsyncAction operation);

        public void getDevicesAsync()
        {
            List<WFDDevice> wfdList = new List<WFDDevice>();
            IEnumerable<PeerInformation> pList = null;

            /* checkPeerFinder : 
             * SupportedDiscoveryTypes 검색 옵션을 PeerFinder와 사용할 수 있는지 확인
             * PeerDiscoveryTypes.Browse는 FindAllPeersAsync, connectAsync를 사용하는데 WiFi Direct를 사용할 수 있는 지 확인
             * 
             * allowWifiDirect :
             * WiFi Direct를 이용하여 StreamSocket을 사용할 수 있는 지 확인
             */
            bool checkPeerFinder = (PeerFinder.SupportedDiscoveryTypes & PeerDiscoveryTypes.Browse) == PeerDiscoveryTypes.Browse;
            bool allowWifiDirect = PeerFinder.AllowWiFiDirect;

            IAsyncAction asyncAction = ThreadPool.RunAsync( async (workItem) =>
            {
                /*to Android*/
                string wfdSelector = Windows.Devices.WiFiDirect.WiFiDirectDevice.GetDeviceSelector();
                DeviceInformationCollection devInfoCollection = await DeviceInformation.FindAllAsync(wfdSelector);

                /* to Windows
                 * PeerFinder에서 WiFi Direct를 사용할 수 있는 지 확인하여 가능 할 경우에만 FindAllPeerAsync함수를 호출한다.
                 */
                if (checkPeerFinder && allowWifiDirect)
                {
                    pList = await PeerFinder.FindAllPeersAsync();
                }

                foreach (DeviceInformation devInfo in devInfoCollection)
                { /* to Android */
                    wfdList.Add(new WFDDevice(devInfo));
                }

                if (pList != null)
                {
                    foreach (PeerInformation peerInfo in pList)
                    { /* to Windows  wfdList에 peerInfo를 추가한다 */
                        wfdList.Add(new WFDDevice(peerInfo));
                    }
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
