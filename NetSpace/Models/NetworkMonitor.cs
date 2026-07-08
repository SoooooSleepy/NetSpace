using LiveCharts.Maps;
using NetSpace.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;
using static SQLite.SQLite3;

namespace NetSpace.Models
{
     internal class NetworkMonitor: INetworkService, ISettingsService
    {
    
        private NetworkSpeed _rawData = new();
        private readonly NetworkInterface? _interface;
        private DispatcherTimer _internalTimer;


        public event Action<NetworkSpeed>? SpeedUpdated;
        public NetworkMonitor()
        {
            _interface = NetworkInterface.GetAllNetworkInterfaces()
              .FirstOrDefault(i =>
              i.OperationalStatus == OperationalStatus.Up &&
              i.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
              !i.Description.ToLower().Contains("virtual") &&
              !i.Description.ToLower().Contains("vpn") &&
              !i.Description.ToLower().Contains("pseudo") &&
              (i.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
               i.NetworkInterfaceType == NetworkInterfaceType.Wireless80211));
            if (_interface != null)
            {
                IPInterfaceStatistics _stats = _interface.GetIPStatistics();

                (_rawData._download, _rawData._upload) = (_stats.BytesReceived, _stats.BytesSent);
                System.Diagnostics.Debug.WriteLine($"Мониторинг запущен на: {_interface.Description}");
            }
            _internalTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _internalTimer.Tick += (s, e) => UpdateInternal();
            _internalTimer.Start();
        }
        private void UpdateInternal()
        {
            if (_interface == null) return;

            IPInterfaceStatistics stats = _interface.GetIPStatistics();
            if (_rawData is { _download: 0, _upload: 0 })
            {
                (_rawData._download, _rawData._upload) = (stats.BytesReceived, stats.BytesSent);
                return;
            }
            long diffIn = stats.BytesReceived - _rawData._download;
            long diffOut = stats.BytesSent - _rawData._upload;
            diffIn = Math.Max(0, diffIn);
            diffOut = Math.Max(0, diffOut);
            _rawData._download = stats.BytesReceived;
            _rawData._upload = stats.BytesSent;

            SpeedUpdated?.Invoke(new NetworkSpeed { _download = diffIn, _upload = diffOut });
        }
      
        public string GetActiveInterface()
        {
            if (_interface == null) return "None";

            return _interface.Description;
        }
        public async Task<bool> GetNetworkStatus() 
        {
            // 1. Проверяем, есть ли вообще сетевое подключение
            if (!NetworkInterface.GetIsNetworkAvailable()) return false;

            // 2. Пробуем "пингнуть" надежный IP (Google DNS)
            try
            {
                using (Ping _ping = new Ping())
                {
                    PingReply _reply = await _ping.SendPingAsync("8.8.8.8", 1000); // Таймаут 1 секунда
                    return _reply.Status == IPStatus.Success;
                }
            }
            catch { return false; }
        }
        public string GetInterfaceType()
        {
           

            if (_interface == null) return "Disconnected";

            // Используем switch expression для красивого перевода
            return _interface.NetworkInterfaceType switch
            {
                NetworkInterfaceType.Ethernet => "Проводное (Ethernet)",
                NetworkInterfaceType.Wireless80211 => "Беспроводное (Wi-Fi, 822.11)",
                NetworkInterfaceType.Wwanpp or NetworkInterfaceType.Wwanpp2 => "Мобильное (LTE/5G)",
                NetworkInterfaceType.Tunnel => "VPN Соединение",
                _ => "Другое"
            };
        }
        public async Task<string> GetWiFiSSID()
        {
            return await Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo _processStartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "wlan show interfaces",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.Default


                    };

