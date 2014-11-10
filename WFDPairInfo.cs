using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using Windows.UI.Xaml;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Networking.Proximity;

namespace Buffalo.WiFiDirect
{
    public class WFDPairInfo
    {
        //private 
        private DependencyObject parentUI;
        private WFDDevice device;
        private EndpointPair deviceEndpointPair = null;
        private StreamSocket socket;

        internal WFDPairInfo(WFDDevice device, DependencyObject parent)
        {
            /*to windows, can't get local or remote Address 'cause it doesn't have EndpointPair*/
            this.device = device;
            this.parentUI = parent;
            

        }
        internal WFDPairInfo(WFDDevice device, EndpointPair deviceEndpointPair, DependencyObject parent)
        {
            this.device = device;
            this.deviceEndpointPair = deviceEndpointPair;
            this.parentUI = parent;
        }

        public string getLocalAddress()
        { 
            return device.IsDevice? deviceEndpointPair.LocalHostName.DisplayName :
                                    "";
        }

        public string getRemoteAddress()
        {
            return device.IsDevice? deviceEndpointPair.RemoteHostName.DisplayName :
                                    "";
        }

        internal WFDDevice getWFDDevice()
        {
            return device;
        }


        /*public StreamSocket getSocket()
        {
            StreamSocketListener socketListener = new StreamSocketListener();
            socketListener.ConnectionReceived += onConnection;
        }*/


        public void receivedSocketAsync(PairSocketConnectedListener l)
        {
            if(device.IsDevice){

            } else{
                StreamSocket socket = null;
               
                parentUI.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    socket = await PeerFinder.ConnectAsync((PeerInformation)device.WFDDeviceInfo);
                    if (socket == null)
                    {
                        Debug.WriteLine("socket is null 주륵 in client");
                    }
                    l.onSocketReceived(socket);
                });
            }
            
        }
        /* 비동기적으로 StreamSocket을 연결*/
        public void connectSocketAsync(PairSocketConnectedListener l)
        {
            Debug.WriteLine("connectSocketAsync");

            if(device.IsDevice) {
            /*to Android*/
                StreamSocketListener socketListener = new StreamSocketListener();
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(10000);

                socketListener.ConnectionReceived += async (StreamSocketListener sender,
                        StreamSocketListenerConnectionReceivedEventArgs args) =>
                    {
                        Debug.WriteLine("ConnectionReceived");
                        //windows-device connection, conncted callback
                        StreamSocket s = args.Socket;
                    
                        await parentUI.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            Debug.WriteLine("Call onSocketConnected");

                            l.onSocketConnected(s);
                        });

                        /*Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher.RunAsync
                        (Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            l.onSocketConnected(s);
                        });*/

                    };
                parentUI.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    await socketListener.BindServiceNameAsync("8988");
                });


            } else { /*to Windows*/
                StreamSocket socket = null;
                /* ConnectAsync를 parentUI의 쓰레드에서 실행한다. */
                parentUI.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async() =>
                {
                    socket = await PeerFinder.ConnectAsync((PeerInformation)device.WFDDeviceInfo);
                    if (socket == null)
                    {
                        Debug.WriteLine("socket is null 주륵 in invoker");
                    }
                    l.onSocketConnected(socket);
                });

            }
            
        }
        
        public interface PairSocketConnectedListener
        {
		    void onSocketConnected(StreamSocket s);
            void onSocketReceived(StreamSocket s);
        }

    }
}
