using System;
using System.Collections.Generic;
using System.IO;
using ModelLib;

namespace GSBPGEMG
{
    public static class SavedProgramSettings
    {
        static string FileName = Path.Combine(FilesIO.BlackPowderFolder, "ProgramSettings.txt");
        static List<string> Settings = new List<string>();

        public static int LatestDataFormatVersion = 1;
        public static int MinCompatibleDataFormatVersion = 1;

        public static bool IsVictorian = true;
        public static bool HardwareFullScreen = false;

        public static void Load()
        {
            if (!File.Exists(FileName))
            {
                Save();
                return;
            }

            string[] lines = File.ReadAllLines(FileName);
            foreach (string line in lines)
            {
                string[] settingValues = line.Split('=');

                switch(settingValues[0])
                {
                    case nameof(IsVictorian): IsVictorian = Convert.ToBoolean(settingValues[1]); break;
                    case nameof(HardwareFullScreen): HardwareFullScreen = Convert.ToBoolean(settingValues[1]); break;
                }
            }
        }

        public static void Save()
        {
            SetSetting(nameof(LatestDataFormatVersion), LatestDataFormatVersion);
            SetSetting(nameof(MinCompatibleDataFormatVersion), MinCompatibleDataFormatVersion);
            SetSetting(nameof(IsVictorian), IsVictorian);
            SetSetting(nameof(HardwareFullScreen), HardwareFullScreen);
            File.WriteAllLines(FileName, Settings);
        }

        public static void SetSetting(string name, object value)
        {
            for (int i=0; i < Settings.Count; i++)
            {
                if (Settings[i].StartsWith(name))
                {
                    Settings[i] = name + "=" + value;
                    return;
                }
            }
            Settings.Add(name + "=" + value);
        }
    }
}
