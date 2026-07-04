using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GMGlobalBProgrammer.Core.J2534
{
    public interface IJ2534Manager
    {
        event EventHandler<J2534DeviceEventArgs> DeviceConnected;
        event EventHandler<J2534DeviceEventArgs> DeviceDisconnected;
        event EventHandler<string> LogMessage;

        List<J2534DeviceInfo> AvailableDevices { get; }
        IJ2534Device CurrentDevice { get; }
        bool IsDeviceConnected { get; }

        Task InitializeAsync();
        Task<List<J2534DeviceInfo>> ScanForDevicesAsync();
        Task<IJ2534Device> ConnectToDeviceAsync(J2534DeviceInfo deviceInfo);
        Task<bool> DisconnectCurrentDeviceAsync();
        Task<bool> TestDeviceAsync(J2534DeviceInfo deviceInfo);
    }

    public class J2534Manager : IJ2534Manager, IDisposable
    {
        private readonly ILogger<J2534Manager> _logger;
        private readonly ILogger<VendorDetector> _vendorLogger;
        private readonly VendorDetector _vendorDetector;
        private readonly List<J2534DeviceInfo> _availableDevices;
        private IJ2534Device _currentDevice;
        private bool _isDisposed;

        public event EventHandler<J2534DeviceEventArgs> DeviceConnected;
        public event EventHandler<J2534DeviceEventArgs> DeviceDisconnected;
        public event EventHandler<string> LogMessage;

        public List<J2534DeviceInfo> AvailableDevices => _availableDevices.ToList();
        public IJ2534Device CurrentDevice => _currentDevice;
        public bool IsDeviceConnected => _currentDevice?.IsConnected ?? false;

        public J2534Manager(ILogger<J2534Manager> logger, ILogger<VendorDetector> vendorLogger)
        {
            _logger = logger;
            _vendorLogger = vendorLogger;
            _vendorDetector = new VendorDetector(vendorLogger);
            _availableDevices = new List<J2534DeviceInfo>();
            _currentDevice = null;
            _isDisposed = false;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing J2534 Manager");
                await ScanForDevicesAsync();
                _logger.LogInformation($"J2534 Manager initialized with {_availableDevices.Count} available devices");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing J2534 Manager");
                throw;
            }
        }

        public async Task<List<J2534DeviceInfo>> ScanForDevicesAsync()
        {
            try
            {
                _logger.LogInformation("Scanning for J2534 devices...");
                
                var devices = _vendorDetector.DetectAvailableDevices();
                _availableDevices.Clear();
                _availableDevices.AddRange(devices);

                // Test each device for connectivity
                foreach (var device in devices)
                {
                    device.IsAvailable = await TestDeviceAsync(device);
                    if (device.IsAvailable)
                    {
                        _logger.LogInformation($"Device available: {device.Name}");
                    }
                    else
                    {
                        _logger.LogWarning($"Device not responding: {device.Name}");
                    }
                }

                LogMessage?.Invoke(this, $"Found {_availableDevices.Count} J2534 devices ({_availableDevices.Count(d => d.IsAvailable)} available)");
                return _availableDevices.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning for devices");
                throw;
            }
        }

        public async Task<IJ2534Device> ConnectToDeviceAsync(J2534DeviceInfo deviceInfo)
        {
            try
            {
                if (_currentDevice != null)
                {
                    await DisconnectCurrentDeviceAsync();
                }

                _logger.LogInformation($"Connecting to device: {deviceInfo.Name}");

                var device = new RealJ2534Device(deviceInfo.DeviceId)
                {
                    Name = deviceInfo.Name,
                    Vendor = deviceInfo.Vendor,
                    DeviceId = deviceInfo.DeviceId
                };
                
                var connected = await device.ConnectAsync();
                if (!connected)
                {
                    _logger.LogError($"Failed to connect to device: {deviceInfo.Name}");
                    device.Dispose();
                    return null;
                }

                // Open channel for ISO15765 (CAN) with 29-bit addressing
                var channelResult = await device.OpenChannelAsync(
                    ProtocolId.ISO15765, 
                    ConnectFlags.CAN_29BIT_ID | ConnectFlags.ISO15765_ADDR_TYPE, 
                    500000);

                if (channelResult != J2534Error.STATUS_NOERROR)
                {
                    _logger.LogError($"Failed to open channel: {channelResult}");
                    await device.DisconnectAsync();
                    device.Dispose();
                    return null;
                }

                _currentDevice = device;
                DeviceConnected?.Invoke(this, new J2534DeviceEventArgs(device));
                LogMessage?.Invoke(this, $"Connected to {deviceInfo.Name} successfully");

                _logger.LogInformation($"Successfully connected to device: {deviceInfo.Name}");
                return device;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception while connecting to device: {deviceInfo.Name}");
                return null;
            }
        }

        public async Task<bool> DisconnectCurrentDeviceAsync()
        {
            try
            {
                if (_currentDevice == null)
                    return true;

                _logger.LogInformation($"Disconnecting from device: {_currentDevice.Name}");

                var disconnected = await _currentDevice.DisconnectAsync();
                var device = _currentDevice;
                _currentDevice = null;

                if (disconnected)
                {
                    DeviceDisconnected?.Invoke(this, new J2534DeviceEventArgs(device));
                    LogMessage?.Invoke(this, $"Disconnected from {device.Name}");
                    _logger.LogInformation($"Successfully disconnected from device: {device.Name}");
                }
                else
                {
                    _logger.LogWarning($"Device reported disconnection failure: {device.Name}");
                }

                device.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while disconnecting device");
                return false;
            }
        }

        public async Task<bool> TestDeviceAsync(J2534DeviceInfo deviceInfo)
        {
            try
            {
                _logger.LogDebug($"Testing device: {deviceInfo.Name}");
                return await Task.Run(() => _vendorDetector.TestDeviceConnection(deviceInfo.DeviceId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error testing device: {deviceInfo.Name}");
                return false;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            try
            {
                _logger.LogInformation("Disposing J2534 Manager");
                DisconnectCurrentDeviceAsync().Wait();
                _isDisposed = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during J2534 Manager disposal");
            }
        }
    }

    public class J2534DeviceEventArgs : EventArgs
    {
        public IJ2534Device Device { get; }

        public J2534DeviceEventArgs(IJ2534Device device)
        {
            Device = device;
        }
    }
}