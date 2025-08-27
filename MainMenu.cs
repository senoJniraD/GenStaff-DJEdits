using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using ModelLib;
using GSBPGEMG.UI;
using static GSBPGEMG.Game1;
using static GSBPGEMG.SavedGameData;
using ButtonTypes = GSBPGEMG.UI.UIStyle.ButtonTypes;
using FontTypes = GSBPGEMG.UI.UIStyle.FontTypes;

namespace GSBPGEMG
{
    public class MainMenu
    {
        public enum Screens
        {
            MainMenu,
            ScenarioSelect,
            SideSelect,
            FogOfWarSelect,
            OpponentTypeSelect,
            SteamPlayerSelect,
            EmailSelect,
            ContinueSavedGame,
            OnlineGames
        }
        public Screens Screen { get; private set; }

        private List<Screens> ScreenStack = [Screens.MainMenu];
        private int ScreenStackPosition = 0;

        private static UIElement_BorderedTexture background;

        private UIElement_Button editorArmyButton;
        private UIElement_Button editorMapButton;
        private UIElement_Button editorScenarioButton;
        private UIElement_Button newGameButton;
        private UIElement_Button continueSavedGameButton;
        private UIElement_Button onlineGamesButton;
        private UIElement_Button styleButton;

        private UIElement_TextBox menuTitle;
        private UIElement_TextBox menuCopyright;
        private UIElement_Button menuSelectButton;
        private UIElement_Button menuBackButton;

        private SavedGameData newSavedGameData;

        private UIElement_List scenarioList;
        private List<string> scenarioListElements;
        private Scenario scenarioSelected;
        private string scenarioSelectedFileName;
        private UIElement_TextBox scenarioNameText;
        private UIElement_TextBox scenarioDescriptionText;
        private List<UIElement_TextBox> scenarioListTextBoxes;
        private Texture2D scenarioMap;
        private Texture2D scenarioMapGreyScale;

        private UIElement_Button sideButton1;
        private UIElement_Button sideButton2;

        private UIElement_Button fowButtonNone;
        private UIElement_Button fowButtonPartial;
        private UIElement_Button fowButtonComplete;

        private UIElement_Button opponentButtonAI;
        private UIElement_Button opponentButtonSteamPlayer;
        private UIElement_Button opponentButtonPlayByEmail;
        private UIElement_Button opponentButtonLocalMultiplayer;

        private UIElement_List steamPlayersList;
        private List<Networking_Steam.NetworkPlayer> steamPlayersListElements;
        private Networking_Steam.NetworkPlayer steamPlayerSelected;
        private List<UIElement_TextBox> steamPlayersListTextBoxes;
        private Networking_Steam.NetworkPlayer steamOpenInvitePlayer;

        private UIElement_Button emailButtonYourEmailAddress;
        private UIElement_Button emailButtonOpponentEmailAddress;
        private UIElement_Button emailButtonSteamVerify;
        private UIElement_Button emailButtonCustomVerify;
        private UIElement_Button emailButtonSteamOrCustomVerify;
        private UIElement_Button emailButtonNoVerification;
        private string emailDefaultEmailAddress = "(Blank)";

        private UIElement_List continueGamesList;
        private List<SavedGameData> continueGamesListElements;
        private SavedGameData continueGameSelected;
        private List<UIElement_TextBox> continueGamesListTextBoxes;
        private UIElement_Button continueGamesButtonEmail;

        private enum OnlineGameOriginTypes { NewInviteFriend, ExistingGame, NewInvitePublic }
        private UIElement_List onlineGamesList;
        private List<(SavedGameData savedGameData, OnlineGameOriginTypes onlineGameOriginType)> onlineGamesListElements;
        private (SavedGameData savedGameData, OnlineGameOriginTypes onlineGameOriginType)? onlineGameSelected;
        private List<UIElement_TextBox> onlineGamesListTextBoxes;

        private List<UIElement_TextBox> optionBoxLabels;
        private List<UIElement_TextBox> optionBoxDescriptions;
        private int optionBoxIndex;

        public MainMenu()
        {
            background = new();

            ButtonTypes buttonType = ButtonTypes.MainMenuButton1;
            FontTypes fontType = FontTypes.MainMenuButton1;
            Vector2 position = new(1050, 135);
            Vector2 size = new(600, 86);

            editorArmyButton = new UIElement_Button("Run the General Staff Army Editor",
                buttonType, fontType, position, size);
            editorMapButton = new UIElement_Button("Run the General Staff Map Editor",
                buttonType, fontType, position += new Vector2(0, 100), size);
            editorScenarioButton = new UIElement_Button("Run the General Staff Scenario Editor",
                buttonType, fontType, position += new Vector2(0, 100), size);
            newGameButton = new UIElement_Button("Start a New Simulation",
                buttonType, fontType, position += new Vector2(0, 150), size);
            continueSavedGameButton = new UIElement_Button("Continue a Saved Simulation",
                buttonType, fontType, position += new Vector2(0, 100), size);
            onlineGamesButton = new UIElement_Button("Join an Online Game or Simulation",
                buttonType, fontType, position += new Vector2(0, 100), size);
            styleButton = new UIElement_Button("Switch to Modern Style",
                buttonType, fontType, position += new Vector2(0, 100), size);

            menuTitle = new UIElement_TextBox(FontTypes.MainMenuHeading);
            menuCopyright = new UIElement_TextBox(FontTypes.MainMenuButton1);

            buttonType = ButtonTypes.MainMenuButton2;
            fontType = FontTypes.MainMenuButton2;

            menuSelectButton = new UIElement_Button("Select", buttonType, fontType, new Vector2(1220, 785), new Vector2(300, 65));
            menuBackButton = new UIElement_Button("Back", buttonType, fontType, new Vector2(870, 785), new Vector2(300, 65));

            scenarioList = new UIElement_List(new(710, 270), new(650, 450), 30, 15);
            scenarioListElements = [];
            scenarioNameText = new() { TooltipMode = UIElement_TextBox.TooltipModes.OnOverflow };
            scenarioDescriptionText = new() { TooltipMode = UIElement_TextBox.TooltipModes.OnOverflow };
            scenarioListTextBoxes = [];

            buttonType = ButtonTypes.MainMenuButton2;
            fontType = FontTypes.MainMenuButton2;

            position = new(1045, 250);
            size = new Vector2(650, 250);
            sideButton1 = new UIElement_Button("", buttonType, fontType, position, size);
            sideButton2 = new UIElement_Button("", buttonType, fontType, position += new Vector2(0, 305), size);

            position = new(1045, 180);
            size = new Vector2(650, 150);
            fowButtonNone = new UIElement_Button("", buttonType, fontType, position, size);
            fowButtonPartial = new UIElement_Button("", buttonType, fontType, position += new Vector2(0, 200), size);
            fowButtonComplete = new UIElement_Button("", buttonType, fontType, position += new Vector2(0, 200), size);

            position = new(1045, 150);
            size = new Vector2(650, 130);
            opponentButtonAI = new UIElement_Button("", buttonType, fontType, position, size);
            opponentButtonSteamPlayer = new UIElement_Button("", buttonType, fontType, position += new Vector2(0, 160), size);
            opponentButtonPlayByEmail = new UIElement_Button("", buttonType, fontType, position += new Vector2(0, 160), size);
            opponentButtonLocalMultiplayer = new UIElement_Button("", buttonType, fontType, position += new Vector2(0, 160), size);

            steamPlayersList = new UIElement_List(new(710, 270), new(650, 450), 30, 15);
            steamPlayersListElements = [];
            steamPlayersListTextBoxes = [];

            continueGamesList = new UIElement_List(new(710, 100), new(650, 540), 90, 6);
            continueGamesListElements = [];
            continueGamesListTextBoxes = [];
            continueGamesButtonEmail = new UIElement_Button("Open Email Attachment",
                buttonType, fontType, new Vector2(1045, 700), new Vector2(650, 65));

            position = new(1045, 130);
            size = new Vector2(650, 65);
            fontType = FontTypes.MainMenuScenarioDescription;
            emailButtonYourEmailAddress = new UIElement_Button("", buttonType, fontType, position, size);
            emailButtonOpponentEmailAddress = new UIElement_Button("", buttonType, fontType, position += new Vector2(0, 65), size);
            emailButtonSteamVerify = new UIElement_Button("", buttonType, fontType, position += new Vector2(0, 100), size += new Vector2(0, 50));
            emailButtonCustomVerify = new UIElement_Button("", buttonType, fontType, position += new Vector2(0, 120), size);
            emailButtonSteamOrCustomVerify = new UIElement_Button("", buttonType, fontType, position += new Vector2(0, 120), size);
            emailButtonNoVerification = new UIElement_Button("", buttonType, fontType, position += new Vector2(0, 120), size);

            onlineGamesList = new UIElement_List(new(710, 100), new(650, 600), 120, 5);
            onlineGamesListElements = [];
            onlineGamesListTextBoxes = [];

            optionBoxLabels = [];
            optionBoxDescriptions = [];
            for (int i = 0; i < 4; i++)
            {
                optionBoxLabels.Add(new UIElement_TextBox(FontTypes.MainMenuScenarioName));
                optionBoxDescriptions.Add(new UIElement_TextBox(FontTypes.MainMenuScenarioDescription));
            }

            Reset();
        }

