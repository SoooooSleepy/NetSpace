using CommunityToolkit.Mvvm.Input;
using NetSpace.Interfaces;
using NetSpace.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Windows;

namespace NetSpace.ViewModels.AppTabs
{
    
    internal class InterfacesViewModel: INotifyPropertyChanged
    {
        private ISettingsService _settings;

        public IRelayCommand TurnOnCommand { get;}
        public IRelayCommand TurnOffCommand {  get;}
        public ObservableCollection<InterfacesListItem> InteracesList => _settings.GetInterfacesList();
        private InterfacesListItem _selectedInterface;
        public InterfacesListItem SelectedInterface
        {
            get { return _selectedInterface; }
            set
            {
                _selectedInterface = value;
                OnPropertyChanged(nameof(SelectedInterface));
            }
        }
        public InterfacesViewModel( ISettingsService settingsService)
        {
            this._settings = settingsService;
          
            TurnOffCommand = new RelayCommand(TurnOff);
            TurnOnCommand = new RelayCommand(TurnOn);

        }
        private async void TurnOn()
        {
            if (SelectedInterface != null)
            {
                _settings.TurnOnInterface(SelectedInterface);
                await Task.Delay(1000);
                OnPropertyChanged(nameof(InteracesList));
            }
            else
            {
                MessageBox.Show($"Выберите интерфейс.", "Ошибка", MessageBoxButton.OK);

            }
}
        private async void TurnOff()
        {
            if (SelectedInterface != null)
            {
                _settings.TurnOffInterface(SelectedInterface);
                await Task.Delay(1000);
                OnPropertyChanged(nameof(InteracesList));
            }
            else
            {
                MessageBox.Show($"Выберите интерфейс.", "Ошибка", MessageBoxButton.OK);

            }

        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    }
}

