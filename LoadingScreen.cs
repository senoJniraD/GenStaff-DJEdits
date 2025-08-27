using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ModelLib;
using TacticalAILib;
using static GSBPGEMG.Game1;

namespace GSBPGEMG
{
    public class LoadingScreen
    {
        public enum LoadingGameScreens
        {
            DownloadingData,
            LoadingScenario,
            CalculatingCourierPaths,
        }
        public LoadingGameScreens LoadingGameScreen;

        private Thread loadingThread = null;

        private Game1 gameRef;
        private SavedGameData savedGameData;

        public LoadingScreen(Game1 game)
        {
            gameRef = game;
        }

        public void Update(SavedGameData requestedSavedGameData)
        {
            if (loadingThread == null)
            {
                savedGameData = requestedSavedGameData;
                LoadingGameScreen = ((gameRef.CurrentSavedGameData.GameOpponentType == SavedGameData.GameOpponentTypes.SteamPlayer) &&
                    (gameRef.CurrentSavedGameData.GameProgressState != SavedGameData.GameProgressStates.NewLocal)) ?
                    LoadingGameScreens.DownloadingData : LoadingGameScreens.LoadingScenario;

                loadingThread = new Thread(LoadThreadFunction) { IsBackground = true };
                loadingThread.Start();
            }
            else
            {
                if (loadingThread?.Join(0) == true)
                {
                    loadingThread = null;
                    gameRef.GameState = GameStates.Gameplay;
                    gameRef.Instance = savedGameData.LoadedGameInstance;
                    gameRef.CurrentSavedGameData = savedGameData;
                    gameRef.Check4EndOfSimulation(gameRef.Instance);
                    if (gameRef.EndGameMessages.Count == 0)
                        gameRef.GameplayScreen = GameplayScreens.Playing;
                    else
                        gameRef.GameplayScreen = GameplayScreens.FinishedScenario;
                    gameRef.Tabs.RefreshForChanges();
                    BigButtonClickInstance.Play();
                    Logging.GameTurnNo = gameRef.Instance.CurrentGameTurn;
                }
            }
        }

