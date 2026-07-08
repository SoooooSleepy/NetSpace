using CommunityToolkit.Mvvm.Input;
using NetSpace.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetSpace.ViewModels.AppTabs
{
    internal class OtherViewModel
    {
        public IRelayCommand ClearStatCommand { get; }
        public IRelayCommand FLushDNSCommand { get; }
        public IRelayCommand RepairCommand { get; }

        private ISettingsService _settings;
        private IDBService _dbService;

        public OtherViewModel(ISettingsService _settings, IDBService _dbService)
        {
            this._settings = _settings;
            this._dbService = _dbService;
            ClearStatCommand = new RelayCommand(ClearStat);
            FLushDNSCommand = new RelayCommand(FlushDNS);
            RepairCommand = new RelayCommand(Repair);
        }
        void ClearStat() => _dbService.ClearTraffic();
        void FlushDNS()=>_settings.FlushDNS();
        void Repair()=>_settings.RepairNetwork();
    }
}
