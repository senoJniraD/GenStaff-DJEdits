using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModelLib;
using static GSBPGEMG.Game1;
using static GSBPGEMG.SavedGameData;
using static GSBPGEMG.Networking_WebRequests;

namespace GSBPGEMG
{
    public static class SavedGamesCollection
    {
        public enum EntryOrigin
        {
            UnsavedGame,
            File,
            Server,
            OnlinePlayer
        }

        public static ConcurrentDictionary<ulong, SavedGameData> AvailableSavedGames { get; private set; } = [];
        public static ConcurrentDictionary<ulong, SavedGameData> AvailableOnlineHostedGames { get; private set; } = [];

        public static ConcurrentQueue<(EntryOrigin origin, object linkToData, LoadedStatus loadedStatus)> PrioritySavedGamesToCheck = [];
        public static ConcurrentQueue<(EntryOrigin origin, object linkToData, LoadedStatus loadedStatus)> BackgroundSavedGamesToCheck = [];

        private static Task retrieveSavedGameTask;

        private static bool existingFilesInitialized;
        private static bool dragAndDropInitialized;

        public static void Update(Game1 game)
        {
            if (!existingFilesInitialized)
            {
                List<string> savedGamesFilenames = Directory.GetFiles(FilesIO.SavedGamesFolder, "*." + FileExtension).ToList();
                savedGamesFilenames = savedGamesFilenames.OrderByDescending(x => File.GetLastWriteTime(x).Ticks).ToList();

                foreach (string savedGameFilename in savedGamesFilenames)
                    BackgroundSavedGamesToCheck.Enqueue((EntryOrigin.File, savedGameFilename, LoadedStatus.GameDetailsOnly));
                RunServerSearch(game);
                existingFilesInitialized = true;
            }

            if (!dragAndDropInitialized)
            {
                try
                {
                    if (GameRef.IsActive && (System.Windows.Forms.Form.ActiveForm?.Handle == GameRef.Window.Handle))
                    {
                        try
                        {
                            System.Windows.Forms.Form.ActiveForm.AllowDrop = true;
                            System.Windows.Forms.Form.ActiveForm.DragEnter += (s, e) =>
                            {
                                e.Effect = System.Windows.Forms.DragDropEffects.Copy;
                            };
                            System.Windows.Forms.Form.ActiveForm.DragDrop += (s, e) =>
                            {
                                string[] filenames = (string[])e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop, false);
                                foreach (string filename in filenames)
                                    PrioritySavedGamesToCheck.Enqueue((EntryOrigin.File, filename, LoadedStatus.FullGameInstance));
                            };
                            dragAndDropInitialized = true;
                        }
                        catch { }
                    }
                }
                catch { }
            }

            if ((PrioritySavedGamesToCheck.Count >= 1) || (BackgroundSavedGamesToCheck.Count >= 1) &&
                (retrieveSavedGameTask?.IsCompleted != false))
            {
                (EntryOrigin origin, object linkToData, LoadedStatus loadedStatus) request = default;
                if (PrioritySavedGamesToCheck.TryDequeue(out request) == false)
                    BackgroundSavedGamesToCheck.TryDequeue(out request);
                if (request != default)
                {
                    retrieveSavedGameTask = Task.Run(() =>
                    { AddOrUpdateCollectionEntry(game, request.origin, request.linkToData, request.loadedStatus); });
                }
            }
        }