        void LoadThreadFunction()
        {
            TimeSpan inGameTargetElapsedTime = gameRef.TargetElapsedTime;
            gameRef.TargetElapsedTime = TimeSpan.FromSeconds(1.0d / 15);

            if (LoadingGameScreen == LoadingGameScreens.DownloadingData)
            {
                if (savedGameData.GameOpponentType == SavedGameData.GameOpponentTypes.SteamPlayer)
                {
                    savedGameData.Downloading = true;
                    //if (savedGameData.GameProgressState == SavedGameData.GameProgressStates.NewInvite)
                    //    SavedGamesCollection.PrioritySavedGamesToCheck.Enqueue((SavedGamesCollection.EntryOrigin.OnlinePlayer, // TODO (noted by MT)
                    //        savedGameData.SavedDataUniqueID, SavedGameData.LoadedStatus.FullGameInstance));
                    //else
                        SavedGamesCollection.PrioritySavedGamesToCheck.Enqueue((SavedGamesCollection.EntryOrigin.Server,
                            savedGameData.SavedDataUniqueID, SavedGameData.LoadedStatus.FullGameInstance));
                }

                while (savedGameData.Downloading)
                {
                    //if (SavedGamesCollection.AvailableSavedGames.TryGetValue(savedGameData.SavedDataUniqueID,
                    //    out SavedGameData downloadedData))
                    //{
                    //    if (downloadedData.LoadedGameStatus == SavedGameData.LoadedStatus.FullGameInstance)
                    //    {
                    //        savedGameData = downloadedData;
                    //        //savedGameData.GameProgressState = SavedGameData.GameProgressStates.NewInvite;
                    //        break;
                    //    }
                    //}

                    Thread.Sleep(250);
                }

                savedGameData.LocalPlayerIndex = savedGameData.FindSteamPlayerArmyIndex(SteamServices.localID);

                LoadingGameScreen = LoadingGameScreens.LoadingScenario;
            }

            if (savedGameData.GameProgressState == SavedGameData.GameProgressStates.NewLocal)
            {
                savedGameData.EditorAssetSet.Load(loadArmies: true, loadMap: true);
                savedGameData.LoadedGameInstance = new(savedGameData.EditorAssetSet, savedGameData.FogOfWarType);
            }
            else
            {
                if ((savedGameData.LoadedGameStatus == SavedGameData.LoadedStatus.GameDetailsOnly) &&
                    (gameRef.UpdatedSavedGameData == null))
                {
                    SavedGamesCollection.PrioritySavedGamesToCheck.Enqueue((SavedGamesCollection.EntryOrigin.File,
                        savedGameData.LocalFileName, SavedGameData.LoadedStatus.FullGameInstance));
                    while (gameRef.UpdatedSavedGameData == null)
                        Thread.Sleep(100);
                }
            }

            if (gameRef.UpdatedSavedGameData != null)
            {
                savedGameData.MergeSavedData(gameRef.UpdatedSavedGameData);
                gameRef.UpdatedSavedGameData = null;
            }

            GameInstance instance = savedGameData.LoadedGameInstance;
            savedGameData.UpdateGameInstance(instance);

            if (Debug.SettingsOverrideSide != null)
                savedGameData.LocalPlayerIndex = (int)Debug.SettingsOverrideSide;

            savedGameData.SetActiveArmy(savedGameData.LocalPlayerIndex);

            if (savedGameData.GameModeType == SavedGameData.GameModeTypes.Simulation)
            {
                LoadingGameScreen = LoadingGameScreens.CalculatingCourierPaths;

                for (int i = 0; i < instance.AllArmies.Count; i++)
                    gameRef.CalculateCorpsValuesForTurn(instance.AllArmies[i]);
            }

            Logging.Write($"General Staff: Black Powder Combat Log for Scenario: {instance.ScenarioName}. {instance.ScenarioDescription}", Logging.MessageType.CombatLog);
            Logging.Write("Actual Date and Time: " + DateTime.Now.ToString(), Logging.MessageType.CombatLog);

            bool saveRequired = false;
            if (((savedGameData.GameOpponentType == SavedGameData.GameOpponentTypes.SteamPlayer) ||
                (savedGameData.GameOpponentType == SavedGameData.GameOpponentTypes.PlayByEmail)) &&
                ((savedGameData.GameProgressState == SavedGameData.GameProgressStates.NewLocal) ||
                (savedGameData.GameProgressState == SavedGameData.GameProgressStates.NewInvite)) &&
                (savedGameData.LocalPlayerArmy != null))
                saveRequired = true;

            if ((savedGameData.GameProgressState == SavedGameData.GameProgressStates.NewLocal) ||
                (savedGameData.GameProgressState == SavedGameData.GameProgressStates.NewInvite))
            {
                savedGameData.GameProgressState = SavedGameData.GameProgressStates.InProgress;
                savedGameData.LocalPlayerArmy.LastUpdateTime = DateTime.UtcNow;
            }

            if (saveRequired)
                savedGameData.SaveToFile(gameRef);

            gameRef.TargetElapsedTime = inGameTargetElapsedTime;
        }

