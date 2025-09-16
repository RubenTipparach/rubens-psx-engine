using System;
using System.IO;
using System.Linq;
using rubens_psx_engine;
using rubens_psx_engine.system.utils;
using rubens_psx_engine.system.config;

try
{
    Logger.Info("Program: Starting application");

    using (rubens_psx_engine.Globals.screenManager = new ScreenManager())
    {
        Logger.Info("Program: ScreenManager created, starting game loop");
        Globals.screenManager.Run();
        Logger.Info("Program: Game loop ended normally");
    }

    Logger.Info("Program: Application shut down successfully");

    // Check if we should open logs on exit
    try
    {
        if (RenderingConfigManager.Config.Development.OpenLogsOnExit)
        {
            OpenLatestLogFile();
        }
    }
    catch (Exception logEx)
    {
        Console.WriteLine($"Failed to open log file: {logEx.Message}");
    }
}
catch (Exception ex)
{
    Logger.Critical("Program: Fatal application error", ex);

    bool showDialog = true;
    try
    {
        showDialog = RenderingConfigManager.Config.Development.ShowErrorDialogs;
    }
    catch
    {
        // Default to showing dialog if config can't be loaded
    }

    if (showDialog)
    {
        // Try to show error to user if possible
        try
        {
            System.Windows.Forms.MessageBox.Show(
                $"A fatal error occurred:\n\n{ex.Message}\n\nCheck the logs folder for detailed error information.",
                "Game Error",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        }
        catch
        {
            // If we can't show a message box, at least write to console
            Console.WriteLine($"FATAL ERROR: {ex.Message}");
            Console.WriteLine("Check the logs folder for detailed error information.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
    else
    {
        Console.WriteLine($"FATAL ERROR: {ex.Message}");
        Console.WriteLine("Check the logs folder for detailed error information.");
    }

    // Try to open log file on fatal error too
    try
    {
        if (RenderingConfigManager.Config.Development.OpenLogsOnExit)
        {
            OpenLatestLogFile();
        }
    }
    catch
    {
        // Ignore errors when trying to open log files
    }

    Environment.Exit(1);
}

static void OpenLatestLogFile()
{
    const string logDirectory = "logs";

    if (!Directory.Exists(logDirectory))
        return;

    var logFiles = Directory.GetFiles(logDirectory, "*.log")
        .OrderByDescending(f => File.GetLastWriteTime(f))
        .ToArray();

    if (logFiles.Length > 0)
    {
        try
        {
            System.Diagnostics.Process.Start("notepad.exe", logFiles[0]);
        }
        catch
        {
            // Try default system association
            try
            {
                System.Diagnostics.Process.Start(logFiles[0]);
            }
            catch
            {
                // If all else fails, just ignore
            }
        }
    }
}




