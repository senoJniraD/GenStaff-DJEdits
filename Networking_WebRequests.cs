using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using ModelLib;
using TacticalAILib;
using static GSBPGEMG.Game1;

namespace GSBPGEMG
{
    public class Networking_WebRequests
    {
        public enum WebRequestType
        {
            SubmitSavedGameData,
            RetrieveSavedGameData,
            AvailableSavedGames, // TODO (noted by MT)
            JoinOpenSavedGame,
            DebugRetrieveServerLogs
        }

        public struct OnlineGamesSearchResult(long savedDataUniqueID, int armyIndex, long playerSteamID, string turnProgress)
        {
            public long SavedDataUniqueID = savedDataUniqueID;
            public int ArmyIndex = armyIndex;
            public long PlayerSteamID = playerSteamID;
            public string TurnProgress = turnProgress;
        }
        public static ConcurrentDictionary<ulong, OnlineGamesSearchResult> OnlineGamesSearchResults { get; private set; } = [];

        public double[] lastRequestTimes;

        internal static string APIAddress = "http://104.131.172.78:13000";

        public Networking_WebRequests()
        {
            lastRequestTimes = new double[typeof(WebRequestType).GetEnumNames().Length];
            Array.Fill(lastRequestTimes, double.MinValue);
        }

        public async Task<bool> SubmitSavedGameData(GameInstance instance, SavedGameData savedGameData)
        {
            using MemoryStream memoryStreamWrite = new();
            using BinaryWriter binaryWriter = new(memoryStreamWrite, Encoding.UTF8);

            binaryWriter.Write((byte)WebRequestType.SubmitSavedGameData);
            binaryWriter.Write(Game1.SteamServices.localID);
            binaryWriter.Write(SavedGameData.LatestDataFormatVersion); // TODO (noted by MT) game version
            binaryWriter.Write(savedGameData.SavedDataUniqueID);
            byte[] data = savedGameData.SaveAsByteArray(instance, SavedGameData.LoadedStatus.FullGameInstance);
            binaryWriter.Write(data.Length);
            binaryWriter.Write(data);
            binaryWriter.Write(savedGameData.TurnsProgress);
            for (int i = 0; i < savedGameData.SavedArmies.Count; i++)
                binaryWriter.Write(savedGameData.SavedArmies[i].SteamID);

            (byte[] data, Exception exception) response = await PerformWebRequest(memoryStreamWrite.ToArray());
            lastRequestTimes[(int)WebRequestType.SubmitSavedGameData] = GameTimeRef.TotalGameTime.TotalSeconds;

            if (response.data != null)
            {
                using MemoryStream memoryStreamRead = new(response.data);
                using BinaryReader binaryReader = new(memoryStreamRead, Encoding.UTF8);
                bool success = binaryReader.ReadBoolean();
                savedGameData.ServerTurnsProgress = binaryReader.ReadString();
                return success;
            }
            else
            {
                Logging.Write(response.exception, false, "Unable To Upload Saved Game",
                    "Unable to upload saved game to the server.");
                return false;
            }
        }

