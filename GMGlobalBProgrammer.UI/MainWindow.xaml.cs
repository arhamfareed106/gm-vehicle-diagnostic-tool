using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GMGlobalBProgrammer.Core.J2534;
using GMGlobalBProgrammer.Core.Functions;

namespace GMGlobalBProgrammer.UI;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MainWindow> _logger;
    private readonly IJ2534Manager _j2534Manager;
    private IBatteryMonitor _batteryMonitor;
    private CancellationTokenSource _batteryMonitorCts;
    
    // Parameterless constructor for XAML designer support
    public MainWindow() : this(null!, null!)
    {
        // This constructor is only for XAML designer support
        // The actual initialization happens in the parameterized constructor
    }
    
    public MainWindow(IServiceProvider serviceProvider, ILogger<MainWindow> logger)
    {
        InitializeComponent();
        
        _serviceProvider = serviceProvider;
        _logger = logger;
        _j2534Manager = serviceProvider?.GetService<IJ2534Manager>();
        
        LoadDeviceList();
    }
    
    private void LoadDeviceList()
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("=== STARTING UI DEVICE LIST LOADING ===");
        _logger.LogInformation("========================================");
        
        StatusText.Text = "Scanning for J2534 devices...";
        DeviceCombo.Items.Clear();
        
        // Verify J2534Manager is properly initialized
        _logger.LogInformation($"J2534Manager instance type: {_j2534Manager?.GetType().Name ?? "NULL"}");
        _logger.LogInformation($"Is J2534Manager null: {_j2534Manager == null}");
        
        if (_j2534Manager == null)
        {
            StatusText.Text = "Error: J2534Manager not available";
            _logger.LogError("J2534Manager is NULL - service registration failed!");
            MessageBox.Show("Failed to initialize J2534 device manager. Please check the application logs.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        // Enhanced diagnostic - Check if services are available
        _logger.LogInformation($"Service provider is null: {_serviceProvider == null}");
        
        // Check if the application is running in proper mode
        _logger.LogInformation($"Application bitness: {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");
        _logger.LogInformation($"System is 64-bit: {Environment.Is64BitOperatingSystem}");
        
        // Force a re-scan to ensure we get fresh data
        _logger.LogInformation("=== TRIGGERING NEW DEVICE SCAN ===");
        var scanTask = _j2534Manager.ScanForDevicesAsync();
        scanTask.Wait(); // Wait synchronously for debugging
        _logger.LogInformation($"Scan completed. Task status: {scanTask.Status}");
        
        // Handle case where services are not available (designer mode)
        if (_serviceProvider == null)
        {
            StatusText.Text = "Design mode - services not available";
            _logger.LogWarning("Running in design mode - services not available");
            return;
        }
        
        // Get devices from the manager
        _logger.LogInformation("Requesting device list from J2534Manager...");
        var devices = _j2534Manager.AvailableDevices;
        
        _logger.LogInformation($"========================================");
        _logger.LogInformation($"J2534Manager returned {devices.Count} devices");
        _logger.LogInformation($"========================================");
        
        if (devices.Count == 0)
        {
            StatusText.Text = "No J2534 devices found. Check installation and registry.";
            
            _logger.LogWarning("========================================");
            _logger.LogWarning("!!! NO J2534 DEVICES DETECTED !!!");
            _logger.LogWarning("========================================");
            
            _logger.LogWarning("");
            _logger.LogWarning("Diagnostic information:");
            _logger.LogWarning($"- Application bitness: {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");
            _logger.LogWarning($"- System is 64-bit: {Environment.Is64BitOperatingSystem}");
            
            _logger.LogWarning("");
            _logger.LogWarning("Possible causes:");
            _logger.LogWarning("1. No J2534 drivers installed");
            _logger.LogWarning("2. Drivers not registered in registry");
            _logger.LogWarning("3. DLL files missing or corrupted");
            _logger.LogWarning("4. Bitness mismatch (32-bit vs 64-bit)");
            _logger.LogWarning("5. Insufficient permissions - try running as Administrator");
            _logger.LogWarning("6. Driver conflicts");
            
            _logger.LogWarning("");
            _logger.LogWarning("To resolve this issue:");
            _logger.LogWarning("1. Check if drivers are properly installed");
            _logger.LogWarning("2. Verify driver registration in Windows Registry");
            _logger.LogWarning("3. Check if the application matches the driver architecture (32-bit/64-bit)");
            _logger.LogWarning("4. Run application as Administrator for registry access");
            return;
        }
        
        // Populate UI with found devices
        _logger.LogInformation($"=== POPULATING DEVICE COMBO BOX ===");
        int deviceCount = 0;
        
        foreach (var device in devices)
        {
            deviceCount++;
            DeviceCombo.Items.Add(device);
            _logger.LogInformation($"{deviceCount}. {device.Name} ({device.Vendor})");
            _logger.LogInformation($"   Device ID: {device.DeviceId}");
            _logger.LogInformation($"   Is Available: {device.IsAvailable}");
            _logger.LogInformation($"   File exists: {File.Exists(device.DeviceId)}");
            _logger.LogInformation("");
        }
        
        DeviceCombo.SelectedIndex = 0;
        StatusText.Text = $"Found {deviceCount} J2534 devices ready to connect";
        
        _logger.LogInformation($"========================================");
        _logger.LogInformation($"=== UI DEVICE LIST LOADED SUCCESSFULLY ===");
        _logger.LogInformation($"Successfully loaded {deviceCount} J2534 devices");
        _logger.LogInformation($"========================================");
    }
    
    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedDevice = DeviceCombo.SelectedItem as J2534DeviceInfo;
        if (selectedDevice == null)
        {
            MessageBox.Show("Please select a device first.");
            return;
        }
        
        StatusText.Text = "Connecting to device...";
        var device = await _j2534Manager.ConnectToDeviceAsync(selectedDevice);
        
        if (device != null)
        {
            StatusText.Text = $"Connected to {selectedDevice.Name} successfully";
        }
        else
        {
            MessageBox.Show($"Failed to connect to {selectedDevice.Name}");
            StatusText.Text = "Connection failed";
        }
    }
    
    private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Disconnected";
    }
    
    private void SBATButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("SBAT functionality");
    }
    
    private void VINWriteButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("VIN Write functionality");
    }
    
    private void InjectorButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Injector functionality");
    }
    
    private void LoadLogFileButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Load log file functionality");
    }
    
    private void ExecuteSBATButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Execute SBAT functionality");
    }
    
    private void WriteVINButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Write VIN functionality");
    }
    
    private void ProgramInjectorsButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Program injectors functionality");
    }
    
    private void LoadInjectorFileButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Load injector file functionality");
    }
    
    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        // Clear log functionality would go here
    }
    
    private void SaveLogButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Save log functionality");
    }
    
    private void BatteryButton_Click(object sender, RoutedEventArgs e)
    {
        // Hide all panels
        SBATPanel.Visibility = Visibility.Collapsed;
        VINWritePanel.Visibility = Visibility.Collapsed;
        InjectorPanel.Visibility = Visibility.Collapsed;
        BatteryPanel.Visibility = Visibility.Visible;
        
        StatusText.Text = "Battery voltage monitoring selected";
    }
    
    private async void ReadBatteryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_batteryMonitor == null)
        {
            MessageBox.Show("Please connect to a device first.", "Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        StatusText.Text = "Reading battery voltage...";
        ReadBatteryButton.IsEnabled = false;
        
        try
        {
            var result = await _batteryMonitor.ReadBatteryVoltageAsync();
            
            if (result.Success)
            {
                UpdateBatteryDisplay(result);
                StatusText.Text = "Battery voltage read successfully";
            }
            else
            {
                MessageBox.Show($"Failed to read battery voltage: {result.ErrorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Failed to read battery voltage";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reading battery voltage: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Error reading battery voltage";
        }
        finally
        {
            ReadBatteryButton.IsEnabled = true;
        }
    }
    
    private async void StartBatteryMonitorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_batteryMonitor == null)
        {
            MessageBox.Show("Please connect to a device first.", "Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        StartBatteryMonitorButton.IsEnabled = false;
        StopBatteryMonitorButton.IsEnabled = true;
        BatteryStatusText.Text = "Monitoring...";
        StatusText.Text = "Battery monitoring started";
        
        _batteryMonitorCts = new CancellationTokenSource();
        
        try
        {
            await _batteryMonitor.StartContinuousMonitoringAsync(UpdateBatteryTopDisplay, _batteryMonitorCts.Token);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Battery monitoring error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            StartBatteryMonitorButton.IsEnabled = true;
            StopBatteryMonitorButton.IsEnabled = false;
            BatteryStatusText.Text = "Not Monitoring";
            StatusText.Text = "Battery monitoring stopped";
        }
    }
    
    private void StopBatteryMonitorButton_Click(object sender, RoutedEventArgs e)
    {
        _batteryMonitorCts?.Cancel();
        StartBatteryMonitorButton.IsEnabled = true;
        StopBatteryMonitorButton.IsEnabled = false;
        BatteryStatusText.Text = "Not Monitoring";
        StatusText.Text = "Battery monitoring stopped";
    }
    
    private void UpdateBatteryTopDisplay(BatteryVoltageResult result)
    {
        Dispatcher.Invoke(() =>
        {
            if (result.Success)
            {
                BatteryVoltageText.Text = $"{result.Voltage:F2}V";
                BatteryStatusText.Text = result.Status;
                
                // Update health indicator color
                BatteryHealthIndicator.Fill = GetHealthColor(result.Voltage);
            }
            else
            {
                BatteryVoltageText.Text = "Error";
                BatteryStatusText.Text = result.ErrorMessage;
                BatteryHealthIndicator.Fill = new SolidColorBrush(Colors.Gray);
            }
        });
    }
    
    private void UpdateBatteryDisplay(BatteryVoltageResult result)
    {
        if (result.Success)
        {
            DetailedVoltageText.Text = $"{result.Voltage:F2}V";
            DetailedStatusText.Text = result.Status;
            LastUpdatedText.Text = result.Timestamp.ToString("HH:mm:ss");
            HealthStatusText.Text = result.IsHealthy ? "Healthy" : "Attention Required";
            DetailedHealthIndicator.Fill = GetHealthColor(result.Voltage);
            
            // Also update top display
            BatteryVoltageText.Text = $"{result.Voltage:F2}V";
            BatteryStatusText.Text = result.Status;
            BatteryHealthIndicator.Fill = GetHealthColor(result.Voltage);
        }
        else
        {
            DetailedVoltageText.Text = "Error";
            DetailedStatusText.Text = result.ErrorMessage;
            DetailedHealthIndicator.Fill = new SolidColorBrush(Colors.Gray);
        }
    }
    
    private SolidColorBrush GetHealthColor(double voltage)
    {
        if (voltage < 11.5 || voltage > 15.5)
            return new SolidColorBrush(Colors.Red);
        else if (voltage < 12.0)
            return new SolidColorBrush(Colors.Orange);
        else if (voltage >= 12.0 && voltage <= 14.8)
            return new SolidColorBrush(Colors.Green);
        else if (voltage > 14.8)
            return new SolidColorBrush(Colors.Blue);
        else
            return new SolidColorBrush(Colors.Gray);
    }
}