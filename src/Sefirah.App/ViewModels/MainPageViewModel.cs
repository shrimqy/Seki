using Sefirah.App.Data.Contracts;
using Sefirah.App.Data.EventArguments;
using Sefirah.App.Data.Models;
using System.IO;
using System.Windows.Input;
using Windows.Storage;

namespace Sefirah.App.ViewModels
{
    public sealed class MainPageViewModel : ObservableObject
    {

        // Dependency Injection
        private ISessionManager SessionManager { get; } = Ioc.Default.GetRequiredService<ISessionManager>();
        private IMessageHandler MessageHandler { get; } = Ioc.Default.GetRequiredService<IMessageHandler>();
        private INotificationService NotificationService { get; } = Ioc.Default.GetRequiredService<INotificationService>();
        private IMdnsService MdnsService { get; } = Ioc.Default.GetRequiredService<IMdnsService>();

        // Properties
        private Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
        private Device _deviceInfo = new();
        private DeviceStatus _deviceStatus = new();
        private bool _connectionStatus = false;
        public ReadOnlyObservableCollection<NotificationMessage> RecentNotifications => NotificationService.NotificationHistory;
        public ObservableCollection<DiscoveredDevice> DiscoveredDevices { get; } = [];

        public Device DeviceInfo
        {
            get => _deviceInfo;
            set => SetProperty(ref _deviceInfo, value);
        }

        public DeviceStatus DeviceStatus
        {
            get => _deviceStatus;
            set => SetProperty(ref _deviceStatus, value);
        }

        public bool ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                if (SetProperty(ref _connectionStatus, value))
                {
                    OnPropertyChanged(nameof(ConnectionButtonText));
                }
            }
        }

        public string ConnectionButtonText => ConnectionStatus ? "Connected" : "Disconnected";

        public ICommand ToggleConnectionCommand { get; }

        // Command to clear notifications
        public ICommand ClearAllNotificationsCommand { get; }

        public MainPageViewModel()
        {
            try
            {
                Debug.WriteLine("Starting MainPageViewModel initialization");

                _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                if (_dispatcher == null)
                {
                    throw new InvalidOperationException("Dispatcher not available");
                }


                Debug.WriteLine("Services initialized");

                // Wire up events
                SessionManager.ClientConnectionStatusChanged += OnConnectionStatusChange;
                MdnsService.DeviceDiscovered += OnDeviceDiscovered;
                MdnsService.DeviceLost += OnDeviceLost;

                Debug.WriteLine("Events wired up");

                // Initialize commands
                ToggleConnectionCommand = new RelayCommand(ToggleConnection);
                ClearAllNotificationsCommand = new RelayCommand(ClearAllNotifications);

                Debug.WriteLine("Commands initialized");

                Debug.WriteLine("MainPageViewModel initialization completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Critical error in MainPageViewModel initialization: {ex}");
                throw;
            }
        }

        private void OnConnectionStatusChange(object? sender, ConnectedSessionArgs args)
        {
            Debug.WriteLine($"connection status changed");

            // If the connection is re-established (changing from false to true)
            if (!_connectionStatus && args.IsConnected || args.IsConnected)
            {
                _dispatcher.TryEnqueue(() => NotificationService.ClearHistory());
            }

            // Only update the ConnectionStatus property if the status changes
            if (_connectionStatus != args.IsConnected)
            {
                _dispatcher.TryEnqueue(() =>
                {
                    ConnectionStatus = args.IsConnected;
                    if (args.Device != null)
                    {
                        DeviceInfo = args.Device;
                    }
                });
            }
        }

        private void ClearAllNotifications()
        {
            NotificationService.ClearAllNotification();
        }

        private void ToggleConnection()
        {
            if (ConnectionStatus)
            {
                //SocketService.Instance.DisconnectAll();
            }
        }

        private void OnDeviceStatusReceived(object? sender, DeviceStatus deviceStatus)
        {
            _dispatcher.TryEnqueue(() => DeviceStatus = deviceStatus);
        }

        private void OnDeviceInfoReceived(object? sender, Device? deviceInfo)
        {
            if (deviceInfo != null)
            {
                _dispatcher.TryEnqueue(() => DeviceInfo = deviceInfo);
            }
        }

        private void OnDeviceDiscovered(object? sender, DiscoveredDevice discoveredDevice)
        {
            if (discoveredDevice == null) return;

            _dispatcher.TryEnqueue(() =>
            {
                var existingDevice = DiscoveredDevices.FirstOrDefault(d => d.ServiceName == discoveredDevice.ServiceName);

                if (existingDevice == null)
                {
                    DiscoveredDevices.Add(discoveredDevice);
                    Debug.WriteLine($"Discovered new device: {discoveredDevice.FormattedKey}");
                }
                else if (existingDevice.FormattedKey != discoveredDevice.FormattedKey)
                {
                    // Directly update the existing device's key
                    int index = DiscoveredDevices.IndexOf(existingDevice);
                    DiscoveredDevices[index] = discoveredDevice;
                    Debug.WriteLine($"Updated device key for: {discoveredDevice.ServiceName}");
                }
            });
        }

        private void OnDeviceLost(object? sender, string device)
        {
            if (device == null) return;

            _dispatcher.TryEnqueue(() =>
            {
                var existingDevice = DiscoveredDevices.FirstOrDefault(d => d.ServiceName == device);
                if (existingDevice != null)
                {
                    Debug.WriteLine($"Removing device: {existingDevice.DeviceName}");
                    DiscoveredDevices.Remove(existingDevice);
                }
            });
        }


        public void RemoveNotification(string notificationKey)
        {
            NotificationService.RemoveNotification(notificationKey, false);
        }


        public void Cleanup()
        {
            SessionManager.ClientConnectionStatusChanged -= OnConnectionStatusChange;
            MdnsService.DeviceDiscovered -= OnDeviceDiscovered;
            MdnsService.DeviceLost -= OnDeviceLost;
        }
    }
}
