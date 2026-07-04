using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GMGlobalBProgrammer.Core.CAN
{
    public interface IISOTPTransport
    {
        event EventHandler<string> TransportLog;
        
        Task<byte[]> SendRequestAsync(byte[] requestData);
        Task<bool> SendResponseAsync(byte[] responseData);
        void SetTimeouts(uint responseTimeoutMs, uint separationTimeMs);
    }

    public class ISOTPTransport : IISOTPTransport
    {
        private readonly ICANController _canController;
        private readonly ILogger<ISOTPTransport> _logger;
        private uint _responseTimeoutMs = 5000;
        private uint _separationTimeMs = 20;
        private uint _blockSize = 8;

        public event EventHandler<string> TransportLog;

        public ISOTPTransport(ICANController canController, ILogger<ISOTPTransport> logger)
        {
            _canController = canController ?? throw new ArgumentNullException(nameof(canController));
            _logger = logger;
        }

        public void SetTimeouts(uint responseTimeoutMs, uint separationTimeMs)
        {
            _responseTimeoutMs = responseTimeoutMs;
            _separationTimeMs = separationTimeMs;
        }

        public async Task<byte[]> SendRequestAsync(byte[] requestData)
        {
            try
            {
                if (requestData == null || requestData.Length == 0)
                    throw new ArgumentException("Request data cannot be null or empty");

                LogMessage($"Sending request: [{string.Join(" ", requestData.Select(b => b.ToString("X2")))}]");

                // Send the request using ISO-TP
                if (requestData.Length <= 7)
                {
                    // Single Frame
                    var sfData = CreateSingleFrame(requestData);
                    await SendCANFrameAsync(sfData);
                }
                else
                {
                    // Multi-Frame
                    await SendMultiFrameRequestAsync(requestData);
                }

                // Wait for response
                var response = await WaitForResponseAsync();
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending ISO-TP request");
                throw;
            }
        }

        public async Task<bool> SendResponseAsync(byte[] responseData)
        {
            try
            {
                if (responseData == null)
                    throw new ArgumentException("Response data cannot be null");

                if (responseData.Length <= 7)
                {
                    // Single Frame response
                    var sfData = CreateSingleFrame(responseData);
                    await SendCANFrameAsync(sfData);
                }
                else
                {
                    // Multi-Frame response
                    await SendMultiFrameResponseAsync(responseData);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending ISO-TP response");
                return false;
            }
        }

        private byte[] CreateSingleFrame(byte[] data)
        {
            var frame = new byte[data.Length + 1];
            frame[0] = (byte)(0x00 | data.Length); // Single Frame with length
            Array.Copy(data, 0, frame, 1, data.Length);
            return frame;
        }

        private async Task SendCANFrameAsync(byte[] data)
        {
            var message = new CANMessage(_canController.TxId, data, true);
            await _canController.SendCANMessageAsync(message);
        }

        private async Task SendMultiFrameRequestAsync(byte[] data)
        {
            // First Frame
            var firstFrame = new byte[8];
            var length = data.Length;
            firstFrame[0] = (byte)(0x10 | (length >> 8)); // FF with length high byte
            firstFrame[1] = (byte)(length & 0xFF);         // length low byte
            Array.Copy(data, 0, firstFrame, 2, 6);         // first 6 data bytes
            
            await SendCANFrameAsync(firstFrame);

            // Wait for Flow Control
            var flowControl = await WaitForFlowControlAsync();
            if (flowControl == null)
                throw new TimeoutException("No flow control received");

            // Process Flow Control
            var blockSize = flowControl[1];
            var separationTime = flowControl[2];
            _blockSize = blockSize;
            _separationTimeMs = separationTime;

            // Send Consecutive Frames
            var dataOffset = 6;
            byte sequence = 1;
            
            while (dataOffset < data.Length)
            {
                var cfData = new byte[8];
                cfData[0] = (byte)(0x20 | (sequence & 0x0F)); // Consecutive Frame with sequence
                var bytesToSend = Math.Min(7, data.Length - dataOffset);
                Array.Copy(data, dataOffset, cfData, 1, bytesToSend);
                
                await SendCANFrameAsync(cfData);
                
                dataOffset += 7;
                sequence++;
                
                if (sequence > 0x0F) sequence = 0;
                
                // Wait separation time if not zero
                if (_separationTimeMs > 0)
                    await Task.Delay((int)_separationTimeMs);
            }
        }

        private async Task SendMultiFrameResponseAsync(byte[] data)
        {
            // Send Flow Control first
            var fcFrame = new byte[] { 0x30, (byte)_blockSize, (byte)_separationTimeMs };
            await SendCANFrameAsync(fcFrame);

            // Wait a bit for sender to process
            await Task.Delay(10);

            // Send data frames (simplified - would need proper flow control from requester)
            var dataOffset = 0;
            byte sequence = 1;
            
            while (dataOffset < data.Length)
            {
                var cfData = new byte[8];
                cfData[0] = (byte)(0x20 | (sequence & 0x0F));
                var bytesToSend = Math.Min(7, data.Length - dataOffset);
                Array.Copy(data, dataOffset, cfData, 1, bytesToSend);
                
                await SendCANFrameAsync(cfData);
                
                dataOffset += 7;
                sequence++;
                
                if (sequence > 0x0F) sequence = 0;
                await Task.Delay((int)_separationTimeMs);
            }
        }

        private async Task<byte[]> WaitForResponseAsync()
        {
            var startTime = DateTime.Now;
            var responseData = new List<byte>();

            while ((DateTime.Now - startTime).TotalMilliseconds < _responseTimeoutMs)
            {
                var messages = await _canController.ReceiveCANMessagesAsync(100);
                
                foreach (var message in messages)
                {
                    if (message.CanId == _canController.RxId && message.Data.Length > 0)
                    {
                        var pci = message.Data[0] >> 4;
                        
                        switch (pci)
                        {
                            case 0: // Single Frame
                                var length = message.Data[0] & 0x0F;
                                responseData.AddRange(message.Data.Skip(1).Take(length));
                                LogMessage($"Received SF response: {length} bytes");
                                return responseData.ToArray();

                            case 1: // First Frame
                                var ffLength = ((message.Data[0] & 0x0F) << 8) | message.Data[1];
                                responseData.AddRange(message.Data.Skip(2).Take(6));
                                LogMessage($"Received FF: {ffLength} total bytes");
                                
                                // Send Flow Control
                                var fc = new byte[] { 0x30, (byte)_blockSize, (byte)_separationTimeMs };
                                await SendCANFrameAsync(fc);
                                break;

                            case 2: // Consecutive Frame
                                var sequence = message.Data[0] & 0x0F;
                                var cfDataLength = Math.Min(7, responseData.Capacity - responseData.Count);
                                responseData.AddRange(message.Data.Skip(1).Take(cfDataLength));
                                LogMessage($"Received CF #{sequence}: {cfDataLength} bytes");
                                
                                if (responseData.Count >= responseData.Capacity)
                                {
                                    return responseData.ToArray();
                                }
                                break;

                            case 3: // Flow Control (for responses we send)
                                // Handle if needed
                                break;
                        }
                    }
                }
                
                await Task.Delay(10);
            }

            throw new TimeoutException("Timeout waiting for ISO-TP response");
        }

        private async Task<byte[]> WaitForFlowControlAsync()
        {
            var startTime = DateTime.Now;
            
            while ((DateTime.Now - startTime).TotalMilliseconds < 1000) // 1 second timeout
            {
                var messages = await _canController.ReceiveCANMessagesAsync(100);
                
                foreach (var message in messages)
                {
                    if (message.CanId == _canController.RxId && message.Data.Length >= 3)
                    {
                        var pci = message.Data[0] >> 4;
                        if (pci == 3) // Flow Control
                        {
                            LogMessage($"Received Flow Control: [{string.Join(" ", message.Data.Take(3).Select(b => b.ToString("X2")))}]");
                            return message.Data.Take(3).ToArray();
                        }
                    }
                }
                
                await Task.Delay(10);
            }

            return null;
        }

        private void LogMessage(string message)
        {
            _logger.LogDebug($"ISO-TP: {message}");
            TransportLog?.Invoke(this, message);
        }
    }
}