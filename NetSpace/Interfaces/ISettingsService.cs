using NetSpace.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace NetSpace.Interfaces
{
    public record IP(string _address, string _mask,string _gateway, bool _isAuto);
    public record DNS(string _ipv4First, string _ipv4Second, string _ipv6First, string _ipv6Second, bool _isAuto, bool _isIpv4Active, bool _isIpv6Active);
    public record InterfacesListItem(string _name, string _description, string _status, NetworkInterfaceType _type);

    internal interface ISettingsService
    {
        Task SaveIpSettings(IP _ipSettings);
        ObservableCollection<string> GetDnsTypes();
        DNS GetCurrentDnsFromSystem();
        bool IsValidDNS(DNS _dns, bool _isIpv4, bool _isIpv6);
         Task SaveDNS(string _dnsType, bool _useIpv4, bool _useIpv6, DNS _dns);
        ObservableCollection<InterfacesListItem> GetInterfacesList();
        void TurnOffInterface(InterfacesListItem _interface);
        void TurnOnInterface(InterfacesListItem _interface);
        IP GetIpv4FromWmi();
        (bool _isValid, string _errorMessage) ValidateIP(IP _data);
        void FlushDNS();
        Task RepairNetwork();
       

    }
}