        public void Update(Game1 game)
        {
            Input.MouseCursorIcon = MouseCursor.Arrow;

            menuSelectButton.visible = menuBackButton.visible = Screen != Screens.MainMenu;
            menuSelectButton.Update();
            menuSelectButton.pressed |= Input.mouseX2Click;

            Screens lastScreen = Screen;
            switch (Screen)
            {
                case Screens.MainMenu: UpdateMainMenu(game); break;
                case Screens.ScenarioSelect: UpdateScenarioSelect(); break;
                case Screens.SideSelect: UpdateSideSelect(); break;
                case Screens.FogOfWarSelect: UpdateFoWSelect(); break;
                case Screens.OpponentTypeSelect: UpdateOpponentTypeSelect(); break;
                case Screens.SteamPlayerSelect: UpdateSteamPlayerSelect(); break;
                case Screens.EmailSelect: UpdateEmailSelect(); break;
                case Screens.ContinueSavedGame: UpdateContinueSavedGame(); break;
                case Screens.OnlineGames: UpdateOnlineGame(); break;
            }

            if (GameRef.GameState == GameStates.LoadingGame)
                return;

            menuBackButton.Update();
            if (Input.KeyIsPressed(Keys.Escape) || Input.mouseX1Click || menuBackButton.pressed)
                ScreenGoBack();
            else if (Input.mouseX2Click && (Screen == lastScreen))
                ScreenGoForward();

            menuSelectButton.pressed = menuBackButton.pressed = false;

            if (ScreenStack.Count == 0)
            {
                ScreenStack.Add(Screens.MainMenu);
                ScreenStackPosition = 0;
            }
        }

        private void UpdateMainMenu(Game1 game)
        {
            editorArmyButton.Update();
            editorMapButton.Update();
            editorScenarioButton.Update();
            newGameButton.Update();
            continueSavedGameButton.Update();
            onlineGamesButton.Update();
            //styleButton.Update();

            if (editorArmyButton.pressed)
                StartEditor("Army Editor", "GSBPArmyEditor.exe");
            if (editorMapButton.pressed)
                StartEditor("Map Editor", "GSBPMapEditor.exe");
            if (editorScenarioButton.pressed)
                StartEditor("Scenario Editor", "GSBPScenarioEditor.exe");

            if (newGameButton.pressed)
            {
                scenarioListElements = null;
                newSavedGameData = new SavedGameData() { LocalPlayerIndex = 0 };
                if (Networking_Steam.RunWithoutSteam)
                    newSavedGameData.EmailVerificationType = EmailVerificationTypes.CustomPassword;
                SetScreen(Screens.ScenarioSelect);
            }

            if (continueSavedGameButton.pressed)
                SetScreen(Screens.ContinueSavedGame);

            if (onlineGamesButton.pressed)
                SetScreen(Screens.OnlineGames);

            //if (styleButton.pressed)
            //    UIStyles.Style = (UIStyles.Styles)(((int)UIStyles.Style + 1) % UIStyles.ContentStylesList.Count);

            editorArmyButton.pressed = editorMapButton.pressed = editorScenarioButton.pressed =
                newGameButton.pressed = continueSavedGameButton.pressed = onlineGamesButton.pressed = styleButton.pressed = false;
        }

        private void UpdateScenarioSelect()
        {
            if (scenarioListElements == null)
            {
                //scenarioListElements = [.. Directory.GetFiles(FilesIO.ScenariosFolder, "*.xml")]; // TODO (noted by MT) initially only 4 scenarios
                scenarioListElements = [
                    "Antietam.xml",
                    "Ligny.xml",
                    "Quatre Bras.xml",
                    "Manassas Campaign.xml"];
                scenarioSelectedFileName = scenarioListElements[0];
                SetScenarioInfo();
            }

            scenarioList.Update(scenarioListElements, true);

            if (Input.mouseLeftClick && scenarioList.highlighted &&
                scenarioSelectedFileName != scenarioListElements[scenarioList.selection])
            {
                scenarioSelectedFileName = scenarioListElements[scenarioList.selection];
                SetScenarioInfo();
            }

            if ((menuSelectButton.pressed) && (scenarioMap != null))
                SetScreen(Screens.SideSelect);
        }

