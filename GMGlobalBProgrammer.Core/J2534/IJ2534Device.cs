using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GMGlobalBProgrammer.Core.J2534
{
    public interface IJ2534Device : IDisposable
    {
        // Device Properties
        string Name { get; }
        string Vendor { get; }
        string DeviceId { get; }
        bool IsConnected { get; }
        uint ChannelId { get; }
        
        // Connection Management
        Task<bool> ConnectAsync();
        Task<bool> DisconnectAsync();
        
        // Channel Operations
        Task<J2534Error> OpenChannelAsync(ProtocolId protocolId, ConnectFlags flags, uint baudRate = 500000);
        Task<J2534Error> CloseChannelAsync();
        
        // Message Operations
        Task<J2534Error> SendMessageAsync(PassThruMsg message, uint timeoutMs = 1000);
        Task<(J2534Error Error, List<PassThruMsg> Messages)> ReadMessagesAsync(uint timeoutMs = 1000, uint maxMessages = 100);
        
        // Filter Operations
        Task<(J2534Error Error, uint FilterId)> StartMsgFilterAsync(PassThruMsgFilter filter);
        Task<J2534Error> StopMsgFilterAsync(uint filterId);
        Task<J2534Error> ClearMsgFiltersAsync();
        
        // Configuration
        Task<J2534Error> SetConfigAsync(SConfig[] config);
        Task<(J2534Error Error, SConfig[] Config)> GetConfigAsync(ConfigParameter[] parameters);
        
        // Buffer Management
        Task<J2534Error> ClearTxBufferAsync();
        Task<J2534Error> ClearRxBufferAsync();
        
        // Utility Functions
        Task<(J2534Error Error, uint Voltage)> ReadBatteryVoltageAsync();
        Task<J2534Error> SetProgrammingVoltageAsync(uint pin, uint voltage);
    }
}