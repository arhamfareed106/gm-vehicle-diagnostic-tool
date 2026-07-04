# GM Global B Programmer - Technical Documentation

## Architecture Overview

The GM Global B Programmer follows a clean, layered architecture designed for maintainability, testability, and extensibility.

### Layer Structure

```
┌─────────────────────────────────────────┐
│              Presentation Layer          │
│           (GMGlobalBProgrammer.UI)       │
│  - WPF UI with MVVM pattern             │
│  - Dependency Injection                 │
│  - Real-time data binding               │
└─────────────────────────────────────────┘
                │
┌─────────────────────────────────────────┐
│              Application Layer           │
│         (Business Logic & Services)      │
│  - Function orchestration               │
│  - Workflow management                  │
│  - Error handling                       │
└─────────────────────────────────────────┘
                │
┌─────────────────────────────────────────┐
│              Domain Layer                │
│      (Core Business Rules & Entities)    │
│  - UDS services implementation          │
│  - Security algorithms                  │
│  - Data models                          │
└─────────────────────────────────────────┘
                │
┌─────────────────────────────────────────┐
│              Infrastructure Layer        │
│         (External Integrations)          │
│  - J2534 device communication           │
│  - CAN protocol handling                │
│  - File I/O operations                  │
└─────────────────────────────────────────┘
```

## Core Components

### 1. J2534 Layer

#### IJ2534Device Interface
```csharp
public interface IJ2534Device
{
    string Name { get; }
    string Vendor { get; }
    bool IsConnected { get; }
    Task<bool> ConnectAsync();
    Task<J2534Error> OpenChannelAsync(ProtocolId protocolId, ConnectFlags flags, uint baudRate = 500000);
    Task<J2534Error> SendMessageAsync(PassThruMsg message, uint timeoutMs = 1000);
    Task<(J2534Error Error, List<PassThruMsg> Messages)> ReadMessagesAsync(uint timeoutMs = 1000, uint maxMessages = 100);
}
```

#### Key Features:
- **Protocol Support**: ISO15765 (CAN), J1850VPW, J1850PWM, ISO9141, ISO14230
- **Multi-vendor**: Supports GM MDI2, DrewTech, Dearborn, Bosch, etc.
- **Error Handling**: Comprehensive J2534 error code management
- **Async Operations**: Non-blocking communication patterns

#### J2534Manager Responsibilities:
- Device discovery and enumeration
- Registry-based device detection
- Connection management and caching
- Device testing and validation

### 2. CAN Layer

#### ICANController Interface
```csharp
public interface ICANController
{
    bool IsConnected { get; }
    uint TxId { get; set; }
    uint RxId { get; set; }
    Task SendCANMessageAsync(CANMessage message);
    Task<List<CANMessage>> ReceiveCANMessagesAsync(uint timeoutMs = 1000);
    void SetFilter(uint canId, uint mask);
}
```

#### ISOTPTransport Implementation
Implements ISO 15765-2 (ISO-TP) multi-frame communication:

**Single Frame (SF)**: 0-7 bytes of data
```
Byte 0: PCI (0x0 + length)
Bytes 1-7: Data
```

**First Frame (FF)**: 8-4095 bytes
```
Byte 0: PCI (0x10 + length high nibble)
Byte 1: Length low byte
Bytes 2-7: First 6 data bytes
```

**Consecutive Frame (CF)**: Continuation frames
```
Byte 0: PCI (0x20 + sequence number)
Bytes 1-7: Data (up to 7 bytes)
```

**Flow Control (FC)**: Transfer control
```
Byte 0: PCI (0x30)
Byte 1: Block size
Byte 2: Separation time (STmin)
```

### 3. UDS Layer

#### UDS Services Implementation

**Diagnostic Session Control (0x10)**
```csharp
public async Task<UDSResponse> DiagnosticSessionControl(byte sessionType)
{
    var request = new byte[] { 0x10, sessionType };
    return await SendUDSRequestAsync(request);
}
```

**Security Access (0x27)**
```csharp
public async Task<UDSResponse> SecurityAccess(byte level, byte[] key = null)
{
    byte[] request = key == null 
        ? new byte[] { 0x27, level }           // Request seed
        : new byte[] { 0x27, (byte)(level + 1) }; // Send key
    
    if (key != null)
        request = request.Concat(key).ToArray();
        
    return await SendUDSRequestAsync(request);
}
```