        private void SetScenarioInfo() // TODO (noted by MT) try/catch
        {
            try
            {
                newSavedGameData.EditorAssetSet = new TacticalAILib.GameEditorAssetSet();
                newSavedGameData.EditorAssetSet.EditorScenario.FileName = scenarioSelectedFileName;
                newSavedGameData.EditorAssetSet.Load(true, false);
                newSavedGameData.LoadedGameInstance = new(newSavedGameData.EditorAssetSet, TacticalAILib.MapFogOfWar.FogOfWarTypes.None);

                newSavedGameData.SavedArmies = [];
                for (int i = 0; i < newSavedGameData.EditorAssetSet.EditorArmies.Count; i++)
                    newSavedGameData.SavedArmies.Add(new SavedGameArmyData() { Index = newSavedGameData.SavedArmies.Count });

                scenarioSelected = newSavedGameData.EditorAssetSet.EditorScenario.Asset as Scenario;
                scenarioNameText.Text = scenarioSelected.ScenarioName;
                scenarioDescriptionText.Text = scenarioSelected.ScenarioDescription;
                scenarioMap?.Dispose();
                scenarioMapGreyScale?.Dispose();

                string zipFile = Path.Combine(FilesIO.MapsFolder, new string(scenarioSelected.MapFileName + ".zip"));
                using ZipArchive archive = ZipFile.Open(zipFile, ZipArchiveMode.Read);
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.Name == "png")
                    {
                        using Stream stream = entry.Open();
                        scenarioMap = Texture2D.FromStream(GraphicsDeviceRef, stream);

                        Color[] data = new Color[scenarioMap.Width * scenarioMap.Height];
                        scenarioMap.GetData(data);
                        for (int i = 0; i < data.Length; i++)
                        {
                            byte value = Math.Max(data[i].R, Math.Max(data[i].G, data[i].B));
                            data[i] = new Color(value, value, value);
                        }

                        scenarioMapGreyScale = new Texture2D(GraphicsDeviceRef, scenarioMap.Width, scenarioMap.Height);
                        scenarioMapGreyScale.SetData(data);
                    }
                }
            }
            catch
            {
                scenarioNameText.Text = "Failed to load details!";
                scenarioDescriptionText.Text = "(Please check the scenario, map & both army files are present).";
                scenarioMap?.Dispose();
                scenarioMap = null;
            }
        }

        private void UpdateSideSelect()
        {
            sideButton1.Update();
            sideButton2.Update();

            if (sideButton1.pressed)
                newSavedGameData.ScenarioHostArmyIndex = newSavedGameData.LocalPlayerIndex = 0;
            if (sideButton2.pressed)
                newSavedGameData.ScenarioHostArmyIndex = newSavedGameData.LocalPlayerIndex = 1;

            if (menuSelectButton.pressed)
                SetScreen(Screens.FogOfWarSelect);
        }

        private void UpdateFoWSelect()
        {
            fowButtonNone.Update();
            fowButtonPartial.Update();
            fowButtonComplete.Update();

            if (fowButtonNone.pressed)
                newSavedGameData.FogOfWarType = TacticalAILib.MapFogOfWar.FogOfWarTypes.None;
            if (fowButtonPartial.pressed)
                newSavedGameData.FogOfWarType = TacticalAILib.MapFogOfWar.FogOfWarTypes.Partial;
            if (fowButtonComplete.pressed)
                newSavedGameData.FogOfWarType = TacticalAILib.MapFogOfWar.FogOfWarTypes.Complete;

            if (menuSelectButton.pressed)
                SetScreen(Screens.OpponentTypeSelect);
        }

        private void UpdateOpponentTypeSelect()
        {
            opponentButtonAI.Update();
            opponentButtonSteamPlayer.Update(Networking_Steam.RunWithoutSteam == false);
            opponentButtonPlayByEmail.Update();
            opponentButtonLocalMultiplayer.Update();

            if (newSavedGameData.GameOpponentType == GameOpponentTypes.AI)
                newSavedGameData.GameOpponentType = Networking_Steam.RunWithoutSteam ? GameOpponentTypes.LocalMultiplayer : GameOpponentTypes.SteamPlayer;
            if (opponentButtonAI.pressed)
                newSavedGameData.GameOpponentType = GameOpponentTypes.AI;

            if (opponentButtonSteamPlayer.pressed)
                newSavedGameData.GameOpponentType = GameOpponentTypes.SteamPlayer;
            if (opponentButtonPlayByEmail.pressed)
                newSavedGameData.GameOpponentType = GameOpponentTypes.PlayByEmail;
            if (opponentButtonLocalMultiplayer.pressed)
                newSavedGameData.GameOpponentType = GameOpponentTypes.LocalMultiplayer;

            if (menuSelectButton.pressed)
            {
                switch (newSavedGameData.GameOpponentType)
                {
                    case GameOpponentTypes.AI:
                    case GameOpponentTypes.LocalMultiplayer:
                        StartGame(newSavedGameData);
                        return;

                    case GameOpponentTypes.SteamPlayer:
                        newSavedGameData.GameOpponentType = GameOpponentTypes.SteamPlayer;
                        newSavedGameData.HostPlayerArmy.SteamName = SteamServices.localName;
                        newSavedGameData.HostPlayerArmy.SteamID = SteamServices.localID;
                        SetScreen(Screens.SteamPlayerSelect);
                        return;

                    case GameOpponentTypes.PlayByEmail:
                        SetScreen(Screens.EmailSelect);
                        return;
                }
            }
        }

        private void UpdateSteamPlayerSelect()
        {
            steamPlayersListElements.Clear();
            steamOpenInvitePlayer ??= new(new Steamworks.CSteamID() { m_SteamID = 0 });
            steamPlayersListElements.Add(steamOpenInvitePlayer);
            foreach (Networking_Steam.NetworkPlayer networkPlayer in SteamServices.allNetworkPlayers.Values)
                if (networkPlayer.IsFriend)
                    steamPlayersListElements.Add(networkPlayer);

            steamPlayersList.Update(steamPlayersListElements, true);
            if (steamPlayersListElements.Count == 0)
                return;

            if (Input.mouseLeftClick && steamPlayersList.highlighted &&
                steamPlayersListElements.Count >= 1 &&
                steamPlayerSelected != steamPlayersListElements[steamPlayersList.selection])
                steamPlayerSelected = steamPlayersListElements[steamPlayersList.selection];

            if (menuSelectButton.pressed && (steamPlayerSelected != null))
            {
                SavedGameArmyData unassignedArmy = newSavedGameData.GetUnassignedArmies().First();
                unassignedArmy.SteamName = steamPlayerSelected.Name;
                unassignedArmy.SteamID = steamPlayerSelected.steamID.m_SteamID;
                StartGame(newSavedGameData);
            }
        }

        private void UpdateEmailSelect()
        {
            emailButtonYourEmailAddress.Update();
            emailButtonOpponentEmailAddress.Update();
            emailButtonSteamVerify.Update();
            emailButtonCustomVerify.Update();
            emailButtonSteamOrCustomVerify.Update();
            emailButtonNoVerification.Update();

            if (emailButtonYourEmailAddress.pressed)
            {
                SavedGameArmyData savedArmy = newSavedGameData.LocalPlayerArmy;
                string emailEntered = KeyboardInput.Show("Your Email Address", "Enter email: ",
                    (savedArmy.Email != emailDefaultEmailAddress) ? savedArmy.Email : "").Result?.ToString();
                savedArmy.Email = (emailEntered?.Length >= 1) ? emailEntered : emailDefaultEmailAddress;
            }
            if (emailButtonOpponentEmailAddress.pressed)
            {
                SavedGameArmyData savedArmy = newSavedGameData.SavedArmies.Find(x => x.Index != newSavedGameData.LocalPlayerIndex);
                string emailEntered = KeyboardInput.Show("Opponent Email Address", "Enter email: ",
                    (savedArmy.Email != emailDefaultEmailAddress) ? savedArmy.Email : "").Result?.ToString();
                savedArmy.Email = (emailEntered?.Length >= 1) ? emailEntered : emailDefaultEmailAddress;
            }

            if ((newSavedGameData.EmailVerificationType == EmailVerificationTypes.Steam) && Networking_Steam.RunWithoutSteam)
                newSavedGameData.EmailVerificationType = EmailVerificationTypes.CustomPassword;

            if (emailButtonSteamVerify.pressed && (Networking_Steam.RunWithoutSteam == false))
                newSavedGameData.EmailVerificationType = EmailVerificationTypes.Steam;
            if (emailButtonCustomVerify.pressed)
                newSavedGameData.EmailVerificationType = EmailVerificationTypes.CustomPassword;
            if (emailButtonSteamOrCustomVerify.pressed && (Networking_Steam.RunWithoutSteam == false))
                newSavedGameData.EmailVerificationType = EmailVerificationTypes.SteamOrCustomPassword;
            if (emailButtonNoVerification.pressed)
                newSavedGameData.EmailVerificationType = EmailVerificationTypes.NoVerification;

            if (menuSelectButton.pressed)
            {
                if (newSavedGameData.SetLocalVerificationMethods(newSavedGameData.HostPlayerArmy))
                    StartGame(newSavedGameData);
            }
        }

        private void UpdateContinueSavedGame()
        {
            continueGamesButtonEmail.Update();
            if (continueGamesButtonEmail.pressed)
                SavedGamesCollection.SelectEmailAttachment();

            if (SavedGamesCollection.AvailableSavedGames.Count == 0)
            {
                string[] savedGamesFilenames = Directory.GetFiles(FilesIO.SavedGamesFolder, "*." + FileExtension);
                foreach (string savedGameFilename in savedGamesFilenames)
                    SavedGamesCollection.BackgroundSavedGamesToCheck.Enqueue((SavedGamesCollection.EntryOrigin.File,
                        savedGameFilename, LoadedStatus.GameDetailsOnly));
            }

            continueGamesListElements.Clear();
            foreach (SavedGameData savedGameData in SavedGamesCollection.AvailableSavedGames.Values)
                if ((savedGameData.GameOpponentType == GameOpponentTypes.PlayByEmail) ||
                    (savedGameData.FindLocalPlayerArmyIndex(displayErrorMessage: false) >= 0))
                    continueGamesListElements.Add(savedGameData);

            continueGamesListElements = continueGamesListElements
               .OrderBy(x => (int)x?.GameProgressState)
               .ThenByDescending(x => x?.LastUpdateTime.Ticks)
               .ToList();

            continueGamesList.Update(continueGamesListElements, true);
            if (continueGamesListElements.Count == 0)
                return;

            if (Input.mouseLeftClick && continueGamesList.highlighted &&
                continueGamesListElements.Count >= 1 &&
                continueGameSelected != continueGamesListElements[continueGamesList.selection])
                continueGameSelected = continueGamesListElements[continueGamesList.selection];

            if (menuSelectButton.pressed && (continueGameSelected != null))
                if (continueGameSelected.FindLocalPlayerArmyIndex(displayErrorMessage: true) >= 0)
                StartGame(continueGameSelected);
        }

        private void UpdateOnlineGame()
        {
            onlineGamesListElements.Clear();

            foreach (SavedGameData savedGameData in SavedGamesCollection.AvailableSavedGames.Values)
            {
                if ((savedGameData.GameOpponentType == GameOpponentTypes.SteamPlayer) &&
                    (savedGameData.ServerTurnsProgress != null))
                {
                    int localPlayerArmyNo = savedGameData.FindSteamPlayerArmyIndex(SteamServices.localID);
                    bool containsLocalPlayer = localPlayerArmyNo >= 0;
                    if (savedGameData.GameProgressState == GameProgressStates.NewInvite)
                    {
                        if (containsLocalPlayer)
                            onlineGamesListElements.Add((savedGameData, OnlineGameOriginTypes.NewInviteFriend));
                        else if (savedGameData.GetUnassignedArmies().Count >= 1)
                            onlineGamesListElements.Add((savedGameData, OnlineGameOriginTypes.NewInvitePublic));
                    }
                    else if (containsLocalPlayer)
                    {
                        onlineGamesListElements.Add((savedGameData, OnlineGameOriginTypes.ExistingGame));
                    }
                }
            }

            foreach (SavedGameData savedGameData in SavedGamesCollection.AvailableOnlineHostedGames.Values)
            {
                if ((savedGameData.GameOpponentType == GameOpponentTypes.SteamPlayer) &&
                    (savedGameData.FindSteamPlayerArmyIndex(SteamServices.localID) >= 0) &&
                    (onlineGamesListElements.Exists(x => x.savedGameData.SavedDataUniqueID == savedGameData.SavedDataUniqueID) == false))
                {
                    if (savedGameData.GameProgressState == GameProgressStates.NewInvite)
                        onlineGamesListElements.Add((savedGameData, OnlineGameOriginTypes.NewInviteFriend));
                    else
                        onlineGamesListElements.Add((savedGameData, OnlineGameOriginTypes.ExistingGame));
                }
            }

            onlineGamesListElements = onlineGamesListElements
                .OrderBy(x => x.onlineGameOriginType)
                .ThenByDescending(x => x.savedGameData?.GetReadyWaitState(SteamServices.localID) == GameReadyWaitStates.WaitingForLocalPlayer)
                .ThenByDescending(x => x.savedGameData?.LastUpdateTime.Ticks)
                .ToList();

            if (onlineGamesListElements.Count == 0)
                return;

            onlineGamesList.Update(onlineGamesListElements, true);

            if (Input.mouseLeftClick && onlineGamesList.highlighted &&
                onlineGameSelected != onlineGamesListElements[onlineGamesList.selection])
                onlineGameSelected = onlineGamesListElements[onlineGamesList.selection];

            //if (SteamServices.SteamLaunchSessionParameters?.Length >= 1)
            //{
            //    ulong launchID = Convert.ToUInt64(SteamServices.SteamLaunchSessionParameters);
            //    string text = "Game Notification Received: " + launchID;
            //    onlineGameSelected = new SavedGameData()
            //    {
            //        SavedDataUniqueID = launchID,
            //        GameProgressState = GameProgressStates.NewInvite,
            //        GameOpponentType = GameOpponentTypes.SteamPlayer
            //    };
            //}

            if (menuSelectButton.pressed && onlineGameSelected.HasValue)
                StartGame(onlineGameSelected.Value.savedGameData);
        }

        private void SetScreen(Screens screen)
        {
            if ((ScreenStackPosition >= 1) && (ScreenStack[ScreenStackPosition - 1] == screen))
            {
                ScreenStackPosition--;
            }
            else if ((ScreenStackPosition < (ScreenStack.Count - 1)) && (ScreenStack[ScreenStackPosition + 1] == screen))
            {
                ScreenStackPosition++;
            }
            else
            {
                if (ScreenStackPosition < (ScreenStack.Count - 1))
                    ScreenStack.RemoveRange(ScreenStackPosition + 1, ScreenStack.Count - ScreenStackPosition - 1);
                ScreenStack.Add(screen);
                ScreenStackPosition++;
            }

            if (Screen != screen)
                Screen = screen;
        }

        private void ScreenGoBack()
        {
            if (ScreenStackPosition >= 1)
                SetScreen(ScreenStack[ScreenStackPosition - 1]);
        }

        private void ScreenGoForward()
        {
            if (ScreenStackPosition < (ScreenStack.Count - 1))
                SetScreen(ScreenStack[ScreenStackPosition + 1]);
        }

        private void StartGame(SavedGameData savedGameData)
        {
            GameRef.CurrentSavedGameData = savedGameData;
            GameRef.GameState = GameStates.LoadingGame;
            Reset();
        }

        private void StartEditor(string folder, string filename)
        {
            // TODO (noted by MT) - dlc check

            try
            {
                string editorPath = Path.Combine(Directory.GetCurrentDirectory(), folder, filename);

#if DEBUG
                if (File.Exists(editorPath) == false)
                {
                    while (true)
                    {
                        editorPath = Directory.GetParent(editorPath).ToString();
                        if (Directory.Exists(editorPath))
                        {
                            if (Directory.GetDirectories(editorPath).ToList().Exists(x => x.Contains(folder.Replace(" ", ""))))
                            {
                                string[] files = Directory.GetFiles(editorPath, "*.exe", SearchOption.AllDirectories);
                                editorPath = files.ToList().Find(x =>
                                    x.Contains(filename) && x.Contains("Debug") && x.Contains("x64") && x.Contains("net8.0-windows"));
                                break;
                            }
                        }
                    }
                }
#endif

                Process.Start(editorPath);
            }
            catch (Exception e)
            {
                Logging.Write(exception: e, title: "Unable To Open Editor", message: "Unable to open editor", showMessageBox: true);
            }
        }

        private void Reset()
        {
            Screen = Screens.MainMenu;
            ScreenStack = [Screen];
            ScreenStackPosition = 0;

            newSavedGameData = null;
            steamPlayerSelected = null;
            continueGameSelected = null;
            onlineGameSelected = null;

            scenarioListElements = null;
            scenarioSelected = null;
            scenarioSelectedFileName = null;
            scenarioMap?.Dispose();
            scenarioMap = null;
            scenarioMapGreyScale?.Dispose();
            scenarioMapGreyScale = null;
        }

        public void Draw(Game1 game)
        {
            UIElement_SpriteLayer spriteBatch = game.spriteLayerTitleScreen;
            spriteBatch.Begin(rasterizerState: game.RasterizerStateDefault);
            UIElement_SpriteLayer spriteBatchTooltips = game.spriteLayerTooltips;
            spriteBatchTooltips.Begin(rasterizerState: game.RasterizerStateDefault);

            background.Size = UIStyles.Current.WindowBackground1.Bounds.Size.ToVector2();
            background.Draw(spriteBatch, UIStyles.Current.WindowBackground1, null, shadowRequest: false);

            spriteBatch.Draw(UIStyles.Current.TitleLogo, new Vector2(30, 0), Color.White);
            menuCopyright.Draw(game, spriteBatch, text: "Copyright 2025\n/v[8]Riverview Artificial Intelligence, LLC", position: new Vector2(335, 750),
                horizontalAlignment: UIElement_TextBox.HorizontalAlignments.Center);

            optionBoxIndex = 0;

            switch (Screen)
            {
                case Screens.MainMenu: DrawMainMenu(game, spriteBatch); break;
                case Screens.ScenarioSelect: DrawScenarioSelect(game, spriteBatch); break;
                case Screens.SideSelect: DrawSideSelect(game, spriteBatch); break;
                case Screens.FogOfWarSelect: DrawFoWSelect(game, spriteBatch); break;
                case Screens.OpponentTypeSelect: DrawOpponentTypeSelect(game, spriteBatch); break;
                case Screens.SteamPlayerSelect: DrawSteamPlayerSelect(game, spriteBatch); break;
                case Screens.EmailSelect: DrawEmailSelect(game, spriteBatch); break;
                case Screens.ContinueSavedGame: DrawContinueSavedGames(game, spriteBatch); break;
                case Screens.OnlineGames: DrawOnlineGames(game, spriteBatch); break;
            }

            if (Screen != Screens.MainMenu)
            {
                menuSelectButton.Draw(game, spriteBatch);
                menuBackButton.Draw(game, spriteBatch);
            }

#if DEBUG
            spriteBatch.DrawString2(UIStyles.Current.Fonts[(int)FontTypes.MainMenuScenarioName],
                Input.mouseMenuPosition.ToPoint().ToString(), Vector2.Zero, Color.Red);
#endif

            if ((scenarioMapGreyScale != null) &&
                (Screen == Screens.SideSelect || Screen == Screens.FogOfWarSelect || Screen == Screens.OpponentTypeSelect ||
                Screen == Screens.SteamPlayerSelect || Screen == Screens.EmailSelect))
                spriteBatch.Draw(scenarioMapGreyScale, new Rectangle(472, 266, 187, 109), Color.White);

            UIElement_SpriteLayer.PreDrawActions();
            game.ScreenResizer.BeginDraw();
            spriteBatch.End();
            spriteBatchTooltips.End();
            game.ScreenResizer.EndDraw(null);
        }

        private void DrawMainMenu(Game1 game, UIElement_SpriteLayer spriteBatch)
        {
            editorArmyButton.Draw(game, spriteBatch);
            editorMapButton.Draw(game, spriteBatch);
            editorScenarioButton.Draw(game, spriteBatch);

            newGameButton.Draw(game, spriteBatch);
            continueSavedGameButton.Draw(game, spriteBatch);
            onlineGamesButton.Draw(game, spriteBatch);
            //styleButton.Draw(game, spriteBatch, text: $"Switch to {(UIStyles.Styles)(((int)UIStyles.Style + 1) %
            //    UIStyles.ContentStylesList.Count)} Style");
        }

        private void DrawScenarioSelect(Game1 game, UIElement_SpriteLayer spriteLayerTitleScreen)
        {
            menuTitle.Draw(game, spriteLayerTitleScreen, text: "[Select|New|Scenario]", position: new Vector2(1040, 8),
                horizontalAlignment: UIElement_TextBox.HorizontalAlignments.Center);

            if (scenarioListElements == null)
                return;

            scenarioNameText.Draw(game, spriteLayerTitleScreen, fontType: FontTypes.MainMenuScenarioName,
               position: new Vector2(1045, 80), horizontalAlignment: UIElement_TextBox.HorizontalAlignments.Center,
               maxWidth: 650, maxHeight: 30);

            scenarioDescriptionText.Draw(game, spriteLayerTitleScreen, fontType: FontTypes.MainMenuScenarioDescription,
               position: new Vector2(1045, 150), horizontalAlignment: UIElement_TextBox.HorizontalAlignments.Center,
               verticalAlignment: UIElement_TextBox.VerticalAlignments.Center, maxWidth: 650, maxHeight: 65);

            if (scenarioMap != null)
                spriteLayerTitleScreen.Draw(scenarioMap, new Rectangle(44, 262, 616, 342), Color.White);

            if (scenarioListElements?.Count >= 0)
                spriteLayerTitleScreen.postEndActions.Add(() =>
                { scenarioList.Draw(game, DrawScenarioSelectListItem, 1f, scenarioListElements.Count); });
        }

        private void DrawScenarioSelectListItem(Game1 game, SpriteBatch spriteBatch, int index, Vector2 position, bool selected)
        {
            while (scenarioListTextBoxes.Count <= index)
                scenarioListTextBoxes.Add(new UIElement_TextBox(FontTypes.MainMenuScenarioName)
                { TooltipMode = UIElement_TextBox.TooltipModes.OnOverflow });

            string scenarioFileName = scenarioListElements[index];
            bool choiceSelected = scenarioFileName == scenarioSelectedFileName;
            UIElement_TextBox textBox = scenarioListTextBoxes[index];

            textBox.Text = Path.GetFileNameWithoutExtension(scenarioFileName);
            Color textColor = menuSelectButton.inactiveTextColor;
            int maxWidth = 580;
            if ((selected && scenarioList.highlighted) || choiceSelected)
            {
                if (choiceSelected)
                {
                    spriteBatch.Draw(UIStyles.Current.ListSelector, position + new Vector2(9, -3), Color.White);
                    position.X += UIStyles.Current.ListSelector.Width + 8;
                    maxWidth -= UIStyles.Current.ListSelector.Width + 5;
                }

                textColor = Color.Black;
                textBox.Draw(game, spriteBatch, fontType: FontTypes.MainMenuScenarioName,
                    color: Color.Black * 0.2f, position: position + new Vector2(11, 3), maxWidth: maxWidth, maxHeight: 30,
                    autoEllipsis: UIElement_TextBox.AutoEllipsisMode.Word);
            }

            textBox.Draw(game, spriteBatch, fontType: FontTypes.MainMenuScenarioName,
                color: textColor, position: position + new Vector2(11, 2), maxWidth: maxWidth, maxHeight: 30,
                autoEllipsis: UIElement_TextBox.AutoEllipsisMode.Word);
        }

        private void DrawSideSelect(Game1 game, UIElement_SpriteLayer spriteLayerTitleScreen)
        {
            menuTitle.Draw(game, spriteLayerTitleScreen, text: "[Select|Side]", position: new Vector2(1040, 8),
                horizontalAlignment: UIElement_TextBox.HorizontalAlignments.Center);

            Sides sideSelected = newSavedGameData.LoadedGameInstance.AllArmies[newSavedGameData.LocalPlayerIndex].Side;

            Sides side1 = newSavedGameData.LoadedGameInstance.AllArmies[0].Side;
            DrawOptionBox(game, spriteLayerTitleScreen, sideButton1, $"Command {side1} Army",
                newSavedGameData.LoadedGameInstance.AllArmies[0].Name, sideSelected == side1, descriptionFont: FontTypes.MainMenuButton2);

            Sides side2 = newSavedGameData.LoadedGameInstance.AllArmies[1].Side;
            DrawOptionBox(game, spriteLayerTitleScreen, sideButton2, $"Command {side2} Army",
                newSavedGameData.LoadedGameInstance.AllArmies[1].Name, sideSelected == side2, descriptionFont: FontTypes.MainMenuButton2);
        }

        private void DrawFoWSelect(Game1 game, UIElement_SpriteLayer spriteLayerTitleScreen)
        {
            menuTitle.Draw(game, spriteLayerTitleScreen, text: "[Fog|of|War|Options]", position: new Vector2(1040, 8),
                horizontalAlignment: UIElement_TextBox.HorizontalAlignments.Center);

            DrawOptionBox(game, spriteLayerTitleScreen, fowButtonNone, "No Fog of War",
                "All units are visible everywhere on the map. No units are hidden.",
                newSavedGameData.FogOfWarType == TacticalAILib.MapFogOfWar.FogOfWarTypes.None);

            DrawOptionBox(game, spriteLayerTitleScreen, fowButtonPartial, "Partial Fog of War",
                "All friendly units are visible as well as all enemy units that are directly observable by any friendly unit are displayed",
                newSavedGameData.FogOfWarType == TacticalAILib.MapFogOfWar.FogOfWarTypes.Partial);

            DrawOptionBox(game, spriteLayerTitleScreen, fowButtonComplete, "Complete Fog of War",
                "Only units units directly observable by your army's HQ are fully displayed. ", // +// TODO (noted by MT)
                //"All other unit positions are reported via couriers and the information will be old by the time it arrives.",
                newSavedGameData.FogOfWarType == TacticalAILib.MapFogOfWar.FogOfWarTypes.Complete);
        }

        private void DrawOpponentTypeSelect(Game1 game, UIElement_SpriteLayer spriteLayerTitleScreen)
        {
            menuTitle.Draw(game, spriteLayerTitleScreen, text: "[Opponent|Type]", position: new Vector2(1040, 8),
                horizontalAlignment: UIElement_TextBox.HorizontalAlignments.Center);

            DrawOptionBox(game, spriteLayerTitleScreen, opponentButtonAI, "AI (Coming Soon)",
                "Your opponent will be the TIGER / MATE tactical Artificial Intelligence.",
                newSavedGameData.GameOpponentType == GameOpponentTypes.AI, disabled: true);

            DrawOptionBox(game, spriteLayerTitleScreen, opponentButtonSteamPlayer,
                "Steam Player" + (Networking_Steam.RunWithoutSteam ? " (Running Without Steam)" : ""),
                "Play a human online by the Steam network.",
                newSavedGameData.GameOpponentType == GameOpponentTypes.SteamPlayer, disabled: Networking_Steam.RunWithoutSteam);

            DrawOptionBox(game, spriteLayerTitleScreen, opponentButtonPlayByEmail, "Play By Email",
                "Play a human by email (PBeM).", newSavedGameData.GameOpponentType == GameOpponentTypes.PlayByEmail);

            DrawOptionBox(game, spriteLayerTitleScreen, opponentButtonLocalMultiplayer, "Local Multiplayer",
                "Play a friend using this one device and swap seats when it's your turn.", newSavedGameData.GameOpponentType == GameOpponentTypes.LocalMultiplayer);
        }

        private void DrawSteamPlayerSelect(Game1 game, UIElement_SpriteLayer spriteLayerTitleScreen)
        {
            menuTitle.Draw(game, spriteLayerTitleScreen, text: "[Player]", position: new Vector2(1040, 8),
                horizontalAlignment: UIElement_TextBox.HorizontalAlignments.Center);

            spriteLayerTitleScreen.postEndActions.Add(() =>
            { steamPlayersList.Draw(game, DrawSteamPlayerSelectListItem, 1f, steamPlayersListElements.Count); });
        }

        private void DrawSteamPlayerSelectListItem(Game1 game, SpriteBatch spriteBatch, int index, Vector2 position, bool selected)
        {
            while (steamPlayersListTextBoxes.Count <= index)
                steamPlayersListTextBoxes.Add(new UIElement_TextBox(FontTypes.MainMenuScenarioName)
                { TooltipMode = UIElement_TextBox.TooltipModes.OnOverflow });

            Networking_Steam.NetworkPlayer steamPlayer = steamPlayersListElements[index];
            bool choiceSelected = steamPlayer == steamPlayerSelected;
            UIElement_TextBox textBox = steamPlayersListTextBoxes[index];

            textBox.Text = (steamPlayer.steamID.m_SteamID >= 1) ? steamPlayer.Name : "Public/Open Invitation";
            Color textColor = menuSelectButton.inactiveTextColor;
            int maxWidth = 580;
            if ((selected && steamPlayersList.highlighted) || choiceSelected)
            {
                if (choiceSelected)
                {
                    spriteBatch.Draw(UIStyles.Current.ListSelector, position + new Vector2(9, -3), Color.White);
                    position.X += UIStyles.Current.ListSelector.Width + 8;
                    maxWidth -= UIStyles.Current.ListSelector.Width + 5;
                }

                textColor = Color.Black;
                textBox.Draw(game, spriteBatch, fontType: FontTypes.MainMenuScenarioName,
                    color: Color.Black * 0.2f, position: position + new Vector2(12, 3), maxWidth: maxWidth, maxHeight: 30,
                    autoEllipsis: UIElement_TextBox.AutoEllipsisMode.Word);
            }

            textBox.Draw(game, spriteBatch, fontType: FontTypes.MainMenuScenarioName,
                color: textColor, position: position + new Vector2(11, 2), maxWidth: 580, maxHeight: 30,
                autoEllipsis: UIElement_TextBox.AutoEllipsisMode.Word);
        }

        private void DrawEmailSelect(Game1 game, UIElement_SpriteLayer spriteLayerTitleScreen)
        {
            menuTitle.Draw(game, spriteLayerTitleScreen, text: "[Email]", position: new Vector2(1040, 8),
                horizontalAlignment: UIElement_TextBox.HorizontalAlignments.Center);

            emailButtonYourEmailAddress.Draw(game, spriteLayerTitleScreen, text: "Your Email Address: " +
                (newSavedGameData.LocalPlayerArmy.Email ?? emailDefaultEmailAddress));
            emailButtonOpponentEmailAddress.Draw(game, spriteLayerTitleScreen, text: "Opponent Email Address: " +
                (newSavedGameData.SavedArmies.Find(x => x.Index != newSavedGameData.LocalPlayerIndex).Email ?? emailDefaultEmailAddress));

            DrawOptionBoxSmall(game, spriteLayerTitleScreen, emailButtonSteamVerify, "Steam verification only",
                "Loading the save file requires a Steam account to be logged in to verify the player.",
                newSavedGameData.EmailVerificationType == EmailVerificationTypes.Steam, disabled: Networking_Steam.RunWithoutSteam);

            DrawOptionBoxSmall(game, spriteLayerTitleScreen, emailButtonCustomVerify, "Custom password only",
                "Each player must set a password for the save file. This password must then be entered each time the save file is loaded.",
                newSavedGameData.EmailVerificationType == EmailVerificationTypes.CustomPassword);

            DrawOptionBoxSmall(game, spriteLayerTitleScreen, emailButtonSteamOrCustomVerify, "Steam verification or custom password",
                "When online, Steam is used to verify the player. In addition when offline (or without Steam running) a custom password can be used instead.",
                newSavedGameData.EmailVerificationType == EmailVerificationTypes.SteamOrCustomPassword, disabled: Networking_Steam.RunWithoutSteam);

            DrawOptionBoxSmall(game, spriteLayerTitleScreen, emailButtonNoVerification, "No verification",
                "No player verification at all. This means anyone with access to the save file can load and progress any other player's gameplay!",
                newSavedGameData.EmailVerificationType == EmailVerificationTypes.NoVerification);
        }

        private void DrawContinueSavedGames(Game1 game, UIElement_SpriteLayer spriteLayerTitleScreen)
        {
            menuTitle.Draw(game, spriteLayerTitleScreen, text: "[Continue|a|Saved|Game]", position: new Vector2(1040, 8),
                horizontalAlignment: UIElement_TextBox.HorizontalAlignments.Center);

            continueGamesButtonEmail.Draw(game, spriteLayerTitleScreen);

            spriteLayerTitleScreen.postEndActions.Add(() =>
            { continueGamesList.Draw(game, DrawContinueSavedGamesListItem, 1f, continueGamesListElements.Count); });
        }

        private void DrawContinueSavedGamesListItem(Game1 game, SpriteBatch spriteBatch, int index, Vector2 position, bool selected)
        {
            while (continueGamesListTextBoxes.Count <= index)
                continueGamesListTextBoxes.Add(new UIElement_TextBox(FontTypes.MainMenuScenarioName)
                { TooltipMode = UIElement_TextBox.TooltipModes.OnOverflow });

            SavedGameData savedGameData = continueGamesListElements[index];
            DateTime localTime = savedGameData.LastUpdateTime.ToLocalTime();
            Scenario editorScenario = (savedGameData.EditorAssetSet.EditorScenario.Asset as Scenario);

            bool choiceSelected = savedGameData == continueGameSelected;
            UIElement_TextBox textBox = continueGamesListTextBoxes[index];

            int localArmyIndex = (savedGameData.GameOpponentType != GameOpponentTypes.PlayByEmail) ?
                savedGameData.FindLocalPlayerArmyIndex(displayErrorMessage: false) : 0;
            string localSide = (localArmyIndex == 0) ? "Red" : "Blue";
            int opponentArmyIndex = (localArmyIndex == 0) ? 1 : 0;
            SavedGameArmyData opponentSavedArmy = savedGameData.SavedArmies[opponentArmyIndex];

            textBox.Text = $"{localTime.ToShortDateString()} {localTime.ToShortTimeString()} - " +
                savedGameData.GameProgressState switch
                {
                    GameProgressStates.InProgress => $"Turn {Math.Max(1, savedGameData.GameTurn)} / {editorScenario.NumTurns}",
                    GameProgressStates.NewInvite => "New Invite",
                    GameProgressStates.Completed => "Completed"
                } +
                $"\n{editorScenario.ScenarioName}\n" +
                savedGameData.GameOpponentType switch
                {
                    GameOpponentTypes.AI =>
                    $"Playing as {localSide} army against " + $"AI",
                    GameOpponentTypes.SteamPlayer =>
                    $"Playing as {localSide} army against {((opponentSavedArmy.SteamName?.Length >= 1) ? opponentSavedArmy.SteamName : "???")}",
                    GameOpponentTypes.PlayByEmail =>
                    $"Play by email: {savedGameData.SavedArmies[localArmyIndex].Email ?? emailDefaultEmailAddress} vs " +
                    $"{savedGameData.SavedArmies[opponentArmyIndex].Email ?? emailDefaultEmailAddress}",
                    GameOpponentTypes.LocalMultiplayer =>
                    $"Local multiplayer game"
                };

            Color textColor = menuSelectButton.inactiveTextColor;
            int maxWidth = 580;
            if ((selected && continueGamesList.highlighted) || choiceSelected)
            {
                if (choiceSelected)
                {
                    spriteBatch.Draw(UIStyles.Current.ListSelector, position + new Vector2(9, 22), Color.White);
                    position.X += UIStyles.Current.ListSelector.Width + 8;
                    maxWidth -= UIStyles.Current.ListSelector.Width + 5;
                }

                textColor = Color.Black;
                textBox.Draw(game, spriteBatch, fontType: FontTypes.MainMenuScenarioName,
                    color: Color.Black * 0.2f, position: position + new Vector2(12, 3), maxWidth: maxWidth, maxHeight: 90,
                    autoEllipsis: UIElement_TextBox.AutoEllipsisMode.Character);
            }

            textBox.Draw(game, spriteBatch, fontType: FontTypes.MainMenuScenarioName,
                color: textColor, position: position + new Vector2(11, 2), maxWidth: maxWidth, maxHeight: 90,
                autoEllipsis: UIElement_TextBox.AutoEllipsisMode.Character);
        }

        private void DrawOnlineGames(Game1 game, UIElement_SpriteLayer spriteLayerTitleScreen)
        {
            menuTitle.Draw(game, spriteLayerTitleScreen, text: "[Join|an|Online|Game]", position: new Vector2(1040, 8),
                horizontalAlignment: UIElement_TextBox.HorizontalAlignments.Center);

            spriteLayerTitleScreen.postEndActions.Add(() =>
            { onlineGamesList.Draw(game, DrawOnlineGamesListItem, 1f, onlineGamesListElements.Count); });
        }

        private void DrawOnlineGamesListItem(Game1 game, SpriteBatch spriteBatch, int index, Vector2 position, bool selected)
        {
            while (onlineGamesListTextBoxes.Count <= index)
                onlineGamesListTextBoxes.Add(new UIElement_TextBox(FontTypes.MainMenuScenarioName)
                { TooltipMode = UIElement_TextBox.TooltipModes.OnOverflow });

            SavedGameData savedGameData = onlineGamesListElements[index].savedGameData;
            OnlineGameOriginTypes onlineGameOrigin = onlineGamesListElements[index].onlineGameOriginType;
            DateTime localTime = savedGameData.LastUpdateTime.ToLocalTime();
            Scenario editorScenario = (savedGameData.EditorAssetSet.EditorScenario.Asset as Scenario);

            bool choiceSelected = savedGameData == onlineGameSelected?.savedGameData;
            UIElement_TextBox textBox = onlineGamesListTextBoxes[index];

            int localIndex = savedGameData.FindLocalPlayerArmyIndex(displayErrorMessage: false);
            string localSide = (localIndex == 0) ? "Red" : "Blue";
            int opponentArmyIndex = (localIndex == 0) ? 1 : 0;
            SavedGameArmyData opponentSavedArmy = savedGameData.SavedArmies[opponentArmyIndex];

            textBox.Text = $"{localTime.ToShortDateString()} {localTime.ToShortTimeString()} - " +
                savedGameData.GameProgressState switch
                {
                    GameProgressStates.NewInvite => (onlineGameOrigin == OnlineGameOriginTypes.NewInviteFriend) ?
                        "New Invite" : "Open/Public Invitation",
                    GameProgressStates.InProgress => $"Turn {Math.Max(1, savedGameData.GameTurn)} / {editorScenario.NumTurns}\n" +
                    savedGameData.GetReadyWaitState(localIndex) switch
                    {
                        GameReadyWaitStates.ReadyForSubmitTurn => "New turn available",
                        GameReadyWaitStates.ReadyProceedToNextTurn => "New turn available",
                        GameReadyWaitStates.WaitingForLocalPlayer => "/c[#ff0000ff]WAITING FOR YOUR TURN!/cd",
                        GameReadyWaitStates.WaitingForOpponent => "Waiting on opponent",
                    },
                    GameProgressStates.Completed => "Completed",
                    _ => ""
                } +
                $"\n{editorScenario.ScenarioName}\n" +
                savedGameData.GameOpponentType switch
                {
                    GameOpponentTypes.AI => $"Playing as {localSide} army against " + $"AI",
                    GameOpponentTypes.SteamPlayer => $"Playing against {((opponentSavedArmy.SteamName?.Length >= 1) ? opponentSavedArmy.SteamName : "???")}",
                    GameOpponentTypes.PlayByEmail => $"Playing against {savedGameData.SavedArmies[opponentArmyIndex].Email ?? "???"}",
                    GameOpponentTypes.LocalMultiplayer => $"Local multiplayer game"
                };

            Color textColor = menuSelectButton.inactiveTextColor;
            int maxWidth = 580;
            if ((selected && onlineGamesList.highlighted) || choiceSelected)
            {
                if (choiceSelected)
                {
                    spriteBatch.Draw(UIStyles.Current.ListSelector, position + new Vector2(9, 22), Color.White);
                    position.X += UIStyles.Current.ListSelector.Width + 8;
                    maxWidth -= UIStyles.Current.ListSelector.Width + 5;
                }

                textColor = Color.Black;
                textBox.Draw(game, spriteBatch, fontType: FontTypes.MainMenuScenarioName,
                    color: Color.Black * 0.2f, position: position + new Vector2(12, 3), maxWidth: maxWidth, maxHeight: 120,
                    autoEllipsis: UIElement_TextBox.AutoEllipsisMode.Word);
            }

            textBox.Draw(game, spriteBatch, fontType: FontTypes.MainMenuScenarioName,
                color: textColor, position: position + new Vector2(11, 2), maxWidth: maxWidth, maxHeight: 120,
                autoEllipsis: UIElement_TextBox.AutoEllipsisMode.Word);
        }

        private void DrawOptionBox(Game1 game, UIElement_SpriteLayer spriteBatch, UIElement_Button button,
            string label, string description, bool selected, FontTypes? descriptionFont = null, bool disabled = false)
        {
            button.Draw(game, spriteBatch);

            Vector2 topCenterPosition = button.Position + new Vector2(0, -button.Size.Y / 2f);

            while (optionBoxLabels.Count <= optionBoxIndex)
            {
                optionBoxLabels.Add(new UIElement_TextBox(FontTypes.MainMenuScenarioName));
                optionBoxDescriptions.Add(new UIElement_TextBox(FontTypes.MainMenuScenarioName));
            }

            optionBoxLabels[optionBoxIndex].Draw(game, spriteBatch, text: label,
                position: topCenterPosition + new Vector2(0, 16), color: Color.Black * (disabled ? 0.4f : 1f),
                horizontalAlignment: UIElement_TextBox.HorizontalAlignments.Center);

            optionBoxDescriptions[optionBoxIndex].Draw(game, spriteBatch, fontType: descriptionFont ?? FontTypes.MainMenuScenarioDescription,
                text: description, position: button.Position + new Vector2(0, 40), color: Color.Black * (disabled ? 0.4f : 1f),
                horizontalAlignment: UIElement_TextBox.HorizontalAlignments.Center,
                verticalAlignment: UIElement_TextBox.VerticalAlignments.Center, maxWidth: 600);

            Vector2 dividerPosition = topCenterPosition + new Vector2(-UIStyles.Current.Divider1.Width / 2, 60);
            Color color = Color.White;
            if (button.highlighted)
            {
                spriteBatch.Draw(UIStyles.Current.Divider1, dividerPosition + new Vector2(1), Color.White * 0.15f);
                spriteBatch.Draw(UIStyles.Current.Divider1, dividerPosition + new Vector2(2), Color.White * 0.15f);
            }
            else
            {
                color *= 0.9f;
            }
            spriteBatch.Draw(UIStyles.Current.Divider1, dividerPosition, color);

            if (selected)
            {
                Rectangle rectangle = new((button.Position - (button.Size / 2)).ToPoint(), button.Size.ToPoint());
                rectangle.Inflate(15, 15);
                GameRef.DrawBoxOutline(spriteBatch, rectangle, Color.Black, 3);

            }

            optionBoxIndex++;
        }

        private void DrawOptionBoxSmall(Game1 game, UIElement_SpriteLayer spriteBatch, UIElement_Button button,
            string label, string description, bool selected, bool disabled = false)
        {
            Vector2 topCenterPosition = button.Position + new Vector2(0, -button.Size.Y / 2f);

            while (optionBoxLabels.Count <= optionBoxIndex)
            {
                optionBoxLabels.Add(new UIElement_TextBox(FontTypes.MainMenuScenarioName));
                optionBoxDescriptions.Add(new UIElement_TextBox(FontTypes.MainMenuScenarioName));
            }

            optionBoxLabels[optionBoxIndex].Draw(game, spriteBatch, text: label,
                position: topCenterPosition + new Vector2(0, 10), color: Color.Black * (disabled ? 0.4f : 1f),
                horizontalAlignment: UIElement_TextBox.HorizontalAlignments.Center);

            optionBoxDescriptions[optionBoxIndex].Draw(game, spriteBatch, text: description,
                position: button.Position + new Vector2(0, 12), color: Color.Black * (disabled ? 0.4f : 1f),
                horizontalAlignment: UIElement_TextBox.HorizontalAlignments.Center,
                verticalAlignment: UIElement_TextBox.VerticalAlignments.Center, maxWidth: 600);

            if (selected)
            {
                Rectangle rectangle = new((button.Position - (button.Size / 2)).ToPoint(), button.Size.ToPoint());
                rectangle.Inflate(15, 0);
                GameRef.DrawBoxOutline(spriteBatch, rectangle, Color.Black, 3);

            }

            optionBoxIndex++;
        }
    }
}
