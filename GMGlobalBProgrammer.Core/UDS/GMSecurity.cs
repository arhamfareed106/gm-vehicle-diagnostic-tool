using System;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace GMGlobalBProgrammer.Core.UDS
{
    public interface IGMSecurityAccess
    {
        byte[] CalculateKey(byte[] seed, byte securityLevel, string ecuType = "ECM");
        bool ValidateSeed(byte[] seed);
        bool ValidateKey(byte[] key);
    }

    public class GMSecurityAccess : IGMSecurityAccess
    {
        private readonly ILogger<GMSecurityAccess> _logger;

        public GMSecurityAccess(ILogger<GMSecurityAccess> logger)
        {
            _logger = logger;
        }

        public byte[] CalculateKey(byte[] seed, byte securityLevel, string ecuType = "ECM")
        {
            try
            {
                if (seed == null || seed.Length != 4)
                {
                    throw new ArgumentException("Seed must be 4 bytes");
                }

                if (!ValidateSeed(seed))
                {
                    throw new ArgumentException("Invalid seed format");
                }

                _logger.LogDebug($"Calculating key for seed: [{string.Join(" ", seed.Select(b => b.ToString("X2")))}], Level: {securityLevel}, ECU: {ecuType}");

                // Convert seed bytes to 32-bit integer (big-endian)
                uint seedValue = (uint)((seed[0] << 24) | (seed[1] << 16) | (seed[2] << 8) | seed[3]);
                
                // Apply GM Global B security algorithm
                uint keyValue = ProcessSeedAlgorithm(seedValue, securityLevel, ecuType);
                
                // Convert back to bytes (big-endian)
                var key = new byte[4];
                key[0] = (byte)(keyValue >> 24);
                key[1] = (byte)(keyValue >> 16);
                key[2] = (byte)(keyValue >> 8);
                key[3] = (byte)keyValue;

                _logger.LogDebug($"Calculated key: [{string.Join(" ", key.Select(b => b.ToString("X2")))}]");
                return key;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating security key");
                throw;
            }
        }

        private uint ProcessSeedAlgorithm(uint seed, byte securityLevel, string ecuType)
        {
            // GM Global B security algorithm implementation
            // This is a simplified version - actual GM algorithm may vary by ECU type
            
            uint key = seed;
            
            // Apply different transformations based on security level
            switch (securityLevel)
            {
                case 1: // Programming session
                    key = ApplyLevel1Algorithm(seed, ecuType);
                    break;
                    
                case 3: // Calibration session
                    key = ApplyLevel3Algorithm(seed, ecuType);
                    break;
                    
                case 5: // Development session
                    key = ApplyLevel5Algorithm(seed, ecuType);
                    break;
                    
                default:
                    // Default algorithm for unknown levels
                    key = ApplyDefaultAlgorithm(seed, securityLevel);
                    break;
            }

            return key;
        }

        private uint ApplyLevel1Algorithm(uint seed, string ecuType)
        {
            // Programming session algorithm (most common)
            uint key = seed;
            
            // Basic transformation - this is a placeholder
            // Actual GM algorithm would be more complex and ECU-specific
            key = ((key << 3) ^ 0x12345678) + 0x87654321;
            key = (key >> 2) ^ (key << 1);
            key = key ^ 0xDEADBEEF;
            
            // ECU-specific variations
            switch (ecuType.ToUpper())
            {
                case "ECM":
                    key = (key * 0x1010101) ^ 0xABCDEF00;
                    break;
                case "TCM":
                    key = (key * 0x01010101) ^ 0x12345678;
                    break;
                case "BCM":
                    key = (key * 0x11111111) ^ 0xFEDCBA98;
                    break;
                default:
                    key = (key * 0x2020202) ^ 0x13579BDF;
                    break;
            }
            
            return key & 0xFFFFFFFF; // Ensure 32-bit result
        }

        private uint ApplyLevel3Algorithm(uint seed, string ecuType)
        {
            // Calibration session algorithm
            uint key = seed;
            key = ((key >> 4) ^ 0x55555555) + 0xAAAAAAAA;
            key = (key << 1) ^ (key >> 3);
            key = key ^ 0xCAFEBABE;
            return key & 0xFFFFFFFF;
        }

        private uint ApplyLevel5Algorithm(uint seed, string ecuType)
        {
            // Development session algorithm
            uint key = seed;
            key = ((key * 0x1234567) ^ 0x89ABCDEF) + 0x76543210;
            key = (key >> 1) ^ (key << 2);
            key = key ^ 0xBADC0FFE;
            return key & 0xFFFFFFFF;
        }

        private uint ApplyDefaultAlgorithm(uint seed, byte securityLevel)
        {
            // Generic algorithm for unknown security levels
            uint key = seed;
            key = ((key << securityLevel) ^ 0x11111111) + (uint)(0x22222222 * (long)securityLevel);
            key = (key >> (securityLevel % 4)) ^ (key << (securityLevel % 3));
            key = key ^ 0x33333333;
            return key & 0xFFFFFFFF;
        }

        public bool ValidateSeed(byte[] seed)
        {
            if (seed == null || seed.Length != 4)
                return false;

            // Check for invalid seed patterns
            // All zeros or all ones are typically invalid
            if (seed.All(b => b == 0) || seed.All(b => b == 0xFF))
                return false;

            // Check for common invalid patterns
            var seedValue = (uint)((seed[0] << 24) | (seed[1] << 16) | (seed[2] << 8) | seed[3]);
            if (seedValue == 0x12345678 || seedValue == 0x87654321)
                return false;

            return true;
        }

        public bool ValidateKey(byte[] key)
        {
            if (key == null || key.Length != 4)
                return false;

            // Check for invalid key patterns
            if (key.All(b => b == 0) || key.All(b => b == 0xFF))
                return false;

            return true;
        }

        // Helper methods for testing and debugging
        public string SeedToHexString(byte[] seed)
        {
            return seed != null ? string.Join("", seed.Select(b => b.ToString("X2"))) : "NULL";
        }

        public string KeyToHexString(byte[] key)
        {
            return key != null ? string.Join("", key.Select(b => b.ToString("X2"))) : "NULL";
        }

        public byte[] HexStringToSeed(string hexString)
        {
            if (string.IsNullOrEmpty(hexString) || hexString.Length != 8)
                throw new ArgumentException("Seed hex string must be 8 characters");

            var bytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }
}