using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.WiFiDirect;
using Windows.Devices.Enumeration;

using Windows.Networking.Proximity;

namespace Buffalo.WiFiDirect
{
    public class WFDDevice
    {
        /* true  : to Android
         * false : to Windows */
        private bool bDevice = false;
        private DeviceInformation wfdDevInfo = null;
        /* to Windows, PeerFiner를 이용한 연결을 지원하기 위한 정보*/
        private PeerInformation peerInfo = null;

        public WFDDevice(DeviceInformation wfdDevInfo)
        {
            this.wfdDevInfo = wfdDevInfo;
            bDevice = true;
        }
        
        public WFDDevice(PeerInformation peerInfo)
        {
            this.peerInfo = peerInfo;
            bDevice = false;
        }

        public Object WFDDeviceInfo
        {
            get
            {
                return bDevice? (Object)wfdDevInfo :
                                (Object)peerInfo;
            }
        }

        public bool IsDevice
        {
            get
            {
                return bDevice;
            }
        }

        public string Name {
            get { return bDevice? wfdDevInfo.Name :
                                  peerInfo.DisplayName; }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
