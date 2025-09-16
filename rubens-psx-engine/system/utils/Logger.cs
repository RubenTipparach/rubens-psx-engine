using System;
using System.IO;
using System.Text;

namespace rubens_psx_engine.system.utils
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public static class Logger
    {
        private static readonly string LogDirectory = "logs";
        private static readonly string LogFileName = $"game_{DateTime.Now:yyyy-MM-dd}.log";
        private static readonly string LogFilePath = Path.Combine(LogDirectory, LogFileName);
        private static readonly object LockObject = new object();

        static Logger()
        {
            try
            {
                // Create logs directory if it doesn't exist
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                // Write startup header
                WriteToFile($"=== Game Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                WriteToFile($"OS: {Environment.OSVersion}");
                WriteToFile($"Runtime: {Environment.Version}");
                WriteToFile($"Working Directory: {Environment.CurrentDirectory}");
                WriteToFile($"Command Line: {Environment.CommandLine}");
                WriteToFile("========================================");
            }
            catch (Exception ex)
            {
                // If we can't initialize logging, write to console as fallback
                Console.WriteLine($"Failed to initialize logger: {ex.Message}");
            }
        }

        public static void Log(LogLevel level, string message, Exception exception = null)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var levelStr = level.ToString().ToUpper().PadRight(8);
                var logEntry = new StringBuilder();

                logEntry.AppendLine($"[{timestamp}] [{levelStr}] {message}");

                if (exception != null)
                {
                    logEntry.AppendLine($"Exception: {exception.GetType().Name}: {exception.Message}");
                    logEntry.AppendLine($"Stack Trace: {exception.StackTrace}");

                    // Log inner exceptions too
                    var innerEx = exception.InnerException;
                    int depth = 1;
                    while (innerEx != null && depth < 5) // Limit depth to prevent infinite loops
                    {
                        logEntry.AppendLine($"Inner Exception {depth}: {innerEx.GetType().Name}: {innerEx.Message}");
                        logEntry.AppendLine($"Inner Stack Trace {depth}: {innerEx.StackTrace}");
                        innerEx = innerEx.InnerException;
                        depth++;
                    }
                }

                WriteToFile(logEntry.ToString());

                // Also write to console if it's available and enabled
                if (level >= LogLevel.Error)
                {
#if DEBUG
                    Console.WriteLine($"[{levelStr}] {message}");
                    if (exception != null)
                    {
                        Console.WriteLine($"Exception: {exception}");
                    }
#endif
                }
            }
            catch (Exception ex)
            {
                // Last resort: write to console
                Console.WriteLine($"Logging failed: {ex.Message}");
                Console.WriteLine($"Original message: [{level}] {message}");
            }
        }

        private static void WriteToFile(string content)
        {
            lock (LockObject)
            {
                try
                {
                    File.AppendAllText(LogFilePath, content + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // If file writing fails, there's not much we can do
                }
            }
        }

        public static void Info(string message) => Log(LogLevel.Info, message);
        public static void Warning(string message) => Log(LogLevel.Warning, message);
        public static void Error(string message, Exception exception = null) => Log(LogLevel.Error, message, exception);
        public static void Critical(string message, Exception exception = null) => Log(LogLevel.Critical, message, exception);

        public static void LogSystemInfo()
        {
            try
            {
                Info("=== SYSTEM INFORMATION ===");
                Info($"OS Version: {Environment.OSVersion}");
                Info($"Is 64-bit OS: {Environment.Is64BitOperatingSystem}");
                Info($"Is 64-bit Process: {Environment.Is64BitProcess}");
                Info($"Processor Count: {Environment.ProcessorCount}");
                Info($"CLR Version: {Environment.Version}");
                Info($"Working Directory: {Environment.CurrentDirectory}");
                Info($"Machine Name: {Environment.MachineName}");
                Info($"User Name: {Environment.UserName}");

                // Graphics adapter information
                try
                {
                    var adapters = Microsoft.Xna.Framework.Graphics.GraphicsAdapter.Adapters;
                    Info($"Graphics Adapters Count: {adapters.Count}");
                    for (int i = 0; i < adapters.Count; i++)
                    {
                        var adapter = adapters[i];
                        Info($"  Adapter {i}: {adapter.Description}");
                        Info($"    Device Name: {adapter.DeviceName}");
                        //Info($"    Current Display Mode: {adapter.CurrentDisplayMode.Width}x{adapter.CurrentDisplayMode.Height} @ {adapter.CurrentDisplayMode.RefreshRate}Hz");
                    }
                }
                catch (Exception ex)
                {
                    Error("Failed to get graphics adapter info", ex);
                }

                Info("=============================");
            }
            catch (Exception ex)
            {
                Error("Failed to log system info", ex);
            }
        }
    }
}