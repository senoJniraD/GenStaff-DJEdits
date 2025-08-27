using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ModelLib;
using TacticalAILib;
using static GSBPGEMG.Networking_Steam;

namespace GSBPGEMG
{
    public class ProcessTurn
    {
        public bool ProcessingAllArmiesTurns;
        public int TurnNoBeforeChanges;
        public bool TurnChangesOccurred;

        public bool unitSpeedKeyErrorLatch; // TODO (noted by MT)

        private TurnProcessor turnProcessor;
        private GameInstance turnProcessorGameInstance;

        private Thread processingThread = null;

        public bool DisplayCalculatingTurn;
        public bool DisplayCalculatingCourierRoutes;
        public bool DisplayCalculatingAI;
        public int ProgressPercentage;
        public TimeSpan ProgressTime;

        private Game1 gameRef;
        private SavedGameData savedGameData;

        public ProcessTurn(Game1 game)
        {
            gameRef = game;
        }

        public void Update(Game1 game, GameInstance instance, SavedGameData currentSavedGameData, Tabs tabs)
        {
            TurnChangesOccurred = false;

            // Set gameplay screen
            if ((game.GameplayScreen != Game1.GameplayScreens.EndingTurn) && (game.GameplayScreen != Game1.GameplayScreens.FinishedScenario))
                if (!LocalTurnTaken(instance))
                    game.GameplayScreen = Game1.GameplayScreens.Playing;
                else
                    game.GameplayScreen = Game1.GameplayScreens.WaitingForOpponent;

            // Force an immediate server fetch if we don't have a full instance or we appear behind
            if ((currentSavedGameData.GameOpponentType == SavedGameData.GameOpponentTypes.SteamPlayer) &&
                (currentSavedGameData.LoadedGameStatus != SavedGameData.LoadedStatus.FullGameInstance ||
                 currentSavedGameData.DownloadRequired(currentSavedGameData.ServerTurnsProgress)))
            {
                SavedGamesCollection.PrioritySavedGamesToCheck.Enqueue(
                    (SavedGamesCollection.EntryOrigin.Server,
                     currentSavedGameData.SavedDataUniqueID,
                     SavedGameData.LoadedStatus.FullGameInstance));
            }


            // Download or upload required
            if ((currentSavedGameData.GameOpponentType == SavedGameData.GameOpponentTypes.SteamPlayer) &&
                (game.GameplayScreen != Game1.GameplayScreens.EndingTurn) &&
                (game.UpdatedSavedGameData == null))
            {
                int resyncRate = 60;
                int resyncMinRate = 2;
                foreach (NetworkPlayer player in Game1.SteamServices.allNetworkPlayers.Values)
                {
                    if (player.IsInSameOnlineGame && player.SavedGameDetails != null)
                        if (currentSavedGameData.DownloadRequired(player.SavedGameDetails.TurnsProgress))
                            resyncRate = resyncMinRate;
                }
                if (currentSavedGameData.DownloadRequired(currentSavedGameData.ServerTurnsProgress))
                    resyncRate = resyncMinRate;

                if (Game1.WebServices.TimeToSend(Networking_WebRequests.WebRequestType.RetrieveSavedGameData, resyncRate))
                    SavedGamesCollection.PrioritySavedGamesToCheck.Enqueue((SavedGamesCollection.EntryOrigin.Server,
                        currentSavedGameData.SavedDataUniqueID, SavedGameData.LoadedStatus.FullGameInstance));

                if (currentSavedGameData.UploadRequired(currentSavedGameData.ServerTurnsProgress))
                {
                    if (Game1.WebServices.TimeToSend(Networking_WebRequests.WebRequestType.SubmitSavedGameData, 5))
                    {
                        GameInstance clonedInstance = instance.Clone();
                        currentSavedGameData.SaveToServer(clonedInstance);
                    }
                }
            }

            // Merge incoming turn(s) from others
            if ((game.UpdatedSavedGameData != null) &&
                ((game.GameplayScreen == Game1.GameplayScreens.WaitingForOpponent) ||
                ((tabs.TabSelected == TabNames.EndTurn) && (game.GameplayScreen != Game1.GameplayScreens.EndingTurn))))
            {
                TurnNoBeforeChanges = instance.CurrentGameTurn;
                currentSavedGameData.MergeSavedData(game.UpdatedSavedGameData);
                game.UpdatedSavedGameData = null;
                tabs.RefreshForChanges();

                if (currentSavedGameData.GameTurn > TurnNoBeforeChanges)
                {
                    game.Instance = instance = currentSavedGameData.LoadedGameInstance;
                    game.CalculateCorpsValuesForTurn(instance.ActiveArmy);
                    TurnChangesOccurred = true;
                }
                else
                {
                    for (int i = 0; i < instance.AllArmies.Count; i++)
                    {
                        ArmyInstance army = instance.AllArmies[i];
                        if (currentSavedGameData.SavedArmies[i].TurnsTaken > army.TurnsTaken)
                        {
                            currentSavedGameData.UpdateGameInstance(instance);
                            TurnChangesOccurred = true;
                        }
                    }
                }
            }

            // Process own turn taken and/or proceed to next turn (if all players are ready)
            if (game.GameplayScreen == Game1.GameplayScreens.EndingTurn)
            {
                if (processingThread == null)
                {
                    TurnNoBeforeChanges = instance.CurrentGameTurn;
                    game.LoggingWriteUnitCommands();
                    game.ClearAllUnitsPulsing(instance);

                    savedGameData = currentSavedGameData;
                    turnProcessorGameInstance = instance.Clone();
                    processingThread = new Thread(EndTurnThreadFunction) { IsBackground = true };
                    processingThread.Start();
                }
                else
                {
                    if (processingThread?.Join(0) == true)
                    {
                        processingThread = null;
                        instance = game.Instance = turnProcessorGameInstance;
                        instance.SetAllSnapshotsByTurn(cloneInProgress: false);
                        instance.SetAllDisplayedValuesToEvent(instance.CurrentEvents[^1], applyVisibilityStatesForArmy: instance.ActiveArmy);
                        turnProcessorGameInstance = null;
                        TurnChangesOccurred = true;
                    }
                    else
                    {
#if DEBUG
                        if (turnProcessor != null)
                            turnProcessor.DebugAnimateEndTurn = Debug.SettingsAnimateEndTurn == true;
                        ProgressTime = turnProcessor?.DebugProgressTime ?? instance.CurrentGameTime;
                        if (turnProcessor?.DebugSnapshotsUpdated == true)
                        {
                            for (int i = 0; i < instance.AllArmyUnits.Count; i++)
                                instance.AllArmyUnits[i].SnapshotDisplayed = new(turnProcessorGameInstance.AllArmyUnits[i].SnapshotDisplayed, instance.AllArmyUnits[i]);
                            for (int i = 0; i < instance.AllPlaces.Count; i++)
                                instance.AllPlaces[i].SnapshotDisplayed = new(turnProcessorGameInstance.AllPlaces[i].SnapshotDisplayed, instance.AllPlaces[i]);
                            turnProcessor.DebugSnapshotsUpdated = false;
                        }
#endif
                    }
                }
            }

            // After changes setup next states/screens/tabs
            if (TurnChangesOccurred == true)
            {
                if (AllArmiesEndTurnAvailable(instance, currentSavedGameData))
                {
                    game.GameplayScreen = Game1.GameplayScreens.EndingTurn;
                    ProcessingAllArmiesTurns = true;
                    instance.SetAllSnapshotsByTurn(cloneInProgress: false);
                    tabs.ChangeDisplayedGameEvent(instance.CurrentEvents[^1]);
                }
                else
                {
                    ProcessingAllArmiesTurns = false;
                    bool playSound = false;

                    TabNames? changeTab = null;
                    if (instance.CurrentGameTurn > TurnNoBeforeChanges)
                    {
                        instance.SetAllSnapshotsByTurn(cloneInProgress: false);

                        game.Check4EndOfSimulation(instance);
                        if (game.GameplayScreen == Game1.GameplayScreens.FinishedScenario)
                        {
                            changeTab = TabNames.Scenario;
                        }
                        else
                        {
                            game.GameplayScreen = Game1.GameplayScreens.Playing;
                            changeTab = TabNames.Reports;
                        }
                        playSound = true;
                    }
                    else
                    {
                        if (LocalTurnTaken(instance) && (game.GameplayScreen != Game1.GameplayScreens.WaitingForOpponent))
                        {
                            game.GameplayScreen = Game1.GameplayScreens.WaitingForOpponent;
                            playSound = true;
                        }
                    }

                    if (playSound)
                        Game1.BigButtonClickInstance.Play();

                    currentSavedGameData.UpdateSavedGameData(game, instance);

                    if ((currentSavedGameData.GameOpponentType == SavedGameData.GameOpponentTypes.SteamPlayer) ||
                        (currentSavedGameData.GameOpponentType == SavedGameData.GameOpponentTypes.PlayByEmail))
                        currentSavedGameData.SaveToFile(game);
                    else
                        game.HeaderMenus.SaveRequired = true;

                    if (currentSavedGameData.GameOpponentType == SavedGameData.GameOpponentTypes.LocalMultiplayer)
                    {
                        int armyIndex = instance.OpponentArmy.Index;
                        currentSavedGameData.SetActiveArmy(armyIndex);
                        currentSavedGameData.LocalPlayerIndex = armyIndex;
                    }

                    tabs.ChangeDisplayedGameEvent(instance.CurrentEvents[^1]);
                    if (changeTab != null)
                    {
                        tabs.SetTabsVisible(gameRef);
                        tabs.ChangeTab((TabNames)changeTab);
                    }
                    tabs.RefreshForChanges();

                    game.Instance = instance;
                    Logging.GameTurnNo = instance.CurrentGameTurn;
                }
            }
        }

