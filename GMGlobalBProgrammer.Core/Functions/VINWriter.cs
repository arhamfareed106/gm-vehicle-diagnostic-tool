using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GMGlobalBProgrammer.Core.UDS;
using Microsoft.Extensions.Logging;

namespace GMGlobalBProgrammer.Core.Functions
{
    public interface IVINWriter
    {
        event EventHandler<string> VINEvent;
        event EventHandler<VINWriteStatus> StatusChanged;

        bool IsRunning { get; }
        VINWriteStatus CurrentStatus { get; }
        string CurrentVIN { get; }

        Task<bool> WriteVINAsync(string vin);
        Task<bool> VerifyVINAsync();
        Task<bool> CancelAsync();
    }

    public class VINWriter : IVINWriter
    {
        private readonly IUDSClient _udsClient;
        private readonly IGMSecurityAccess _securityAccess;
        private readonly ILogger<VINWriter> _logger;
        private bool _isRunning;
        private VINWriteStatus _currentStatus;
        private string _currentVIN;

        public event EventHandler<string> VINEvent;
        public event EventHandler<VINWriteStatus> StatusChanged;

        public bool IsRunning => _isRunning;
        public VINWriteStatus CurrentStatus => _currentStatus;
        public string CurrentVIN => _currentVIN;

        public VINWriter(IUDSClient udsClient, IGMSecurityAccess securityAccess, ILogger<VINWriter> logger)
        {
            _udsClient = udsClient ?? throw new ArgumentNullException(nameof(udsClient));
            _securityAccess = securityAccess ?? throw new ArgumentNullException(nameof(securityAccess));
            _logger = logger;
            _isRunning = false;
            _currentStatus = VINWriteStatus.Idle;
            _currentVIN = string.Empty;
        }

        public async Task<bool> WriteVINAsync(string vin)
        {
            if (_isRunning)
            {
                LogEvent("VIN write already in progress");
                return false;
            }

            if (string.IsNullOrEmpty(vin) || vin.Length != 17)
            {
                LogEvent("Invalid VIN format - must be 17 characters");
                return false;
            }

            try
            {
                _currentVIN = vin.ToUpper();
                _isRunning = true;
                UpdateStatus(VINWriteStatus.Initializing);
                LogEvent($"Starting VIN write: {_currentVIN}");

                // Step 1: Establish programming session
                UpdateStatus(VINWriteStatus.EstablishingSession);
                LogEvent("Establishing programming session");
                
                var sessionResponse = await _udsClient.DiagnosticSessionControl(0x02); // Programming session
                if (!sessionResponse.IsPositive)
                {
                    LogEvent($"Failed to establish programming session: {sessionResponse.ErrorMessage}");
                    UpdateStatus(VINWriteStatus.Failed);
                    return false;
                }
                LogEvent("Programming session established");

                // Step 2: Security access
                UpdateStatus(VINWriteStatus.PerformingSecurityAccess);
                LogEvent("Performing security access");
                
                if (!await PerformSecurityAccessAsync())
                {
                    LogEvent("Security access failed");
                    UpdateStatus(VINWriteStatus.Failed);
                    return false;
                }
                LogEvent("Security access successful");

                // Step 3: Write VIN
                UpdateStatus(VINWriteStatus.WritingVIN);
                LogEvent("Writing VIN to ECU");
                
                var vinBytes = Encoding.ASCII.GetBytes(_currentVIN);
                var writeResponse = await _udsClient.WriteDataByIdentifier(GMDIDs.VIN, vinBytes);
                
                if (!writeResponse.IsPositive)
                {
                    LogEvent($"VIN write failed: {writeResponse.ErrorMessage}");
                    UpdateStatus(VINWriteStatus.Failed);
                    return false;
                }
                LogEvent("VIN written successfully");

                // Step 4: Verify VIN
                UpdateStatus(VINWriteStatus.VerifyingVIN);
                LogEvent("Verifying written VIN");
                
                var verifySuccess = await VerifyVINAsync();
                if (!verifySuccess)
                {
                    LogEvent("VIN verification failed");
                    UpdateStatus(VINWriteStatus.CompletedWithWarnings);
                    return true; // Consider it successful but with warnings
                }
                
                LogEvent("VIN verified successfully");
                UpdateStatus(VINWriteStatus.CompletedSuccessfully);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing VIN");
                LogEvent($"VIN write failed: {ex.Message}");
                UpdateStatus(VINWriteStatus.Failed);
                return false;
            }
            finally
            {
                _isRunning = false;
            }
        }