        public void LoadCorpsLists(List<MATEUnitInstance> units, List<CorpsOrder> corps) // TODO (noted by MT) recalc per turn?
        {
            // 1. go through army and find HQs (except for 0)
            // 2. store name
            // 3. find all units that have this unit as HQ
            // 4. keep list of all units
            // 5. add up strength

            for (int i = 1; i < units.Count; i++)
            {
                if (units[i].IsHQ)
                {
                    MATEUnitInstance HQUnit = units[i];
                    int CorpsStrength = 0;
                    CorpsOrder tObj = new();
                    tObj.Name = units[i].Name;
                    tObj.Commander = units[i];
                    tObj.UnitsInCorps.Add(units[i]);

                    for (int j = 1; j < units.Count; j++)
                    {
                        if (units[j].CommandingUnit == units[i]
                            || units[j] == units[j].CommandingUnit
                            && !units[j].IsHQ)
                        {
                            CorpsStrength += units[j].Strength;
                            tObj.UnitsInCorps.Add(units[j]);
                        }
                    }

                    tObj.Strength = CorpsStrength;
                    tObj.OrdersDelay = units[i].OrderDelayTimeFromGHQ; // TODO (noted by MT) move text to tab
                    string tString = units[i].Name + ".  Strength: " + CorpsStrength.ToString("N0") + ". ";
                    tString += "Couriers from " + units[0].Name + " to this HQ will travel " + ((int)units[i].CourierRouteDistanceGHQ).ToString("N0") + " meters taking " +
                        units[i].OrderDelayTimeFromGHQ.ToString() + " minutes at " + gameRef.CourierSpeedKmh.ToString("0.00") + " KmPH.";
                    tString += " Leadership values (" + units[0].Leadership.ToString() + "% + " +
                        units[i].Leadership.ToString() + "%) = a delay of " + gameRef.GetLeadershipCost(units[i]) + " minutes.";
                    string[] mString = gameRef.JustifyTextStrings(VictorianReportsFont, tString, 180);
                    tObj.LineOfType = mString;
                    corps.Add(tObj);
                }
            }
        }

        public void Draw(Game1 game)
        {
            game.ScreenResizer.BeginDraw();

            SpriteBatch spriteBatch = game.spriteLayerTitleScreen;
            spriteBatch.Begin(rasterizerState: game.RasterizerStateDefault);

            Vector2 width;
            string tString;

            Rectangle r = new Rectangle(0, 0, game.WindowWidth, game.WindowHeight);
            spriteBatch.Draw(SplashBackground, r, Color.White);
            spriteBatch.Draw(SplashLogo, new Vector2((game.WindowWidth / 2) - 325, 30), Color.White);

            game.DrawBoxOutline(spriteBatch, new Rectangle(0, 0, game.WindowWidth, game.WindowHeight - 1), Color.Black, 1);

            tString = "";
            switch (LoadingGameScreen)
            {
                case LoadingGameScreens.DownloadingData: tString = "Downloading Data"; break;
                case LoadingGameScreens.LoadingScenario: tString = "Loading Scenario"; break;
                case LoadingGameScreens.CalculatingCourierPaths: tString = "Calculating Courier Paths"; break;
            }
            width = SplashBannerFont.MeasureString(tString);
            spriteBatch.DrawString(SplashBannerFont, tString, new Vector2((game.WindowWidth / 2) - (width.X / 2), 370), Color.Black);
            spriteBatch.DrawString(SplashBannerFont, ".....".Substring(0, 1 + (int)(GameTimeRef.TotalGameTime.TotalSeconds * 3d % 5)),
                new Vector2((game.WindowWidth / 2) + (width.X / 2), 370), Color.Black);

            tString = "Copyright 2025 Riverview Artificial Intelligence, LLC.";
            width = Ephinol12.MeasureString(tString);
            spriteBatch.DrawString(VictorianCopyrightNoticeFont, tString, new Vector2(game.WindowWidth / 2 - width.X / 2 - 26, 700), Color.Black);

            DateTime buildDate = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).LastWriteTime;
            string BuildDate = buildDate.Date.ToString("yyMMdd"); // TODO (noted by MT)

            string version = "Build " + BuildDate;
            spriteBatch.DrawString(VictorianCopyrightNoticeFont, version, new Vector2(1190, 700), Color.Black);

#if DEBUG
            tString = "Development Version";
            width = ModernScenarioName.MeasureString(tString);
            spriteBatch.DrawString(ModernScenarioName, tString, new Vector2((game.WindowWidth / 2) - (width.X / 2), 450), Color.Red);

            tString = "— Before uploading switch to Release configuration to disable debug menus, cheats, etc. —";
            width = ModernScenarioName.MeasureString(tString);
            spriteBatch.DrawString(ModernScenarioName, tString, new Vector2((game.WindowWidth / 2) - (width.X / 2), 495), Color.Red);
#endif

            spriteBatch.End();
            game.ScreenResizer.EndDraw(null);
        }
    }
}
