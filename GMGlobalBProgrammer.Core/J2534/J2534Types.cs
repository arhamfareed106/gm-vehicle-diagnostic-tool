using System;

namespace GMGlobalBProgrammer.Core.J2534
{
    // J2534 Protocol IDs
    public enum ProtocolId : uint
    {
        J1850VPW = 1,
        J1850PWM = 2,
        ISO9141 = 3,
        ISO14230 = 4,
        CAN = 5,
        ISO15765 = 6,
        SCI_A_ENGINE = 7,
        SCI_A_TRANS = 8,
        SCI_B_ENGINE = 9,
        SCI_B_TRANS = 10
    }

    // J2534 Filter Types
    public enum FilterType : uint
    {
        PASS_FILTER = 0x00000001,
        BLOCK_FILTER = 0x00000002,
        FLOW_CONTROL_FILTER = 0x00000003
    }

    // J2534 Connect Flags
    public enum ConnectFlags : uint
    {
        NONE = 0x00000000,
        CAN_29BIT_ID = 0x00000100,
        ISO15765_ADDR_TYPE = 0x00000200,
        CAN_ID_BOTH = 0x00000800
    }

    // J2534 Error Codes
    public enum J2534Error : uint
    {
        STATUS_NOERROR = 0x00,
        ERR_NOT_SUPPORTED = 0x01,
        ERR_INVALID_CHANNEL_ID = 0x02,
        ERR_INVALID_PROTOCOL_ID = 0x03,
        ERR_NULL_PARAMETER = 0x04,
        ERR_INVALID_IOCTL_VALUE = 0x05,
        ERR_INVALID_FLAGS = 0x06,
        ERR_FAILED = 0x07,
        ERR_DEVICE_NOT_CONNECTED = 0x08,
        ERR_TIMEOUT = 0x09,
        ERR_INVALID_MSG = 0x0A,
        ERR_INVALID_TIME_INTERVAL = 0x0B,
        ERR_EXCEEDED_LIMIT = 0x0C,
        ERR_INVALID_MSG_ID = 0x0D,
        ERR_DEVICE_IN_USE = 0x0E,
        ERR_INVALID_IOCTL_ID = 0x0F,
        ERR_BUFFER_EMPTY = 0x10,
        ERR_BUFFER_FULL = 0x11,
        ERR_BUFFER_OVERFLOW = 0x12,
        ERR_PIN_INVALID = 0x13,
        ERR_CHANNEL_IN_USE = 0x14,
        ERR_MSG_PROTOCOL_ID = 0x15,
        ERR_INVALID_FILTER_ID = 0x16,
        ERR_NO_FLOW_CONTROL = 0x17,
        ERR_NOT_UNIQUE = 0x18,
        ERR_INVALID_BAUDRATE = 0x19,
        ERR_INVALID_DEVICE_ID = 0x1A
    }

    // J2534 IOCTL IDs
    public enum IoctlId : uint
    {
        GET_CONFIG = 0x01,
        SET_CONFIG = 0x02,
        READ_VBATT = 0x03,
        FIVE_BAUD_INIT = 0x04,
        FAST_INIT = 0x05,
        CLEAR_TX_BUFFER = 0x07,
        CLEAR_RX_BUFFER = 0x08,
        CLEAR_PERIODIC_MSGS = 0x09,
        CLEAR_MSG_FILTERS = 0x0A,
        SET_PROG_VOLTAGE = 0x0B,
        SW_CAN_HS = 0x0C,
        SW_CAN_NS = 0x0D
    }

    // J2534 Configuration Parameters
    public enum ConfigParameter : uint
    {
        DATA_RATE = 0x01,
        LOOPBACK = 0x03,
        NODE_ADDRESS = 0x04,
        NETWORK_LINE = 0x05,
        P1_MIN = 0x06,
        P1_MAX = 0x07,
        P2_MIN = 0x08,
        P2_MAX = 0x09,
        P3_MIN = 0x0A,
        P3_MAX = 0x0B,
        P4_MIN = 0x0C,
        P4_MAX = 0x0D,
        W0 = 0x19,
        W1 = 0x1A,
        W2 = 0x1B,
        W3 = 0x1C,
        W4 = 0x1D,
        W5 = 0x1E,
        TIDLE = 0x1F,
        TINIL = 0x20,
        TWUP = 0x21,
        PARITY = 0x22,
        BIT_SAMPLE_POINT = 0x23,
        SYNC_JUMP_WIDTH = 0x24,
        T1_MAX = 0x25,
        T2_MAX = 0x26,
        T3_MAX = 0x27,
        T4_MAX = 0x28,
        T5_MAX = 0x29,
        ISO15765_BS = 0x2A,
        ISO15765_STMIN = 0x2B,
        ISO15765_BS_TX = 0x2C,
        ISO15765_STMIN_TX = 0x2D,
        DATA_BITS = 0x2E,
        FIVE_BAUD_MOD = 0x2F,
        ISO9141_K_LINE = 0x30,
        ISO9141_L_LINE = 0x31
    }

    // J2534 Message Structure
    public struct PassThruMsg
    {
        public ProtocolId ProtocolID;
        public uint RxStatus;
        public uint TxFlags;
        public uint Timestamp;
        public uint DataSize;
        public uint ExtraDataIndex;
        public byte[] Data;

        public PassThruMsg(ProtocolId protocolId, byte[] data)
        {
            ProtocolID = protocolId;
            RxStatus = 0;
            TxFlags = 0;
            Timestamp = 0;
            DataSize = (uint)data.Length;
            ExtraDataIndex = 0;
            Data = new byte[data.Length];
            Array.Copy(data, Data, data.Length);
        }

        public override string ToString()
        {
            return $"Protocol: {ProtocolID}, Data: [{string.Join(" ", Data)}]";
        }
    }

    // J2534 Configuration Structure
    public struct SConfig
    {
        public ConfigParameter Parameter;
        public uint Value;

        public SConfig(ConfigParameter parameter, uint value)
        {
            Parameter = parameter;
            Value = value;
        }
    }

    // J2534 Filter Structure
    public struct PassThruMsgFilter
    {
        public FilterType FilterType;
        public uint MaskSize;
        public uint PatternSize;
        public uint FlowControlSize;
        public byte[] Mask;
        public byte[] Pattern;
        public byte[] FlowControl;

        public PassThruMsgFilter(FilterType filterType, byte[] mask, byte[] pattern, byte[] flowControl = null)
        {
            FilterType = filterType;
            MaskSize = (uint)mask.Length;
            PatternSize = (uint)pattern.Length;
            FlowControlSize = flowControl != null ? (uint)flowControl.Length : 0;
            Mask = mask;
            Pattern = pattern;
            FlowControl = flowControl ?? new byte[0];
        }
    }
}