        public async Task<bool> VerifyVINAsync()
        {
            try
            {
                LogEvent("Reading VIN from ECU for verification");
                
                var readResponse = await _udsClient.ReadDataByIdentifier(GMDIDs.VIN);
                if (!readResponse.IsPositive)
                {
                    LogEvent($"Failed to read VIN: {readResponse.ErrorMessage}");
                    return false;
                }

                var readVIN = Encoding.ASCII.GetString(readResponse.Data).TrimEnd('\0');
                LogEvent($"Read VIN: {readVIN}");

                if (readVIN.Equals(_currentVIN, StringComparison.OrdinalIgnoreCase))
                {
                    LogEvent("VIN verification successful");
                    return true;
                }
                else
                {
                    LogEvent($"VIN mismatch - Expected: {_currentVIN}, Read: {readVIN}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying VIN");
                LogEvent($"VIN verification error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CancelAsync()
        {
            if (!_isRunning)
                return true;

            try
            {
                LogEvent("Cancelling VIN write operation");
                UpdateStatus(VINWriteStatus.Cancelling);
                _isRunning = false;
                UpdateStatus(VINWriteStatus.Cancelled);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling VIN write");
                return false;
            }
        }

        private async Task<bool> PerformSecurityAccessAsync()
        {
            try
            {
                // Request seed
                LogEvent("Requesting security seed");
                var seedResponse = await _udsClient.SecurityAccess(0x01); // Request seed for level 1
                
                if (!seedResponse.IsPositive)
                {
                    LogEvent($"Failed to get security seed: {seedResponse.ErrorMessage}");
                    return false;
                }

                if (seedResponse.Data.Length < 4)
                {
                    LogEvent("Invalid seed length received");
                    return false;
                }

                // Extract seed
                var seed = seedResponse.Data.Take(4).ToArray();
                LogEvent($"Received seed: [{string.Join(" ", seed.Select(b => b.ToString("X2")))}]");

                // Validate seed
                if (!_securityAccess.ValidateSeed(seed))
                {
                    LogEvent("Invalid seed received");
                    return false;
                }

                // Calculate key
                var key = _securityAccess.CalculateKey(seed, 0x01, "ECM");
                LogEvent($"Calculated key: [{string.Join(" ", key.Select(b => b.ToString("X2")))}]");

                // Validate key
                if (!_securityAccess.ValidateKey(key))
                {
                    LogEvent("Invalid key calculated");
                    return false;
                }

                // Send key
                LogEvent("Sending security key");
                var keyResponse = await _udsClient.SecurityAccess(0x01, key); // Send key
                
                if (!keyResponse.IsPositive)
                {
                    LogEvent($"Security access denied: {keyResponse.ErrorMessage}");
                    return false;
                }

                LogEvent("Security access granted");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during security access");
                LogEvent($"Security access failed: {ex.Message}");
                return false;
            }
        }

        private void UpdateStatus(VINWriteStatus status)
        {
            _currentStatus = status;
            StatusChanged?.Invoke(this, status);
            _logger.LogDebug($"VIN Write Status: {status}");
        }

        private void LogEvent(string message)
        {
            _logger.LogInformation($"VIN: {message}");
            VINEvent?.Invoke(this, message);
        }
    }

    public enum VINWriteStatus
    {
        Idle,
        Initializing,
        EstablishingSession,
        PerformingSecurityAccess,
        WritingVIN,
        VerifyingVIN,
        CompletedSuccessfully,
        CompletedWithWarnings,
        Failed,
        Cancelling,
        Cancelled
    }
}