**Read Data By Identifier (0x22)**
```csharp
public async Task<UDSResponse> ReadDataByIdentifier(ushort did)
{
    var request = new byte[] { 0x22, (byte)(did >> 8), (byte)did };
    return await SendUDSRequestAsync(request);
}
```

#### Response Handling
```csharp
private UDSResponse ParseUDSResponse(byte serviceId, byte[] responseData)
{
    var response = new UDSResponse { ServiceId = serviceId };
    
    if (responseData[0] == (serviceId + 0x40))
    {
        // Positive response
        response.IsPositive = true;
        response.Data = responseData.Skip(1).ToArray();
    }
    else if (responseData[0] == 0x7F)
    {
        // Negative response
        response.IsPositive = false;
        response.ResponseCode = (UDSResponseCode)responseData[2];
        response.ErrorMessage = NegativeResponseCode.GetDescription(response.ResponseCode);
    }
    
    return response;
}
```

### 4. Security Layer

#### GM Security Access Algorithm
```csharp
public byte[] CalculateKey(byte[] seed, byte securityLevel, string ecuType = "ECM")
{
    // Convert seed to 32-bit integer
    uint seedValue = (uint)((seed[0] << 24) | (seed[1] << 16) | (seed[2] << 8) | seed[3]);
    
    // Apply security level specific algorithm
    uint keyValue = ProcessSeedAlgorithm(seedValue, securityLevel, ecuType);
    
    // Convert back to bytes
    return new byte[] 
    {
        (byte)(keyValue >> 24),
        (byte)(keyValue >> 16),
        (byte)(keyValue >> 8),
        (byte)keyValue
    };
}
```

**Security Level Algorithms:**
- **Level 1 (Programming)**: Basic transformation with ECU-specific variations
- **Level 3 (Calibration)**: More complex algorithm for injector programming
- **Level 5 (Development)**: Advanced algorithm for engineering access

### 5. Function Layer

#### SBAT Function Implementation
```csharp
public async Task<bool> ExecuteSBATAsync()
{
    // 1. Establish programming session
    var sessionResponse = await _udsClient.DiagnosticSessionControl(0x02);
    if (!sessionResponse.IsPositive) return false;
    
    // 2. Execute SBAT routine
    var sbatResponse = await _udsClient.RoutineControl(0x01, 0xFF00);
    if (!sbatResponse.IsPositive) return false;
    
    // 3. Wait for completion
    await Task.Delay(2000);
    
    // 4. Check routine status
    var statusResponse = await _udsClient.RoutineControl(0x03, 0xFF00);
    return statusResponse.IsPositive;
}
```

#### VIN Writer Implementation
```csharp
public async Task<bool> WriteVINAsync(string vin)
{
    // 1. Establish programming session
    var sessionResponse = await _udsClient.DiagnosticSessionControl(0x02);
    if (!sessionResponse.IsPositive) return false;
    
    // 2. Perform security access
    if (!await PerformSecurityAccessAsync()) return false;
    
    // 3. Write VIN
    var vinBytes = Encoding.ASCII.GetBytes(vin);
    var writeResponse = await _udsClient.WriteDataByIdentifier(GMDIDs.VIN, vinBytes);
    if (!writeResponse.IsPositive) return false;
    
    // 4. Verify VIN
    return await VerifyVINAsync();
}
```

#### Injector Programmer Implementation
```csharp
public async Task<bool> ProgramInjectorsAsync()
{
    // 1. Load calibration data
    if (!_injectors.Any()) return false;
    
    // 2. Establish programming session
    var sessionResponse = await _udsClient.DiagnosticSessionControl(0x02);
    if (!sessionResponse.IsPositive) return false;
    
    // 3. Perform security access (level 3 for calibration)
    if (!await PerformSecurityAccessAsync(0x03)) return false;
    
    // 4. Program each injector
    foreach (var injector in _injectors)
    {
        await ProgramSingleInjectorAsync(injector);
    }
    
    // 5. Verify programming
    return await VerifyProgrammingAsync();
}
```

## Data Flow Architecture

### Message Flow Diagram
```
UI Layer
    ↓ (User Action)
Application Layer
    ↓ (Function Call)
Domain Layer (UDS Services)
    ↓ (Service Request)
Infrastructure Layer (CAN Controller)
    ↓ (CAN Message)
J2534 Device
    ↓ (Physical CAN Bus)
Vehicle ECU
```

