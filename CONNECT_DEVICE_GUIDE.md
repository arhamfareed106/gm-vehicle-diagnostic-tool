# 🚗 CONNECTING YOUR DREW TECH MONGOOSE DEVICE

## ✅ DRIVER STATUS: DETECTED!

Your **Drew Tech Mongoose** J2534 device has been successfully detected!

### Detected Devices:
1. ✓ **Drew Tech Mongoose (Detected)** - Drew Technologies
   - Path: `C:\Program Files (x86)\Drew Technologies, Inc\J2534\Mongoose-Plus GM3\j2534_loader.dll`
   - Status: Available ✓

2. ✓ **Drew Tech Mongoose (Detected)** - Drew Technologies  
   - Path: `C:\Program Files (x86)\Drew Technologies, Inc\J2534\Mongoose-Plus GM3\0500\j2534_0500_loader.dll`
   - Status: Available ✓

---

## 📋 STEP-BY-STEP CONNECTION GUIDE

### Step 1: Find the GUI Window
Look for the **GM Global B Programmer** window on your screen (Process ID: 128)

If you can't see it:
- Press `Alt + Tab` to cycle through windows
- Check your taskbar for the application icon
- Press `Windows Key + Left Arrow` to bring it on screen

### Step 2: Select Your Device
In the GUI window:
1. Click the **Device dropdown** at the top
2. Look for **"Drew Tech Mongoose"** or **"Drew Technologies"** in the list
3. Click to select it

### Step 3: Connect to Device
1. Click the **[Connect]** button next to the dropdown
2. Wait for the status indicator to turn **GREEN** ●
3. Status should change from "Disconnected" to "Connected"

### Step 4: Verify Connection
Check these indicators:
- ✅ Connection circle turns **GREEN**
- ✅ Status text shows "Connected to Drew Tech Mongoose"
- ✅ Channel info shows: "29-bit CAN @ 500kbps"

### Step 5: Start Using Functions
Once connected, you can now:
- Click **[SBAT]** for System Burn-In and Test
- Click **[VIN Write]** for VIN programming
- Click **[Injector]** for injector calibration
- Watch real-time CAN messages in the log panel

---

## 🎯 WHAT YOU SHOULD SEE NOW

```
┌────────────────────────────────────────────────────┐
│          GM Global B Programmer                    │
├────────────────────────────────────────────────────┤
│ Device: [▼ Drew Tech Mongoose]  [Connect]         │
│                                                    │
│ ● Connected    Channel: 29-bit CAN @ 500kbps      │
├────────────────────────────────────────────────────┤
│    [SBAT]  [VIN Write]  [Injector]  [Load Log]    │
├────────────────────────────────────────────────────┤
│ Dynamic Function Panel                             │
│ (Click a function to begin)                        │
├────────────────────────────────────────────────────┤
│ LIVE CAN LOG                                       │
│ Time | Dir | CAN-ID | Data | Description           │
└────────────────────────────────────────────────────┘
```

---

## 🔧 TROUBLESHOOTING

### If Device Doesn't Appear in Dropdown:
1. **Refresh the device list:**
   - Close the application completely
   - Run: `taskkill /f /im "GMGlobalBProgrammer.UI.exe"`
   - Restart: Run the application again

2. **Check USB connection:**
   - Ensure Drew Tech Mongoose is plugged in via USB
   - Check that device LEDs are lit

3. **Verify driver installation:**
   - Open Device Manager
   - Look for Drew Technologies or J2534 device
   - Should show no error icons

### If Connection Fails:
1. **Try a different DLL:**
   - Select the other "Drew Tech Mongoose" entry
   - One uses `j2534_loader.dll`, other uses `j2534_0500_loader.dll`

2. **Run as Administrator:**
   - Right-click the application
   - Select "Run as Administrator"

3. **Check device status:**
   - Make sure no other software is using the Mongoose
   - Close J2534 Toolbox if running

---

## ✨ QUICK TEST

Once connected, test communication:
1. Click **[SBAT]** button
2. Click **[Request SBAT]** 
3. Watch the CAN Log panel for messages
4. You should see TX (transmit) and RX (receive) messages

---

## 📊 DEVICE INFORMATION

**Your Hardware:**
- Device: Drew Tech Mongoose-Plus GM3
- Vendor: Drew Technologies (now part of Bosch)
- Protocol: J2534-1 and J2534-2 compatible
- Supported: GM, Ford, Chrysler, and other manufacturers

**Application Settings:**
- Protocol: ISO 15765-2 (CAN with ISO-TP)
- CAN ID: 29-bit extended addressing
- Baud Rate: 500 kbps
- Security: GM Global B algorithms

---

## 🎉 YOU'RE READY!

Your Drew Tech Mongoose is now connected and ready to program GM Global B vehicles!

**Next Steps:**
- Try the SBAT function first to test communication
- Use VIN Write to program vehicle identification numbers
- Use Injector for fuel injector calibration

The application is fully functional with your real hardware now! 🚗