        private static void AddOrUpdateCollectionEntry(Game1 game, EntryOrigin entryOrigin, object linkToData, LoadedStatus loadedStatus)
        {
            //try
            //{
            SavedGameData availableSavedGame = null;
            ConcurrentDictionary<ulong, SavedGameData> savedGameDataCollection = null;
            switch (entryOrigin)
            {
                case EntryOrigin.UnsavedGame:
                    availableSavedGame = (SavedGameData)linkToData;
                    savedGameDataCollection = AvailableSavedGames;
                    break;
                case EntryOrigin.File:
                    availableSavedGame = LoadFromFile((string)linkToData, loadedStatus);
                    availableSavedGame.LocalFileName = Path.GetFileName((string)linkToData);
                    savedGameDataCollection = AvailableSavedGames;
                    break;
                case EntryOrigin.Server:
                    availableSavedGame = LoadFromServer((ulong)linkToData, game.CurrentSavedGameData, loadedStatus);
                    savedGameDataCollection = AvailableSavedGames;
                    break;
                case EntryOrigin.OnlinePlayer:
                    availableSavedGame = LoadFromOnlinePlayer((Networking_Steam.NetworkPlayer)linkToData, loadedStatus);
                    savedGameDataCollection = AvailableOnlineHostedGames;
                    break;
            }

            SavedGameData collectionEntry = null;
            if (availableSavedGame != null)
            {
                if (savedGameDataCollection.TryGetValue(availableSavedGame.SavedDataUniqueID, out collectionEntry))
                {
                    if (collectionEntry.LoadedGameStatus == LoadedStatus.GameDetailsOnly &&
                        availableSavedGame.LoadedGameStatus == LoadedStatus.FullGameInstance)
                    {
                        game.UpdatedSavedGameData = availableSavedGame;
                    }
                    else
                    {
                        if (collectionEntry.LoadedGameStatus == LoadedStatus.GameDetailsOnly)
                            if (collectionEntry.TurnsProgress != availableSavedGame.TurnsProgress)
                                collectionEntry.MergeSavedData(availableSavedGame);

                        if (collectionEntry.LoadedGameStatus == LoadedStatus.FullGameInstance)
                        {
                            if (collectionEntry.SavedDataUniqueID == game.CurrentSavedGameData?.SavedDataUniqueID)
                            {
                                if (availableSavedGame.TurnsProgress != game.CurrentSavedGameData?.TurnsProgress)
                                {
                                    if (game.UpdatedSavedGameData == null)
                                        game.UpdatedSavedGameData = availableSavedGame;

                                }
                            }
                            else
                            {
                                if (collectionEntry.TurnsProgress != availableSavedGame.TurnsProgress)
                                {
                                    collectionEntry.MergeSavedData(availableSavedGame);
                                    collectionEntry.SaveToFile(game);
                                }
                            }
                        }
                    }
                }
                else
                {
                    savedGameDataCollection.GetOrAdd(availableSavedGame.SavedDataUniqueID, availableSavedGame);
                    collectionEntry = availableSavedGame;
                }

                if ((entryOrigin is EntryOrigin.Server) && (collectionEntry != null))
                    collectionEntry.Downloading = false;
            }

            if (entryOrigin is EntryOrigin.OnlinePlayer)
                ((Networking_Steam.NetworkPlayer)linkToData).MergeInProgress = false;
            //}
            //catch (Exception e)
            //{
            //    Logging.Write(e, false, message: "SavedGamesCollection: Add/Update " + entryOrigin + " failed");
            //}
        }

        private static void RunServerSearch(Game1 game)
        {
            Task.Run(async () =>
            {
                Thread.Sleep(1000);
                await WebServices.AvailableSavedGames();

                foreach (OnlineGamesSearchResult onlineGamesSearchResult in OnlineGamesSearchResults.Values)
                {
                    if (AvailableSavedGames.TryGetValue((ulong)onlineGamesSearchResult.SavedDataUniqueID, out SavedGameData savedGameData))
                    {
                        if (savedGameData.LoadedGameStatus == LoadedStatus.GameDetailsOnly)
                            savedGameData.ServerTurnsProgress = onlineGamesSearchResult.TurnProgress;
                    }
                    else
                    {
                        BackgroundSavedGamesToCheck.Enqueue((EntryOrigin.Server, (ulong)onlineGamesSearchResult.SavedDataUniqueID,
                            LoadedStatus.GameDetailsOnly));
                    }
                }
            });
        }

        public static void SelectEmailAttachment()
        {
            Logging.Write("SelectEmailAttachment", Logging.MessageType.GeneralInformation);
            Input.MouseClearButtons();
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog()
            {
                Title = "Select Email Attachment",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                DefaultExt = FileExtension,
                Filter = "GSBP Saved Game|*." + FileExtension
            };
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filename = openFileDialog.FileName;
                BackgroundSavedGamesToCheck.Enqueue((EntryOrigin.File, filename, LoadedStatus.FullGameInstance));
            }
        }
    }
}
