using System;
using System.Threading.Tasks;
using GMGlobalBProgrammer.Core.UDS;
using Microsoft.Extensions.Logging;

namespace GMGlobalBProgrammer.Core.Functions
{
    public interface ISBATFunction
    {
        event EventHandler<string> SBATEvent;
        event EventHandler<SBATStatus> StatusChanged;

        bool IsRunning { get; }
        SBATStatus CurrentStatus { get; }

        Task<bool> ExecuteSBATAsync();
        Task<bool> CancelAsync();
    }

    public class SBATFunction : ISBATFunction
    {
        private readonly IUDSClient _udsClient;
        private readonly ILogger<SBATFunction> _logger;
        private bool _isRunning;
        private SBATStatus _currentStatus;

        public event EventHandler<string> SBATEvent;
        public event EventHandler<SBATStatus> StatusChanged;

        public bool IsRunning => _isRunning;
        public SBATStatus CurrentStatus => _currentStatus;

        public SBATFunction(IUDSClient udsClient, ILogger<SBATFunction> logger)
        {
            _udsClient = udsClient ?? throw new ArgumentNullException(nameof(udsClient));
            _logger = logger;
            _isRunning = false;
            _currentStatus = SBATStatus.Idle;
        }

        public async Task<bool> ExecuteSBATAsync()
        {
            if (_isRunning)
            {
                LogEvent("SBAT already running");
                return false;
            }

            try
            {
                _isRunning = true;
                UpdateStatus(SBATStatus.Initializing);
                LogEvent("Starting SBAT execution");

                // Step 1: Establish diagnostic session
                UpdateStatus(SBATStatus.EstablishingSession);
                var sessionResponse = await _udsClient.DiagnosticSessionControl(0x02); // Programming session
                
                if (!sessionResponse.IsPositive)
                {
                    LogEvent($"Failed to establish programming session: {sessionResponse.ErrorMessage}");
                    UpdateStatus(SBATStatus.Failed);
                    return false;
                }

                LogEvent("Programming session established successfully");

                // Step 2: Execute SBAT routine
                UpdateStatus(SBATStatus.ExecutingSBAT);
                LogEvent("Executing SBAT routine control");
                
                // SBAT routine identifier for GM Global B (typical value - may vary by ECU)
                ushort sbatRoutineId = 0xFF00;
                
                var sbatResponse = await _udsClient.RoutineControl(0x01, sbatRoutineId); // Start routine
                
                if (!sbatResponse.IsPositive)
                {
                    LogEvent($"SBAT routine failed: {sbatResponse.ErrorMessage}");
                    UpdateStatus(SBATStatus.Failed);
                    return false;
                }

                LogEvent("SBAT routine executed successfully");
                
                // Step 3: Wait for completion (if needed)
                UpdateStatus(SBATStatus.WaitingForCompletion);
                await Task.Delay(2000); // Wait 2 seconds for routine completion

                // Step 4: Check routine status
                var statusResponse = await _udsClient.RoutineControl(0x03, sbatRoutineId); // Request routine results
                
                if (!statusResponse.IsPositive)
                {
                    LogEvent($"Failed to get SBAT status: {statusResponse.ErrorMessage}");
                    UpdateStatus(SBATStatus.CompletedWithWarnings);
                    return true; // Consider it successful but with warnings
                }

                // Parse routine status
                if (statusResponse.Data.Length >= 1)
                {
                    var routineStatus = statusResponse.Data[0];
                    if (routineStatus == 0x00)
                    {
                        LogEvent("SBAT completed successfully");
                        UpdateStatus(SBATStatus.CompletedSuccessfully);
                    }
                    else
                    {
                        LogEvent($"SBAT completed with status: 0x{routineStatus:X2}");
                        UpdateStatus(SBATStatus.CompletedWithWarnings);
                    }
                }
                else
                {
                    LogEvent("SBAT completed (no status data)");
                    UpdateStatus(SBATStatus.CompletedSuccessfully);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SBAT");
                LogEvent($"SBAT execution failed: {ex.Message}");
                UpdateStatus(SBATStatus.Failed);
                return false;
            }
            finally
            {
                _isRunning = false;
            }
        }

        public async Task<bool> CancelAsync()
        {
            if (!_isRunning)
                return true;

            try
            {
                LogEvent("Cancelling SBAT execution");
                UpdateStatus(SBATStatus.Cancelling);
                
                // Try to stop the routine if it's running
                await _udsClient.RoutineControl(0x02, 0xFF00); // Stop routine
                
                UpdateStatus(SBATStatus.Cancelled);
                _isRunning = false;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling SBAT");
                return false;
            }
        }

        private void UpdateStatus(SBATStatus status)
        {
            _currentStatus = status;
            StatusChanged?.Invoke(this, status);
            _logger.LogDebug($"SBAT Status: {status}");
        }

        private void LogEvent(string message)
        {
            _logger.LogInformation($"SBAT: {message}");
            SBATEvent?.Invoke(this, message);
        }
    }

    public enum SBATStatus
    {
        Idle,
        Initializing,
        EstablishingSession,
        ExecutingSBAT,
        WaitingForCompletion,
        CompletedSuccessfully,
        CompletedWithWarnings,
        Failed,
        Cancelling,
        Cancelled
    }
}