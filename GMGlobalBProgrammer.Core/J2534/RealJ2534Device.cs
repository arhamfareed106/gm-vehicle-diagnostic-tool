using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GMGlobalBProgrammer.Core.J2534
{
    public class RealJ2534Device : IJ2534Device
    {
        private bool _isConnected;
        private IntPtr _deviceHandle = IntPtr.Zero;
        private IntPtr _channelHandle = IntPtr.Zero;
        private string _dllPath;
        private IntPtr _loadedDll = IntPtr.Zero;

        // J2534 API delegates
        private delegate uint PassThruOpenDelegate(IntPtr pName, ref IntPtr pDeviceID);
        private delegate uint PassThruCloseDelegate(IntPtr DeviceID);
        private delegate uint PassThruConnectDelegate(IntPtr DeviceID, uint ProtocolID, uint Flags, uint Baudrate, ref IntPtr pChannelID, ref uint pActualBaudrate);
        private delegate uint PassThruDisconnectDelegate(IntPtr ChannelID);
        private delegate uint PassThruReadMsgsDelegate(IntPtr ChannelID, IntPtr pMsg, ref uint pNumMsgs, uint Timeout);
        private delegate uint PassThruWriteMsgsDelegate(IntPtr ChannelID, IntPtr pMsg, ref uint pNumMsgs, uint Timeout);
        private delegate uint PassThruStartPeriodicMsgDelegate(IntPtr ChannelID, IntPtr pMsg, ref IntPtr pMsgID, uint TimeInterval);
        private delegate uint PassThruStopPeriodicMsgDelegate(IntPtr ChannelID, IntPtr MsgID);
        private delegate uint PassThruStartMsgFilterDelegate(IntPtr ChannelID, uint FilterType, IntPtr pMaskMsg, IntPtr pPatternMsg, IntPtr pFlowControlMsg, ref IntPtr pFilterID);
        private delegate uint PassThruStopMsgFilterDelegate(IntPtr ChannelID, IntPtr FilterID);
        private delegate uint PassThruIoctlDelegate(IntPtr Handle, uint IoctlID, IntPtr pInput, IntPtr pOutput);

        // Delegates
        private PassThruOpenDelegate _passThruOpen;
        private PassThruCloseDelegate _passThruClose;
        private PassThruConnectDelegate _passThruConnect;
        private PassThruDisconnectDelegate _passThruDisconnect;
        private PassThruReadMsgsDelegate _passThruReadMsgs;
        private PassThruWriteMsgsDelegate _passThruWriteMsgs;
        private PassThruStartPeriodicMsgDelegate _passThruStartPeriodicMsg;
        private PassThruStopPeriodicMsgDelegate _passThruStopPeriodicMsg;
        private PassThruStartMsgFilterDelegate _passThruStartMsgFilter;
        private PassThruStopMsgFilterDelegate _passThruStopMsgFilter;
        private PassThruIoctlDelegate _passThruIoctl;

        public string Name { get; set; }
        public string Vendor { get; set; }
        public string DeviceId { get; set; }
        public bool IsConnected => _isConnected;
        public uint ChannelId => _channelHandle != IntPtr.Zero ? (uint)_channelHandle.ToInt32() : 0;

        public RealJ2534Device(string dllPath)
        {
            _dllPath = dllPath;
            LoadJ2534Library();
        }

        private void LoadJ2534Library()
        {
            try
            {
                // Attempt to load the J2534 DLL using LoadLibrary
                _loadedDll = LoadLibrary(_dllPath);
                if (_loadedDll == IntPtr.Zero)
                {
                    // If direct loading fails, try with the full path
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(_dllPath);
                    var systemPath = System.IO.Path.Combine(Environment.SystemDirectory, fileName + ".dll");
                    if (System.IO.File.Exists(systemPath))
                    {
                        _loadedDll = LoadLibrary(systemPath);
                    }
                }

                if (_loadedDll != IntPtr.Zero)
                {
                    // Get function pointers
                    var openPtr = GetProcAddress(_loadedDll, "PassThruOpen");
                    var closePtr = GetProcAddress(_loadedDll, "PassThruClose");
                    var connectPtr = GetProcAddress(_loadedDll, "PassThruConnect");
                    var disconnectPtr = GetProcAddress(_loadedDll, "PassThruDisconnect");
                    var readMsgsPtr = GetProcAddress(_loadedDll, "PassThruReadMsgs");
                    var writeMsgsPtr = GetProcAddress(_loadedDll, "PassThruWriteMsgs");
                    var startPeriodicPtr = GetProcAddress(_loadedDll, "PassThruStartPeriodicMsg");
                    var stopPeriodicPtr = GetProcAddress(_loadedDll, "PassThruStopPeriodicMsg");
                    var startFilterPtr = GetProcAddress(_loadedDll, "PassThruStartMsgFilter");
                    var stopFilterPtr = GetProcAddress(_loadedDll, "PassThruStopMsgFilter");
                    var ioctlPtr = GetProcAddress(_loadedDll, "PassThruIoctl");

                    // Create delegates
                    if (openPtr != IntPtr.Zero)
                        _passThruOpen = Marshal.GetDelegateForFunctionPointer<PassThruOpenDelegate>(openPtr);
                    if (closePtr != IntPtr.Zero)
                        _passThruClose = Marshal.GetDelegateForFunctionPointer<PassThruCloseDelegate>(closePtr);
                    if (connectPtr != IntPtr.Zero)
                        _passThruConnect = Marshal.GetDelegateForFunctionPointer<PassThruConnectDelegate>(connectPtr);
                    if (disconnectPtr != IntPtr.Zero)
                        _passThruDisconnect = Marshal.GetDelegateForFunctionPointer<PassThruDisconnectDelegate>(disconnectPtr);
                    if (readMsgsPtr != IntPtr.Zero)
                        _passThruReadMsgs = Marshal.GetDelegateForFunctionPointer<PassThruReadMsgsDelegate>(readMsgsPtr);
                    if (writeMsgsPtr != IntPtr.Zero)
                        _passThruWriteMsgs = Marshal.GetDelegateForFunctionPointer<PassThruWriteMsgsDelegate>(writeMsgsPtr);
                    if (startPeriodicPtr != IntPtr.Zero)
                        _passThruStartPeriodicMsg = Marshal.GetDelegateForFunctionPointer<PassThruStartPeriodicMsgDelegate>(startPeriodicPtr);
                    if (stopPeriodicPtr != IntPtr.Zero)
                        _passThruStopPeriodicMsg = Marshal.GetDelegateForFunctionPointer<PassThruStopPeriodicMsgDelegate>(stopPeriodicPtr);
                    if (startFilterPtr != IntPtr.Zero)
                        _passThruStartMsgFilter = Marshal.GetDelegateForFunctionPointer<PassThruStartMsgFilterDelegate>(startFilterPtr);
                    if (stopFilterPtr != IntPtr.Zero)
                        _passThruStopMsgFilter = Marshal.GetDelegateForFunctionPointer<PassThruStopMsgFilterDelegate>(stopFilterPtr);
                    if (ioctlPtr != IntPtr.Zero)
                        _passThruIoctl = Marshal.GetDelegateForFunctionPointer<PassThruIoctlDelegate>(ioctlPtr);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading J2534 DLL: {ex.Message}");
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        public async Task<bool> ConnectAsync()
        {
            if (_passThruOpen == null || _passThruConnect == null)
                return false;

            try
            {
                uint result = _passThruOpen(IntPtr.Zero, ref _deviceHandle);
                if (result != (uint)J2534Error.STATUS_NOERROR)
                {
                    System.Diagnostics.Debug.WriteLine($"PassThruOpen failed: {result}");
                    return false;
                }

                uint actualBaudrate = 0;
                result = _passThruConnect(_deviceHandle, (uint)ProtocolId.ISO15765, 0, 500000, ref _channelHandle, ref actualBaudrate);
                if (result != (uint)J2534Error.STATUS_NOERROR)
                {
                    System.Diagnostics.Debug.WriteLine($"PassThruConnect failed: {result}");
                    // Close the device if connect failed
                    _passThruClose(_deviceHandle);
                    _deviceHandle = IntPtr.Zero;
                    return false;
                }

                _isConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in ConnectAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            if (_passThruDisconnect == null || _passThruClose == null)
                return false;

            try
            {
                if (_channelHandle != IntPtr.Zero)
                {
                    _passThruDisconnect(_channelHandle);
                    _channelHandle = IntPtr.Zero;
                }

                if (_deviceHandle != IntPtr.Zero)
                {
                    _passThruClose(_deviceHandle);
                    _deviceHandle = IntPtr.Zero;
                }

                _isConnected = false;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in DisconnectAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<J2534Error> OpenChannelAsync(ProtocolId protocolId, ConnectFlags flags, uint baudRate = 500000)
        {
            if (_passThruConnect == null)
                return J2534Error.ERR_DEVICE_NOT_CONNECTED;

            try
            {
                if (_deviceHandle == IntPtr.Zero)
                    return J2534Error.ERR_DEVICE_NOT_CONNECTED;

                // Close existing channel if any
                if (_channelHandle != IntPtr.Zero)
                {
                    _passThruDisconnect(_channelHandle);
                    _channelHandle = IntPtr.Zero;
                }

                uint actualBaudrate = 0;
                var result = _passThruConnect(_deviceHandle, (uint)protocolId, (uint)flags, baudRate, ref _channelHandle, ref actualBaudrate);

                return (J2534Error)result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in OpenChannelAsync: {ex.Message}");
                return J2534Error.ERR_FAILED;
            }
        }

        public async Task<J2534Error> CloseChannelAsync()
        {
            if (_passThruDisconnect == null)
                return J2534Error.ERR_DEVICE_NOT_CONNECTED;

            try
            {
                if (_channelHandle == IntPtr.Zero)
                    return J2534Error.ERR_INVALID_CHANNEL_ID;

                var result = _passThruDisconnect(_channelHandle);
                _channelHandle = IntPtr.Zero;

                return (J2534Error)result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in CloseChannelAsync: {ex.Message}");
                return J2534Error.ERR_FAILED;
            }
        }

        public async Task<J2534Error> SendMessageAsync(PassThruMsg message, uint timeoutMs = 1000)
        {
            if (_passThruWriteMsgs == null)
                return J2534Error.ERR_DEVICE_NOT_CONNECTED;

            try
            {
                if (_channelHandle == IntPtr.Zero)
                    return J2534Error.ERR_DEVICE_NOT_CONNECTED;

                // Create message structure in unmanaged memory
                var msgStruct = CreateJ2534Message(message);
                var msgPtr = Marshal.AllocHGlobal(Marshal.SizeOf(msgStruct));
                Marshal.StructureToPtr(msgStruct, msgPtr, false);

                uint numMsgs = 1;
                var result = _passThruWriteMsgs(_channelHandle, msgPtr, ref numMsgs, timeoutMs);

                Marshal.FreeHGlobal(msgPtr);

                return (J2534Error)result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in SendMessageAsync: {ex.Message}");
                return J2534Error.ERR_FAILED;
            }
        }

        public async Task<(J2534Error Error, List<PassThruMsg> Messages)> ReadMessagesAsync(uint timeoutMs = 1000, uint maxMessages = 100)
        {
            if (_passThruReadMsgs == null)
                return (J2534Error.ERR_DEVICE_NOT_CONNECTED, new List<PassThruMsg>());

            try
            {
                if (_channelHandle == IntPtr.Zero)
                    return (J2534Error.ERR_DEVICE_NOT_CONNECTED, new List<PassThruMsg>());

                // Allocate array of message structures
                var msgArray = new J2534_Message[maxMessages];
                var msgSize = Marshal.SizeOf<J2534_Message>();
                var msgPtr = Marshal.AllocHGlobal(msgSize * (int)maxMessages);

                uint numMsgs = maxMessages;
                var result = _passThruReadMsgs(_channelHandle, msgPtr, ref numMsgs, timeoutMs);

                var messages = new List<PassThruMsg>();
                if (result == (uint)J2534Error.STATUS_NOERROR && numMsgs > 0)
                {
                    for (int i = 0; i < numMsgs; i++)
                    {
                        var currentPtr = IntPtr.Add(msgPtr, i * msgSize);
                        var j2534Msg = Marshal.PtrToStructure<J2534_Message>(currentPtr);
                        messages.Add(ConvertFromJ2534Message(j2534Msg));
                    }
                }

                Marshal.FreeHGlobal(msgPtr);

                return ((J2534Error)result, messages);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in ReadMessagesAsync: {ex.Message}");
                return (J2534Error.ERR_FAILED, new List<PassThruMsg>());
            }
        }

        public async Task<(J2534Error Error, uint FilterId)> StartMsgFilterAsync(PassThruMsgFilter filter)
        {
            if (_passThruStartMsgFilter == null)
                return (J2534Error.ERR_DEVICE_NOT_CONNECTED, 0);

            try
            {
                if (_channelHandle == IntPtr.Zero)
                    return (J2534Error.ERR_DEVICE_NOT_CONNECTED, 0);

                // Create J2534 message structures for mask, pattern, and flow control
                var maskMsg = CreateJ2534Message(new PassThruMsg(filter.FilterType == FilterType.FLOW_CONTROL_FILTER ? ProtocolId.ISO15765 : ProtocolId.ISO15765, filter.Mask));
                var patternMsg = CreateJ2534Message(new PassThruMsg(filter.FilterType == FilterType.FLOW_CONTROL_FILTER ? ProtocolId.ISO15765 : ProtocolId.ISO15765, filter.Pattern));
                var flowCtrlMsg = filter.FlowControl.Length > 0 ? CreateJ2534Message(new PassThruMsg(ProtocolId.ISO15765, filter.FlowControl)) : new J2534_Message();

                var maskPtr = Marshal.AllocHGlobal(Marshal.SizeOf(maskMsg));
                var patternPtr = Marshal.AllocHGlobal(Marshal.SizeOf(patternMsg));
                var flowCtrlPtr = filter.FlowControl.Length > 0 ? Marshal.AllocHGlobal(Marshal.SizeOf(flowCtrlMsg)) : IntPtr.Zero;

                Marshal.StructureToPtr(maskMsg, maskPtr, false);
                Marshal.StructureToPtr(patternMsg, patternPtr, false);
                if (flowCtrlPtr != IntPtr.Zero)
                    Marshal.StructureToPtr(flowCtrlMsg, flowCtrlPtr, false);

                IntPtr filterId = IntPtr.Zero;
                var result = _passThruStartMsgFilter(_channelHandle, (uint)filter.FilterType, maskPtr, patternPtr, flowCtrlPtr, ref filterId);

                Marshal.FreeHGlobal(maskPtr);
                Marshal.FreeHGlobal(patternPtr);
                if (flowCtrlPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(flowCtrlPtr);

                return ((J2534Error)result, (uint)filterId.ToInt32());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in StartMsgFilterAsync: {ex.Message}");
                return (J2534Error.ERR_FAILED, 0);
            }
        }

        public async Task<J2534Error> StopMsgFilterAsync(uint filterId)
        {
            if (_passThruStopMsgFilter == null)
                return J2534Error.ERR_DEVICE_NOT_CONNECTED;

            try
            {
                if (_channelHandle == IntPtr.Zero)
                    return J2534Error.ERR_DEVICE_NOT_CONNECTED;

                var filterIdPtr = new IntPtr((int)filterId);
                var result = _passThruStopMsgFilter(_channelHandle, filterIdPtr);

                return (J2534Error)result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in StopMsgFilterAsync: {ex.Message}");
                return J2534Error.ERR_FAILED;
            }
        }

        public async Task<J2534Error> ClearMsgFiltersAsync()
        {
            if (_passThruIoctl == null)
                return J2534Error.ERR_DEVICE_NOT_CONNECTED;

            try
            {
                if (_channelHandle == IntPtr.Zero)
                    return J2534Error.ERR_DEVICE_NOT_CONNECTED;

                var result = _passThruIoctl(_channelHandle, (uint)IoctlId.CLEAR_MSG_FILTERS, IntPtr.Zero, IntPtr.Zero);

                return (J2534Error)result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in ClearMsgFiltersAsync: {ex.Message}");
                return J2534Error.ERR_FAILED;
            }
        }

        public async Task<J2534Error> SetConfigAsync(SConfig[] config)
        {
            if (_passThruIoctl == null)
                return J2534Error.ERR_DEVICE_NOT_CONNECTED;

            try
            {
                if (_channelHandle == IntPtr.Zero)
                    return J2534Error.ERR_DEVICE_NOT_CONNECTED;

                // Create SCONFIG_LIST structure
                var configList = new SCONFIG_LIST
                {
                    NumOfParams = (uint)config.Length,
                    ConfigPtr = IntPtr.Zero
                };

                if (config.Length > 0)
                {
                    var configPtr = Marshal.AllocHGlobal(Marshal.SizeOf<SConfig>() * config.Length);
                    for (int i = 0; i < config.Length; i++)
                    {
                        var offset = Marshal.SizeOf<SConfig>() * i;
                        var currentPtr = IntPtr.Add(configPtr, offset);
                        Marshal.StructureToPtr(new SConfig { Parameter = config[i].Parameter, Value = config[i].Value }, currentPtr, false);
                    }
                    configList.ConfigPtr = configPtr;
                }

                var configListPtr = Marshal.AllocHGlobal(Marshal.SizeOf(configList));
                Marshal.StructureToPtr(configList, configListPtr, false);

                var result = _passThruIoctl(_channelHandle, (uint)IoctlId.SET_CONFIG, configListPtr, IntPtr.Zero);

                Marshal.FreeHGlobal(configListPtr);
                if (configList.ConfigPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(configList.ConfigPtr);

                return (J2534Error)result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in SetConfigAsync: {ex.Message}");
                return J2534Error.ERR_FAILED;
            }
        }

        public async Task<(J2534Error Error, SConfig[] Config)> GetConfigAsync(ConfigParameter[] parameters)
        {
            if (_passThruIoctl == null)
                return (J2534Error.ERR_DEVICE_NOT_CONNECTED, new SConfig[0]);

            try
            {
                if (_channelHandle == IntPtr.Zero)
                    return (J2534Error.ERR_DEVICE_NOT_CONNECTED, new SConfig[0]);

                // Create input SCONFIG_LIST
                var inputConfigList = new SCONFIG_LIST
                {
                    NumOfParams = (uint)parameters.Length,
                    ConfigPtr = IntPtr.Zero
                };

                if (parameters.Length > 0)
                {
                    var configPtr = Marshal.AllocHGlobal(Marshal.SizeOf<SConfig>() * parameters.Length);
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var offset = Marshal.SizeOf<SConfig>() * i;
                        var currentPtr = IntPtr.Add(configPtr, offset);
                        Marshal.StructureToPtr(new SConfig { Parameter = parameters[i], Value = 0 }, currentPtr, false);
                    }
                    inputConfigList.ConfigPtr = configPtr;
                }

                // Create output SCONFIG_LIST
                var outputConfigList = new SCONFIG_LIST
                {
                    NumOfParams = (uint)parameters.Length,
                    ConfigPtr = IntPtr.Zero
                };

                if (parameters.Length > 0)
                {
                    outputConfigList.ConfigPtr = Marshal.AllocHGlobal(Marshal.SizeOf<SConfig>() * parameters.Length);
                }

                var inputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(inputConfigList));
                var outputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(outputConfigList));
                Marshal.StructureToPtr(inputConfigList, inputPtr, false);
                Marshal.StructureToPtr(outputConfigList, outputPtr, false);

                var result = _passThruIoctl(_channelHandle, (uint)IoctlId.GET_CONFIG, inputPtr, outputPtr);

                var outputList = Marshal.PtrToStructure<SCONFIG_LIST>(outputPtr);
                var configs = new SConfig[outputList.NumOfParams];

                if (outputList.ConfigPtr != IntPtr.Zero)
                {
                    for (int i = 0; i < configs.Length; i++)
                    {
                        var offset = Marshal.SizeOf<SConfig>() * i;
                        var currentPtr = IntPtr.Add(outputList.ConfigPtr, offset);
                        configs[i] = Marshal.PtrToStructure<SConfig>(currentPtr);
                    }
                }

                Marshal.FreeHGlobal(inputPtr);
                Marshal.FreeHGlobal(outputPtr);
                if (inputConfigList.ConfigPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(inputConfigList.ConfigPtr);
                if (outputList.ConfigPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(outputList.ConfigPtr);

                return ((J2534Error)result, configs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in GetConfigAsync: {ex.Message}");
                return (J2534Error.ERR_FAILED, new SConfig[0]);
            }
        }

        public async Task<J2534Error> ClearTxBufferAsync()
        {
            if (_passThruIoctl == null)
                return J2534Error.ERR_DEVICE_NOT_CONNECTED;

            try
            {
                if (_channelHandle == IntPtr.Zero)
                    return J2534Error.ERR_DEVICE_NOT_CONNECTED;

                var result = _passThruIoctl(_channelHandle, (uint)IoctlId.CLEAR_TX_BUFFER, IntPtr.Zero, IntPtr.Zero);

                return (J2534Error)result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in ClearTxBufferAsync: {ex.Message}");
                return J2534Error.ERR_FAILED;
            }
        }

        public async Task<J2534Error> ClearRxBufferAsync()
        {
            if (_passThruIoctl == null)
                return J2534Error.ERR_DEVICE_NOT_CONNECTED;

            try
            {
                if (_channelHandle == IntPtr.Zero)
                    return J2534Error.ERR_DEVICE_NOT_CONNECTED;

                var result = _passThruIoctl(_channelHandle, (uint)IoctlId.CLEAR_RX_BUFFER, IntPtr.Zero, IntPtr.Zero);

                return (J2534Error)result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in ClearRxBufferAsync: {ex.Message}");
                return J2534Error.ERR_FAILED;
            }
        }

        public async Task<(J2534Error Error, uint Voltage)> ReadBatteryVoltageAsync()
        {
            if (_passThruIoctl == null)
                return (J2534Error.ERR_DEVICE_NOT_CONNECTED, 0);

            try
            {
                if (_channelHandle == IntPtr.Zero)
                    return (J2534Error.ERR_DEVICE_NOT_CONNECTED, 0);

                uint voltage = 0;
                var voltagePtr = Marshal.AllocHGlobal(sizeof(uint));
                Marshal.WriteInt32(voltagePtr, 0);

                var result = _passThruIoctl(_channelHandle, (uint)IoctlId.READ_VBATT, IntPtr.Zero, voltagePtr);

                voltage = (uint)Marshal.ReadInt32(voltagePtr);
                Marshal.FreeHGlobal(voltagePtr);

                return ((J2534Error)result, voltage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in ReadBatteryVoltageAsync: {ex.Message}");
                return (J2534Error.ERR_FAILED, 0);
            }
        }

        public async Task<J2534Error> SetProgrammingVoltageAsync(uint pin, uint voltage)
        {
            if (_passThruIoctl == null)
                return J2534Error.ERR_DEVICE_NOT_CONNECTED;

            try
            {
                if (_channelHandle == IntPtr.Zero)
                    return J2534Error.ERR_DEVICE_NOT_CONNECTED;

                // Create SBYTE_ARRAY structure for pin and voltage
                var byteArray = new SBYTE_ARRAY
                {
                    NumOfBytes = 8, // Pin (4 bytes) + Voltage (4 bytes)
                    BytePtr = Marshal.AllocHGlobal(8)
                };

                // Write pin and voltage to byte array
                Marshal.WriteInt32(byteArray.BytePtr, 0, (int)pin);
                Marshal.WriteInt32(IntPtr.Add(byteArray.BytePtr, 4), 0, (int)voltage);

                var byteArrayPtr = Marshal.AllocHGlobal(Marshal.SizeOf(byteArray));
                Marshal.StructureToPtr(byteArray, byteArrayPtr, false);

                var result = _passThruIoctl(_channelHandle, (uint)IoctlId.SET_PROG_VOLTAGE, byteArrayPtr, IntPtr.Zero);

                Marshal.FreeHGlobal(byteArray.BytePtr);
                Marshal.FreeHGlobal(byteArrayPtr);

                return (J2534Error)result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in SetProgrammingVoltageAsync: {ex.Message}");
                return J2534Error.ERR_FAILED;
            }
        }

        private J2534_Message CreateJ2534Message(PassThruMsg msg)
        {
            var j2534Msg = new J2534_Message
            {
                ProtocolID = (uint)msg.ProtocolID,
                RxStatus = msg.RxStatus,
                TxFlags = msg.TxFlags,
                Timestamp = msg.Timestamp,
                DataSize = msg.DataSize,
                ExtraDataIndex = msg.ExtraDataIndex
            };

            // Copy data
            if (msg.Data != null && msg.Data.Length > 0)
            {
                var dataSize = Math.Min(msg.Data.Length, 4128); // J2534 max data size
                j2534Msg.Data = new byte[4128];
                Array.Copy(msg.Data, j2534Msg.Data, dataSize);
            }

            return j2534Msg;
        }

        private PassThruMsg ConvertFromJ2534Message(J2534_Message j2534Msg)
        {
            var msg = new PassThruMsg((ProtocolId)j2534Msg.ProtocolID, new byte[j2534Msg.DataSize])
            {
                RxStatus = j2534Msg.RxStatus,
                TxFlags = j2534Msg.TxFlags,
                Timestamp = j2534Msg.Timestamp,
                DataSize = j2534Msg.DataSize,
                ExtraDataIndex = j2534Msg.ExtraDataIndex
            };

            // Copy data
            if (j2534Msg.Data != null)
            {
                Array.Copy(j2534Msg.Data, msg.Data, (int)j2534Msg.DataSize);
            }

            return msg;
        }

        public void Dispose()
        {
            if (_isConnected)
            {
                DisconnectAsync().Wait();
            }

            if (_loadedDll != IntPtr.Zero)
            {
                FreeLibrary(_loadedDll);
                _loadedDll = IntPtr.Zero;
            }
        }

        // Internal J2534 structures
        [StructLayout(LayoutKind.Sequential)]
        internal struct J2534_Message
        {
            public uint ProtocolID;
            public uint RxStatus;
            public uint TxFlags;
            public uint Timestamp;
            public uint DataSize;
            public uint ExtraDataIndex;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4128)]
            public byte[] Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SCONFIG_LIST
        {
            public uint NumOfParams;
            public IntPtr ConfigPtr;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SBYTE_ARRAY
        {
            public uint NumOfBytes;
            public IntPtr BytePtr;
        }
    }
}