using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GMGlobalBProgrammer.Core.Utils
{
    public static class Extensions
    {
        public static string ToHexString(this byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;
            
            return string.Join(" ", data.Select(b => b.ToString("X2")));
        }

        public static byte[] FromHexString(this string hexString)
        {
            if (string.IsNullOrEmpty(hexString))
                return new byte[0];

            hexString = hexString.Replace(" ", "").Replace("-", "");
            
            if (hexString.Length % 2 != 0)
                throw new ArgumentException("Hex string must have even length");

            var bytes = new byte[hexString.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        public static bool IsValidVIN(this string vin)
        {
            if (string.IsNullOrEmpty(vin) || vin.Length != 17)
                return false;

            // Basic VIN validation
            // Check for valid characters (no I, O, Q characters)
            var invalidChars = new[] { 'I', 'O', 'Q' };
            if (vin.Any(c => invalidChars.Contains(char.ToUpper(c))))
                return false;

            // Check for valid format (alphanumeric except invalid chars)
            return vin.All(c => char.IsLetterOrDigit(c) && !invalidChars.Contains(char.ToUpper(c)));
        }

        public static string CalculateVINChecksum(this string vin)
        {
            if (string.IsNullOrEmpty(vin) || vin.Length < 17)
                return string.Empty;

            // VIN checksum calculation (simplified)
            // Real implementation would use proper GM VIN checksum algorithm
            var weights = new[] { 8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2 };
            var charValues = new Dictionary<char, int>
            {
                {'A', 1}, {'B', 2}, {'C', 3}, {'D', 4}, {'E', 5}, {'F', 6}, {'G', 7}, {'H', 8},
                {'J', 1}, {'K', 2}, {'L', 3}, {'M', 4}, {'N', 5}, {'P', 7}, {'R', 9}, {'S', 2},
                {'T', 3}, {'U', 4}, {'V', 5}, {'W', 6}, {'X', 7}, {'Y', 8}, {'Z', 9}
            };

            int sum = 0;
            for (int i = 0; i < 17; i++)
            {
                if (i == 8) continue; // Skip checksum position

                char c = char.ToUpper(vin[i]);
                int value = char.IsDigit(c) ? c - '0' : charValues.GetValueOrDefault(c, 0);
                sum += value * weights[i];
            }

            int checksum = sum % 11;
            return checksum == 10 ? "X" : checksum.ToString();
        }
    }

    public static class TimingHelper
    {
        public static async Task<bool> RetryAsync(Func<Task<bool>> operation, int maxRetries = 3, int delayMs = 1000)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var result = await operation();
                    if (result)
                        return true;
                }
                catch (Exception)
                {
                    if (i == maxRetries - 1)
                        throw;
                }

                if (i < maxRetries - 1)
                {
                    await Task.Delay(delayMs * (i + 1)); // Exponential backoff
                }
            }
            return false;
        }

        public static async Task<T> RetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3, int delayMs = 1000)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception)
                {
                    if (i == maxRetries - 1)
                        throw;
                }

                if (i < maxRetries - 1)
                {
                    await Task.Delay(delayMs * (i + 1));
                }
            }
            return default(T);
        }

        public static async Task WithTimeoutAsync(Task task, int timeoutMs)
        {
            using (var cts = new CancellationTokenSource(timeoutMs))
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cts.Token));
                if (completedTask == task)
                {
                    await task; // Re-throw any exceptions
                }
                else
                {
                    throw new TimeoutException($"Operation timed out after {timeoutMs}ms");
                }
            }
        }

        public static async Task<T> WithTimeoutAsync<T>(Task<T> task, int timeoutMs)
        {
            using (var cts = new CancellationTokenSource(timeoutMs))
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cts.Token));
                if (completedTask == task)
                {
                    return await task; // Re-throw any exceptions
                }
                else
                {
                    throw new TimeoutException($"Operation timed out after {timeoutMs}ms");
                }
            }
        }
    }
}