        public void EndTurnThreadFunction()
        {
            TimeSpan inGameTargetElapsedTime = gameRef.TargetElapsedTime;
            gameRef.TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 30);

            GameInstance instance = turnProcessorGameInstance;

            instance.ActiveArmy.TurnsTaken = instance.CurrentGameTurn;

            if (ProcessingAllArmiesTurns)
            {
                DisplayCalculatingTurn = true;
                for (int i = 0; i < instance.AllArmies.Count; i++)
                    if (instance.AllArmies[i] == instance.OpponentArmy)
                        gameRef.CalculateCorpsValuesForTurn(instance.AllArmies[i]);

                if (savedGameData.GameOpponentType == SavedGameData.GameOpponentTypes.AI)
                    DisplayCalculatingAI = true;
                DisplayCalculatingCourierRoutes = true;
                ProgressPercentage = 0;

                if (savedGameData.GameOpponentType == SavedGameData.GameOpponentTypes.AI)
                {
                    savedGameData.SetActiveArmy(instance.OpponentArmy.Index);
                    instance.ActiveArmy.StrategicAI ??= new AI_Strategy();
                    instance.ActiveArmy.StrategicAI.SetUpStrategicAI(instance.ActiveArmy);
                    savedGameData.SetActiveArmy(savedGameData.LocalPlayerIndex);
                    DisplayCalculatingAI = false;
                }

                turnProcessor = new(turnProcessorGameInstance, ((int)(savedGameData.SavedDataUniqueID % 1000000) * savedGameData.GameTurn) % int.MaxValue);
#if DEBUG
                turnProcessor.DebugAnimateEndTurn = Debug.SettingsAnimateEndTurn == true;
#endif
                turnProcessor.Process();

                gameRef.LoggingWriteUnitCommands();
                DisplayCalculatingTurn = false;

                if (instance.CurrentGameTurn < instance.ScenarioNumTurns)
                    instance.CurrentEvents.AddEvent(new Event_GameTurnChanged(instance.CurrentGameTurn + 1));

                instance.AllArmies.CalculateMaps();

                for (int i = 0; i < instance.AllArmies.Count; i++)
                    gameRef.CalculateCorpsValuesForTurn(instance.AllArmies[i]);
                DisplayCalculatingCourierRoutes = false;
            }

