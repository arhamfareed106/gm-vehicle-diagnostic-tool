using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace GMGlobalBProgrammer.Core.CAN
{
    public enum CANMessageType
    {
        SingleFrame,
        FirstFrame,
        ConsecutiveFrame,
        FlowControl
    }

    public enum FlowControlType
    {
        ContinueToSend,
        Wait,
        Overflow
    }

    public class CANMessage
    {
        public uint CanId { get; set; }
        public byte[] Data { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsTransmitted { get; set; }
        public string Direction => IsTransmitted ? ">>" : "<<";

        public CANMessage()
        {
            Timestamp = DateTime.Now;
            Data = new byte[0];
        }

        public CANMessage(uint canId, byte[] data, bool isTransmitted = false)
        {
            CanId = canId;
            Data = data ?? new byte[0];
            IsTransmitted = isTransmitted;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            var timeStr = Timestamp.ToString("HH:mm:ss.fff");
            var idStr = CanId.ToString("X3");
            var dataStr = string.Join(" ", Data.Select(b => b.ToString("X2")));
            return $"{timeStr} {Direction} {idStr} [{dataStr}]";
        }

        public string ToDetailedString()
        {
            return $"CAN ID: 0x{CanId:X3}, Length: {Data.Length}, Data: [{string.Join(" ", Data.Select(b => b.ToString("X2")))}], Direction: {Direction}, Time: {Timestamp:HH:mm:ss.fff}";
        }
    }

    public interface ICANController
    {
        event EventHandler<CANMessageEventArgs> MessageReceived;
        event EventHandler<CANMessageEventArgs> MessageTransmitted;

        bool IsConnected { get; }
        uint TxId { get; set; }
        uint RxId { get; set; }

        void Connect(J2534.IJ2534Device device);
        void Disconnect();
        void SetFilter(uint canId, uint mask);
        Task SendCANMessageAsync(CANMessage message);
        Task<List<CANMessage>> ReceiveCANMessagesAsync(uint timeoutMs = 1000);
    }

    public class CANController : ICANController
    {
        private J2534.IJ2534Device _device;
        private readonly ILogger<CANController> _logger;
        private bool _isConnected;
        private uint _filterId;

        public event EventHandler<CANMessageEventArgs> MessageReceived;
        public event EventHandler<CANMessageEventArgs> MessageTransmitted;

        public bool IsConnected => _isConnected && _device?.IsConnected == true;
        public uint TxId { get; set; } = 0x7E0;
        public uint RxId { get; set; } = 0x7E8;

        public CANController(ILogger<CANController> logger)
        {
            _logger = logger;
            _device = null;
            _isConnected = false;
            _filterId = 0;
        }

        public void Connect(J2534.IJ2534Device device)
        {
            try
            {
                if (device == null || !device.IsConnected)
                {
                    throw new InvalidOperationException("Device not connected");
                }

                _device = device;
                _isConnected = true;
                
                // Set up CAN filter for standard ECU communication
                SetFilter(RxId, 0x7F8); // Match 7E8 with mask 7F8
                
                _logger.LogInformation($"CAN Controller connected - TX: 0x{TxId:X3}, RX: 0x{RxId:X3}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting CAN controller");
                throw;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (_filterId != 0 && _device != null)
                {
                    _device.StopMsgFilterAsync(_filterId).Wait();
                    _filterId = 0;
                }

                _device = null;
                _isConnected = false;
                _logger.LogInformation("CAN Controller disconnected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting CAN controller");
            }
        }

        public void SetFilter(uint canId, uint mask)
        {
            try
            {
                if (!IsConnected) return;

                // Stop existing filter if any
                if (_filterId != 0)
                {
                    _device.StopMsgFilterAsync(_filterId).Wait();
                    _filterId = 0;
                }

                // Create new filter
                var maskBytes = new byte[] { (byte)(mask >> 24), (byte)(mask >> 16), (byte)(mask >> 8), (byte)mask };
                var patternBytes = new byte[] { (byte)(canId >> 24), (byte)(canId >> 16), (byte)(canId >> 8), (byte)canId };
                
                var filter = new J2534.PassThruMsgFilter(
                    J2534.FilterType.PASS_FILTER,
                    maskBytes,
                    patternBytes);

                var result = _device.StartMsgFilterAsync(filter).Result;
                
                if (result.Error == J2534.J2534Error.STATUS_NOERROR)
                {
                    _filterId = result.FilterId;
                    _logger.LogInformation($"CAN filter set - ID: 0x{canId:X3}, Mask: 0x{mask:X3}");
                }
                else
                {
                    _logger.LogError($"Failed to set CAN filter: {result.Error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting CAN filter");
            }
        }

        public async Task SendCANMessageAsync(CANMessage message)
        {
            try
            {
                if (!IsConnected)
                {
                    throw new InvalidOperationException("CAN controller not connected");
                }

                // Convert to J2534 message format
                var j2534Msg = new J2534.PassThruMsg(
                    J2534.ProtocolId.ISO15765,
                    message.Data);

                j2534Msg.TxFlags = (uint)(J2534.ConnectFlags.CAN_29BIT_ID | J2534.ConnectFlags.ISO15765_ADDR_TYPE);

                var result = await _device.SendMessageAsync(j2534Msg);
                
                if (result == J2534.J2534Error.STATUS_NOERROR)
                {
                    message.IsTransmitted = true;
                    MessageTransmitted?.Invoke(this, new CANMessageEventArgs(message));
                    _logger.LogDebug($"CAN message sent: {message}");
                }
                else
                {
                    _logger.LogError($"Failed to send CAN message: {result}");
                    throw new InvalidOperationException($"J2534 error: {result}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending CAN message");
                throw;
            }
        }

        public async Task<List<CANMessage>> ReceiveCANMessagesAsync(uint timeoutMs = 1000)
        {
            try
            {
                if (!IsConnected)
                {
                    return new List<CANMessage>();
                }

                var result = await _device.ReadMessagesAsync(timeoutMs, 100);
                
                if (result.Error != J2534.J2534Error.STATUS_NOERROR)
                {
                    _logger.LogWarning($"Failed to read CAN messages: {result.Error}");
                    return new List<CANMessage>();
                }

                var canMessages = new List<CANMessage>();
                
                foreach (var j2534Msg in result.Messages)
                {
                    // Extract CAN ID from J2534 message (assuming it's in the data for ISO-TP)
                    // This is simplified - real implementation would depend on J2534 device specifics
                    uint canId = RxId; // Default to standard response ID
                    
                    var canMessage = new CANMessage(canId, j2534Msg.Data, false);
                    canMessages.Add(canMessage);
                    MessageReceived?.Invoke(this, new CANMessageEventArgs(canMessage));
                }

                return canMessages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving CAN messages");
                return new List<CANMessage>();
            }
        }
    }

    public class CANMessageEventArgs : EventArgs
    {
        public CANMessage Message { get; }

        public CANMessageEventArgs(CANMessage message)
        {
            Message = message;
        }
    }
}