# 🖥️ GM GLOBAL B PROGRAMMER - GUI VISUAL GUIDE

## ✅ APPLICATION STATUS: RUNNING
**Process ID:** 3096, 13044  
**Window Title:** GM Global B Programmer  
**Status:** Active and Ready

---

## 📋 WHAT YOU SHOULD SEE ON YOUR SCREEN

The application window should be visible somewhere on your desktop. Look for a window titled **"GM Global B Programmer"** with dimensions approximately 1000x700 pixels.

### 🔍 WINDOW APPEARANCE (Visual Layout)

```
╔══════════════════════════════════════════════════════════════════╗
║                    GM Global B Programmer                        ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  ┌────────────────────────────────────────────────────────────┐ ║
║  │ DEVICE CONNECTION PANEL                                    │ ║
║  │                                                            │ ║
║  │  Device: [▼ GM MDI2                    ]  [Connect]        │ ║
║  │         DrewTech Mongoose               [Disconnect]       │ ║
║  │         Dearborn DPA5                                      │ ║
║  │                                                            │ ║
║  │  ● Disconnected    Channel: 29-bit CAN @ 500kbps          │ ║
║  └────────────────────────────────────────────────────────────┘ ║
║                                                                  ║
║  ┌────────────────────────────────────────────────────────────┐ ║
║  │ FUNCTION BUTTONS                                           │ ║
║  │                                                            │ ║
║  │     [SBAT]      [VIN Write]    [Injector]    [Load Log]   │ ║
║  │                                                            │ ║
║  └────────────────────────────────────────────────────────────┘ ║
║                                                                  ║
║  ┌────────────────────────────────────────────────────────────┐ ║
║  │ DYNAMIC FUNCTION PANEL                                     │ ║
║  │                                                            │ ║
║  │           Select a function to begin                       │ ║
║  │                                                            │ ║
║  └────────────────────────────────────────────────────────────┘ ║
║                                                                  ║
║  ┌────────────────────────────────────────────────────────────┐ ║
║  │ LIVE CAN LOG                                    [Clear]    │ ║
║  │ ────────────────────────────────────────────────[Save Log] │ ║
║  │ Time     │ Dir │ CAN-ID  │ Data              │ Description │ ║
║  │ ─────────┴─────┴─────────┴───────────────────┴──────────── │ ║
║  │                                                            │ ║
║  │ Filter: [All ▼]                                            │ ║
║  └────────────────────────────────────────────────────────────┘ ║
║                                                                  ║
╠══════════════════════════════════════════════════════════════════╣
║ Status: Ready                              Progress:             ║
╚══════════════════════════════════════════════════════════════════╝
```

---

## 🎯 KEY INTERFACE ELEMENTS

### 1. **Device Dropdown** (Top Left)
Contains mock devices for testing:
- GM MDI2
- DrewTech Mongoose  
- Dearborn DPA5

### 2. **Connection Buttons**
- **Connect** - Establishes connection to selected device
- **Disconnect** - Disconnects from current device
- **Red/Green Circle** - Connection status indicator

### 3. **Main Function Buttons**
Four large buttons in the center:
- **SBAT** - System Burn-In and Test programming
- **VIN Write** - Vehicle Identification Number writing
- **Injector** - Fuel injector calibration programming
- **Load Log File** - Load CAN log files

### 4. **CAN Log Display** (Bottom)
Real-time communication monitor showing:
- Timestamp
- Message direction (TX/RX)
- CAN ID
- Data bytes in hexadecimal
- Message description

### 5. **Status Bar** (Very Bottom)
Shows current operation status and progress

---

## 🔧 IF YOU CAN'T SEE THE WINDOW

### Possible Issues & Solutions:

1. **Window is minimized**
   - Check your taskbar for the GM Global B Programmer icon
   - Click it to restore the window

2. **Window is behind other applications**
   - Press `Alt + Tab` to cycle through open windows
   - Look for "GM Global B Programmer"

3. **Window opened off-screen**
   - Press `Windows Key + Left/Right Arrow` to snap window to screen
   - Or press `Alt + Space`, then `M`, then use arrow keys to move window

4. **Application crashed on startup**
   - Check terminal for error messages
   - Verify .NET 8.0 runtime is installed

---

## ✨ QUICK START GUIDE

Once you see the GUI:

1. **Select a Device** from the dropdown (any of the mock devices will work)
2. **Click "Connect"** - Status circle should turn green
3. **Click a Function Button** (SBAT, VIN Write, or Injector)
4. **Follow the on-screen prompts** for that function
5. **Watch the CAN Log** at the bottom for real-time activity

---

## 🎨 COLOR SCHEME

The application uses a professional color scheme:
- **Background**: Light gray/white
- **Buttons**: Standard Windows button colors
- **Status Indicator**: Red (disconnected) → Green (connected)
- **Text**: Black on white/light backgrounds
- **Borders**: Gray borders around panels

---

## 📞 TROUBLESHOOTING

If the GUI still doesn't appear:

1. Check if process is running: `Get-Process | Where-Object {$_.ProcessName -like "*GMGlobalB*"}`
2. Try restarting: Close all instances and run the executable again
3. Check for errors in the console output
4. Verify your display settings and multiple monitor configurations

**The application IS running (confirmed by Process IDs: 3096, 13044)** - you just need to locate the window on your desktop!