        public async Task<byte[]> RetrieveSavedGameData(ulong savedDataUniqueID, SavedGameData currentSavedGameData)
        {
            using MemoryStream memoryStreamWrite = new MemoryStream();
            using BinaryWriter binaryWriter = new BinaryWriter(memoryStreamWrite, Encoding.UTF8);

            binaryWriter.Write((byte)WebRequestType.RetrieveSavedGameData);
            binaryWriter.Write(Game1.SteamServices.localID);
            binaryWriter.Write(SavedGameData.LatestDataFormatVersion); // TODO (noted by MT) game version
            binaryWriter.Write(savedDataUniqueID);
            if ((savedDataUniqueID == currentSavedGameData?.SavedDataUniqueID) &&
                (currentSavedGameData.ServerTurnsProgress != null) &&
                (currentSavedGameData.GameProgressState != SavedGameData.GameProgressStates.NewInvite) &&
                (currentSavedGameData.LoadedGameStatus == SavedGameData.LoadedStatus.FullGameInstance))
                binaryWriter.Write(currentSavedGameData.TurnsProgress);
            else
                binaryWriter.Write("");

            (byte[] data, Exception exception) response = await PerformWebRequest(memoryStreamWrite.ToArray());
            lastRequestTimes[(int)WebRequestType.RetrieveSavedGameData] = GameTimeRef.TotalGameTime.TotalSeconds;

            if (response.data != null)
            {
                using MemoryStream memoryStreamRead = new MemoryStream(response.data);
                using BinaryReader binaryReader = new BinaryReader(memoryStreamRead, Encoding.UTF8);
                bool success = binaryReader.ReadBoolean();
                if (success)
                {
                    bool newTurnsFound = binaryReader.ReadBoolean();
                    if (newTurnsFound)
                    {
                        int dataLength = binaryReader.ReadInt32();
                        byte[] data = binaryReader.ReadBytes(dataLength);
                        return data;
                    }
                    else
                    {
                        return [];
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                Logging.Write(response.exception, false, "Unable To Download Saved Game",
                    "Unable to download saved game from the server.");
                return null;
            }
        }

        public async Task AvailableSavedGames()
        {
            using MemoryStream memoryStreamWrite = new();
            using BinaryWriter binaryWriter = new(memoryStreamWrite, Encoding.UTF8);

            binaryWriter.Write((byte)WebRequestType.AvailableSavedGames);
            binaryWriter.Write(Game1.SteamServices.localID);
            binaryWriter.Write(SavedGameData.LatestDataFormatVersion); // TODO (noted by MT) game version

            (byte[] data, Exception exception) response = await PerformWebRequest(memoryStreamWrite.ToArray());
            lastRequestTimes[(int)WebRequestType.AvailableSavedGames] = GameTimeRef.TotalGameTime.TotalSeconds;

            if (response.data != null)
            {
                using MemoryStream memoryStreamRead = new(response.data);
                using BinaryReader binaryReader = new(memoryStreamRead, Encoding.UTF8);

                int resultsCount = binaryReader.ReadInt32();
                for (int i = 0; i < resultsCount; i++)
                {
                    long savedDataUniqueID = binaryReader.ReadInt64();
                    int armyIndex = binaryReader.ReadInt32();
                    long playerSteamID = binaryReader.ReadInt64();
                    string turnProgress = binaryReader.ReadString();
                    OnlineGamesSearchResult result = new(savedDataUniqueID, armyIndex, playerSteamID, turnProgress);
                    OnlineGamesSearchResults.AddOrUpdate((ulong)savedDataUniqueID, result, (key, existingValue) => result);
                }
            }
            else
            {
                Logging.Write(response.exception, false, "Unable To Retrieve Available Saved Games",
                    "Unable to retrieve available saved games from server.");
            }
        }

        public async Task<bool> JoinOpenSavedGame(ulong savedDataUniqueID, SavedGameData joinSavedGameData, int armyIndex)
        {
            using MemoryStream memoryStreamWrite = new();
            using BinaryWriter binaryWriter = new(memoryStreamWrite, Encoding.UTF8);

            binaryWriter.Write((byte)WebRequestType.JoinOpenSavedGame);
            binaryWriter.Write(Game1.SteamServices.localID);
            binaryWriter.Write(SavedGameData.LatestDataFormatVersion); // TODO (noted by MT) game version
            binaryWriter.Write(joinSavedGameData.SavedDataUniqueID);
            binaryWriter.Write(armyIndex);

            (byte[] data, Exception exception) response = await PerformWebRequest(memoryStreamWrite.ToArray());
            lastRequestTimes[(int)WebRequestType.JoinOpenSavedGame] = GameTimeRef.TotalGameTime.TotalSeconds;

            if (response.data != null)
            {
                using MemoryStream memoryStreamRead = new(response.data);
                using BinaryReader binaryReader = new(memoryStreamRead, Encoding.UTF8);

                bool success = binaryReader.ReadBoolean();
                if (success)
                    return true;
            }

            Logging.Write(response.exception, false, "Unable To Join Open Saved Game",
                "Unable to join the open saved game on the server.");
            return false;
        }

#if DEBUG
        public async Task DebugLogs()
        {
            using MemoryStream memoryStreamWrite = new MemoryStream();
            using BinaryWriter binaryWriter = new BinaryWriter(memoryStreamWrite, Encoding.UTF8);

            binaryWriter.Write((byte)WebRequestType.DebugRetrieveServerLogs);
            binaryWriter.Write(Game1.SteamServices.localID);

            (byte[] data, Exception exception) response = await PerformWebRequest(memoryStreamWrite.ToArray());
            lastRequestTimes[(int)WebRequestType.DebugRetrieveServerLogs] = GameTimeRef.TotalGameTime.TotalSeconds;

            if (response.data != null)
            {
                using MemoryStream memoryStreamRead = new(response.data);
                using BinaryReader binaryReader = new(memoryStreamRead, Encoding.UTF8);

                int resultsCount = binaryReader.ReadInt32();
                for (int i = 0; i < resultsCount; i++)
                {
                    string info = binaryReader.ReadString();
                    DateTime date = new(binaryReader.ReadInt64());
                    int type = binaryReader.ReadInt32();

                    Color textColor = type switch
                    {
                        0 => textColor = Color.White,// General
                        1 => textColor = Color.Green, // Success
                        2 => textColor = Color.Red, // Failed
                        3 => textColor = Color.Yellow // Received
                    };

                    Logging.Write(info, Logging.MessageType.ServerLogs, textColor.ToWindowsMediaColor(), date);
                }
            }
            else
            {
                Logging.Write(response.exception, false, "Unable To Retrieve Server Logs",
                    "Unable to retrieve server logs.");
            }
        }
#endif

        public bool TimeToSend(WebRequestType type, double minTimeInterval)
        {
            if (GameTimeRef.TotalGameTime.TotalSeconds >= (lastRequestTimes[(int)type] + minTimeInterval))
            {
                lastRequestTimes[(int)type] = GameTimeRef.TotalGameTime.TotalSeconds;
                return true;
            }
            return false;
        }


        public async Task<(byte[], Exception)> PerformWebRequest(byte[] data)
        {
            const int maxAttempts = 4;
            TimeSpan timeout = TimeSpan.FromSeconds(45);

            Exception lastEx = null;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using HttpClient client = new() { Timeout = timeout };
                    using HttpContent content = new ByteArrayContent(data);
                    using HttpResponseMessage response = await client.PostAsync(APIAddress, content);
                    response.EnsureSuccessStatusCode();
                    byte[] resp = await response.Content.ReadAsByteArrayAsync();
                    return (resp, null);
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    int backoffMs = (int)(200 * Math.Pow(2, attempt)) + Random.Shared.Next(0, 200);
                    await Task.Delay(backoffMs);
                }
            }
            return (null, lastEx);
        }

    }
}
