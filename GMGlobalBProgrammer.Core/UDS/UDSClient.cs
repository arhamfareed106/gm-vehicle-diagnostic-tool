using System;
using System.Linq;
using System.Threading.Tasks;
using GMGlobalBProgrammer.Core.CAN;
using Microsoft.Extensions.Logging;

namespace GMGlobalBProgrammer.Core.UDS
{
    public interface IUDSClient
    {
        event EventHandler<string> UDSEvent;

        Task<UDSResponse> DiagnosticSessionControl(byte sessionType);
        Task<UDSResponse> ReadDataByIdentifier(ushort did);
        Task<UDSResponse> WriteDataByIdentifier(ushort did, byte[] data);
        Task<UDSResponse> SecurityAccess(byte level, byte[] key = null);
        Task<UDSResponse> RoutineControl(byte routineControlType, ushort routineIdentifier, byte[] routineControlOptionRecord = null);
        Task<UDSResponse> RequestDownload(uint memoryAddress, uint memorySize);
        Task<UDSResponse> TransferData(byte blockSequenceCounter, byte[] transferRequestParameterRecord);
        Task<UDSResponse> RequestTransferExit();
        Task<UDSResponse> WriteMemoryByAddress(uint memoryAddress, byte[] data);
        Task<UDSResponse> SendRawRequest(byte[] requestData);
    }

    public class UDSClient : IUDSClient
    {
        private readonly IISOTPTransport _transport;
        private readonly ILogger<UDSClient> _logger;

        public event EventHandler<string> UDSEvent;
        
        // Expose transport for UI binding
        public IISOTPTransport Transport => _transport;

        public UDSClient(IISOTPTransport transport, ILogger<UDSClient> logger)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _logger = logger;
        }

        public async Task<UDSResponse> DiagnosticSessionControl(byte sessionType)
        {
            var request = new byte[] { 0x10, sessionType };
            LogEvent($"DiagnosticSessionControl: 0x{sessionType:X2}");
            return await SendUDSRequestAsync(request);
        }

        public async Task<UDSResponse> ReadDataByIdentifier(ushort did)
        {
            var request = new byte[] { 0x22, (byte)(did >> 8), (byte)did };
            LogEvent($"ReadDataByIdentifier: 0x{did:X4}");
            return await SendUDSRequestAsync(request);
        }

        public async Task<UDSResponse> WriteDataByIdentifier(ushort did, byte[] data)
        {
            var request = new byte[3 + data.Length];
            request[0] = 0x2E;
            request[1] = (byte)(did >> 8);
            request[2] = (byte)did;
            Array.Copy(data, 0, request, 3, data.Length);
            
            LogEvent($"WriteDataByIdentifier: 0x{did:X4} [{string.Join(" ", data.Select(b => b.ToString("X2")))}]");
            return await SendUDSRequestAsync(request);
        }

        public async Task<UDSResponse> SecurityAccess(byte level, byte[] key = null)
        {
            byte[] request;
            
            if (key == null)
            {
                // Request seed
                request = new byte[] { 0x27, level };
                LogEvent($"SecurityAccess - Request Seed: Level 0x{level:X2}");
            }
            else
            {
                // Send key
                request = new byte[2 + key.Length];
                request[0] = 0x27;
                request[1] = (byte)(level + 1); // Key level is seed level + 1
                Array.Copy(key, 0, request, 2, key.Length);
                LogEvent($"SecurityAccess - Send Key: Level 0x{level + 1:X2} [{string.Join(" ", key.Select(b => b.ToString("X2")))}]");
            }

            return await SendUDSRequestAsync(request);
        }

        public async Task<UDSResponse> RoutineControl(byte routineControlType, ushort routineIdentifier, byte[] routineControlOptionRecord = null)
        {
            var request = routineControlOptionRecord == null
                ? new byte[] { 0x31, routineControlType, (byte)(routineIdentifier >> 8), (byte)routineIdentifier }
                : new byte[4 + routineControlOptionRecord.Length];

            request[0] = 0x31;
            request[1] = routineControlType;
            request[2] = (byte)(routineIdentifier >> 8);
            request[3] = (byte)routineIdentifier;
            
            if (routineControlOptionRecord != null)
            {
                Array.Copy(routineControlOptionRecord, 0, request, 4, routineControlOptionRecord.Length);
            }

            LogEvent($"RoutineControl: Type=0x{routineControlType:X2}, Routine=0x{routineIdentifier:X4}");
            return await SendUDSRequestAsync(request);
        }

