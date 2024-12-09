using MeaMod.DNS.Model;
using MeaMod.DNS.Multicast;
using Sefirah.App.Data.Contracts;
using Sefirah.App.Data.Models;
using Sefirah.App.Utils;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace Sefirah.App.Services;

public class MdnsService(ILogger logger, ISocketService socketService) : IMdnsService
{
    private readonly ILogger _logger = logger;
    private readonly ISocketService _socketService = socketService ?? throw new ArgumentNullException(nameof(socketService));
    private MulticastService? _multicastService;
    private ServiceProfile? _serviceProfile;
    private ServiceDiscovery? _serviceDiscovery;
    public event EventHandler<DiscoveredDevice>? DeviceDiscovered;
    public event EventHandler<string>? DeviceLost;

    /// <inheritdoc />
    public async Task AdvertiseServiceAsync()
    {
        try
        {
            // Generate ECDH keys
            var publicKey = ECDHHelper.GenerateKeys();
            var port = _socketService.Port;
            _logger.Debug("port {0}", port);

            // Fetch current user information
            var (deviceID, username, _) = await CurrentUserInformation.GetCurrentUserInfoAsync();

            // Set up the service profile
            _serviceProfile = new ServiceProfile(deviceID, "_foo._tcp", 1024);
            _serviceProfile.AddProperty("ipAddress", NetworkHelper.GetLocalIPAddress());
            _serviceProfile.AddProperty("deviceName", username);
            _serviceProfile.AddProperty("port", port.ToString());
            _serviceProfile.AddProperty("publicKey", publicKey);

            // Advertise the service
            _multicastService = new MulticastService();
            _serviceDiscovery = new ServiceDiscovery(_multicastService);
            _serviceDiscovery.Advertise(_serviceProfile);

            _logger.Info("Advertising service for {0}", _serviceProfile.InstanceName);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to advertise service", ex);
            throw;
        }
    }

    /// <inheritdoc />
    public void UnAdvertiseService()
    {
        if (_serviceDiscovery != null && _serviceProfile != null)
        {
            _logger.Info("Un-advertising service for {0}", _serviceProfile.InstanceName);
            _serviceDiscovery.Unadvertise(_serviceProfile);
        }
        else
        {
            _logger.Warn("Service not advertised or already unadvertised");
        }
    }

    /// <inheritdoc />
    public void StartDiscovery()
    {
        try
        {
            if (_serviceDiscovery == null || _multicastService == null) return;

            _serviceDiscovery.ServiceInstanceDiscovered += (sender, args) =>
            {
                if (_serviceProfile != null && args.ServiceInstanceName == _serviceProfile.FullyQualifiedName) return;

                _logger.Info("Discovered service instance: {0}", args.ServiceInstanceName);
                _multicastService.SendQuery(args.ServiceInstanceName, type: DnsType.TXT);

                _multicastService.AnswerReceived += (s, e) =>
                {
                    var txtRecords = e.Message.Answers.OfType<TXTRecord>();
                    foreach (var txtRecord in txtRecords)
                    {
                        string? deviceName = null;
                        string? publicKey = null;

                        foreach (var txtData in txtRecord.Strings)
                        {
                            var cleanTxtData = txtData.Trim();
                            var parts = cleanTxtData.Split('=', 2);
                            if (parts.Length == 2)
                            {
                                if (parts[0] == "deviceName")
                                    deviceName = parts[1];
                                else if (parts[0] == "publicKey")
                                    publicKey = parts[1];
                            }
                        }

                        if (!string.IsNullOrEmpty(deviceName) && !string.IsNullOrEmpty(publicKey))
                        {
                            var discoveredDevice = new DiscoveredDevice
                            {
                                ServiceName = args.ServiceInstanceName.ToString(),
                                PublicKey = publicKey,
                                DeviceName = deviceName,
                            };
                            discoveredDevice = ECDHHelper.DeriveSharedSecret(discoveredDevice, publicKey);
                            DeviceDiscovered?.Invoke(this, discoveredDevice);
                        }
                    }
                };
            };

            _serviceDiscovery.ServiceInstanceShutdown += (sender, args) =>
            {
                _logger.Info("Service instance shutdown: {0}", args.ServiceInstanceName);
                DeviceLost?.Invoke(this, args.ServiceInstanceName.ToString());
            };

            _multicastService.Start();
            _logger.Info("Started mDNS discovery service");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to start discovery service", ex);
            throw;
        }
    }
}