            gameRef.TargetElapsedTime = inGameTargetElapsedTime;
        }

        public bool LocalTurnTaken(GameInstance instance) =>
            instance.ActiveArmy?.TurnsTaken >= instance.CurrentGameTurn;

        public bool LocalEndTurnAvailable(GameInstance instance) =>
             (LocalTurnTaken(instance) == false) && (gameRef.GameplayScreen == Game1.GameplayScreens.Playing);

        public bool AllArmiesEndTurnAvailable(GameInstance instance, SavedGameData savedGameData) =>
            LocalTurnTaken(instance) && (instance.CurrentGameTurn < instance.ScenarioNumTurns) &&
                ((NoOfOpponentsNotTakenTurn(instance, savedGameData) == 0) || (savedGameData?.GameOpponentType == SavedGameData.GameOpponentTypes.AI));

        public int NoOfOpponentsNotTakenTurn(GameInstance instance, SavedGameData savedGameData)
        {
            int count = 0;
            if (instance.AllArmies != null)
                for (int i = 0; i < instance.AllArmies.Count; i++)
                    if ((instance.AllArmies[i] != instance.ActiveArmy) &&
                        (instance.AllArmies[i].TurnsTaken < instance.CurrentGameTurn) &&
                        (savedGameData.GameProgressState != SavedGameData.GameProgressStates.Completed))
                        count++;
            return count;
        }
    }
}
