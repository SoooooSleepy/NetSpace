using System;
using System.Collections.Generic;
using System.Text;

namespace NetSpace.Interfaces
{
    public class NetworkSpeed
    {
        public long _download { get; set; }
        public long _upload {  get; set; }
    }

    internal interface INetworkService
    {
       // NetworkSpeed GetSpeedUpdate();
        string GetActiveInterface();
        Task<bool> GetNetworkStatus();
         Task<string> GetWiFiSSID();
        string GetInterfaceType();
        (string _ip4, string _ip6) GetIP();

        Task<string> GetSignalStrenght();
        event Action<NetworkSpeed> SpeedUpdated;
    }
}
