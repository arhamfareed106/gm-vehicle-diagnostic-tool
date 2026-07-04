using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace GMGlobalBProgrammer.Core.J2534
{
    public class VendorDetector
    {
        private readonly ILogger<VendorDetector> _logger;

        public VendorDetector(ILogger<VendorDetector> logger)
        {
            _logger = logger;
        }

        public List<J2534DeviceInfo> DetectAvailableDevices()
        {
            var devices = new List<J2534DeviceInfo>();
            
            try
            {
                _logger.LogInformation("=====================================");
                _logger.LogInformation("=== STARTING J2534 DEVICE DETECTION ===");
                _logger.LogInformation("=====================================");
                
                // System Information
                _logger.LogInformation($"OS Architecture: {(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")}");
                _logger.LogInformation($"Process Architecture: {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");
                _logger.LogInformation($"Running with Admin privileges: {IsRunningAsAdmin()}");
                
                // Enhanced diagnostics - Check if DLLs can be loaded
                _logger.LogInformation("");
                _logger.LogInformation("=== PHASE 0: SYSTEM CAPABILITY CHECK ===");
                TestSystemCapabilities();
                
                // Check registry for J2534 devices
                _logger.LogInformation("");
                _logger.LogInformation("=== PHASE 1: REGISTRY SCANNING ===");
                var registryDevices = DetectFromRegistry();
                devices.AddRange(registryDevices);
                _logger.LogInformation($"Registry scan found: {registryDevices.Count} devices");
                
                // Add known devices that might not be in registry
                _logger.LogInformation("");
                _logger.LogInformation("=== PHASE 2: KNOWN PATH SCANNING ===");
                var knownDevices = GetKnownDevices();
                devices.AddRange(knownDevices);
                _logger.LogInformation($"Known paths scan found: {knownDevices.Count} devices");
                
                // If no devices found, try system-wide search
                if (devices.Count == 0)
                {
                    _logger.LogInformation("");
                    _logger.LogInformation("=== PHASE 2B: SYSTEM-WIDE DLL SEARCH ===");
                    var systemDevices = SearchSystemForJ2534Dlls();
                    devices.AddRange(systemDevices);
                    _logger.LogInformation($"System-wide search found: {systemDevices.Count} devices");
                }
                
                // Remove duplicates and log final results
                var uniqueDevices = devices.Distinct().ToList();
                _logger.LogInformation("");
                _logger.LogInformation("=== PHASE 3: FINAL RESULTS ===");
                _logger.LogInformation($"Total devices found: {uniqueDevices.Count}");
                
                if (uniqueDevices.Count > 0)
                {
                    _logger.LogInformation("");
                    _logger.LogInformation("=== DETECTED DEVICES ===");
                    for (int i = 0; i < uniqueDevices.Count; i++)
                    {
                        var device = uniqueDevices[i];
                        _logger.LogInformation($"{i + 1}. {device.Name} ({device.Vendor})");
                        _logger.LogInformation($"   Device ID: {device.DeviceId}");
                        _logger.LogInformation($"   File exists: {File.Exists(device.DeviceId)}");
                        _logger.LogInformation($"   Is Available: {device.IsAvailable}");
                        _logger.LogInformation("");
                    }
                }
                else
                {
                    _logger.LogWarning("=====================================");
                    _logger.LogWarning("!!! NO J2534 DEVICES DETECTED !!!");
                    _logger.LogWarning("=====================================");
                    
                    _logger.LogWarning("");
                    _logger.LogWarning("=== DIAGNOSTIC INFORMATION ===");
                    _logger.LogWarning($"System is 64-bit: {Environment.Is64BitOperatingSystem}");
                    _logger.LogWarning($"Process is 64-bit: {Environment.Is64BitProcess}");
                    _logger.LogWarning($"Running as Admin: {IsRunningAsAdmin()}");
                    
                    _logger.LogWarning("");
                    _logger.LogWarning("=== COMMON FAILURE POINTS ===");
                    _logger.LogWarning("1. No J2534 drivers installed");
                    _logger.LogWarning("2. Drivers not registered in registry");
                    _logger.LogWarning("3. DLL files missing or corrupted");
                    _logger.LogWarning("4. Bitness mismatch (32-bit vs 64-bit)");
                    _logger.LogWarning("5. Insufficient permissions");
                    
                    _logger.LogWarning("");
                    _logger.LogWarning("=== RUNNING ADDITIONAL DIAGNOSTICS ===");
                    
                    // Check common installation directories
                    CheckCommonDirectories();
                    
                    // Check registry keys manually
                    CheckRegistryManually();
                }
                
                _logger.LogInformation("=====================================");
                _logger.LogInformation($"=== DETECTION COMPLETE - {uniqueDevices.Count} DEVICES ===");
                _logger.LogInformation("=====================================");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting J2534 devices");
            }

            return devices.Distinct().ToList();
        }
        
        private bool IsRunningAsAdmin()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
        
        private void TestSystemCapabilities()
        {
            _logger.LogInformation("Testing system capabilities for J2534 detection...");
            
            // Test if we can access common system directories
            var testPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                @"C:\Program Files",
                @"C:\Program Files (x86)"
            };
            
            foreach (var path in testPaths)
            {
                try
                {
                    bool exists = Directory.Exists(path);
                    _logger.LogInformation($"Path access test - {path}: {(exists ? "ACCESSIBLE" : "NOT ACCESSIBLE")}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Path access test failed for {path}: {ex.Message}");
                }
            }
            
            // Test registry access
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE"))
                {
                    _logger.LogInformation($"Registry SOFTWARE key access: {(key != null ? "SUCCESS" : "FAILED")}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Registry access test failed: {ex.Message}");
            }
            
            // Test DLL loading capability
            _logger.LogInformation("Testing DLL loading capability...");
            TestDllLoading();
            
            _logger.LogInformation("System capability check completed.");
        }
        
        private void TestDllLoading()
        {
            // Test if we can load common system DLLs
            var testDlls = new[]
            {
                "kernel32.dll",
                "user32.dll",
                "advapi32.dll"
            };
            
            foreach (var dll in testDlls)
            {
                try
                {
                    var handle = LoadLibrary(dll);
                    if (handle != IntPtr.Zero)
                    {
                        _logger.LogInformation($"DLL load test - {dll}: SUCCESS");
                        FreeLibrary(handle);
                    }
                    else
                    {
                        _logger.LogWarning($"DLL load test - {dll}: FAILED");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"DLL load test failed for {dll}: {ex.Message}");
                }
            }
        }
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);
        
        private void CheckRegistryManually()
        {
            _logger.LogInformation("=== MANUAL REGISTRY CHECK ===");
            
            var registryPaths = new[]
            {
                @"HKEY_LOCAL_MACHINE\SOFTWARE\PassThru\J2534",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\PassThru\J2534"
            };
            
            foreach (var path in registryPaths)
            {
                _logger.LogInformation($"Checking: {path}");
                try
                {
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path.Replace("HKEY_LOCAL_MACHINE\\", "")))
                    {
                        if (key != null)
                        {
                            _logger.LogInformation($"  ✓ Registry key exists");
                            var subKeys = key.GetSubKeyNames();
                            if (subKeys.Length > 0)
                            {
                                _logger.LogInformation($"  Found {subKeys.Length} subkeys:");
                                foreach (var subKey in subKeys)
                                {
                                    _logger.LogInformation($"    - {subKey}");
                                }
                            }
                            else
                            {
                                _logger.LogInformation($"  No subkeys found");
                            }
                        }
                        else
                        {
                            _logger.LogWarning($" ✗ Registry key not found");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"  Error checking {path}: {ex.Message}");
                }
            }
        }
        
        private void CheckCommonDirectories()
        {
            _logger.LogInformation("=== Starting Fallback Directory Scan ===");
            
            var commonPaths = new[]
            {
                @"C:\Program Files",
                @"C:\Program Files (x86)",
                @"C:\Program Files (x86)\Common Files",
                @"C:\Program Files\Common Files"
            };
            
            var j2534Dlls = new List<string>();
            
            foreach (var basePath in commonPaths)
            {
                if (!Directory.Exists(basePath))
                {
                    _logger.LogWarning($"Base path does not exist: {basePath}");
                    continue;
                }
                
                _logger.LogInformation($"Scanning: {basePath}");
                
                try
                {
                    // Get all directories in the base path
                    var directories = Directory.GetDirectories(basePath);
                    
                    // Look for J2534-related directories
                    var j2534Dirs = directories.Where(d => 
                        d.Contains("Drew", StringComparison.OrdinalIgnoreCase) ||
                        d.Contains("GM", StringComparison.OrdinalIgnoreCase) ||
                        d.Contains("Autel", StringComparison.OrdinalIgnoreCase) ||
                        d.Contains("Mongoose", StringComparison.OrdinalIgnoreCase) ||
                        d.Contains("MDI", StringComparison.OrdinalIgnoreCase) ||
                        d.Contains("Bosch", StringComparison.OrdinalIgnoreCase) ||
                        d.Contains("Tactrix", StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                    
                    if (j2534Dirs.Any())
                    {
                        _logger.LogInformation($"Found {j2534Dirs.Count} potential J2534 directories:");
                        foreach (var dir in j2534Dirs)
                        {
                            _logger.LogInformation($"  - {Path.GetFileName(dir)}");
                            SearchForJ2534Dlls(dir, j2534Dlls);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No J2534-related directories found in base path");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error scanning {basePath}: {ex.Message}");
                }
            }
            
            _logger.LogInformation($"=== Fallback Scan Complete: Found {j2534Dlls.Count} potential J2534 DLLs ===");
            
            // Add found DLLs to available devices
            foreach (var dllPath in j2534Dlls)
            {
                var deviceInfo = CreateDeviceInfoFromDllPath(dllPath);
                if (deviceInfo != null && deviceInfo.IsAvailable)
                {
                    _logger.LogInformation($"✓ Adding fallback device: {deviceInfo.Name}");
                    // This would need to be added to the main device list
                }
            }
        }
        
        private void SearchForJ2534Dlls(string directory, List<string> foundDlls)
        {
            try
            {
                // Search for J2534 DLLs in this directory and subdirectories
                var dllFiles = Directory.GetFiles(directory, "*J2534*.dll", SearchOption.AllDirectories);
                
                foreach (var dllPath in dllFiles)
                {
                    _logger.LogInformation($"  Found J2534 DLL: {dllPath}");
                    if (File.Exists(dllPath))
                    {
                        foundDlls.Add(dllPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Error searching in {directory}: {ex.Message}");
            }
        }
        
        private J2534DeviceInfo CreateDeviceInfoFromDllPath(string dllPath)
        {
            var fileName = Path.GetFileName(dllPath);
            var dirName = Path.GetDirectoryName(dllPath);
            
            // Determine vendor based on path
            string vendor = "Unknown Vendor";
            string name = "J2534 Device";
            
            if (dllPath.Contains("Drew", StringComparison.OrdinalIgnoreCase) || 
                dllPath.Contains("Mongoose", StringComparison.OrdinalIgnoreCase))
            {
                vendor = "Drew Technologies";
                name = "Drew Tech J2534 (Detected)";
            }
            else if (dllPath.Contains("GM", StringComparison.OrdinalIgnoreCase) || 
                     dllPath.Contains("MDI", StringComparison.OrdinalIgnoreCase))
            {
                vendor = "General Motors";
                name = "GM MDI2 (Detected)";
            }
            else if (dllPath.Contains("Autel", StringComparison.OrdinalIgnoreCase))
            {
                vendor = "Autel";
                name = "Autel J2534 (Detected)";
            }
            else if (dllPath.Contains("Bosch", StringComparison.OrdinalIgnoreCase))
            {
                vendor = "Bosch";
                name = "Bosch J2534 (Detected)";
            }
            
            return new J2534DeviceInfo
            {
                Name = name,
                Vendor = vendor,
                DeviceId = dllPath,
                IsAvailable = File.Exists(dllPath)
            };
        }

        private List<J2534DeviceInfo> DetectFromRegistry()
        {
            var devices = new List<J2534DeviceInfo>();
                            
            try
            {
                _logger.LogInformation($"Application architecture: {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");
                                
                // Check 64-bit registry using proper RegistryView
                _logger.LogInformation("=== SCANNING 64-BIT REGISTRY ===");
                _logger.LogInformation("Checking: HKLM\\SOFTWARE\\PassThru\\J2534");
                                
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    using (var key = baseKey.OpenSubKey(@"SOFTWARE\PassThru\J2534"))
                    {
                        if (key != null)
                        {
                            _logger.LogInformation("✓ 64-bit registry key found");
                            var reg64Devices = ReadDevicesFromKey(key);
                            devices.AddRange(reg64Devices);
                            _logger.LogInformation($"Found {reg64Devices.Count} devices in 64-bit registry");
                                            
                            // Log each device found
                            foreach (var device in reg64Devices)
                            {
                                _logger.LogInformation($"  - {device.Name} ({device.Vendor})");
                                _logger.LogInformation($"    DLL Path: {device.DeviceId}");
                                _logger.LogInformation($"    File exists: {File.Exists(device.DeviceId)}");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("✗ 64-bit registry key not found");
                        }
                    }
                }
                
                // Check 32-bit registry on 64-bit systems
                _logger.LogInformation("");
                _logger.LogInformation("=== SCANNING 32-BIT REGISTRY ===");
                _logger.LogInformation("Checking: HKLM\\SOFTWARE\\WOW6432Node\\PassThru\\J2534");
                                
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    using (var key = baseKey.OpenSubKey(@"SOFTWARE\WOW6432Node\PassThru\J2534"))
                    {
                        if (key != null)
                        {
                            _logger.LogInformation("✓ 32-bit registry key found");
                            var reg32Devices = ReadDevicesFromKey(key);
                            devices.AddRange(reg32Devices);
                            _logger.LogInformation($"Found {reg32Devices.Count} devices in 32-bit registry");
                                            
                            // Log each device found
                            foreach (var device in reg32Devices)
                            {
                                _logger.LogInformation($"  - {device.Name} ({device.Vendor})");
                                _logger.LogInformation($"    DLL Path: {device.DeviceId}");
                                _logger.LogInformation($"    File exists: {File.Exists(device.DeviceId)}");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("✗ 32-bit registry key not found");
                        }
                    }
                }
                                
                // Try alternative registry paths
                _logger.LogInformation("");
                _logger.LogInformation("=== SCANNING ALTERNATIVE REGISTRY PATHS ===");
                ScanAlternativeRegistryPaths(devices);
                        
                // Try manual registry scanning
                _logger.LogInformation("");
                _logger.LogInformation("=== SCANNING REGISTRY MANUALLY ===");
                CheckRegistryManually();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading registry for J2534 devices");
            }
                
            return devices;
        }

        private void ScanAlternativeRegistryPaths(List<J2534DeviceInfo> devices)
        {
            // Try alternative registry paths that some vendors might use
            var alternativePaths = new[]
            {
                new { Path = @"SOFTWARE\PassThruSupport", View = RegistryView.Registry64 },
                new { Path = @"SOFTWARE\WOW6432Node\PassThruSupport", View = RegistryView.Registry32 },
                new { Path = @"SOFTWARE\J2534", View = RegistryView.Registry64 },
                new { Path = @"SOFTWARE\WOW6432Node\J2534", View = RegistryView.Registry32 }
            };
                    
            foreach (var altPath in alternativePaths)
            {
                try
                {
                    _logger.LogInformation($"Checking alternative path: {altPath.Path}");
                            
                    using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, altPath.View))
                    {
                        using (var key = baseKey.OpenSubKey(altPath.Path))
                        {
                            if (key != null)
                            {
                                _logger.LogInformation($"✓ Found alternative registry path: {altPath.Path}");
                                var altDevices = ReadDevicesFromKey(key);
                                if (altDevices.Count > 0)
                                {
                                    devices.AddRange(altDevices);
                                    _logger.LogInformation($"Found {altDevices.Count} devices from alternative path");
                                }
                            }
                            else
                            {
                                _logger.LogDebug($"Alternative path not found: {altPath.Path}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"Error checking alternative path {altPath.Path}: {ex.Message}");
                }
            }
        }
        
        private List<J2534DeviceInfo> ReadDevicesFromKey(RegistryKey key)
        {
            var devices = new List<J2534DeviceInfo>();
            
            try
            {
                var subKeys = key.GetSubKeyNames();
                _logger.LogDebug($"Found {subKeys.Length} subkeys in registry key");
                
                foreach (var vendorName in subKeys)
                {
                    using (var vendorKey = key.OpenSubKey(vendorName))
                    {
                        if (vendorKey == null) 
                        {
                            _logger.LogDebug($"Could not open vendor key: {vendorName}");
                            continue;
                        }

                        var name = vendorKey.GetValue("Name")?.ToString() ?? vendorName;
                        var dllPath = vendorKey.GetValue("FunctionLibrary")?.ToString();

                        _logger.LogDebug($"Found vendor: {vendorName}");
                        _logger.LogDebug($"  Name: {name}");
                        _logger.LogDebug($"  DLL Path: {dllPath}");
                        
                        if (string.IsNullOrEmpty(dllPath))
                        {
                            _logger.LogWarning($"  Warning: No FunctionLibrary value for {vendorName}");
                            continue;
                        }

                        if (System.IO.File.Exists(dllPath))
                        {
                            var deviceInfo = new J2534DeviceInfo
                            {
                                Name = name,
                                Vendor = vendorName,
                                DeviceId = dllPath,
                                IsAvailable = true
                            };
                            devices.Add(deviceInfo);
                            _logger.LogInformation($"  ✓ Added device: {name}");
                        }
                        else
                        {
                            _logger.LogWarning($" ✗ DLL file not found: {dllPath}");
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Access denied reading registry key - try running as Administrator");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading vendor key");
            }

            return devices;
        }

        private List<J2534DeviceInfo> GetKnownDevices()
        {
            var knownDevices = new List<J2534DeviceInfo>();
            
            _logger.LogInformation("=== SEARCHING FOR J2534 DRIVERS IN KNOWN INSTALLATION PATHS ===");
            
            // Drew Tech paths - Enhanced
            _logger.LogInformation("Searching for Drew Technologies (Mongoose) drivers...");
            var mongoosePaths = new[]
            {
                // Mongoose GM III Plus
                @"C:\Program Files\Drew Technologies\MongooseIII\MongooseJ2534.dll",
                @"C:\Program Files (x86)\Drew Technologies\MongooseIII\MongooseJ2534.dll",
                
                // Mongoose Pro GM
                @"C:\Program Files\Drew Technologies\MongoosePro\MongooseProJ2534.dll",
                @"C:\Program Files (x86)\Drew Technologies\MongoosePro\MongooseProJ2534.dll",
                
                // General Drew Tech paths
                @"C:\Program Files\Drew Technologies\J2534\MongooseJ2534.dll",
                @"C:\Program Files (x86)\Drew Technologies\J2534\MongooseJ2534.dll",
                
                // Alternative Drew Tech paths
                @"C:\Program Files\Drew Tech\MongooseIII\MongooseJ2534.dll",
                @"C:\Program Files (x86)\Drew Tech\MongooseIII\MongooseJ2534.dll",
                
                // J2534 Toolbox paths
                @"C:\Program Files\Drew Technologies\J2534 Toolbox\J2534Toolbox.dll",
                @"C:\Program Files (x86)\Drew Technologies\J2534 Toolbox\J2534Toolbox.dll",
                
                // Additional Drew Tech variations
                @"C:\Program Files\Drew Technologies\Mongoose III Plus\MongooseJ2534.dll",
                @"C:\Program Files (x86)\Drew Technologies\Mongoose III Plus\MongooseJ2534.dll",
                @"C:\Program Files\Drew Technologies\MongooseIII Plus\MongooseJ2534.dll",
                @"C:\Program Files (x86)\Drew Technologies\MongooseIII Plus\MongooseJ2534.dll"
            };
            
            foreach (var path in mongoosePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    var name = path.Contains("MongooseIII") ? "Drew Tech Mongoose GM III Plus" : "Drew Tech Mongoose Pro GM";
                    knownDevices.Add(new J2534DeviceInfo
                    {
                        Name = name,
                        Vendor = "Drew Technologies",
                        DeviceId = path,
                        IsAvailable = true
                    });
                    _logger.LogInformation($"Found Drew Tech driver: {path}");
                }
            }
            
            _logger.LogInformation("");
            _logger.LogInformation("Searching for General Motors (MDI2) drivers...");
            
            // GM MDI2 paths - Enhanced
            var mdi2Paths = new[]
            {
                // Standard MDI2 paths
                @"C:\Program Files\GM\MDI2\GM MDI2 J2534.dll",
                @"C:\Program Files (x86)\GM\MDI2\GM MDI2 J2534.dll",
                
                // Full name paths
                @"C:\Program Files\General Motors\MDI2\GM MDI2 J2534.dll",
                @"C:\Program Files (x86)\General Motors\MDI2\GM MDI2 J2534.dll",
                
                // Simplified paths
                @"C:\Program Files\GM MDI2\GM MDI2 J2534.dll",
                @"C:\Program Files (x86)\GM MDI2\GM MDI2 J2534.dll",
                
                // Alternative MDI2 paths
                @"C:\Program Files\GM\MDI2\PassThruJ2534.dll",
                @"C:\Program Files (x86)\GM\MDI2\PassThruJ2534.dll",
                
                // GMLAN MDI2 paths
                @"C:\Program Files\GM\MDI2\GMMDI2_J2534.dll",
                @"C:\Program Files (x86)\GM\MDI2\GMMDI2_J2534.dll",
                
                // Additional MDI2 variations
                @"C:\Program Files\General Motors\MDI2\PassThruJ2534.dll",
                @"C:\Program Files (x86)\General Motors\MDI2\PassThruJ2534.dll",
                @"C:\Program Files\GM\MDI2\J2534\PassThruJ2534.dll",
                @"C:\Program Files (x86)\GM\MDI2\J2534\PassThruJ2534.dll"
            };
            
            foreach (var path in mdi2Paths)
            {
                if (System.IO.File.Exists(path))
                {
                    knownDevices.Add(new J2534DeviceInfo
                    {
                        Name = "GM MDI2",
                        Vendor = "General Motors",
                        DeviceId = path,
                        IsAvailable = true
                    });
                    _logger.LogInformation($"Found GM MDI2 driver: {path}");
                }
            }
            
            _logger.LogInformation("");
            _logger.LogInformation("Searching for Autel drivers...");
            
            // Autel paths - Enhanced
            var autelPaths = new[]
            {
                // MaxiPC Suite
                @"C:\Program Files\Autel\MaxiPCSuite\AutelJ2534.dll",
                @"C:\Program Files (x86)\Autel\MaxiPCSuite\AutelJ2534.dll",
                
                // MaxiCOM
                @"C:\Program Files\Autel\MaxiCOM\AutelJ2534.dll",
                @"C:\Program Files (x86)\Autel\MaxiCOM\AutelJ2534.dll",
                
                // MaxiPC Suite (alternative spelling)
                @"C:\Program Files\Autel\MaxiPC Suite\AutelJ2534.dll",
                @"C:\Program Files (x86)\Autel\MaxiPC Suite\AutelJ2534.dll",
                
                // Alternative Autel paths
                @"C:\Program Files\Autel\J2534\AutelJ2534.dll",
                @"C:\Program Files (x86)\Autel\J2534\AutelJ2534.dll",
                
                // MaxiSys
                @"C:\Program Files\Autel\MaxiSys\AutelJ2534.dll",
                @"C:\Program Files (x86)\Autel\MaxiSys\AutelJ2534.dll",
                
                // MaxiPC VCI
                @"C:\Program Files\Autel\MaxiPCVCI\AutelJ2534.dll",
                @"C:\Program Files (x86)\Autel\MaxiPCVCI\AutelJ2534.dll",
                
                // Additional Autel variations
                @"C:\Program Files\Autel\Maxi Elite\AutelJ2534.dll",
                @"C:\Program Files (x86)\Autel\Maxi Elite\AutelJ2534.dll",
                @"C:\Program Files\Autel\MaxiSys Elite\AutelJ2534.dll",
                @"C:\Program Files (x86)\Autel\MaxiSys Elite\AutelJ2534.dll"
            };
            
            foreach (var path in autelPaths)
            {
                if (System.IO.File.Exists(path))
                {
                    var name = path.Contains("MaxiCOM") ? "Autel MaxiCOM" : "Autel MaxiPC Suite";
                    knownDevices.Add(new J2534DeviceInfo
                    {
                        Name = name,
                        Vendor = "Autel",
                        DeviceId = path,
                        IsAvailable = true
                    });
                    _logger.LogInformation($"Found Autel driver: {path}");
                }
            }
            
            // Additional search in common directories
            SearchCommonDirectories(knownDevices);
            
            // Remove duplicates based on DeviceId
            var uniqueDevices = knownDevices.Where(d => d.IsAvailable)
                                          .GroupBy(d => d.DeviceId)
                                          .Select(g => g.First())
                                          .ToList();
            
            _logger.LogInformation($"Found {uniqueDevices.Count} unique J2534 drivers in known paths");
            
            // If no devices found, try system-wide DLL search
            if (uniqueDevices.Count == 0)
            {
                _logger.LogInformation("No devices found in known paths, searching system-wide...");
                var systemDevices = SearchSystemForJ2534Dlls();
                uniqueDevices.AddRange(systemDevices);
                _logger.LogInformation($"Found {systemDevices.Count} additional devices from system search");
            }
            
            return uniqueDevices;
        }
        
        private void SearchCommonDirectories(List<J2534DeviceInfo> knownDevices)
        {
            var searchPaths = new[]
            {
                @"C:\Program Files",
                @"C:\Program Files (x86)"
            };
            
            var vendorKeywords = new[] 
            { 
                "Drew", "GM", "Autel", "Bosch", "Tactrix", "Mongoose", "MDI",
                "PassThru", "J2534", "Vehicle", "Diagnostic", "OBD", "CAN"
            };
            
            foreach (var basePath in searchPaths)
            {
                if (!System.IO.Directory.Exists(basePath)) continue;
                
                try
                {
                    var directories = System.IO.Directory.GetDirectories(basePath);
                    
                    foreach (var dir in directories)
                    {
                        var dirName = System.IO.Path.GetFileName(dir);
                        if (vendorKeywords.Any(keyword => dirName.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                        {
                            _logger.LogDebug($"Searching directory: {dir}");
                            SearchForJ2534DLLs(dir, knownDevices);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"Error searching {basePath}: {ex.Message}");
                }
            }
        }
        
        private void SearchForJ2534DLLs(string directory, List<J2534DeviceInfo> knownDevices)
        {
            try
            {
                var dllFiles = System.IO.Directory.GetFiles(directory, "*J2534*.dll", System.IO.SearchOption.AllDirectories);
                
                foreach (var dllPath in dllFiles)
                {
                    var fileName = System.IO.Path.GetFileName(dllPath);
                    
                    // Skip if already found
                    if (knownDevices.Any(d => d.DeviceId.Equals(dllPath, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    
                    var deviceInfo = CreateDeviceInfoFromPath(dllPath);
                    if (deviceInfo != null)
                    {
                        knownDevices.Add(deviceInfo);
                        _logger.LogInformation($"Found J2534 DLL via directory search: {dllPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Error searching in {directory}: {ex.Message}");
            }
        }
        
        private J2534DeviceInfo CreateDeviceInfoFromPath(string dllPath)
        {
            var pathLower = dllPath.ToLower();
            var fileName = System.IO.Path.GetFileName(dllPath);
            
            if (pathLower.Contains("mongoose"))
            {
                return new J2534DeviceInfo
                {
                    Name = "Drew Tech Mongoose (Detected)",
                    Vendor = "Drew Technologies",
                    DeviceId = dllPath,
                    IsAvailable = true
                };
            }
            else if (pathLower.Contains("mdi") && pathLower.Contains("gm"))
            {
                return new J2534DeviceInfo
                {
                    Name = "GM MDI2 (Detected)",
                    Vendor = "General Motors",
                    DeviceId = dllPath,
                    IsAvailable = true
                };
            }
            else if (pathLower.Contains("autel"))
            {
                return new J2534DeviceInfo
                {
                    Name = "Autel J2534 (Detected)",
                    Vendor = "Autel",
                    DeviceId = dllPath,
                    IsAvailable = true
                };
            }
            else if (pathLower.Contains("bosch"))
            {
                return new J2534DeviceInfo
                {
                    Name = "Bosch J2534 (Detected)",
                    Vendor = "Bosch",
                    DeviceId = dllPath,
                    IsAvailable = true
                };
            }
            
            // Generic J2534 device
            return new J2534DeviceInfo
            {
                Name = $"J2534 Device ({fileName})",
                Vendor = "Unknown Vendor",
                DeviceId = dllPath,
                IsAvailable = true
            };
        }

        public bool TestDeviceConnection(string deviceId)
        {
            try
            {
                // Try to load the DLL and test basic connectivity
                using (var device = new RealJ2534Device(deviceId))
                {
                    // Attempt to connect to the device
                    var connectTask = device.ConnectAsync();
                    connectTask.Wait(TimeSpan.FromSeconds(3)); // Wait up to 3 seconds
                    
                    if (connectTask.Result)
                    {
                        // If connection succeeds, disconnect and return true
                        var disconnectTask = device.DisconnectAsync();
                        disconnectTask.Wait(TimeSpan.FromSeconds(2));
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to test device connection: {deviceId}");
                return false;
            }
        }
        
        private List<J2534DeviceInfo> SearchSystemForJ2534Dlls()
        {
            var foundDevices = new List<J2534DeviceInfo>();
            _logger.LogInformation("=== Starting System-Wide J2534 DLL Search ===");
            
            // Common system paths to search
            var searchPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                @"C:\Windows\System32",
                @"C:\Windows\SysWOW64",
                @"C:\ProgramData",
                @"C:\Program Files\Common Files",
                @"C:\Program Files (x86)\Common Files"
            };
            
            // Common J2534 DLL name patterns
            var dllPatterns = new[]
            {
                "*J2534*.dll",
                "*PassThru*.dll",
                "*Mongoose*.dll",
                "*MDI*.dll",
                "*Autel*.dll",
                "*Drew*.dll",
                "*GM*.dll",
                "*Vehicle*.dll",
                "*Diagnostic*.dll",
                "*OBD*.dll",
                "*CAN*.dll"
            };
            
            foreach (var basePath in searchPaths)
            {
                if (!System.IO.Directory.Exists(basePath))
                {
                    _logger.LogDebug($"Skipping non-existent path: {basePath}");
                    continue;
                }
                
                _logger.LogInformation($"Searching: {basePath}");
                
                try
                {
                    foreach (var pattern in dllPatterns)
                    {
                        var dllFiles = System.IO.Directory.GetFiles(basePath, pattern, System.IO.SearchOption.AllDirectories);
                        
                        foreach (var dllPath in dllFiles)
                        {
                            // Skip system directories that might have false positives
                            var dirPath = System.IO.Path.GetDirectoryName(dllPath)?.ToLower();
                            if (dirPath != null && 
                                (dirPath.Contains("windows\\winsxs") || 
                                 dirPath.Contains("windows\\assembly") ||
                                 dirPath.Contains("microsoft") ||
                                 dirPath.Contains("dotnet")))
                            {
                                continue;
                            }
                            
                            if (System.IO.File.Exists(dllPath))
                            {
                                var deviceInfo = CreateDeviceInfoFromPath(dllPath);
                                if (deviceInfo != null && deviceInfo.IsAvailable)
                                {
                                    // Avoid duplicates
                                    if (!foundDevices.Any(d => d.DeviceId.Equals(deviceInfo.DeviceId, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        foundDevices.Add(deviceInfo);
                                        _logger.LogInformation($"Found J2534 DLL: {dllPath}");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    _logger.LogDebug($"Access denied to path: {basePath}");
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"Error searching {basePath}: {ex.Message}");
                }
            }
            
            _logger.LogInformation($"=== System search complete: Found {foundDevices.Count} J2534 DLLs ===");
            return foundDevices;
        }
    }

    public class J2534DeviceInfo : IEquatable<J2534DeviceInfo>
    {
        public string Name { get; set; }
        public string Vendor { get; set; }
        public string DeviceId { get; set; }
        public bool IsAvailable { get; set; }

        public bool Equals(J2534DeviceInfo other)
        {
            if (other == null) return false;
            return DeviceId.Equals(other.DeviceId, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as J2534DeviceInfo);
        }

        public override int GetHashCode()
        {
            return DeviceId?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return $"{Name} ({Vendor})";
        }
    }
}
