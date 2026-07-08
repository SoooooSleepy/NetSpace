using CommunityToolkit.Mvvm.Input;
using LiveCharts;
using NetSpace.Interfaces;
using NetSpace.ViewModels.AppTabs;
using NetSpace.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NetSpace.ViewModels
{
       
        internal class MainViewModel: INotifyPropertyChanged
        {
            INetworkService _networkService;
        private string _wifiSignal = string.Empty;
        public object SelectedPage { get; set; }
        private (DnsViewModel _dns, InterfacesViewModel _interfaces, IPViewModel _dhcp, OtherViewModel _other) ViewModels { get; set; }
        public string WifiSignal
        {
            get => _wifiSignal;
            set { _wifiSignal = value; OnPropertyChanged(nameof(WifiSignal)); }
        }
        public string ActiveNetworkInterface => _networkService.GetActiveInterface();
        private string _activeSSID = string.Empty;
        public string ActiveSSID 
        {
            get=> _activeSSID;
            set 
            { 
                _activeSSID = value; OnPropertyChanged(nameof(ActiveSSID));
            }
        }
        public string ActiveConnectionType => _networkService.GetInterfaceType();
        private string _currentIp4 = string.Empty;    
        public string CurrentIp4 
        { 
            get=> _currentIp4;
            set
            {
                _currentIp4 = value;
                OnPropertyChanged(nameof(CurrentIp4));
            } 
        }
        private string _currentIp6 = string.Empty;
        public string CurrentIp6
        {
            get => _currentIp6;
            set
            {
                _currentIp6 = value;
                OnPropertyChanged(nameof(CurrentIp6));
            }
        }
        public ChartValues<double> DownloadValues { get; } = new ChartValues<double>();
        public ChartValues<double> UploadValues { get; } = new ChartValues<double>();
        public Func<double, string> YFormatter { get; set; } = value =>
        {
            double bits = value * 8.0;
            return bits switch
            {
                >= 1048576 => $"{(value / 1048576.0):F1} Мб/с",
                >= 1024 => $"{(value / 1024.0):F1} Кб/с",
                _ => $"{value:F0} Б/с"
            };
        };


        public IRelayCommand DnsCommand { get;}
        public IRelayCommand InterfacesCommand { get; }
        public IRelayCommand IPCommand { get; }
        public IRelayCommand OtherCommand { get; }
        public MainViewModel(INetworkService _networkService, 
            (DnsViewModel _dns, InterfacesViewModel _interfaces, IPViewModel _dhcp, OtherViewModel _other) _viemModels)
        {
            this._networkService = _networkService;
            ViewModels = _viemModels;
            DnsCommand = new RelayCommand(OpenDNS);
            IPCommand = new RelayCommand(OpenIP);
            OtherCommand = new RelayCommand(OpenOther);
            InterfacesCommand = new RelayCommand(OpenInterfaces);
            if (Application.Current == null) return;
                _networkService.SpeedUpdated += OnSpeedUpdated;
        }
        private void OpenOther()
        {
            SelectedPage = new OtherView() { DataContext = ViewModels._other};
            OnPropertyChanged(nameof(SelectedPage));
        }
        private void OpenDNS()
        {
            SelectedPage = new DnsView() { DataContext = ViewModels._dns };
            OnPropertyChanged(nameof(SelectedPage));
        }
        private void OpenInterfaces()
        {
            SelectedPage = new InterfacesView() { DataContext = ViewModels._interfaces };
            OnPropertyChanged(nameof(SelectedPage));
        }

        private void OpenIP()
        {
            SelectedPage = new IPView() { DataContext = ViewModels._dhcp };
            OnPropertyChanged(nameof(SelectedPage));
        }
        private void OnSpeedUpdated(NetworkSpeed _speed)
        {
            Task.Run(async () =>
            {
                (string _ipv4, string _ipv6) = _networkService.GetIP();
                 string _signal = await _networkService.GetSignalStrenght();
                 string _ssid = await _networkService.GetWiFiSSID();
                // Переходим в UI поток
                Application.Current?.Dispatcher.Invoke(async () =>
                {
                    // Добавляем СЫРЫЕ БАЙТЫ. Математика в YFormatter сама всё сконвертирует.
                    DownloadValues.Add((double)_speed._download);
                    UploadValues.Add((double)_speed._upload);

                    if (DownloadValues.Count > 30)
                    {
                        DownloadValues.RemoveAt(0);
                        UploadValues.RemoveAt(0);
                    }
                    WifiSignal = _signal;
                    ActiveSSID = _ssid;
                    CurrentIp6 = _ipv6;
                    CurrentIp4 = _ipv4;
                });
            });

        }
        public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        }
    }
