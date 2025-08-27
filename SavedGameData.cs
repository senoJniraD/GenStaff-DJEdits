using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using ModelLib;
using TacticalAILib;

namespace GSBPGEMG
{
    [System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
    public class SavedGameData
    {
        public readonly static string FileExtension = "gsbp-save";
        public readonly static int LatestDataFormatVersion = 1;
        public readonly static int MinCompatibleDataFormatVersion = 1;

        public enum GameProgressStates
        {
            NewLocal,
            NewInvite,
            InProgress,
            Completed
        }

        public enum GameOpponentTypes
        {
            AI,
            SteamPlayer,
            PlayByEmail,
            LocalMultiplayer
        }

        public enum GameModeTypes
        {
            Simulation,
            Game
        }

        public enum EmailVerificationTypes
        {
            Steam,
            CustomPassword,
            SteamOrCustomPassword,
            NoVerification
        }

        public enum GameReadyWaitStates
        {
            None,
            ReadyForSubmitTurn,
            ReadyProceedToNextTurn,
            WaitingForLocalPlayer,
            WaitingForOpponent,
            GameCompleted
        }

        public ulong SavedDataUniqueID = (ulong)(DateTime.UtcNow.Ticks + Randomisation.StandardRandom.Next(1000000));

        public int SavedDataFormatVersion; // TODO (noted by MT)
        public int SavedDataMinCompatibleFormatVersion;

        public GameEditorAssetSet EditorAssetSet = new();

        public string TurnsProgress
        {
            get
            {
                string state = "|" + GameTurn + "|";
                if (SavedArmies != null)
                    foreach (SavedGameArmyData savedArmyData in SavedArmies)
                        state += savedArmyData.TurnsTaken + "|";
                return state;
            }
        }

        public GameProgressStates GameProgressState;
        public GameOpponentTypes GameOpponentType;
        public GameModeTypes GameModeType;
        public EmailVerificationTypes EmailVerificationType;
        public MapFogOfWar.FogOfWarTypes FogOfWarType;

        public TimeSpan GameTime;
        public int GameTurn = 1;

        public List<SavedGameArmyData> SavedArmies = [];

        public int ScenarioHostArmyIndex;
        public SavedGameArmyData LocalPlayerArmy { get => ((LocalPlayerIndex >= 0) && (LocalPlayerIndex < SavedArmies.Count)) ? SavedArmies[LocalPlayerIndex] : null; }
        public SavedGameArmyData HostPlayerArmy { get => (ScenarioHostArmyIndex < SavedArmies.Count) ? SavedArmies[ScenarioHostArmyIndex] : null; }
        public DateTime LastUpdateTime
        {
            get
            {
                long highestTicks = 0;
                for (int i = 0; i < SavedArmies.Count; i++)
                    highestTicks = Math.Max(highestTicks, SavedArmies[i].LastUpdateTime.Ticks);
                return new DateTime(highestTicks);
            }
        }

        public int LocalPlayerIndex = -1;
        public bool Downloading;
        public string ServerTurnsProgress;

        public enum LoadedStatus
        {
            None,
            GameDetailsOnly,
            FullGameInstance
        }
        public LoadedStatus LoadedGameStatus { get; private set; }
        public GameInstance LoadedGameInstance = null;

        public string LocalFileName { get; set; }

        public static SavedGameData LoadFromFile(string fileName, LoadedStatus loadedStatus)
        {
            Logging.Write("LoadFromFile > " + fileName, Logging.MessageType.GeneralInformation);
            byte[] data = File.ReadAllBytes(Path.Combine(FilesIO.SavedGamesFolder, fileName));
            return LoadData(data, loadedStatus);
        }

        public static SavedGameData LoadFromServer(ulong SavedDataUniqueID, SavedGameData existingSavedGameData, LoadedStatus loadedStatus)
        {
            Logging.Write("LoadFromServer > " + SavedDataUniqueID, Logging.MessageType.GeneralInformation);

            int? joinUnassignedArmyIndex = null;
            List<SavedGameArmyData> unassignedArmies = existingSavedGameData.GetUnassignedArmies();
            if ((unassignedArmies.Count >= 1) && (loadedStatus == LoadedStatus.FullGameInstance) &&
                (existingSavedGameData.FindLocalPlayerArmyIndex(false) == -1))
            {
                joinUnassignedArmyIndex = unassignedArmies[0].Index;
                bool joinedOpenSavedGame = Game1.WebServices.JoinOpenSavedGame(SavedDataUniqueID,
                    existingSavedGameData, joinUnassignedArmyIndex.Value).Result;

                if (joinedOpenSavedGame == false)
                {
                    Logging.Write("LoadFromServer: Join Open Saved Game Failed", Logging.MessageType.GeneralInformation);
                    return null;
                }
            }

            byte[] data = Game1.WebServices.RetrieveSavedGameData(SavedDataUniqueID, existingSavedGameData).Result;
            if (data != null)
            {
                if (data.Length >= 1)
                {
                    SavedGameData savedGameData = LoadData(data, loadedStatus);
                    savedGameData.ServerTurnsProgress = savedGameData.TurnsProgress;

                    if (joinUnassignedArmyIndex.HasValue)
                    {
                        SavedGameArmyData savedGameArmyData = savedGameData.SavedArmies[joinUnassignedArmyIndex.Value];
                        savedGameArmyData.SteamID = Game1.SteamServices.localID;
                        savedGameArmyData.SteamName = Game1.SteamServices.localName;
                    }

                    savedGameData.FindLocalPlayerArmyIndex(displayErrorMessage: false);
                    if ((savedGameData.LocalPlayerArmy?.LastUpdateTime == DateTime.MinValue) ||
                        (savedGameData.GetUnassignedArmies().Count >= 1))
                        savedGameData.GameProgressState = GameProgressStates.NewInvite;

                    return savedGameData;
                }
                else
                {
                    Logging.Write("LoadFromServer: Already up to date", Logging.MessageType.GeneralInformation);
                    return null;
                }
            }
            else
            {
                Logging.Write("LoadFromServer: Not Found", Logging.MessageType.GeneralInformation);
                return null;
            }
        }

        public static SavedGameData LoadFromOnlinePlayer(Networking_Steam.NetworkPlayer player, LoadedStatus loadedStatus)
        {
            Logging.Write("LoadFromOnlinePlayer > ", Logging.MessageType.GeneralInformation);
            player.SavedGameDownloaded = LoadData(player.SavedGameDownloadedData, loadedStatus);
            player.SavedGameDownloadedData = null;

            int localPlayerArmyNo = player.SavedGameDownloaded.FindSteamPlayerArmyIndex(Game1.SteamServices.localID);
            player.SavedGameDownloaded.FindLocalPlayerArmyIndex(displayErrorMessage: false);
            if (player.SavedGameDownloaded.LocalPlayerArmy?.LastUpdateTime == DateTime.MinValue)
                player.SavedGameDownloaded.GameProgressState = GameProgressStates.NewInvite;

            player.SavedGameDetails = player.SavedGameDownloaded;
            return player.SavedGameDownloaded;
        }

        public static SavedGameData LoadData(byte[] data, LoadedStatus loadedStatus)
        {
            using MemoryStream compressedStream = new(data);
            using MemoryStream uncompressedStream = new();
            using (GZipStream gzip = new(compressedStream, CompressionMode.Decompress))
                gzip.CopyTo(uncompressedStream);
            byte[] uncompressedData = uncompressedStream.ToArray();

            SerializerData deserializer = new();
            deserializer.SetReadBuffer(uncompressedData);

            SavedGameData savedGameData = new();

            byte[] gameDetailsData = deserializer.ReadByteArray();
            bool hashSuccess = deserializer.ReadMD5Hash(gameDetailsData);
            if (hashSuccess)
            {
                savedGameData.DeserializeGameDetails(gameDetailsData);
                savedGameData.EditorAssetSet.Load(loadArmies: loadedStatus == LoadedStatus.FullGameInstance,
                    loadMap: loadedStatus == LoadedStatus.FullGameInstance);
            }
            else
            {
                throw new Exception($"Load Game Details MD5 Hash Failed: {savedGameData.SavedDataUniqueID}");
            }

            if (loadedStatus == LoadedStatus.FullGameInstance)
            {
                savedGameData.EditorAssetSet.Load(loadArmies: true, loadMap: true);
                savedGameData.LoadedGameInstance = new(savedGameData.EditorAssetSet, savedGameData.FogOfWarType);

                byte[] gameInstanceData = deserializer.ReadByteArray();
                hashSuccess = deserializer.ReadMD5Hash(gameInstanceData);
                if (hashSuccess)
                    savedGameData.DeserializeGameInstance(savedGameData.LoadedGameInstance, gameInstanceData);
                else
                    throw new Exception($"Load Game Instance MD5 Hash Failed: {savedGameData.SavedDataUniqueID}");
            }

            savedGameData.LoadedGameStatus = loadedStatus;

            return savedGameData;
        }

        public bool SaveToFile(Game1 game, string fileName = null) // TODO (noted by MT) clone/queue/async
        {
            //try
            //{
            if (fileName == null)
                fileName = Path.Combine(FilesIO.SavedGamesFolder, $"SaveID-{SavedDataUniqueID} Scenario-{LoadedGameInstance.ScenarioName}.{FileExtension}");
            LocalFileName = fileName;
            Logging.Write("SaveToFile > " + fileName, Logging.MessageType.GeneralInformation);

            byte[] bytes = SaveAsByteArray(LoadedGameInstance, SavedGameData.LoadedStatus.FullGameInstance);

            //#if DEBUG
            // Save zip file (for debug only and not for release)
            using (FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read))
                fileStream.Write(bytes);
            //#endif

            // Obfuscate save data to help prevent cheating by xml editing
            // ObfuscationWriter(memoryStream); // TODO (noted by MT)
            //}
            //catch (Exception e)
            //{
            //    if (SavedDataUniqueID == game.CurrentSavedGameData?.SavedDataUniqueID)
            //    {
            //        string exceptionMessage = Logging.Write(e, true, "Unable To Save File",
            //            message: "Unable to write save file. Please try again.\n" +
            //            "(Also if the program doesn't have write permissions for this folder, " +
            //            "it may be necessary to select a different folder).");

            //        try
            //        {
            //            Logging.SetCurrentMessageType(Logging.MessageType.Exception);
            //            using LogsStreamWriter writer = new LogsStreamWriter(Path.Combine(Logging.LogsFolderForTurn, "SaveGameErrors.txt"));
            //            writer.WriteLine(exceptionMessage);
            //            writer.WriteLine();
            //        }
            //        catch (Exception ex)
            //        {
            //            Logging.Write(ex, true, "Unable To Write Save Error Log",
            //                "Unable to write both save file & save error log to disk.");
            //        }
            //    }
            //}

            game.HeaderMenus.SaveRequired = false;
            return true;
        }

        public async void SaveToServer(GameInstance instance)
        {
            Logging.Write("SaveToServer > " + SavedDataUniqueID, Logging.MessageType.GeneralInformation);
            bool success = await Game1.WebServices.SubmitSavedGameData(instance, this);
            if (!success)
                Logging.Write("SaveToServer: Blocked (resync new turns and resubmit)", Logging.MessageType.GeneralInformation);
        }

        public byte[] SaveAsByteArray(GameInstance instance, LoadedStatus loadedStatus)
        {
            SerializerData serializer = new();

            byte[] gameDetailsBytes = SerializeGameDetails();
            serializer.WriteByteArray(gameDetailsBytes);
            serializer.WriteMD5Hash(gameDetailsBytes);

            if (loadedStatus == LoadedStatus.FullGameInstance)
            {
                byte[] gameInstanceBytes = SerializeGameInstance(instance);
                serializer.WriteByteArray(gameInstanceBytes);
                serializer.WriteMD5Hash(gameInstanceBytes);
            }

            using MemoryStream compressedStream = new();
            using (GZipStream gzip = new(compressedStream, CompressionLevel.SmallestSize, true))
                gzip.Write(serializer.GetWriteBuffer().ToArray());
            return compressedStream.ToArray();
        }

        public void UpdateGameInstance(GameInstance instance)
        {
            Logging.Write("LoadSavedGameData", Logging.MessageType.GeneralInformation);

            LoadedGameInstance = instance;

            if (GameProgressState == GameProgressStates.NewLocal)
            {
                instance.CurrentEvents.AddEvent(new Event_GameTurnChanged(1));
                instance.CurrentEvents.AddEvent(new Event_GameTimeChanged(instance.ScenarioStartTime));
                for (int i = 0; i < instance.AllArmies.Count; i++)
                    instance.CurrentEvents.AddEvent(new Event_InitialCourierReport(instance.AllArmies[i]));

                GameTime = instance.CurrentGameTime;
                GameTurn = instance.CurrentGameTurn;
            }
            else
            {
                for (int i = 0; i < SavedArmies.Count; i++)
                {
                    instance.AllArmies[i].TurnsTaken = SavedArmies[i].TurnsTaken;
                    SavedArmies[i].SetPlannedEvents(instance);
                }
            }

            instance.SetAllSnapshotsByTurn(cloneInProgress: false);
        }

        public void UpdateSavedGameData(Game1 game, GameInstance instance)
        {
            Logging.Write("UpdateLoadedSavedGameData", Logging.MessageType.GeneralInformation);

            LoadedGameInstance = instance;

            if (game.GameplayScreen == Game1.GameplayScreens.FinishedScenario)
                GameProgressState = GameProgressStates.Completed;
            GameTime = LoadedGameInstance.CurrentGameTime;
            GameTurn = LoadedGameInstance.CurrentGameTurn;

            if (LoadedGameInstance.CurrentGameTurn >= LocalPlayerArmy.TurnsTaken)
            {
                LocalPlayerArmy.LastUpdateTime = DateTime.UtcNow;
                LocalPlayerArmy.TurnsTaken = LoadedGameInstance.AllArmies[LocalPlayerArmy.Index].TurnsTaken;
            }

            if ((GameOpponentType == GameOpponentTypes.PlayByEmail) &&
                (EmailVerificationType == EmailVerificationTypes.SteamOrCustomPassword) &&
                (LocalPlayerArmy.SteamID == 0))
            {
                LocalPlayerArmy.SteamName = Game1.SteamServices.localName;
                LocalPlayerArmy.SteamID = Game1.SteamServices.localID;
            }

            for (int i = 0; i < SavedArmies.Count; i++)
            {
                SavedArmies[i].TurnsTaken = LoadedGameInstance.AllArmies[i].TurnsTaken;
                SavedArmies[i].SetPlannedEvents(instance);
            }
        }

        public void MergeSavedData(SavedGameData dataToMerge)
        {
            if (SavedDataUniqueID != dataToMerge?.SavedDataUniqueID)
                return;

            Logging.Write("MergeSavedData " + SavedDataUniqueID + " " + TurnsProgress + " > " + dataToMerge.TurnsProgress,
                Logging.MessageType.GeneralInformation);

            bool initialLoad = ((LoadedGameStatus == LoadedStatus.GameDetailsOnly) && (dataToMerge.LoadedGameStatus == LoadedStatus.FullGameInstance));
            if (initialLoad)
            {
                LoadedGameStatus = LoadedStatus.FullGameInstance;
                LoadedGameInstance = dataToMerge.LoadedGameInstance;
                EditorAssetSet = dataToMerge.EditorAssetSet;
                LocalPlayerIndex = dataToMerge.FindLocalPlayerArmyIndex(displayErrorMessage: false);
            }

            ServerTurnsProgress = dataToMerge.ServerTurnsProgress;

            if (!initialLoad && (TurnsProgress == dataToMerge.TurnsProgress) && (GameProgressState == GameProgressStates.InProgress))
                return;

            for (int i = 0; i < SavedArmies.Count; i++)
            {
                if (initialLoad ||
                    (dataToMerge.SavedArmies[i].TurnsTaken > SavedArmies[i].TurnsTaken) ||
                    (dataToMerge.GameTurn > GameTurn))
                {
                    SavedGameArmyData savedArmy = SavedArmies[i];
                    SavedGameArmyData latestSavedArmy = dataToMerge.SavedArmies[i];

                    if ((string.IsNullOrEmpty(savedArmy.SteamName) && (string.IsNullOrEmpty(latestSavedArmy.SteamName) == false)) ||
                        (GameProgressState == GameProgressStates.NewInvite))
                        savedArmy.SteamName = latestSavedArmy.SteamName;
                    if (savedArmy.SteamID == 0 && latestSavedArmy.SteamID >= 1)
                        savedArmy.SteamID = latestSavedArmy.SteamID;
                    if (string.IsNullOrEmpty(savedArmy.Email) && (string.IsNullOrEmpty(latestSavedArmy.Email) == false))
                        savedArmy.Email = latestSavedArmy.Email;
                    if (string.IsNullOrEmpty(savedArmy.PasswordHash) && (string.IsNullOrEmpty(latestSavedArmy.PasswordHash) == false))
                        savedArmy.PasswordHash = latestSavedArmy.PasswordHash;

                    if (dataToMerge.LoadedGameStatus == LoadedStatus.FullGameInstance)
                        savedArmy.MergePlannedEvents(latestSavedArmy);

                    savedArmy.LastUpdateTime = latestSavedArmy.LastUpdateTime;
                    savedArmy.TurnsTaken = latestSavedArmy.TurnsTaken;
                }
            }

            if (dataToMerge.GameTurn > GameTurn)
            {
                GameTime = dataToMerge.GameTime;
                GameTurn = dataToMerge.GameTurn;

                LoadedGameInstance = dataToMerge.LoadedGameInstance;
                if (dataToMerge.LoadedGameStatus == LoadedStatus.FullGameInstance)
                    SetActiveArmy(LocalPlayerIndex);

                if (dataToMerge.GameProgressState == GameProgressStates.Completed)
                    GameProgressState = GameProgressStates.Completed;
            }
        }


        public bool DownloadRequired(string remoteTurnsProgress)
        {
            // If we don't have a full instance locally, always fetch a full snapshot on rejoin
            if (LoadedGameStatus != LoadedStatus.FullGameInstance)
                return true;

            if (remoteTurnsProgress == null)
                return false;

            if ((GameProgressState == GameProgressStates.NewLocal) ||
                (GameProgressState == GameProgressStates.NewInvite))
                return true;

            string[] localTurns = TurnsProgress.Split('|')[1..^1];
            string[] remoteTurns = remoteTurnsProgress.Split('|')[1..^1];
            for (int i = 0; i < localTurns.Length; i++)
                if (Convert.ToInt32(remoteTurns[i]) > Convert.ToInt32(localTurns[i]))
                    return true;

            return false;
        }


        public bool UploadRequired(string remoteTurnsProgress)
        {
            if (GameOpponentType != GameOpponentTypes.SteamPlayer)
                return false;

            if (remoteTurnsProgress == null)
                return true;

            bool processAllTurnsAvailable = true;
            string[] localTurns = TurnsProgress.Split('|')[1..^1];
            for (int i = 1; i < localTurns.Length; i++)
            {
                if (localTurns[i] != localTurns[0])
                {
                    processAllTurnsAvailable = false;
                    break;
                }
            }
            if (processAllTurnsAvailable)
                return false;

            bool uploadPending = false;
            string[] remoteTurns = remoteTurnsProgress.Split('|')[1..^1];
            for (int i = 0; i < localTurns.Length; i++)
            {
                if (Convert.ToInt32(localTurns[i]) > Convert.ToInt32(remoteTurns[i]))
                    uploadPending = true;
                if (Convert.ToInt32(localTurns[i]) < Convert.ToInt32(remoteTurns[i]))
                    return false;
            }
            return uploadPending;
        }

        public int FindLocalPlayerArmyIndex(bool displayErrorMessage)
        {
            if (GameOpponentType == GameOpponentTypes.AI)
                return LocalPlayerIndex = ScenarioHostArmyIndex;

            if ((GameOpponentType == GameOpponentTypes.SteamPlayer) ||
                ((GameOpponentType == GameOpponentTypes.PlayByEmail) &&
                ((EmailVerificationType == EmailVerificationTypes.Steam) || (EmailVerificationType == EmailVerificationTypes.SteamOrCustomPassword))))
            {
                int steamPlayerArmyNo = FindSteamPlayerArmyIndex(Game1.SteamServices.localID);
                if (steamPlayerArmyNo >= 0)
                    return LocalPlayerIndex = steamPlayerArmyNo;
            }

            if ((GameOpponentType == GameOpponentTypes.PlayByEmail) &&
                ((EmailVerificationType == EmailVerificationTypes.CustomPassword) ||
                (EmailVerificationType == EmailVerificationTypes.SteamOrCustomPassword)))
            {
                string hash = GetLocalPasswordHash();
                for (int i = 0; i < SavedArmies.Count; i++)
                    if (SavedArmies[i].PasswordHash == hash)
                        return LocalPlayerIndex = i;
            }

            if ((GameOpponentType == GameOpponentTypes.LocalMultiplayer) ||
                ((GameOpponentType == GameOpponentTypes.PlayByEmail) && (EmailVerificationType == EmailVerificationTypes.NoVerification)))
            {
                SavedGameArmyData lowestTurnsTakenPlayer = null;
                for (int i = 0; i < SavedArmies.Count; i++)
                {
                    if ((lowestTurnsTakenPlayer == null) || (SavedArmies[i].TurnsTaken < lowestTurnsTakenPlayer.TurnsTaken))
                        lowestTurnsTakenPlayer = SavedArmies[i];
                }
                return LocalPlayerIndex = lowestTurnsTakenPlayer.Index;
            }

            if (displayErrorMessage)
                System.Windows.Forms.MessageBox.Show("Loading failed as the players' sides could not be determined.", "Loading Failed",
                    System.Windows.Forms.MessageBoxButtons.OK);
            return LocalPlayerIndex = -1;
        }

        public int FindSteamPlayerArmyIndex(ulong steamID)
        {
            for (int i = 0; i < SavedArmies.Count; i++)
                if (SavedArmies[i]?.SteamID == steamID)
                    return i;
            return -1;
        }

        public GameReadyWaitStates GetReadyWaitState(ulong steamID)
        {
            int steamPlayerArmyNo = FindSteamPlayerArmyIndex(steamID);
            if (steamPlayerArmyNo >= 0)
                return GetReadyWaitState(steamPlayerArmyNo);
            else
                return GameReadyWaitStates.None;
        }

        public GameReadyWaitStates GetReadyWaitState(int playerArmyIndex)
        {
            if (GameTurn < Convert.ToInt32(ServerTurnsProgress.Split('|')[1]))
                return GameReadyWaitStates.ReadyProceedToNextTurn;

            if (GameProgressState == GameProgressStates.Completed)
                return GameReadyWaitStates.GameCompleted;

            int[] playerTurns = [.. ServerTurnsProgress.Split('|')[2..^1].Select(x => Convert.ToInt32(x))];
            int opponentLowestTurn = int.MaxValue;
            int opponentHighestTurn = int.MinValue;
            for (int i = 0; i < playerTurns.Length; i++)
            {
                if (i == playerArmyIndex)
                    continue;

                int opponentTurn = playerTurns[i];
                if (opponentTurn < opponentLowestTurn)
                    opponentLowestTurn = playerTurns[i];
                if (opponentTurn > opponentHighestTurn)
                    opponentHighestTurn = playerTurns[i];
            }

            if (playerArmyIndex > opponentHighestTurn)
                return GameReadyWaitStates.WaitingForOpponent;
            if (playerArmyIndex < opponentLowestTurn)
                return GameReadyWaitStates.WaitingForLocalPlayer;

            return GameReadyWaitStates.ReadyForSubmitTurn;
        }

        public string GetLocalPasswordHash()
        {
            List<SavedGameArmyData> UnassignedArmies = GetUnassignedArmies();
            if (UnassignedArmies.Count >= 1)
            {
                if (System.Windows.Forms.MessageBox.Show("Not all armies have an assigned player yet.\n\n" +
                    "Have you already selected a side and set a custom password?", "Already Selected A Side?",
                    System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                {
                    SetLocalVerificationMethods(UnassignedArmies[0]);
                    return UnassignedArmies[0].PasswordHash;
                }
            }

            string password = KeyboardInput.Show("Password Required",
                "Enter the password you set for opening this save file:", "", true).Result?.ToString();
            if (password?.Length >= 1)
                return
                    Convert.ToBase64String(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(
                        string.Concat(password.Reverse()).ToString().Insert((password[0] % password.Length), (password.Length ^ 2).ToString()))));
            return null;
        }

        public bool SetLocalVerificationMethods(SavedGameArmyData savedArmyData)
        {
            if ((EmailVerificationType == EmailVerificationTypes.Steam) ||
                (EmailVerificationType == EmailVerificationTypes.SteamOrCustomPassword))
            {
                savedArmyData.SteamName = Game1.SteamServices.localName;
                savedArmyData.SteamID = Game1.SteamServices.localID;
                if (EmailVerificationType == EmailVerificationTypes.Steam)
                    return true;
            }

            if ((EmailVerificationType == EmailVerificationTypes.CustomPassword) ||
                (EmailVerificationType == EmailVerificationTypes.SteamOrCustomPassword))
            {
                string password1 = KeyboardInput.Show("Custom Password",
                    "Enter a custom password for playing your turns: ", "", true).Result?.ToString();
                if ((password1?.Length >= 1) == false)
                    return false;
                string password2 = KeyboardInput.Show("Confirm Password",
                    "Confirm custom password: ", "", true).Result?.ToString();
                if ((password2?.Length >= 1) == false)
                    return false;
                if (password1 == password2)
                {
                    savedArmyData.PasswordHash =
                        Convert.ToBase64String(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(
                            string.Concat(password1.Reverse()).ToString().Insert((password1[0] % password1.Length), (password1.Length ^ 2).ToString()))));
                    return true;
                }
                else
                {
                    MessageBox.Show("Different Passwords Entered", "Different passwords were entered. Please try again.", ["OK"]);
                    return false;
                }
            }

            if ((EmailVerificationType == EmailVerificationTypes.NoVerification) &&
                (savedArmyData.Index == ScenarioHostArmyIndex))
                if (System.Windows.Forms.MessageBox.Show("You have selected not to use any verification method. " +
                    "This means anyone with the file can play your turns, including your opponent.\n\n" +
                    "Do you wish to proceed?", "No Verification Method!",
                    System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    return true;

            return false;
        }

        public List<SavedGameArmyData> GetUnassignedArmies()
        {
            List<SavedGameArmyData> UnassignedArmies = [];
            for (int i = 0; i < SavedArmies.Count; i++)
                if (SavedArmies[i].AssignedToAPlayer == false)
                    UnassignedArmies.Add(SavedArmies[i]);
            return UnassignedArmies;
        }

        public void SetActiveArmy(int armyIndex)
        {
            LoadedGameInstance.ActiveArmy = LoadedGameInstance.AllArmies[armyIndex];
            LoadedGameInstance.OpponentArmy = LoadedGameInstance.ActiveArmy.EnemyArmyData;

            if (GameProgressState == GameProgressStates.InProgress)
                UpdateGameInstance(LoadedGameInstance);

            LoadedGameInstance.SetAllSnapshotsByTurn(cloneInProgress: false);
        }

        public byte[] SerializeGameDetails()
        {
            SerializerData serializer = new();

            serializer.WriteUInt64(SavedDataUniqueID);

            serializer.WriteUInt16(LatestDataFormatVersion);
            serializer.WriteUInt16(MinCompatibleDataFormatVersion);

            EditorAssetSet.SerializeDetails(serializer);

            serializer.WriteByte((byte)GameProgressState);
            serializer.WriteByte((byte)GameOpponentType);
            serializer.WriteByte((byte)GameModeType);
            serializer.WriteByte((byte)EmailVerificationType);
            serializer.WriteByte((byte)FogOfWarType);
            serializer.WriteByte(ScenarioHostArmyIndex);

            serializer.WriteUInt16(GameTurn);
            serializer.WriteTimeSpan(GameTime);

            serializer.WriteByte((byte)SavedArmies.Count);
            for (int i = 0; i < SavedArmies.Count; i++)
                SavedArmies[i].SerializePlayerDetails(serializer);

            return serializer.GetWriteBuffer().ToArray();
        }

        public void DeserializeGameDetails(byte[] data)
        {
            SerializerData deserializer = new();
            deserializer.SetReadBuffer(data);

            SavedDataUniqueID = deserializer.ReadUInt64();
            SavedDataFormatVersion = deserializer.ReadUInt16();
            SavedDataMinCompatibleFormatVersion = deserializer.ReadUInt16();

            EditorAssetSet = new(deserializer);

            GameProgressState = (GameProgressStates)deserializer.ReadByte();
            GameOpponentType = (GameOpponentTypes)deserializer.ReadByte();
            GameModeType = (GameModeTypes)deserializer.ReadByte();
            EmailVerificationType = (EmailVerificationTypes)deserializer.ReadByte();
            FogOfWarType = (MapFogOfWar.FogOfWarTypes)deserializer.ReadByte();
            ScenarioHostArmyIndex = deserializer.ReadByte();

            GameTurn = deserializer.ReadUInt16();
            GameTime = deserializer.ReadTimeSpan();

            int savedArmiesCount = deserializer.ReadByte();
            SavedArmies = new List<SavedGameArmyData>(savedArmiesCount);
            for (int i = 0; i < savedArmiesCount; i++)
            {
                SavedGameArmyData savedArmyData = new();
                savedArmyData.DeserializePlayerDetails(deserializer);
                SavedArmies.Add(savedArmyData);
            }
        }

        public byte[] SerializeGameInstance(GameInstance gameInstance)
        {
            SerializerEvent serializer = new SerializerEvent(gameInstance);

            serializer.WriteEventsList(gameInstance.CurrentEvents, includePlannedEvents: false);
            for (int i = 0; i < SavedArmies.Count; i++)
            {
                SavedArmies[i].SetPlannedEvents(gameInstance);
                SavedArmies[i].SerializePlannedEvents(serializer);
            }
            return serializer.GetWriteBuffer().ToArray();
        }

        public GameInstance DeserializeGameInstance(GameInstance gameInstance, byte[] data)
        {
            SerializerEvent deserializer = new(gameInstance);
            deserializer.SetReadBuffer(data);
            deserializer.ReadEventsList();
            gameInstance.SetAllSnapshotsByTurn(true);
            for (int i = 0; i < SavedArmies.Count; i++)
                SavedArmies[i].DeserializePlannedEvents(deserializer);

            return gameInstance;
        }

        public override string ToString() => $"{SavedDataUniqueID}: Turn Progress {TurnsProgress} {GameOpponentType} {LoadedGameStatus} {GameProgressState}";
    }
}
