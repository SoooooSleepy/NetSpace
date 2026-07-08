using Hardcodet.Wpf.TaskbarNotification;
using NetSpace.Interfaces;
using NetSpace.Models;
using NetSpace.ViewModels;
using NetSpace.ViewModels.AppTabs;
using NetSpace.Views;
using NetSpace.Views.TaskBar;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Forms;

namespace NetSpace
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private TaskbarIcon? _tray;
        private  ITimeService _timeService;
        private  INetworkService _networkService;
        private  IDBService _dbService;
        private ISettingsService _settings;
        private readonly string _dbPath = "stats.db";
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);


            NetworkMonitor _monitor = new NetworkMonitor();
            // 1. Создаем сервисы
            _timeService = new TimeMonitor();
            _networkService = _monitor;
            _dbService = new NetSpace.Models.ApplicationContext(_dbPath);
            _settings = _monitor;

            // 2. Достаем иконку
            _tray = (TaskbarIcon)FindResource("TrayIcon");

            // 3. Создаем ДВЕ разные вьюмодели
            TrayViewModel _menuVM = new TrayViewModel(); // Для кнопок Open/Exit
            TrayContextViewModel tooltipVM = new TrayContextViewModel(_timeService, _networkService, _dbService); // Для графиков

            // 4. ПРИВЯЗЫВАЕМ ИХ РАЗДЕЛЬНО
            // Основной контекст иконки (уйдет в ToolTip автоматически)
            _tray.DataContext = tooltipVM;

            // Контекст для меню (назначаем напрямую свойству ContextMenu)
            if (_tray.ContextMenu != null)
            {
                _tray.ContextMenu.DataContext = _menuVM;
            }
            (DnsViewModel _dns, InterfacesViewModel _interfaces, IPViewModel _dhcp, OtherViewModel _other) _viewModels =
                (new DnsViewModel(_dbService, _settings), new InterfacesViewModel(_settings), new IPViewModel(_settings),  new OtherViewModel(_settings,_dbService));
            MainViewModel _mainViewModel = new MainViewModel(_networkService, _viewModels);
            MainWindow _main = new MainWindow() { DataContext = _mainViewModel};

            
            
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _tray?.Dispose();
            base.OnExit(e);
        }
    }

}
