using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace GSBPGEMG.Diagnostics
{
    public static class GameLogger
    {
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "GeneralStaff", "Logs", $"GameLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        );

        public static void Initialize()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));
                File.AppendAllText(LogFilePath, $"[Game Start] {DateTime.Now}\n");

                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

                Log("[PHASE] Logger initialized.");
                Log("[PHASE] Hooking UnhandledException.");
                Log("[PHASE] Hooking ProcessExit.");
                Log("[PHASE] GameLogger fully initialized.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Logger Init Failed] {ex.Message}");
            }
        }

        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogFilePath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
            }
            catch { /* Silent fail */ }
        }

        public static void LogException(Exception ex)
        {
            try
            {
                Log("Exception Type: " + ex.GetType().FullName);
                Log("Message: " + ex.Message);
                Log("Stack Trace:\n" + ex.StackTrace);
                if (ex.InnerException != null)
                {
                    Log("Inner Exception Type: " + ex.InnerException.GetType().FullName);
                    Log("Inner Message: " + ex.InnerException.Message);
                    Log("Inner Stack Trace:\n" + ex.InnerException.StackTrace);
                }
            }
            catch (Exception loggingEx)
            {
                Log("Failed to log exception details: " + loggingEx);
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log("Unhandled exception occurred:");
            try
            {
                if (e.ExceptionObject is Exception ex)
                    LogException(ex);
                else
                    Log("Exception Object: " + e.ExceptionObject.ToString());
            }
            catch (Exception loggingEx)
            {
                Log("Error while logging exception details: " + loggingEx);
            }
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            Log("Game shutting down.");
        }
    }
}