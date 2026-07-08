using CommunityToolkit.Mvvm.Input;
using NetSpace.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Windows;

namespace NetSpace.ViewModels.AppTabs
{
    class IPViewModel:INotifyPropertyChanged
    {
        private string _selectedDnsType;
        public IP IPSet { get; set; }
      
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
        public bool HandleEdit => !IsAutoDns;
        public IRelayCommand SaveCommand { get; }

        private ISettingsService _settings;
   


        public ObservableCollection<string> DnsTypes => _settings.GetDnsTypes();

        public IPViewModel( ISettingsService _settings)
        {
           
            this._settings = _settings;
            
            SaveCommand = new RelayCommand(Save);
            InitializationData();

        }
        private async void Save()
        {
        

            try
            {
                // 2. Валидация (используем ваш метод с передачей рекорда IP)
                var ipData = IPSet with { _isAuto = IsAutoDns };
                (bool isValid, string error) = _settings.ValidateIP(ipData);

                if (!isValid)
                {
                    MessageBox.Show(error, "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 3. Сохранение настроек в систему (вызов netsh)
                await _settings.SaveIpSettings(ipData);

                // 4. КРИТИЧЕСКАЯ ПАУЗА (Даем Windows время обновить реестр и статус адаптера)
                // В этот момент в консоли могут сыпаться PingException — это нормально
                await Task.Delay(2500);

                // 5. ПЕРЕЧИТЫВАЕМ ДАННЫЕ ИЗ СИСТЕМЫ (чтобы убедиться, что всё применилось)
                // Вызываем ваш метод получения данных через WMI по ID адаптера
                var updatedIp = _settings.GetIpv4FromWmi();

                // Обновляем свойства, чтобы TextBox-ы показали реальный результат
                IPSet = updatedIp;
                IsAutoDns = updatedIp._isAuto;

                MessageBox.Show("Настройки успешно применены и подтверждены системой.",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка");
            }
       
        }
        private void InitializationData()
        {
            IPSet = _settings.GetIpv4FromWmi();
            IsAutoDns = IPSet._isAuto;
          
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    }
}