        public async Task<UDSResponse> RequestDownload(uint memoryAddress, uint memorySize)
        {
            var request = new byte[] { 
                0x34, 0x00, // Service + DataFormatIdentifier
                (byte)(memoryAddress >> 24), (byte)(memoryAddress >> 16), (byte)(memoryAddress >> 8), (byte)memoryAddress,
                (byte)(memorySize >> 24), (byte)(memorySize >> 16), (byte)(memorySize >> 8), (byte)memorySize
            };

            LogEvent($"RequestDownload: Address=0x{memoryAddress:X8}, Size=0x{memorySize:X8}");
            return await SendUDSRequestAsync(request);
        }

        public async Task<UDSResponse> TransferData(byte blockSequenceCounter, byte[] transferRequestParameterRecord)
        {
            var request = new byte[2 + transferRequestParameterRecord.Length];
            request[0] = 0x36;
            request[1] = blockSequenceCounter;
            Array.Copy(transferRequestParameterRecord, 0, request, 2, transferRequestParameterRecord.Length);

            LogEvent($"TransferData: Block={blockSequenceCounter} [{string.Join(" ", transferRequestParameterRecord.Select(b => b.ToString("X2")))}]");
            return await SendUDSRequestAsync(request);
        }

        public async Task<UDSResponse> RequestTransferExit()
        {
            var request = new byte[] { 0x37 };
            LogEvent("RequestTransferExit");
            return await SendUDSRequestAsync(request);
        }

        public async Task<UDSResponse> WriteMemoryByAddress(uint memoryAddress, byte[] data)
        {
            var request = new byte[6 + data.Length];
            request[0] = 0x3D;
            request[1] = 0x04; // AddressAndLengthFormatIdentifier
            request[2] = (byte)(memoryAddress >> 24);
            request[3] = (byte)(memoryAddress >> 16);
            request[4] = (byte)(memoryAddress >> 8);
            request[5] = (byte)memoryAddress;
            Array.Copy(data, 0, request, 6, data.Length);

            LogEvent($"WriteMemoryByAddress: 0x{memoryAddress:X8} [{string.Join(" ", data.Select(b => b.ToString("X2")))}]");
            return await SendUDSRequestAsync(request);
        }

        public async Task<UDSResponse> SendRawRequest(byte[] requestData)
        {
            LogEvent($"Raw Request: [{string.Join(" ", requestData.Select(b => b.ToString("X2")))}]");
            return await SendUDSRequestAsync(requestData);
        }

        private async Task<UDSResponse> SendUDSRequestAsync(byte[] requestData)
        {
            try
            {
                var responseData = await _transport.SendRequestAsync(requestData);
                return ParseUDSResponse(requestData[0], responseData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending UDS request");
                return new UDSResponse
                {
                    IsPositive = false,
                    ServiceId = requestData[0],
                    ResponseCode = UDSResponseCode.GeneralReject,
                    ErrorMessage = ex.Message
                };
            }
        }

        private UDSResponse ParseUDSResponse(byte serviceId, byte[] responseData)
        {
            var response = new UDSResponse { ServiceId = serviceId };

            if (responseData == null || responseData.Length == 0)
            {
                response.IsPositive = false;
                response.ResponseCode = UDSResponseCode.GeneralReject;
                response.ErrorMessage = "No response received";
                return response;
            }

            var responseServiceId = responseData[0];

            if (responseServiceId == (serviceId + 0x40))
            {
                // Positive response
                response.IsPositive = true;
                response.Data = responseData.Length > 1 ? responseData.Skip(1).ToArray() : new byte[0];
            }
            else if (responseServiceId == 0x7F)
            {
                // Negative response
                response.IsPositive = false;
                if (responseData.Length >= 3)
                {
                    response.ResponseCode = (UDSResponseCode)responseData[2];
                    response.ErrorMessage = NegativeResponseCode.GetDescription(response.ResponseCode);
                }
                else
                {
                    response.ResponseCode = UDSResponseCode.GeneralReject;
                    response.ErrorMessage = "Invalid negative response format";
                }
            }
            else
            {
                response.IsPositive = false;
                response.ResponseCode = UDSResponseCode.GeneralReject;
                response.ErrorMessage = $"Unexpected response service ID: 0x{responseServiceId:X2}";
            }

            LogEvent($"Response: {response}");
            return response;
        }

        private void LogEvent(string message)
        {
            _logger.LogDebug($"UDS: {message}");
            UDSEvent?.Invoke(this, message);
        }
    }
}