# GM Global B Programmer - User Manual

## Table of Contents
1. [Introduction](#introduction)
2. [System Requirements](#system-requirements)
3. [Installation](#installation)
4. [Getting Started](#getting-started)
5. [Main Interface](#main-interface)
6. [Function Guide](#function-guide)
7. [Troubleshooting](#troubleshooting)
8. [Technical Support](#technical-support)

## Introduction

The GM Global B Programmer is a professional Windows application designed for automotive technicians and engineers to program GM Global B vehicles. The software provides three essential programming functions:

- **SBAT (System Burn-In and Test)**: Execute system diagnostic tests
- **VIN Writing**: Change or program vehicle identification numbers
- **Injector Programming**: Program fuel injector calibration data

## System Requirements

### Minimum Requirements
- **Operating System**: Windows 10/11 (64-bit)
- **Processor**: Intel Core i5 or equivalent
- **Memory**: 8 GB RAM
- **Storage**: 500 MB available space
- **J2534 Interface**: Compatible hardware device (GM MDI2, DrewTech Mongoose, etc.)

### Recommended Requirements
- **Operating System**: Windows 11 Pro
- **Processor**: Intel Core i7 or equivalent
- **Memory**: 16 GB RAM
- **Storage**: 1 GB available space (SSD recommended)
- **J2534 Interface**: Latest firmware version

## Installation

### Method 1: Using Installer
1. Download the installer package
2. Extract the contents to a folder
3. Run `GMGlobalBProgrammer.UI.exe`
4. Windows may show a security warning - click "More info" then "Run anyway"

### Method 2: From Source
1. Install .NET 8.0 SDK
2. Clone the repository
3. Run `build.bat` to compile
4. Execute from `publish` directory

## Getting Started

### First Launch
When you first run the application, you'll see the main interface with:
- Device connection panel at the top
- Function selection buttons in the middle
- CAN log display at the bottom
- Status bar at the bottom

### Device Connection
1. **Select Device**: Choose your J2534 interface from the dropdown
2. **Connect**: Click the "Connect" button
3. **Verify Connection**: The indicator light will turn green when connected

**Connection Status Indicators:**
- 🔴 Red: Disconnected
- 🟢 Green: Connected
- 🟡 Yellow: Connecting

## Main Interface

### Device Connection Panel
```
[Device: ▼] [Connect] [Disconnect] ● [Channel: 29-bit CAN @ 500kbps]
```

**Components:**
- **Device Dropdown**: Lists all detected J2534 devices
- **Connect Button**: Establishes connection to selected device
- **Disconnect Button**: Terminates device connection
- **Status Indicator**: Visual connection status
- **Channel Info**: Shows current CAN configuration

### Function Selection Panel
```
[SBAT] [VIN Write] [Injector] [Load Log File]
```

**Buttons:**
- **SBAT**: Opens System Burn-In and Test interface
- **VIN Write**: Opens VIN programming interface
- **Injector**: Opens injector programming interface
- **Load Log File**: Loads CAN log files for injector programming

### Dynamic Function Panel
This area changes based on the selected function and displays:
- Function-specific controls
- Progress indicators
- Status messages
- Data grids (for injector programming)

### CAN Log Panel
```
┌─────────────────────────────────────────────────────────────┐
│ Time     Dir  CAN-ID  Data                    Description   │
│ 12:34:56 >>   7E0     02 10 02               Session Ctrl  │
│ 12:34:57 <<   7E8     02 50 02               Positive Resp │
└─────────────────────────────────────────────────────────────┘
[Clear] [Save Log] [Filter: All ▼]
```

**Features:**
- **Real-time Display**: Shows all CAN communication
- **Direction Indicators**: >> (transmit) << (receive)
- **Filtering**: Filter by TX, RX, or Errors
- **Export**: Save logs to file for analysis
- **Clear**: Remove all log entries

### Status Bar
```
[Ready] -------------------------------------------------- [Progress: 0%]
```

**Information Displayed:**
- Current operation status
- Progress percentage for long operations
- Error messages
- Connection status

## Function Guide

### 1. SBAT (System Burn-In and Test)

**Purpose**: Execute comprehensive system diagnostic tests

**Steps:**
1. Click the "SBAT" button in the function panel
2. Click "Request SBAT" to start the test
3. Monitor progress in the status area
4. Wait for completion message

**What Happens:**
1. Establishes programming session (0x10 0x02)
2. Sends SBAT routine control command (0x31)
3. Waits for routine completion
4. Verifies results and reports status

**Status Messages:**
- "Initializing" - Setting up connection
- "Establishing Session" - Opening programming session
- "Executing SBAT" - Running the test routine
- "Waiting for Completion" - Monitoring results
- "Completed Successfully" - Test passed
- "Completed with Warnings" - Test completed with issues
- "Failed" - Test could not be completed

### 2. VIN Writing

**Purpose**: Program or change the vehicle identification number

**Steps:**
1. Click the "VIN Write" button
2. Enter the new 17-character VIN in the text box
3. Click "Write VIN" to start programming
4. Wait for verification completion

**VIN Format Requirements:**
- Exactly 17 characters
- Valid characters: A-Z, 0-9
- No special characters or spaces
- Example: 1G1YY2FG5A5123456

**What Happens:**
1. Establishes programming session (0x10 0x02)
2. Performs security access (seed/key exchange)
3. Writes new VIN using 0x2E service
4. Reads back VIN to verify programming
5. Reports success or failure

**Security Process:**
1. Request seed (0x27 0x01)
2. Calculate key using GM algorithm
3. Send key (0x27 0x02)
4. Proceed with VIN programming

### 3. Injector Programming

**Purpose**: Program fuel injector calibration data

**Steps:**
1. Click the "Injector" button
2. Click "Load Log File" to select CAN log
3. Select log file containing calibration data
4. Review loaded injector data in the grid
5. Click "Program Injectors" to start programming
6. Monitor progress and status

**Supported Log Formats:**
- Vector CANalyzer/ASC format
- PCAN trace files (.trc)
- Standard CAN log files (.log)

**What Happens:**
1. Parse log file for injector calibration data
2. Identify cylinder-specific data
3. Establish programming session
4. Perform security access
5. Program each injector individually
6. Verify programming success
7. Report overall status

**Data Grid Information:**
- **Cylinder**: Injector cylinder number (1-8)
- **Part Number**: Injector part identification
- **Status**: Programming status for each injector

## CAN Log Analysis

### Log Filtering
Use the filter dropdown to view specific types of messages:
- **All**: Show all CAN messages
- **TX**: Show only transmitted messages
- **RX**: Show only received messages
- **Errors**: Show only error messages

### Log Export
1. Click "Save Log" button
2. Choose location and filename
3. File saved in text format for external analysis
4. Compatible with Vector CANalyzer, PCAN, and other tools

### Log Interpretation
Common message patterns:
```
>> 7E0 02 10 02        # Request programming session
<< 7E8 02 50 02        # Positive response
>> 7E0 02 27 01        # Request security seed
<< 7E8 06 67 01 12 34 56 78  # Seed response
>> 7E0 06 27 02 AA BB CC DD  # Send security key
<< 7E8 02 67 02        # Security access granted
```

## Troubleshooting

### Common Issues

**1. Device Not Found**
- Check if J2534 device is properly connected
- Verify device drivers are installed
- Try different USB port
- Check Windows Device Manager

**2. Connection Failed**
- Ensure device is not in use by another application
- Restart the J2534 device
- Check cable connections
- Verify correct device selection

**3. Security Access Denied**
- Verify correct ECU type is selected
- Check if ECU is in proper state
- Try cycling ignition
- Verify battery voltage is adequate

**4. Programming Fails**
- Check vehicle battery voltage (should be >11.5V)
- Ensure stable connection to vehicle
- Verify correct log file format
- Check for ECU errors or fault codes

**5. VIN Validation Errors**
- Verify VIN format (17 characters)
- Check for invalid characters
- Ensure no spaces or special characters
- Confirm VIN checksum is correct

### Error Messages

| Error | Meaning | Solution |
|-------|---------|----------|
| "Device not found" | No J2534 devices detected | Check connections and drivers |
| "Connection failed" | Unable to connect to device | Restart device, check cables |
| "Security access denied" | Key calculation failed | Verify ECU type, retry |
| "Invalid VIN format" | VIN doesn't meet requirements | Check 17-character format |
| "Programming timeout" | No response from ECU | Check connections, retry |
| "Invalid log file" | Cannot parse log data | Verify file format |

## Technical Support

### Contact Information
For technical support, please provide:
- Application version number
- J2534 device model and firmware version
- Vehicle information (year, make, model)
- Detailed description of the issue
- CAN log files if applicable

### Diagnostic Information
To help troubleshoot issues:
1. Enable debug logging in application settings
2. Reproduce the issue
3. Save the CAN log
4. Include system information (Windows version, .NET version)

### Version Information
Current Version: 1.0.0
Release Date: February 2026
Supported .NET Version: 8.0

## Appendix

### UDS Service Reference
| Service | Description | Usage |
|---------|-------------|-------|
| 0x10 | Diagnostic Session Control | Session management |
| 0x22 | Read Data By Identifier | Read ECU parameters |
| 0x27 | Security Access | Seed/key authentication |
| 0x2E | Write Data By Identifier | Write ECU parameters |
| 0x31 | Routine Control | Execute routines |
| 0x34 | Request Download | Start data transfer |
| 0x36 | Transfer Data | Send data blocks |
| 0x37 | Request Transfer Exit | Complete transfer |
| 0x3D | Write Memory By Address | Direct memory write |

### GM Security Levels
| Level | Purpose | Typical Use |
|-------|---------|-------------|
| 0x01 | Programming | VIN writing, calibration |
| 0x03 | Calibration | Injector programming |
| 0x05 | Development | Engineering access |

### CAN ID Reference
| ID | Direction | Purpose |
|----|-----------|---------|
| 0x7E0 | TX | Standard ECU request |
| 0x7E8 | RX | Standard ECU response |
| 0x18DAF110 | TX | Extended addressing |
| 0x18DA10F1 | RX | Extended response |

---
*This document is part of the GM Global B Programmer software package*
*Version 1.0.0 - February 2026*