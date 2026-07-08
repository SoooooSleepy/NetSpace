using NetSpace.Interfaces;
using NetSpace.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing.Printing;
using System.Text;
using System.Windows.Threading;

namespace NetSpace.ViewModels
{
    
    internal class TrayContextViewModel : INotifyPropertyChanged
    {
        private double _sessionDownloadBytes { get; set; }
        private double _sessionUploadBytes { get; set; }
        private double _totalDownloadBytes { get; set; }
        private double _totalUploadBytes { get; set; }
        private double _todayDownloadBytes { get; set; }
        private double _todayUploadBytes { get; set; }
        private readonly ITimeService _timeService;
        private readonly INetworkService _networkService;
        private readonly IDBService _dbService;
        public string Uptime => _timeService.GetUptimeString();
        public string ActiveNetworkInterface => _networkService.GetActiveInterface();
        public string DownloadSpeed { get; private set; }
        public string DownloadType { get; private set; }
        public string UploadSpeed { get; private set; }
        public string UploadType { get; private set; }

        public double SessionDownload { get; private set; }
        public string SessionDownloadType { get; private set; }
        public double SessionUpload { get; private set; }
        public string SessionUploadType { get; private set; }
        public double TodayDownload { get; private set; }
        public string TodayDownloadType { get; private set; }
        public double TodayUpload { get; private set; }
        public string TodayUploadType { get; private set; }
        public double TotalDownload { get; private set; }
        public string TotalDownloadType { get; private set; }
        public double TotalUpload { get; private set; }
        public string TotalUploadType { get; private set; }


        enum SpeedTypes
        {
            Download,
            Upload
        }
        private string[] _activeColors = new string[4]
        {
            "#1b314c",
            "#731211",
            "#FF82AFE5",
            "White"
        };
        private string[] _activeString = new string[2]
        {
            "Интернет подключен",
            "Не удалось подключиться"
        };
        public string ActiveBackground { get; private set; }
        public string ActiveForeground { get; private set; }
        public string ActiveString { get; private set; }

        public TrayContextViewModel(ITimeService _timeService, INetworkService _networkService, IDBService _dbService)
        {
            this._timeService = _timeService;
            this._networkService = _networkService;
            this._dbService = _dbService;

            InitializeData();
            ActiveBackground = _activeColors[1];
            ActiveForeground = _activeColors[3];
            ActiveString = _activeString[1];

            /*DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => UpdateUI();
            _timer.Start();*/
            _networkService.SpeedUpdated += UpdateUI;
            DispatcherTimer _timerSave = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _timerSave.Tick += (s, e) => AutoSave();
            _timerSave.Start();

        }
        private string FormatSpeed(long _bytes, SpeedTypes _type)
        {
            double bitsPerSecond = _bytes * 8.0; // Конвертируем в биты

            return bitsPerSecond switch
            {
                >= 1048576 => SynchronizationSpeed($"{(bitsPerSecond / 1048576.0):F1}", "Mbps", _type),
                >= 1024 => SynchronizationSpeed($"{(bitsPerSecond / 1024.0):F1}", "Kbps", _type),
                _ => SynchronizationSpeed($"{bitsPerSecond:F0}", "bps", _type)


            };

        }

        private string SynchronizationSpeed(string _bytes, string _speed, SpeedTypes _type)
        {
            if (_type == SpeedTypes.Download) DownloadType = _speed;
            if (_type == SpeedTypes.Upload) UploadType = _speed;
            return $"{_bytes}";
        }
        private async void UpdateUI(NetworkSpeed _speed)
        {
            // NetworkSpeed _speed = _networkService.GetSpeedUpdate();
            bool _status = await _networkService.GetNetworkStatus();
            UpdateStatus(_status);
            DownloadSpeed = FormatSpeed(_speed._download, SpeedTypes.Download);
            UploadSpeed = FormatSpeed(_speed._upload, SpeedTypes.Upload);
 



            _totalDownloadBytes += _speed._download;
            _totalUploadBytes += _speed._upload;
            _todayDownloadBytes += _speed._download;
            _todayUploadBytes += _speed._upload;
            _sessionDownloadBytes += _speed._download;
            _sessionUploadBytes += _speed._upload;

            (SessionDownload, SessionDownloadType) = GetFinalData(_sessionDownloadBytes);
            (SessionUpload, SessionUploadType) = GetFinalData(_sessionUploadBytes);

            (TodayDownload, TodayDownloadType) = GetFinalData(_todayDownloadBytes);
            (TodayUpload, TodayUploadType) = GetFinalData(_todayUploadBytes);

            (TotalDownload, TotalDownloadType) = GetFinalData(_totalDownloadBytes);
            (TotalUpload, TotalUploadType) = GetFinalData(_totalUploadBytes);

            OnPropertyChanged(string.Empty);
        }
        private (double Value, string Type) GetFinalData(double _bytes)
        {
            var (_value, _type) = GetFormattedData(_bytes);
            return (Math.Round(_value, 2), _type);
        }
        private void UpdateStatus(bool _status)
        {
            switch (_status)
            {
                case true:
                    ActiveBackground = _activeColors[0];
                    ActiveForeground = _activeColors[2];
                    ActiveString = _activeString[0];
                    break;
                case false:
                    ActiveBackground = _activeColors[1];
                    ActiveForeground = _activeColors[3];
                    ActiveString = _activeString[1];
                    break;

            }
        }



        private void InitializeData()
        {
            DBTable data = _dbService.LoadTraffic();

            // Если день сменился — обнуляем "Сегодня", но оставляем "Total"
            if (data.LastUpdateDate.Date != DateTime.Today)
            {
                _totalDownloadBytes = data.TotalDownload;
                _totalUploadBytes = data.TotalUpload;
                _todayDownloadBytes = 0;
                _todayUploadBytes = 0;

            }
            else
            {
                _totalDownloadBytes = data.TotalDownload;
                _totalUploadBytes = data.TotalUpload;
                _todayDownloadBytes = data.TodayDownload;
                _todayUploadBytes = data.TodayUpload;
            }
        }
        private void AutoSave()
        {
            _dbService.SaveTraffic(new DBTable
            {
                TotalDownload = _totalDownloadBytes,
                TotalUpload = _totalUploadBytes,
                TodayDownload = _todayDownloadBytes,
                TodayUpload = _todayUploadBytes,
                LastUpdateDate = DateTime.Today
            });
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private (double Value, string Type) GetFormattedData(double _bytes)
        {
            double bitsPerSecond = _bytes * 8.0; // Конвертируем в биты
            return bitsPerSecond switch
            {
                >= 1073741824 => (_bytes / 1073741824.0, "Gb"), // Гигабайты
                >= 1048576 => (_bytes / 1048576.0, "Mb"),    // Мегабайты
                >= 1024 => (_bytes / 1024.0, "Kb"),       // Килобайты
                _ => (_bytes, "B")                  // Байты
            };
        }
        

    }
}