### Error Handling Flow
```
1. J2534 Error → Retry with exponential backoff
2. CAN Timeout → Reconnect and retry
3. UDS Negative Response → Parse error code and handle appropriately
4. Security Access Failed → Retry with different algorithm if applicable
5. Application Error → Log and display user-friendly message
```

## Configuration Management

### Application Settings
```json
{
  "J2534": {
    "DefaultBaudRate": 500000,
    "ResponseTimeoutMs": 5000,
    "RetryAttempts": 3
  },
  "CAN": {
    "DefaultTxId": "0x7E0",
    "DefaultRxId": "0x7E8",
    "BlockSize": 8,
    "SeparationTimeMs": 20
  },
  "Logging": {
    "LogLevel": "Information",
    "LogToFile": true,
    "MaxLogSizeMB": 10
  }
}
```

## Testing Strategy

### Unit Testing
```csharp
[TestClass]
public class GMSecurityAccessTests
{
    [TestMethod]
    public void CalculateKey_ValidSeed_ReturnsValidKey()
    {
        var securityAccess = new GMSecurityAccess(new MockLogger());
        var seed = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        
        var key = securityAccess.CalculateKey(seed, 0x01);
        
        Assert.IsNotNull(key);
        Assert.AreEqual(4, key.Length);
        Assert.IsTrue(securityAccess.ValidateKey(key));
    }
}
```

### Integration Testing
- Mock J2534 device for testing without hardware
- CAN log replay for simulating real communication
- Error injection for testing fault handling

### Performance Testing
- Message throughput testing
- Connection establishment timing
- Memory usage monitoring
- CPU utilization analysis

## Deployment Architecture

### Build Process
```
1. Source Code → 2. Compilation → 3. Testing → 4. Packaging → 5. Deployment
```

### Deployment Options
- **Portable**: Single executable with no installation
- **Installer**: Traditional Windows installer (MSI)
- **ClickOnce**: Auto-updating deployment
- **MSIX**: Modern Windows package format

## Security Considerations

### Data Protection
- VIN encryption in memory
- Secure key storage
- Access logging for security operations
- Audit trail for programming operations

### Communication Security
- Secure J2534 channel establishment
- Message integrity verification
- Replay attack prevention
- Session timeout management

## Performance Optimization

### Memory Management
- Object pooling for CAN messages
- Efficient buffer management
- Garbage collection optimization
- Memory leak prevention

### Concurrency
- Async/await pattern for I/O operations
- Thread-safe data structures
- Lock-free algorithms where possible
- Proper resource disposal

## Monitoring and Diagnostics

### Logging Framework
- Structured logging with correlation IDs
- Multiple log levels (Debug, Info, Warning, Error)
- Log file rotation and management
- Real-time log viewing capability

### Performance Metrics
- Operation timing measurements
- Success/failure rate tracking
- Resource utilization monitoring
- Connection statistics

## Future Enhancements

### Planned Features
- Multi-ECU programming support
- Cloud-based VIN database integration
- Advanced diagnostic capabilities
- Mobile companion application
- Remote programming support

### Technical Improvements
- Enhanced security algorithms
- Improved error recovery mechanisms
- Better performance optimization
- Extended hardware support

## API Reference

### Core Interfaces

#### IJ2534Manager
```csharp
Task InitializeAsync();
Task<List<J2534DeviceInfo>> ScanForDevicesAsync();
Task<IJ2534Device> ConnectToDeviceAsync(J2534DeviceInfo deviceInfo);
Task<bool> DisconnectCurrentDeviceAsync();
```

#### IUDSClient
```csharp
Task<UDSResponse> DiagnosticSessionControl(byte sessionType);
Task<UDSResponse> ReadDataByIdentifier(ushort did);
Task<UDSResponse> SecurityAccess(byte level, byte[] key = null);
Task<UDSResponse> WriteDataByIdentifier(ushort did, byte[] data);
```

#### IISOTPTransport
```csharp
Task<byte[]> SendRequestAsync(byte[] requestData);
Task<bool> SendResponseAsync(byte[] responseData);
void SetTimeouts(uint responseTimeoutMs, uint separationTimeMs);
```

## Glossary

**CAN**: Controller Area Network
**ECU**: Electronic Control Unit
**ISO-TP**: ISO Transport Protocol (ISO 15765-2)
**J2534**: SAE J2534 Pass-Thru standard
**UDS**: Unified Diagnostic Services (ISO 14229)
**VIN**: Vehicle Identification Number
**SBAT**: System Burn-In and Test

---
*Technical Documentation Version 1.0.0*
*Last Updated: February 2026*