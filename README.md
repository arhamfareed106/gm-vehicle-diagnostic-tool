# GM Global B Vehicle Programming Tool

## Overview
This is a complete Windows desktop application for programming GM Global B vehicles using J2534 Pass-Thru interfaces. The application provides three core functions:
1. SBAT (System Burn-In and Test) requests
2. VIN writing/changing
3. Injector programming with CAN log parsing

## Features
- Multi-vendor J2534 support (GM MDI2, DrewTech Mongoose, Dearborn DPA5, etc.)
- 29-bit CAN communication with ISO-TP (ISO 15765-2) multi-frame support
- UDS (Unified Diagnostic Services) implementation
- GM Global B security access (seed/key) algorithms
- Real-time CAN logging with filtering
- Professional WPF UI with dark/light theme support
- Comprehensive error handling and retry mechanisms

## Architecture
The application follows a clean architecture pattern with the following layers:

### Core Layer (GMGlobalBProgrammer.Core)
- **J2534**: J2534 Pass-Thru interface implementation
- **CAN**: CAN controller and ISO-TP transport layer
- **UDS**: Unified Diagnostic Services client
- **Functions**: Business logic for SBAT, VIN writing, and injector programming
- **Parsers**: CAN log file parsing for injector data extraction
- **Utils**: Utility classes and extensions

### UI Layer (GMGlobalBProgrammer.UI)
- WPF-based user interface
- MVVM pattern implementation
- Real-time data binding
- Responsive design with collapsible panels

### Testing (GMGlobalBProgrammer.Tests)
- Unit tests for core functionality
- Mock implementations for testing without hardware

## Prerequisites
- Windows 10/11 (64-bit)
- .NET 8.0 SDK
- J2534-compatible hardware interface
- Visual Studio 2022 or later (recommended)

## Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/YOUR-USERNAME/gm-global-b-programmer.git
   cd gm-global-b-programmer
   ```
2. Open the solution in Visual Studio
3. Restore NuGet packages
4. Build the solution
5. Run the application

## Usage
1. **Device Connection**: Select your J2534 device from the dropdown and click "Connect"
2. **SBAT Function**: Click "SBAT" button and then "Request SBAT" to execute system burn-in test
3. **VIN Writing**: Click "VIN Write" button, enter the new 17-character VIN, and click "Write VIN"
4. **Injector Programming**: Click "Injector" button, load a CAN log file with calibration data, and click "Program Injectors"
5. **CAN Logging**: Monitor real-time CAN communication in the log panel at the bottom

## Technical Details

### J2534 Implementation
- Auto-detection of installed J2534 devices from Windows registry
- Support for 32-bit and 64-bit DLLs
- Proper error handling for device communication issues
- Cached device connections for faster startup

### CAN Communication
- ISO15765 (CAN) protocol with 29-bit addressing
- 500 kbps baud rate (configurable)
- Complete ISO-TP implementation for multi-frame messages
- Single Frame (SF), First Frame (FF), Consecutive Frames (CF), and Flow Control (FC) handling

### UDS Services Implemented
- 0x10: Diagnostic Session Control
- 0x22: ReadDataByIdentifier
- 0x27: Security Access
- 0x2E: WriteDataByIdentifier
- 0x31: RoutineControl
- 0x34: RequestDownload
- 0x36: TransferData
- 0x37: RequestTransferExit
- 0x3D: WriteMemoryByAddress

### GM Security Access
- 64-bit seed/key algorithm implementation
- Support for multiple security levels (1, 3, 5)
- ECU-specific key generation (ECM, TCM, BCM)
- Automatic seed request and key validation

## File Structure
```
GMGlobalBProgrammer/
├── GMGlobalBProgrammer.Core/
│   ├── CAN/
│   │   ├── CANMessage.cs
│   │   ├── ISOTPTransport.cs
│   │   └── CANController.cs
│   ├── Functions/
│   │   ├── SBATFunction.cs
│   │   ├── VINWriter.cs
│   │   └── InjectorProgrammer.cs
│   ├── J2534/
│   │   ├── IJ2534Device.cs
│   │   ├── J2534Device.cs
│   │   ├── J2534Manager.cs
│   │   ├── J2534Types.cs
│   │   └── VendorDetector.cs
│   ├── Parsers/
│   │   └── CANLogParser.cs
│   ├── UDS/
│   │   ├── GMSecurity.cs
│   │   ├── UDSClient.cs
│   │   └── UDSServices.cs
│   └── Utils/
│       └── Extensions.cs
├── GMGlobalBProgrammer.UI/
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── MainWindow.xaml
│   └── MainWindow.xaml.cs
├── GMGlobalBProgrammer.Tests/
│   └── UnitTest1.cs
└── ConsoleTest/
    ├── ConsoleTest.csproj
    └── Program.cs
```

## Testing
Run unit tests using:
```bash
dotnet test
```

Run console test application:
```bash
cd ConsoleTest
dotnet run
```

## Known Issues
- J2534-Sharp NuGet package has compatibility warnings with .NET 8.0
- Real hardware testing requires actual J2534 devices
- Some GM-specific security algorithms may need adjustment based on ECU variant

## Future Enhancements
- Additional ECU support
- Enhanced logging and diagnostics
- Multi-language support
- Remote programming capabilities
- Integration with vehicle databases

## Support
For issues and feature requests, please contact the development team.

## License
This project is proprietary and confidential. All rights reserved.# gm-vehicle-diagnostic-tool
