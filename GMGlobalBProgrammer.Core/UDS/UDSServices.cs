namespace GMGlobalBProgrammer.Core.UDS
{
    public enum UDSResponseCode : byte
    {
        PositiveResponse = 0x00,
        GeneralReject = 0x10,
        ServiceNotSupported = 0x11,
        SubFunctionNotSupported = 0x12,
        IncorrectMessageLengthOrInvalidFormat = 0x13,
        ResponseTooLong = 0x14,
        BusyRepeatRequest = 0x21,
        ConditionsNotCorrect = 0x22,
        RequestSequenceError = 0x24,
        NoResponseFromSubnetComponent = 0x25,
        FailurePreventsExecutionOfRequestedAction = 0x26,
        RequestOutOfRange = 0x31,
        SecurityAccessDenied = 0x33,
        InvalidKey = 0x35,
        ExceedNumberOfAttempts = 0x36,
        RequiredTimeDelayNotExpired = 0x37,
        UploadDownloadNotAccepted = 0x40,
        TransferDataSuspended = 0x41,
        GeneralProgrammingFailure = 0x72,
        WrongBlockSequenceCounter = 0x73,
        RequestCorrectlyReceived_ResponsePending = 0x78,
        SubFunctionNotSupportedInActiveSession = 0x7E,
        ServiceNotSupportedInActiveSession = 0x7F
    }

    public class UDSResponse
    {
        public bool IsPositive { get; set; }
        public byte ServiceId { get; set; }
        public UDSResponseCode ResponseCode { get; set; }
        public byte[] Data { get; set; }
        public string ErrorMessage { get; set; }

        public UDSResponse()
        {
            Data = new byte[0];
        }

        public override string ToString()
        {
            if (IsPositive)
            {
                return $"POS: 0x{ServiceId:X2} [{string.Join(" ", Data.Select(b => b.ToString("X2")))}]";
            }
            else
            {
                return $"NEG: 0x{ServiceId:X2} Code=0x{ResponseCode:X2} ({ErrorMessage})";
            }
        }
    }

    public static class GMDIDs
    {
        // GM-specific Data Identifiers
        public const ushort VIN = 0xF190;           // Vehicle Identification Number
        public const ushort ECUHardwareNumber = 0xF112;
        public const ushort ECUHardwareVersion = 0xF113;
        public const ushort ECUProgrammingInfo = 0xF114;
        public const ushort ECUProgrammingDate = 0xF115;
        public const ushort ECUProgrammingTesterNumber = 0xF116;
        public const ushort ECUProgrammingCounter = 0xF117;
        public const ushort CalibrationIdentification = 0xF118;
        public const ushort CalibrationVerificationNumber = 0xF119;
        public const ushort ApplicationSoftwareIdentification = 0xF11A;
        public const ushort BootSoftwareIdentification = 0xF11B;
        public const ushort ApplicationDataIdentification = 0xF11C;
        public const ushort BootSoftwareVersion = 0xF11D;
        public const ushort ApplicationSoftwareVersion = 0xF11E;
        public const ushort BootSoftwareFingerprint = 0xF11F;
        public const ushort ApplicationSoftwareFingerprint = 0xF120;
        public const ushort ApplicationDataFingerprint = 0xF121;
        public const ushort ActiveDiagnosticSession = 0xF122;
        public const ushort VehicleManufacturerSparePartNumber = 0xF123;
        public const ushort VehicleManufacturerECUSoftwareNumber = 0xF124;
        public const ushort SystemSupplierIdentifier = 0xF125;
        public const ushort ECUManufacturingDate = 0xF126;
        public const ushort RepairShopCodeOrTesterSerialNumber = 0xF127;
        public const ushort ProgrammingDate = 0xF128;
        public const ushort CalibrationDate = 0xF129;
        public const ushort CalibrationEquipmentSoftwareNumber = 0xF12A;
        public const ushort ECUInstallationDate = 0xF12B;
        public const ushort ODXFileIdentifier = 0xF12C;
        public const ushort EntityIdentifier = 0xF12D;
    }

    public static class NegativeResponseCode
    {
        public static string GetDescription(UDSResponseCode code)
        {
            return code switch
            {
                UDSResponseCode.PositiveResponse => "Positive Response",
                UDSResponseCode.GeneralReject => "General Reject",
                UDSResponseCode.ServiceNotSupported => "Service Not Supported",
                UDSResponseCode.SubFunctionNotSupported => "SubFunction Not Supported",
                UDSResponseCode.IncorrectMessageLengthOrInvalidFormat => "Incorrect Message Length Or Invalid Format",
                UDSResponseCode.ResponseTooLong => "Response Too Long",
                UDSResponseCode.BusyRepeatRequest => "Busy Repeat Request",
                UDSResponseCode.ConditionsNotCorrect => "Conditions Not Correct",
                UDSResponseCode.RequestSequenceError => "Request Sequence Error",
                UDSResponseCode.NoResponseFromSubnetComponent => "No Response From Subnet Component",
                UDSResponseCode.FailurePreventsExecutionOfRequestedAction => "Failure Prevents Execution Of Requested Action",
                UDSResponseCode.RequestOutOfRange => "Request Out Of Range",
                UDSResponseCode.SecurityAccessDenied => "Security Access Denied",
                UDSResponseCode.InvalidKey => "Invalid Key",
                UDSResponseCode.ExceedNumberOfAttempts => "Exceed Number Of Attempts",
                UDSResponseCode.RequiredTimeDelayNotExpired => "Required Time Delay Not Expired",
                UDSResponseCode.UploadDownloadNotAccepted => "Upload Download Not Accepted",
                UDSResponseCode.TransferDataSuspended => "Transfer Data Suspended",
                UDSResponseCode.GeneralProgrammingFailure => "General Programming Failure",
                UDSResponseCode.WrongBlockSequenceCounter => "Wrong Block Sequence Counter",
                UDSResponseCode.RequestCorrectlyReceived_ResponsePending => "Request Correctly Received - Response Pending",
                UDSResponseCode.SubFunctionNotSupportedInActiveSession => "SubFunction Not Supported In Active Session",
                UDSResponseCode.ServiceNotSupportedInActiveSession => "Service Not Supported In Active Session",
                _ => $"Unknown Response Code: 0x{(byte)code:X2}"
            };
        }
    }
}