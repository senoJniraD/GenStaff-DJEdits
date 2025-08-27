using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GSBPGEMG;
using GSBPGEMG.Diagnostics;

static class Program
{
    public static bool Restart = false;

    [STAThread]
    static void Main(string[] args)
    {
        GameLogger.Initialize(); // Log game startup and unhandled exceptions

        ModelLib.Logging.GameRunning = true;

        if (Networking_Steam.Initialize() == true)
        {
            using (var game = new Game1())
                game.Run();
        }

        ModelLib.Logging.ShutdownLoggingThread = true;

        if (Restart)
        {
            System.Windows.Forms.Application.Restart();
        }
    }
}
