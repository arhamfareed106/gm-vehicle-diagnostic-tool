using System;
using System.Threading;
using System.Threading.Tasks;
using GMGlobalBProgrammer.Core.UDS;

namespace GMGlobalBProgrammer.Core.Functions
{
    /// <summary>
    /// Monitors battery voltage from the vehicle's ECU in real-time
    /// </summary>
    public interface IBatteryMonitor
    {
        Task<BatteryVoltageResult> ReadBatteryVoltageAsync();
        Task StartContinuousMonitoringAsync(Action<BatteryVoltageResult> callback, CancellationToken cancellationToken);
        void StopMonitoring();
    }

    public class BatteryMonitor : IBatteryMonitor
    {
        private readonly IUDSClient _udsClient;
        private CancellationTokenSource _monitoringCts;
        private bool _isMonitoring;

        // DID for battery voltage (GM Global B standard)
        private const ushort BATTERY_VOLTAGE_DID = 0xF442;

        public BatteryMonitor(IUDSClient udsClient)
        {
            _udsClient = udsClient ?? throw new ArgumentNullException(nameof(udsClient));
            _isMonitoring = false;
        }

        /// <summary>
        /// Reads the current battery voltage from the ECU
        /// </summary>
        public async Task<BatteryVoltageResult> ReadBatteryVoltageAsync()
        {
            try
            {
                // Read battery voltage using UDS service 0x22 (ReadDataByIdentifier)
                var response = await _udsClient.ReadDataByIdentifier(BATTERY_VOLTAGE_DID);

                if (!response.Success)
                {
                    return new BatteryVoltageResult
                    {
                        Success = false,
                        ErrorMessage = $"Failed to read battery voltage: {response.ErrorDescription}",
                        Timestamp = DateTime.Now
                    };
                }

                // Parse voltage from response data
                // Format: 2 bytes, big-endian, unit: 0.01V
                if (response.Data == null || response.Data.Length < 2)
                {
                    return new BatteryVoltageResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid response data length",
                        Timestamp = DateTime.Now
                    };
                }

                // Convert bytes to voltage value
                ushort rawValue = (ushort)((response.Data[0] << 8) | response.Data[1]);
                double voltage = rawValue * 0.01; // Convert to volts

                return new BatteryVoltageResult
                {
                    Success = true,
                    Voltage = voltage,
                    IsHealthy = voltage >= 11.5 && voltage <= 15.5,
                    Status = GetVoltageStatus(voltage),
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                return new BatteryVoltageResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.Now
                };
            }
        }

        /// <summary>
        /// Starts continuous battery voltage monitoring
        /// </summary>
        public async Task StartContinuousMonitoringAsync(Action<BatteryVoltageResult> callback, CancellationToken cancellationToken)
        {
            if (_isMonitoring)
            {
                throw new InvalidOperationException("Monitoring is already running");
            }

            _isMonitoring = true;
            _monitoringCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                while (!_monitoringCts.Token.IsCancellationRequested)
                {
                    var result = await ReadBatteryVoltageAsync();
                    callback?.Invoke(result);

                    // Wait 1 second before next reading
                    await Task.Delay(1000, _monitoringCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
            }
            finally
            {
                _isMonitoring = false;
            }
        }

        /// <summary>
        /// Stops continuous monitoring
        /// </summary>
        public void StopMonitoring()
        {
            _monitoringCts?.Cancel();
            _isMonitoring = false;
        }

        /// <summary>
        /// Determines the voltage status based on the reading
        /// </summary>
        private string GetVoltageStatus(double voltage)
        {
            if (voltage < 11.5)
                return "Critical Low";
            else if (voltage < 12.0)
                return "Low";
            else if (voltage >= 12.0 && voltage <= 14.8)
                return "Normal";
            else if (voltage > 14.8 && voltage <= 15.5)
                return "High";
            else
                return "Critical High";
        }
    }

    /// <summary>
    /// Result of a battery voltage reading
    /// </summary>
    public class BatteryVoltageResult
    {
        public bool Success { get; set; }
        public double Voltage { get; set; }
        public bool IsHealthy { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            if (Success)
                return $"{Voltage:F2}V - {Status}";
            else
                return $"Error: {ErrorMessage}";
        }
    }
}