                    using (Process? process = Process.Start(_processStartInfo))
                    {
                        string _output = process.StandardOutput.ReadToEnd();

                        // 1. Разбиваем вывод на строки
                        string[] _lines = _output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                        // 2. Ищем строку, где есть слово "SSID", но НЕТ слова "BSSID"
                        // Сравниваем без учета регистра (ToLower)
                        string? _ssidLine = _lines.FirstOrDefault(l =>
                            l.ToLower().Contains("ssid") && !l.ToLower().Contains("bssid"));

                        if (_ssidLine != null)
                        {
                            // 3. Берем всё, что после двоеточия
                            string[] _parts = _ssidLine.Split(':');
                            if (_parts.Length > 1)
                            {
                                return _parts[1].Trim();
                            }
                        }
                    }
                }
                catch (Exception _exception)
                {
                    Console.WriteLine($"{_exception}");
                }

                return "Ethernet / No Wi-Fi";
            });
        }
        public (string _ip4,string _ip6) GetIP()
        {
            if (_interface == null) return ("N/A", "N/A");
            IPInterfaceProperties _properties = _interface.GetIPProperties();
            // Ищем IPv4
            string _ipv4 = _properties.UnicastAddresses
                .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                ?.Address.ToString() ?? "N/A";

            // Ищем IPv6 (отфильтровываем временные и локальные ссылки, если нужно)
            string _ipv6 = _properties.UnicastAddresses
                .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
                                  && !a.Address.IsIPv6LinkLocal)
                ?.Address.ToString() ?? "N/A";

            return (_ipv4, _ipv6);
        }
        public async Task<string> GetSignalStrenght()
        {
            return await Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo _processStartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "wlan show interfaces",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.Default
                    };

                    using (Process _process = Process.Start(_processStartInfo))
                    {
                        string _output = _process.StandardOutput.ReadToEnd();
                        string[] _lines = _output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                        // Ищем строку "Signal" или "Сигнал"
                        string? _signalLine = _lines.FirstOrDefault(l =>
                            l.ToLower().Contains("signal") || l.ToLower().Contains("сигнал"));

                        if (_signalLine != null)
                        {
                            string[] _parts = _signalLine.Split(':');
                            if (_parts.Length > 1)
                            {
                                // Возвращает строку вида "85%"
                                return _parts[1].Trim();
                            }
                        }
                    }
                }
                catch (Exception _exception)
                {
                    Console.WriteLine($"{_exception}");
                }

                return "0% or Ethernet"; // Если Ethernet или Wi-Fi отключен
            });
        }

        public async Task SaveDNS(string _dnsType, bool _useIpv4, bool _useIpv6, DNS _dnsSet)
        {
            // 1. Ищем активный сетевой интерфейс
      

            if (_interface == null) throw new Exception("Активный сетевой адаптер не найден.");

            string _name = _interface.Name;
            bool _isAuto = _dnsType == "Автоматически(DHCP)";

            // 2. Выполняем системные команды в фоновом потоке
            await  Task.Run(() =>
            {
                // --- Настройка IPv4 ---
                if (_useIpv4)
                {
                    if (_isAuto)
                    {
                        RunNetsh($"interface ipv4 set dnsservers name=\"{_name}\" source=dhcp");
                       
                    }
                    else
                    {
                        // Основной IPv4
                        var result1 = RunNetsh($"interface ipv4 set dnsservers name=\"{_name}\" static {_dnsSet._ipv4First} validate=no");
                        Debug.WriteLine($"Команда 1: {result1}");
                        // Дополнительный IPv4 (если не пуст)
                        if (!string.IsNullOrWhiteSpace(_dnsSet._ipv4Second))
                        {
                            var result2 = RunNetsh($"interface ipv4 add dnsservers name=\"{_name}\" {_dnsSet._ipv4Second} index=2 validate=no");
                            Debug.WriteLine($"Команда 2: {result2}");
                        }
                           
                
                    }
                }
                else
                {
                    // ОТКЛЮЧАЕМ IPv4 (сброс на получение DNS автоматически)
                    RunNetsh($"interface ipv4 set dnsservers name=\"{_name}\" source=dhcp");
                }
                // --- Настройка IPv6 ---
                if (_useIpv6)
                {
                    if (_isAuto)
                    {
                        RunNetsh($"interface ipv6 set dnsservers name=\"{_name}\" source=dhcp");
                    }
                    else
                    {
                        // Основной IPv6
                        var result3 = RunNetsh($"interface ipv6 set dnsservers name=\"{_name}\" static {_dnsSet._ipv6First} validate=no");
                        Debug.WriteLine($"Команда 3: {result3}");
                        // Дополнительный IPv6 (если не пуст)
                        if (!string.IsNullOrWhiteSpace(_dnsSet._ipv6Second))
                        {
                            var result4 = RunNetsh($"interface ipv6 add dnsservers name=\"{_name}\" {_dnsSet._ipv6Second} index=2 validate=no");
                            Debug.WriteLine($"Команда 4: {result4}");
                        }
                           
                    }
                }
                else
                {
                    // ОТКЛЮЧАЕМ IPv4 (сброс на получение DNS автоматически)
                    RunNetsh($"interface ipv6 set dnsservers name=\"{_name}\" source=dhcp");
                }
            });
        }

        private async Task<string> RunNetsh(string args)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        // /C - выполнить команду и закрыться. 
                        // Мы передаем всю строку netsh целиком, как в консоли.
                        Arguments = $"/c netsh {args}",
                        Verb = "runas",              // Запрос прав админа
                        UseShellExecute = true,      // Нужно для "runas"
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(psi))
                    {
                        process?.WaitForExit();
                        return "Success"; // CMD не возвращает вывод в Redirect при ShellExecute=true
                    }
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            });
        }
        public ObservableCollection<InterfacesListItem> GetInterfacesList()
        {
            IEnumerable<NetworkInterface> _systemInterfaces = NetworkInterface.GetAllNetworkInterfaces()
           .Where(n => n.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                       n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
            .OrderByDescending(n => n.OperationalStatus == OperationalStatus.Up); ;
            ObservableCollection<InterfacesListItem> _interfaces = new ObservableCollection<InterfacesListItem>();
            foreach (var ni in _systemInterfaces)
            {
                _interfaces.Add(new InterfacesListItem
                (
                     ni.Name,
                     ni.Description,

                    ni.OperationalStatus == OperationalStatus.Up ? "Включен" : "Отключен",
                    ni.NetworkInterfaceType
                ));
            }
            return _interfaces;
        }
        public void TurnOffInterface(InterfacesListItem _interface)
        {
            if (_interface != null)
            {
                string _args = $"interface set interface name=\"{_interface._name}\" admin=disabled";

                try
                {
           
                    RunNetsh(_args);
                }
                catch (Exception ex)
                {
                    // Например, если пользователь нажал "Нет" в окне UAC
                    MessageBox.Show($"Ошибка при выключении интерфейса: {ex.Message}", "Ошибка", MessageBoxButton.OK);
                }
            }
            else
            {
                MessageBox.Show($"Выберите интерфейс.", "Ошибка", MessageBoxButton.OK);

            }

        }
        public void TurnOnInterface(InterfacesListItem _interface)
        {
           
                

            if (_interface != null)
            {
                string _args = $"interface set interface name=\"{_interface._name}\" admin=enabled";

                try
                {

                    RunNetsh(_args);
                }
                catch (Exception ex)
                {
                    // Например, если пользователь нажал "Нет" в окне UAC
                    MessageBox.Show($"Ошибка при выключении интерфейса: {ex.Message}", "Ошибка", MessageBoxButton.OK);
                }
            }
            else
            {
                MessageBox.Show($"Выберите интерфейс.", "Ошибка", MessageBoxButton.OK);

            }
        }
        public bool IsValidDNS(DNS _dns, bool _isIpv4, bool _isIpv6)
        {
            bool IsStrictIpv4(string ip)
            {
                if (string.IsNullOrWhiteSpace(ip)) return false;

                // Считаем точки: должно быть ровно 3
                if (ip.Count(c => c == '.') != 3) return false;

                // Проверяем корректность чисел (0-255) и отсутствие мусора
                return IPAddress.TryParse(ip, out var addr) &&
                       addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
            }
            bool IsStrictIpv6(string ip)
            {
                if (string.IsNullOrWhiteSpace(ip)) return false;

                // В IPv6 должно быть минимум 2 двоеточия (для краткой записи ::1) 
                // и максимум 7 (для полной записи).
                int colonCount = ip.Count(c => c == ':');
                if (colonCount < 2 || colonCount > 7) return false;

                // Проверяем системным парсером на соответствие формату IPv6
                return IPAddress.TryParse(ip, out var addr) &&
                       addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6;
            }
            // 2. Проверка для необязательных полей (Либо пусто, либо строгий формат)
            bool IsOptionalIpv4(string ip) => string.IsNullOrWhiteSpace(ip) || IsStrictIpv4(ip);
            bool IsOptionalIpv6(string ip) => string.IsNullOrWhiteSpace(ip) || IsStrictIpv6(ip);
            // Итоговая логика:
            // d1 (IPv4 Main) — Обязателен
            // d2 (IPv4 Alt)  — Опционален
            // d3 (IPv6 Main) — Обязателен (если вы используете IPv4 формат для него)
            // d4 (IPv6 Alt)  — Опционален
            bool _ipv6;
            bool _ipv4;
            if (_isIpv4)
            {
                _ipv4 = IsStrictIpv4(_dns._ipv4First) &&
                 IsOptionalIpv4(_dns._ipv4Second);
            }
            else
            {
                _ipv4 = true;
            }
            if (_isIpv6)
            {
                _ipv6 = IsStrictIpv6(_dns._ipv6First) &&
                 IsOptionalIpv6(_dns._ipv6Second);
            }
            else
            {
                _ipv6 = true;
            }
            return _ipv4 && _ipv6;
        }
        public DNS GetCurrentDnsFromSystem()
        {

            if (_interface == null)
                return new DNS("", "", "", "", true, false, false);

            // 1. ТОЧНАЯ ПРОВЕРКА АКТИВНОСТИ (включен ли стек в настройках)
            (bool v4Active, bool v6Active) wmiStatus = GetProtocolStatusWmi();
            bool v4Active = wmiStatus.v4Active;
            bool v6Active = wmiStatus.v6Active;

            // 2. Получаем DNS адреса
            var ipProps = _interface.GetIPProperties();
            var dnsAddresses = ipProps.DnsAddresses;

            var v4List = dnsAddresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                                     .Select(a => a.ToString()).ToList();

            // Очищаем IPv6 от Scope ID (%12), если протокол активен
            var v6List = v6Active
                ? dnsAddresses.Where(a => a.AddressFamily == AddressFamily.InterNetworkV6)
                              .Select(a => a.ToString().Split('%')[0]).ToList()
                : new List<string>();

            // 3. Проверяем DHCP (обычно если DHCP включен для IP, он включен и для DNS)
            bool isAuto = IsDnsDhcpEnabled();
            DNS _result = new DNS(
                v4List.ElementAtOrDefault(0) ?? "",
                v4List.ElementAtOrDefault(1) ?? "",
                v6List.ElementAtOrDefault(0) ?? "",
                v6List.ElementAtOrDefault(1) ?? "",
                isAuto,
                v4Active,
                v6Active
            );
            Debug.WriteLine(_result);
            return _result;
        }
        private bool IsDnsDhcpEnabled()
        {
            try
            {
                // У каждого адаптера есть уникальный ID (GUID)
                string adapterId = _interface.Id;
                string registryPath = $@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{adapterId}";

                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (key == null) return true;

                    // Параметр NameServer содержит статические DNS. 
                    // Если он пуст или отсутствует — значит DNS приходят по DHCP.
                    var nameServer = key.GetValue("NameServer") as string;
                    return string.IsNullOrWhiteSpace(nameServer);
                }
            }
            catch
            {
                return true; // По умолчанию считаем DHCP, если нет доступа к реестру
            }
        }
        public (bool v4Active, bool v6Active) GetProtocolStatusWmi()
        {
            bool v4 = false;
            bool v6 = false;

            try
            {
                // Ищем строго по GUID адаптера, это исключает ошибки в именах
                string query = $"SELECT IPEnabled, IPAddress FROM Win32_NetworkAdapterConfiguration WHERE SettingID = '{_interface.Id}'";

                using (var searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        // Если IPEnabled = false, значит стек протоколов на этом адаптере выключен
                        if (!(bool)obj["IPEnabled"]) return (false, false);

                        string[] addresses = (string[])obj["IPAddress"];
                        if (addresses != null)
                        {
                            foreach (string addr in addresses)
                            {
                                // Проверяем наличие IPv4
                                if (addr.Contains(".")) v4 = true;

                                // Проверяем наличие IPv6 (игнорируя сервисные fe80)
                                if (addr.Contains(":") && !addr.StartsWith("fe80", StringComparison.OrdinalIgnoreCase))
                                    v6 = true;
                            }
                        }
                    }
                }
            }
            catch { /* Обработка ошибок доступа */ }

            return (v4, v6);
        }
        public ObservableCollection<string> GetDnsTypes() => _dnsTypes;
        private readonly ObservableCollection<string> _dnsTypes = new ObservableCollection<string>()
        {
              "Автоматически(DHCP)",
            "Вручную"
        };
        public IP GetIpv4FromWmi()
        {
            string address = "";
            string mask = "";
            string gateway = "";
            bool isAuto = true; // По умолчанию считаем DHCP

            try
            {
                // Добавили DHCPEnabled в запрос
                string query = $"SELECT IPAddress, IPSubnet, DefaultIPGateway, DHCPEnabled FROM Win32_NetworkAdapterConfiguration WHERE SettingID = '{_interface.Id}'";

                using (var searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        // Получаем статус DHCP (True = Авто, False = Вручную)
                        isAuto = (bool)obj["DHCPEnabled"];

                        string[] addresses = (string[])obj["IPAddress"];
                        string[] subnets = (string[])obj["IPSubnet"];
                        string[] gateways = (string[])obj["DefaultIPGateway"];

                        if (addresses != null)
                        {
                            int v4Index = Array.FindIndex(addresses, a => a.Contains("."));
                            if (v4Index != -1)
                            {
                                address = addresses[v4Index];
                                mask = (subnets != null && subnets.Length > v4Index) ? subnets[v4Index] : "";
                            }
                        }

                        if (gateways != null)
                        {
                            gateway = gateways.FirstOrDefault(g => g.Contains(".")) ?? "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WMI IPv4 Error: {ex.Message}");
            }

            return new IP(address, mask, gateway, isAuto);
        }
        public async Task SaveIpSettings(IP _ipSettings)
        {

            // Используем кавычки, чтобы имена вроде "Wi-Fi 2" не ломали команду
            string interfaceIdentifier = $"name=\"{_interface.Id}\"";

            await Task.Run(() =>
            {
                // 1. Получаем индекс адаптера (Index). Это число, которое netsh понимает лучше всего.
                // Ищем системный адаптер по ID, который хранится в вашей модели _interface
                var adapter = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(n => n.Id == _interface.Id);

                if (adapter == null)
                {
                    Debug.WriteLine("Ошибка: Адаптер не найден в системе.");
                    return;
                }

                // Получаем IPv4 индекс (например, 12)
                int interfaceIndex = adapter.GetIPProperties().GetIPv4Properties().Index;

                // Теперь идентификатор — это просто число без кавычек и скобок
                string interfaceIdentifier = interfaceIndex.ToString();

                string args;
                if (_ipSettings._isAuto)
                {
                    // 1. Сброс IP на DHCP через Индекс
                    args = $"interface ipv4 set address {interfaceIdentifier} source=dhcp";
                    var resIp = RunNetsh(args);
                    Debug.WriteLine($"IP DHCP Reset (Index {interfaceIndex}): {resIp}");

                    // 2. Сброс DNS на DHCP
                    var resDns = RunNetsh($"interface ipv4 set dnsservers {interfaceIdentifier} source=dhcp");
                    Debug.WriteLine($"DNS DHCP Reset: {resDns}");
                }
                else
                {
                    // Формируем команду для статики через Индекс
                    string ip = _ipSettings._address.Trim();
                    string mask = _ipSettings._mask.Trim();
                    string gate = _ipSettings._gateway?.Trim();

                    if (string.IsNullOrWhiteSpace(gate))
                    {
                        // Используем store=active для мгновенного применения без лишних проверок
                        args = $"interface ipv4 set address {interfaceIdentifier} static {ip} {mask} store=active";
                    }
                    else
                    {
                        // 1 — метрика шлюза
                        args = $"interface ipv4 set address {interfaceIdentifier} static {ip} {mask} {gate} 1 store=active";
                    }

                    Debug.WriteLine($"COMMAND: netsh {args}");
                    var result = RunNetsh(args);
                    Debug.WriteLine($"IP Static Set: {result}");
                }
            });
        }
        public (bool _isValid, string _errorMessage) ValidateIP(IP _data)
        {
            bool IsStrictIpv4(string ip)
            {
                if (string.IsNullOrWhiteSpace(ip)) return false;
                if (ip.Count(c => c == '.') != 3) return false;
                return IPAddress.TryParse(ip, out var addr) &&
                       addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
            }

            if (_data._isAuto) return (true, string.Empty);

            // 1. Базовая проверка формата
            if (!IsStrictIpv4(_data._address)) return (false, "Некорректный IP-адрес");
            if (!IsStrictIpv4(_data._mask)) return (false, "Некорректная маска подсети");

            // 2. Проверка на 127.x.x.x
            if (_data._address.StartsWith("127."))
                return (false, "IP-адрес не может начинаться с 127 (Loopback)");

            // 3. Проверка логики Маски (упрощенно: не должна быть 0.0.0.0 или 255.255.255.255 в большинстве случаев)
            if (_data._mask == "0.0.0.0" || _data._mask == "255.255.255.255")
                return (false, "Недопустимая маска подсети");

            // 4. Проверка Шлюза
            if (!string.IsNullOrWhiteSpace(_data._gateway))
            {
                if (!IsStrictIpv4(_data._gateway))
                    return (false, "Некорректный формат шлюза");

                if (_data._address == _data._gateway)
                    return (false, "IP и Шлюз не могут совпадать");

                // [ADVANCED] Проверка на одну подсеть
                try
                {
                    var ipAddr = IPAddress.Parse(_data._address).GetAddressBytes();
                    var maskAddr = IPAddress.Parse(_data._mask).GetAddressBytes();
                    var gateAddr = IPAddress.Parse(_data._gateway).GetAddressBytes();

                    for (int i = 0; i < 4; i++)
                    {
                        if ((ipAddr[i] & maskAddr[i]) != (gateAddr[i] & maskAddr[i]))
                            return (false, "Шлюз должен находиться в той же подсети, что и IP");
                    }
                }
                catch { return (false, "Ошибка расчета подсети"); }
            }

            return (true, string.Empty);
        }
        public void FlushDNS()
        {
            RunNetsh("/c ipconfig /flushdns");
            MessageBox.Show("Кэш очищен.", "Уведомление", MessageBoxButton.OK);

        }
        public async Task RepairNetwork()
        {
            await Task.Run(() => {
                // 1. Освобождаем текущий IP
                RunNetsh( "/c ipconfig /release");
                // 2. Очистка кэша DNS
                RunNetsh( "/c ipconfig /flushdns");
                // 3. Сброс каталога Winsock (исправляет ошибки сокетов)
                RunNetsh( "winsock reset");
                // 4. Сброс стека TCP/IP
                RunNetsh("int ip reset");
                // 5. Обновляем IP (запрашиваем у роутера заново)
                RunNetsh("/c ipconfig /renew");
            });
            MessageBox.Show("Система успешно выполнила следующие операции:\r\n• Стек TCP/IP и каталог Winsock сброшены\r\n• Кэш DNS полностью очищен\r\n• IP-адрес обновлен через DHCP\r\n", "Уведомление", MessageBoxButton.OK);
        }
      
    }
}
