using CommunityToolkit.Mvvm.Input;
using NetSpace.Interfaces;
using NetSpace.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Windows;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NetSpace.ViewModels.AppTabs
{
    internal class DnsViewModel: INotifyPropertyChanged
    {
        private IDBService _dbService;
        private ISettingsService _settings;
        public IRelayCommand SaveCommand { get; }
  
        public DNS DnsSet { get; set; }
        public bool HandleEdit => !IsAutoDns;

        private bool _switchIpv4;
        public bool SwitchIpv4
        {
            get =>_switchIpv4;
            set
            {
                _switchIpv4 = value; 
                OnPropertyChanged(nameof(SwitchIpv4));
            } 
        }
        private bool _isAutoDns;
        public bool IsAutoDns
        {
            get => _isAutoDns;
            set
            {
                _isAutoDns = value;
                OnPropertyChanged(nameof(IsAutoDns));

                // Синхронизируем текст обратно, если bool поменялся из кода (например, при загрузке из системы)
                SelectedDnsType = value ? "Автоматически(DHCP)" : "Вручную";

                // Можно сразу уведомлять UI о доступности полей ввода
                OnPropertyChanged(nameof(HandleEdit));
            }
        }

        private bool _switchIpv6;
        public bool SwitchIpv6
        {
            get => _switchIpv6;
            set
            {
                _switchIpv6 = value;
                OnPropertyChanged(nameof(SwitchIpv6));
            }
        }

        public ObservableCollection<string> DnsTypes => _settings.GetDnsTypes();
        private string _selectedDnsType;
        public string SelectedDnsType 
        {
            get => _selectedDnsType;
            set 
            {
                if (_selectedDnsType != value)
                {
                    _selectedDnsType = value;
                    OnPropertyChanged(nameof(SelectedDnsType));

                    // Автоматическое обновление bool флага
                    IsAutoDns = (value == "Автоматически(DHCP)");
                }
            } 
        }

        public DnsViewModel(IDBService _dbService, ISettingsService _settings)
        {
            this._dbService = _dbService;
            this._settings = _settings;
            SaveCommand = new RelayCommand(Save);
            InitializationData();

        }
        private void InitializationData()
        {
            DnsSet = _settings.GetCurrentDnsFromSystem();
            IsAutoDns = DnsSet._isAuto;
            SwitchIpv4 = DnsSet._isIpv4Active;
            SwitchIpv6 = DnsSet._isIpv6Active;
        }
        private void Save()
        {

            if(_settings.IsValidDNS(DnsSet, SwitchIpv4, SwitchIpv6))
            {
               
                _settings.SaveDNS(SelectedDnsType, SwitchIpv4, SwitchIpv6, DnsSet);
                MessageBox.Show("Натсройки обновлены.", "Уведомление", MessageBoxButton.OK);
              
            }
            else
            {
                MessageBox.Show("Некоректный DNS адрес.\nПроверьте вводимые данные", "Ошибка", MessageBoxButton.OK);
            }
        }
      
        
      
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    }
}
