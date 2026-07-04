using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GMGlobalBProgrammer.Core.Parsers
{
    public interface ICANLogParser
    {
        Task<List<InjectorCalibrationData>> ParseLogFileAsync(string filePath);
        bool CanParseFile(string filePath);
    }

    public class ASCLogParser : ICANLogParser
    {
        private readonly ILogger<ASCLogParser> _logger;

        public ASCLogParser(ILogger<ASCLogParser> logger)
        {
            _logger = logger;
        }

        public bool CanParseFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLower();
            return extension == ".asc" || extension == ".log";
        }

        public async Task<List<InjectorCalibrationData>> ParseLogFileAsync(string filePath)
        {
            var calibrationData = new List<InjectorCalibrationData>();

            try
            {
                _logger.LogInformation($"Parsing ASC log file: {filePath}");
                
                var lines = await File.ReadAllLinesAsync(filePath);
                var injectorData = new Dictionary<int, List<byte>>();

                foreach (var line in lines)
                {
                    var data = ParseASLine(line);
                    if (data != null && IsInjectorCalibrationMessage(data))
                    {
                        var cylinder = ExtractCylinderNumber(data);
                        if (cylinder > 0 && cylinder <= 8) // Support up to 8 cylinders
                        {
                            if (!injectorData.ContainsKey(cylinder))
                                injectorData[cylinder] = new List<byte>();
                            
                            injectorData[cylinder].AddRange(data.Data.Skip(1)); // Skip first byte (command)
                        }
                    }
                }

                // Convert to calibration data objects
                foreach (var kvp in injectorData)
                {
                    calibrationData.Add(new InjectorCalibrationData
                    {
                        CylinderNumber = kvp.Key,
                        PartNumber = $"INJ-{kvp.Key:D2}",
                        CalibrationData = kvp.Value.ToArray()
                    });
                }

                _logger.LogInformation($"Extracted calibration data for {calibrationData.Count} injectors");
                return calibrationData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing ASC log file: {filePath}");
                return calibrationData;
            }
        }

        private CANLogData ParseASLine(string line)
        {
            // Parse Vector ASC format: timestamp channel ID data
            // Example: 12.345 1 7E0 02 10 02
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 4)
                return null;

            try
            {
                var timestamp = double.Parse(parts[0]);
                var channel = int.Parse(parts[1]);
                var id = Convert.ToUInt32(parts[2], 16);
                var data = parts.Skip(3).Select(x => Convert.ToByte(x, 16)).ToArray();

                return new CANLogData
                {
                    Timestamp = timestamp,
                    Channel = channel,
                    Id = id,
                    Data = data
                };
            }
            catch
            {
                return null;
            }
        }

        private bool IsInjectorCalibrationMessage(CANLogData data)
        {
            // Look for typical injector programming patterns
            // This is a simplified pattern matching approach
            if (data.Id != 0x7E0) // Standard ECU request ID
                return false;

            if (data.Data.Length < 2)
                return false;

            // Look for programming-related service IDs
            var serviceId = data.Data[0];
            var subFunction = data.Data.Length > 1 ? data.Data[1] : (byte)0;

            // Common injector programming services
            return serviceId == 0x34 ||  // RequestDownload
                   serviceId == 0x36 ||  // TransferData
                   serviceId == 0x3D ||  // WriteMemoryByAddress
                   (serviceId == 0x31 && subFunction == 0x01); // RoutineControl - start
        }

        private int ExtractCylinderNumber(CANLogData data)
        {
            // Extract cylinder number from data (this is implementation-specific)
            // In real implementation, this would depend on the specific ECU protocol
            if (data.Data.Length >= 3)
            {
                // Example: byte 2 might contain cylinder info
                return (data.Data[2] & 0x0F) + 1; // Extract lower nibble and add 1
            }
            return 1; // Default to cylinder 1
        }
    }

    public class PCANLogParser : ICANLogParser
    {
        private readonly ILogger<PCANLogParser> _logger;

        public PCANLogParser(ILogger<PCANLogParser> logger)
        {
            _logger = logger;
        }

        public bool CanParseFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLower();
            return extension == ".trc" || extension == ".log";
        }

        public async Task<List<InjectorCalibrationData>> ParseLogFileAsync(string filePath)
        {
            var calibrationData = new List<InjectorCalibrationData>();

            try
            {
                _logger.LogInformation($"Parsing PCAN log file: {filePath}");
                
                var lines = await File.ReadAllLinesAsync(filePath);
                var injectorData = new Dictionary<int, List<byte>>();

                foreach (var line in lines)
                {
                    var data = ParsePCANLine(line);
                    if (data != null && IsInjectorProgrammingMessage(data))
                    {
                        var cylinder = ExtractCylinderInfo(data);
                        if (cylinder > 0)
                        {
                            if (!injectorData.ContainsKey(cylinder))
                                injectorData[cylinder] = new List<byte>();
                            
                            injectorData[cylinder].AddRange(data.Data);
                        }
                    }
                }

                foreach (var kvp in injectorData)
                {
                    calibrationData.Add(new InjectorCalibrationData
                    {
                        CylinderNumber = kvp.Key,
                        PartNumber = $"INJ-{kvp.Key:D2}",
                        CalibrationData = kvp.Value.ToArray()
                    });
                }

                return calibrationData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing PCAN log file: {filePath}");
                return calibrationData;
            }
        }

        private CANLogData ParsePCANLine(string line)
        {
            // Parse PCAN format: timestamp ID data
            // Example: 12.345 7E0 02 10 02
            var match = Regex.Match(line, @"^(\d+\.\d+)\s+([0-9A-F]+)\s+(.+)$");
            
            if (!match.Success)
                return null;

            try
            {
                var timestamp = double.Parse(match.Groups[1].Value);
                var id = Convert.ToUInt32(match.Groups[2].Value, 16);
                var dataStr = match.Groups[3].Value;
                var dataBytes = dataStr.Split(' ')
                                     .Where(x => !string.IsNullOrEmpty(x))
                                     .Select(x => Convert.ToByte(x, 16))
                                     .ToArray();

                return new CANLogData
                {
                    Timestamp = timestamp,
                    Id = id,
                    Data = dataBytes
                };
            }
            catch
            {
                return null;
            }
        }

        private bool IsInjectorProgrammingMessage(CANLogData data)
        {
            // PCAN-specific injector programming detection
            return data.Id >= 0x7E0 && data.Id <= 0x7EF && data.Data.Length >= 2;
        }

        private int ExtractCylinderInfo(CANLogData data)
        {
            // Extract cylinder information from PCAN format
            if (data.Data.Length >= 2)
            {
                // Example extraction logic
                return (data.Data[1] & 0x07) + 1; // Extract 3 bits for cylinder 1-8
            }
            return 1;
        }
    }

    public class GenericCANLogParser : ICANLogParser
    {
        private readonly List<ICANLogParser> _parsers;
        private readonly ILogger<GenericCANLogParser> _logger;

        public GenericCANLogParser(
            ILogger<GenericCANLogParser> logger,
            ILogger<ASCLogParser> ascLogger,
            ILogger<PCANLogParser> pcanLogger)
        {
            _logger = logger;
            _parsers = new List<ICANLogParser>
            {
                new ASCLogParser(ascLogger),
                new PCANLogParser(pcanLogger)
            };
        }

        public bool CanParseFile(string filePath)
        {
            return _parsers.Any(p => p.CanParseFile(filePath));
        }

        public async Task<List<InjectorCalibrationData>> ParseLogFileAsync(string filePath)
        {
            foreach (var parser in _parsers)
            {
                if (parser.CanParseFile(filePath))
                {
                    _logger.LogInformation($"Using {parser.GetType().Name} to parse {filePath}");
                    return await parser.ParseLogFileAsync(filePath);
                }
            }

            _logger.LogWarning($"No suitable parser found for file: {filePath}");
            return new List<InjectorCalibrationData>();
        }
    }

    public class CANLogData
    {
        public double Timestamp { get; set; }
        public int Channel { get; set; }
        public uint Id { get; set; }
        public byte[] Data { get; set; }
    }

    public class InjectorCalibrationData
    {
        public int CylinderNumber { get; set; }
        public string PartNumber { get; set; }
        public byte[] CalibrationData { get; set; }
    }
}