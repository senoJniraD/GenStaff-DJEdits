using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ModelLib;
using TacticalAILib;
using static TacticalAILib.MapPathfinding;
using static TacticalAILib.MATEUnitInstance;
using static TacticalAILib.SpatialAStar;

namespace GSBPGEMG
{
    public partial class Game1 : Game
    {
        #region Enums
        public enum GameStates
        {
            MainMenu,
            LoadingGame,
            Gameplay,
            DebugTool
        }
        public GameStates GameState;
        public GameStates LastGameState;

        public enum GameplayScreens
        {
            Playing,
            EndingTurn,
            WaitingForOpponent,
            FinishedScenario
        }
        public GameplayScreens GameplayScreen;

        #endregion

        #region Variables

        private TabNames _activeTab = TabNames.Scenario; 

        public static Game1 GameRef;
        public static GameTime GameTimeRef;
        public static GraphicsDeviceManager GraphicsDeviceManagerRef;
        public static GraphicsDevice GraphicsDeviceRef;
        public static readonly object DrawingThreadLock = new();

        public SpriteBatch spriteLayerMap;
        public SpriteBatch spriteLayerPaths;
        public SpriteBatch spriteLayerBelowUnits;
        public SpriteBatch spriteLayerUnits;
        public SpriteBatch spriteLayerAboveUnits;
        public UI.UIElement_SpriteLayer spriteLayerTabs;
        public SpriteBatch spriteLayerHeaderMenus;
        public SpriteBatch spriteLayerControls;
        public UI.UIElement_SpriteLayer spriteLayerTitleScreen;
        public UI.UIElement_SpriteLayer spriteLayerTooltips;
        public FontStashSharp.FontSystem fontSystem;
        public RasterizerState RasterizerStateDefault = new();
        public RasterizerState RasterizerStateClipping = new() { ScissorTestEnable = true };

        public int WindowWidth = 1430;
        public int WindowHeight = 839;
        public int ImageBorder = 8;

        public int LeftMapOffset = 271;
        public int TopMapOffset = 30;
        public Vector2 MapOffset => new(LeftMapOffset, TopMapOffset);

        public MainMenu GameState_MainMenu;
        public LoadingScreen GameState_Loading;

        public HeaderMenus HeaderMenus;
        public Tabs Tabs;
        public DisplayBanner DisplayBanner;

        public UIScreenResizer ScreenResizer;

        int flagAnimationFrameNumber;
        public int timeSinceLastFrame { get; private set; }
        int millisecondsPerFlagAnimation = 40;

        public float FireArrowDistance;

        public Texture2D MapTexture;

        public Texture2D fogOfWarTexture;
        public bool fogOfWarCalculating;
        public int fogOfWarTextureInstanceID;
        public int fogOfWarTextureArmyIndex;
        public MATEUnitInstance fogOfWarTextureUnit;
        public EventBase fogOfWarTextureGameEvent;
        public float fogOfWarFadeIn;

        public MATEUnitInstance DisplayUnit; // TODO (noted by MT) move to tabs?
        public bool DisplayUnitInfo;

        public int NearestPlaceIndex;
        public bool DisplayPlaceInfo;

        public Thread loadingThread; // TODO (noted by MT) swap & remove

        public bool ShowAllPlaces;
        public bool ShowRangeOfUnit;
        public bool ShowGrid;
        public bool ShowRuler;
        public bool ShowTerrainVisualizer;

        public List<string> EndGameMessages = new List<string>();

        public GameInstance Instance;

        public string ElevationString = "";
        public string TerrainString = "";

        public float UnitPulsingScale = 0.5f;
        public bool UnitPulsingDirection;

        public double CourierSpeedKmh = 17.5;

        public ProcessTurn ProcessTurn;

        public static Networking_Steam SteamServices { get; set; } = new();
        public static Networking_WebRequests WebServices { get; set; } = new();

        public SavedGameData CurrentSavedGameData = new();
        public SavedGameData UpdatedSavedGameData; // TODO (noted by MT) queue

        #endregion

        #region Main MonoGame Loop

        /* MonoGame Loop (README)
          
            Constructor
                 ↓
             Initialize
                 ↓
            LoadContent
                 ↓
              Update
                ↓ ↑
               Draw
                ↓ ↑
              EndDraw
                 ↓
             OnExiting
        */

        public Game1()
        {
            InitializeGameBeforeGraphicsDevice();
            GameRef = this;
            GraphicsDeviceManagerRef = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = WindowWidth,
                PreferredBackBufferHeight = WindowHeight,
                PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8,
                PreferMultiSampling = true
            };
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d);
        }

        protected override void Initialize()
        {
            base.Initialize();
            GraphicsDeviceRef = GraphicsDevice;
            InitializeGameAfterGraphicsDevice();
        }

        protected override void Update(GameTime gameTime)
        {
            GameTimeRef = gameTime;
            UpdateGame();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            Monitor.Enter(DrawingThreadLock);
            GameTimeRef = gameTime;
            DrawGame();
            base.Draw(gameTime);
        }

        protected override void EndDraw()
        {
            base.EndDraw();
            Monitor.Exit(DrawingThreadLock);
        }

        #endregion

        #region Initialize

        public void InitializeGameBeforeGraphicsDevice()
        {
            // Before GraphicsDevice Created
            string startLogging = Logging.LogsFolderForSession;
            SavedProgramSettings.Load();
        }

        public void InitializeGameAfterGraphicsDevice()
        {
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            Window.Title = "General Staff: Black Powder";
            Window.AllowUserResizing = true;

            Debug.LoadSettings();
            Input.Initialize();
            ScreenResizer = new(this);

            spriteLayerMap = new SpriteBatch(GraphicsDevice);
            spriteLayerPaths = new SpriteBatch(GraphicsDevice);
            spriteLayerBelowUnits = new SpriteBatch(GraphicsDevice);
            spriteLayerUnits = new SpriteBatch(GraphicsDevice);
            spriteLayerAboveUnits = new SpriteBatch(GraphicsDevice);
            spriteLayerTabs = new UI.UIElement_SpriteLayer(GraphicsDevice);
            spriteLayerHeaderMenus = new SpriteBatch(GraphicsDevice);
            spriteLayerControls = new SpriteBatch(GraphicsDevice);
            spriteLayerTitleScreen = new UI.UIElement_SpriteLayer(GraphicsDevice);
            spriteLayerTooltips = new UI.UIElement_SpriteLayer(GraphicsDevice);
            fontSystem = new FontStashSharp.FontSystem();

            LoadContentFiles();
            GenerateRuntimeContent();

            UI.UIStyles.Load(Content, GraphicsDevice);

            HeaderMenus = new(this);
            Tabs = new();
            DisplayBanner = new();

            ProcessTurn = new ProcessTurn(this);

            GameState_MainMenu = new();
            GameState_Loading = new(this);

            GameState = GameStates.MainMenu;
        }

        #endregion

        #region Update Game

        public void UpdateGame()
        {
            GraphicsDeviceManagerRef.HardwareModeSwitch = SavedProgramSettings.HardwareFullScreen;
            if (Input.KeyIsPressed(Microsoft.Xna.Framework.Input.Keys.F11))
                GraphicsDeviceManagerRef.ToggleFullScreen();

            Debug.Update(this, Instance);
            Input.Update(this);
            ScreenResizer.Update(this);
            SavedGamesCollection.Update(this);
            SteamServices.Update(this);

            switch (GameState)
            {
                case GameStates.MainMenu:
                    GameState_MainMenu.Update(this); break;
                case GameStates.LoadingGame:
                    GameState_Loading.Update(CurrentSavedGameData); break;
                case GameStates.Gameplay:
                    UpdateGameplay(); break;
            }

            LastGameState = GameState;
        }

        public void UpdateGameplay()
        {
            ClearAllUnitsPulsing(Instance);

            if ((GameplayScreen != GameplayScreens.EndingTurn) && (GameplayScreen != GameplayScreens.FinishedScenario))
                if (ProcessTurn.LocalTurnTaken(Instance) == false)
                    GameplayScreen = GameplayScreens.Playing;
                else
                    GameplayScreen = GameplayScreens.WaitingForOpponent;



            DisplayBanner.Update(this);
            HeaderMenus.Update(this);

            // Global quick actions
            if (ProcessTurn.LocalEndTurnAvailable(Instance)
                && Input.KeyIsPressed(Keys.Enter)
                && _activeTab == TabNames.EndTurn) 
            {
                GameplayScreen = GameplayScreens.EndingTurn;
                CurrentSavedGameData.LocalPlayerArmy.SetPlannedEvents(Instance);
            }

            
            if (Input.KeyIsPressed(Keys.E))
            {
                Tabs.ChangeTab(TabNames.EndTurn);
                _activeTab = TabNames.EndTurn;
            }
            if (Input.KeyIsPressed(Keys.D))
            {
                Tabs.ChangeTab(TabNames.DirectOrder);
                _activeTab = TabNames.DirectOrder;
            }
            if (Input.KeyIsPressed(Keys.R))
            {
                Tabs.ChangeTab(TabNames.Reports);
                _activeTab = TabNames.Reports;
            }
            if (Input.KeyIsPressed(Keys.S))
            {
                Tabs.ChangeTab(TabNames.Scenario);
                _activeTab = TabNames.Scenario;
            }


            Tabs.Update(this, Instance);
            ProcessTurn.Update(this, Instance, CurrentSavedGameData, Tabs);
            SetAnimationFrames();
        }

        public void Check4EndOfSimulation(GameInstance instance)
        {
            string filename = Path.Combine(Logging.LogsFolderForTurn, "EndScenarioChecklist.txt");

            bool ActiveWins = true;
            bool OpponentWins = true;
            bool MaxTurnsReached = false;

            try
            {
                Logging.SetCurrentMessageType(Logging.MessageType.EndScenarioChecklist);
                using LogsStreamWriter writer = new(filename);

                writer.WriteLine("End of Scenario Check.");
                writer.WriteLine("Scenario: " + Instance.ScenarioName + ". " + Instance.ScenarioDescription);
                writer.WriteLine("Current Game Time: " + Instance.CurrentGameTime.ToString("hh\\:mm") + ". Current Game Turn: " + Instance.CurrentGameTurn.ToString());
                int remainingValue = Instance.ScenarioNumTurns - Instance.CurrentGameTurn;
                writer.WriteLine("Number of turns until scenario end: " + remainingValue.ToString());
                if (remainingValue <= 0)
                    MaxTurnsReached = true;
                writer.WriteLine("________________________________________________________________________________________________");

                for (int i = 0; i < 2; i++)
                {
                    ArmyInstance army = (i == 0) ? Instance.ActiveArmy : Instance.OpponentArmy;
                    ArmyGainsLossesSnapshot armyGainsLossesSnapshot = army.GainsLossesSnapshot;

                    writer.WriteLine($"For {army.Side} to Win:");
                    int writerStartPosition = writer.GetLineCount();

                    remainingValue = armyGainsLossesSnapshot.MyVP2Win - armyGainsLossesSnapshot.MyVPIControl;
                    if (remainingValue > 0)
                        writer.WriteLine("VPs to capture = " + remainingValue);

                    remainingValue = armyGainsLossesSnapshot.numEnemyInfantry2KIA - armyGainsLossesSnapshot.numEnemyKIAInfantry;
                    if (remainingValue > 0)
                        writer.WriteLine("Enemy infantry to capture = " + remainingValue);

                    remainingValue = armyGainsLossesSnapshot.numEnemyLightInfantry2KIA - armyGainsLossesSnapshot.numEnemyKIALightInfantry;
                    if (remainingValue > 0)
                        writer.WriteLine("Enemy light infantry to capture = " + remainingValue);

                    remainingValue = armyGainsLossesSnapshot.numEnemyCavalry2KIA - armyGainsLossesSnapshot.numEnemyKIACavalry;
                    if (remainingValue > 0)
                        writer.WriteLine("Enemy cavalry to capture = " + remainingValue);

                    remainingValue = armyGainsLossesSnapshot.numEnemyLightCavalry2KIA - armyGainsLossesSnapshot.numEnemyKIALightCavalry;
                    if (remainingValue > 0)
                        writer.WriteLine("Enemy light cavalry to capture = " + remainingValue);

                    remainingValue = armyGainsLossesSnapshot.numEnemyArtillery2KIA - armyGainsLossesSnapshot.numEnemyKIAArtillery;
                    if (remainingValue > 0)
                        writer.WriteLine("Enemy artillery to capture = " + remainingValue);

                    remainingValue = armyGainsLossesSnapshot.numEnemyHorseArtillery2KIA - armyGainsLossesSnapshot.numEnemyKIAHorseArtillery;
                    if (remainingValue > 0)
                        writer.WriteLine("Enemy horse artillery to capture = " + remainingValue);

                    remainingValue = armyGainsLossesSnapshot.numEnemySupplies2KIA - armyGainsLossesSnapshot.numEnemyKIASupplies;
                    if (remainingValue > 0)
                        writer.WriteLine("Enemy supplies to capture = " + remainingValue);

                    bool notWonYetAsLinesWritten = writer.GetLineCount() > writerStartPosition;
                    if (notWonYetAsLinesWritten)
                        if (i == 0)
                            ActiveWins = false;
                        else
                            OpponentWins = false;
                }

                if (MaxTurnsReached || ActiveWins || OpponentWins)
                {
                    Instance.CurrentEvents.AddEvent(new Event_GameCompleted());
                    GameplayScreen = GameplayScreens.FinishedScenario;
                    if ((ActiveWins || OpponentWins) && (ActiveWins != OpponentWins))
                    {
                        writer.WriteLine((ActiveWins ? Instance.ActiveArmy.Name : Instance.OpponentArmy.Name) + " has won the scenario.");
                    }
                    else
                    {
                        writer.WriteLine("The Scenario has now ended.");
                        EndGameMessages.Add("A DRAW!"); // need something better
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Write(e, true, "Unable To Write End Scenario Checklist Log",
                    "Unable to write " + filename + "\nIt's probably open in another program");
            }

            if (GameplayScreen == GameplayScreens.FinishedScenario)
            {
                string slug = "";
                string slug2 = "";

                if (ActiveWins)
                {
                    EndGameMessages.Add("VICTORY!");
                    slug = Instance.ActiveArmy.Name + " victorious after fierce fighting at " + Instance.ScenarioName + "!";
                    slug2 = "Enemy casualties reported to be heavy!";
                }
                if (OpponentWins)
                {
                    EndGameMessages.Add("DEFEAT!");
                    slug = Instance.ActiveArmy.Name + " defeated after fierce fighting at " + Instance.ScenarioName + "!";
                    slug2 = "Our casualties reported to be heavy!";
                }
                EndGameMessages.Add(Instance.ScenarioDate);
                EndGameMessages.Add(slug);

                slug = slug2 + " Losses include: "; // TODO (noted by MT) losses of who?

                string SlugPrefix(int value, string group, string unitType)
                { return ConvertNumberToWord(value) + " " + ((value == 1) ? group : ((group == "Battery") ? "Batteries" : group + "s")) + " of " + unitType + ". "; }

                ArmyGainsLossesSnapshot armyGainsLossesSnapshot = Instance.ActiveArmy.GainsLossesSnapshot;
                if (armyGainsLossesSnapshot.numEnemyKIAInfantry > 0)
                    slug += SlugPrefix(armyGainsLossesSnapshot.numEnemyKIAInfantry, "Command", "Infantry");

                if (armyGainsLossesSnapshot.numEnemyKIALightInfantry > 0)
                    slug += SlugPrefix(armyGainsLossesSnapshot.numEnemyKIALightInfantry, "Command", "Light Infantry");

                if (armyGainsLossesSnapshot.numEnemyKIACavalry > 0)
                    slug += SlugPrefix(armyGainsLossesSnapshot.numEnemyKIACavalry, "Command", "Cavalry");

                if (armyGainsLossesSnapshot.numEnemyKIALightCavalry > 0)
                    slug += SlugPrefix(armyGainsLossesSnapshot.numEnemyKIALightCavalry, "Command", "Light Cavalry");

                if (armyGainsLossesSnapshot.numEnemyKIAArtillery > 0)
                    slug += SlugPrefix(armyGainsLossesSnapshot.numEnemyKIAArtillery, "Battery", "Artillery");

                if (armyGainsLossesSnapshot.numEnemyKIAHorseArtillery > 0)
                    slug += SlugPrefix(armyGainsLossesSnapshot.numEnemyKIAHorseArtillery, "Battery", "Horse Artillery");

                if (armyGainsLossesSnapshot.numEnemyKIASupplies > 0)
                    slug += SlugPrefix(armyGainsLossesSnapshot.numEnemyKIASupplies, "Command",
                        "Supply Train" + ((armyGainsLossesSnapshot.numEnemyKIASupplies != 1) ? "s" : ""));

                EndGameMessages.Add(slug);

                slug = "Places captured: ";
                /*
                for (int i = 0; i < ActiveArmy.StrategicAIData.MyVPs.Count; i++)
                {
                    string cleaned = ActiveArmy.StrategicAIData.MyVPs[i].Name.Replace("\n", "").Replace("\r", "");
                    slug += ((i == 0) ? "" : " and ") + cleaned + ((i < ActiveArmy.StrategicAIData.MyVPs.Count - 1) ? "," : "!");
                }
                */
                string cleaned;
                if (armyGainsLossesSnapshot.MyVPs.Count == 1)
                {
                    // Ezra added spaces on carriage returns 06/24/25
                    cleaned = armyGainsLossesSnapshot.MyVPs[0].Name.Replace("\n", " ").Replace("\r", " ");
                    slug += cleaned + "!";
                }
                else
                {
                    for (int i = 0; i < armyGainsLossesSnapshot.MyVPs.Count; i++)
                    {
                        // Ezra added spaces on carriage returns 06/24/25
                        cleaned = armyGainsLossesSnapshot.MyVPs[i].Name.Replace("\n", " ").Replace("\r", " ");
                        if (i < armyGainsLossesSnapshot.MyVPs.Count - 1)
                            slug += cleaned + ", ";
                        else
                            slug += "and " + cleaned;
                    }
                    slug += "!";
                }
                EndGameMessages.Add(slug);

                for (int i = 0; i < EndGameMessages.Count; i++)
                    Logging.Write(EndGameMessages[i]);
            }
        }

        public void SetAnimationFrames()
        {
            // Flag
            timeSinceLastFrame += GameTimeRef.ElapsedGameTime.Milliseconds;
            if (timeSinceLastFrame > millisecondsPerFlagAnimation)
            {
                timeSinceLastFrame -= millisecondsPerFlagAnimation;
                flagAnimationFrameNumber = (flagAnimationFrameNumber + 1) % 6;
            }

            // Unit Pulsing
            UnitPulsingScale += 0.006f * (UnitPulsingDirection ? 1 : -1);
            if (UnitPulsingScale > 1.25f)
                UnitPulsingDirection = false;
            if (UnitPulsingScale < 0.75f)
                UnitPulsingDirection = true;
        }

        #endregion

        #region Army Unit Functions

        public int GetLeadershipCost(MATEUnitInstance unit)
        {
            int combinedLeadership = unit.Leadership;
            if (unit.CommandingUnit != null)
            {
                combinedLeadership += unit.CommandingUnit.Leadership;
                combinedLeadership /= 2;
            }
            double minutes = 1000 / Math.Max(1, combinedLeadership);
            return (int)minutes;
        }

        public void ClearAllUnitsPulsing(GameInstance instance)
        {
            for (int i = 0; i < instance.AllArmyUnits.Count; i++)
            {
                instance.AllArmyUnits[i].IsHighlighted = false;
                instance.AllArmyUnits[i].IsPulsing = false;
                instance.AllArmyUnits[i].IsTempHiddenForGameUI = false;
            }
        }

        #endregion

        #region Order Map Click Functions

        public MATEUnitInstance MouseOverUnitSnapshot(List<MATEUnitInstance> units)
        {
            for (int i = 0; i < units.Count; i++)
            {
                MATEUnitInstance unit = units[i];
                MATEUnitSnapshot snapshot = unit.SnapshotDisplayed;

                float scale = Instance.ScenarioPieceSize * (unit.IsHighlighted ? 1.4f : 1.0f);

                if (snapshot.VisibilityToArmies[Instance.ActiveArmy.Index].SightingKnown == false)
                    continue;

                if (Input.MousePositionWithinArea(Input.MouseCoordinateType.Map, Input.MouseOriginType.Center, snapshot.Location.ToMGPoint(),
                    (unit.SpriteCoordinatesForFormation(snapshot.Formation).Size.ToMGVector2() * scale).ToPoint(), snapshot.Facing))
                    return unit;
            }

            return null;
        }

        public bool IsPlaceUnderMouse()
        {
            for (int i = 0; i < Instance.AllPlaces.Count; i++)
            {
                if (Input.MousePositionWithinArea(Input.MouseCoordinateType.Map, Input.MouseOriginType.Center,
                    Instance.AllPlaces[i].Location.ToMGPoint(), new Point(20)))
                {
                    NearestPlaceIndex = i;
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Courier Functions

        public void CalculateCorpsValuesForTurn(ArmyInstance army)
        {
            CalculateCourierPaths(army);
            army.MapPathfinding.ClearPathfindingCache();
            GameState_Loading.LoadCorpsLists(army.Units, army.CorpsList); // recalc strength
        }

        public void CalculateCourierPaths(ArmyInstance army)
        {
            List<Task> tasks = [];
            for (int i = 1; i < army.Units.Count; i++)
            {
                MATEUnitInstance unit = army.Units[i];
                GameInstance instance = army.GameInstance;
                Task task = Task.Run(() =>
                {
                    if (unit.IsOnBoard)
                    {
                        List<PointI> tPointList = unit.Army.MapPathfinding.GetPathFromA2B(unit.CommandingUnit.Location, unit.Location, WallTypes.None,
                            ((unit.Army == instance.ActiveArmy) ? WallTypes.ROIOpponentArmy : WallTypes.ROIActiveArmy) | WallTypes.Water,
                            AvoidWallCostForCouriers);
                        tPointList ??= UtilityMethods.BresenhamLine(unit.Location, unit.CommandingUnit.Location);

                        if (tPointList != null)
                        {
                            List<PointI> tList = tPointList;
                            unit.CourierRoute = tList;
                        }

                        unit.CourierRouteDistanceHQ = CalculateCourierRouteDistance(unit.CourierRoute);
                    }
                });
                if ((i == 1) || (!MultithreadingEnabled))
                    task.Wait();
                tasks.Add(task);
            }

            while (tasks.Count > 0)
            {
                for (int i = 0; i < tasks.Count; i++)
                {
                    if (tasks[i].IsCompleted == true)
                    {
                        tasks[i].Dispose();
                        tasks.RemoveAt(i);
                        i--;
                    }
                }
                Thread.Sleep(16);
            }

            for (int i = 1; i < army.Units.Count; i++)
            {
                MATEUnitInstance unit = army.Units[i];
                int ordersDelayInMinutes = CalculateCourierTime(unit.CourierRouteDistanceGHQ);
                int leadershipCostInMinutes = GetLeadershipCost(unit);
                unit.OrderDelayTimeFromHQ = ordersDelayInMinutes + leadershipCostInMinutes;
            }
        }

        public int CalculateCourierRouteDistance(List<PointI> courierRoute)
        {
            double distance = 0;

            for (int i = 1; i < courierRoute.Count; i++)
            {
                if ((courierRoute[i - 1].X == courierRoute[i].X) ||
                    (courierRoute[i - 1].Y == courierRoute[i].Y))
                    distance += 1;
                else
                    distance += ModelLib.MathHelper.SquareRoot2Double;
            }

            distance *= Map.MetersPerPixel;
            return (int)distance;
        }

        public int CalculateCourierTime(double distance) // TODO (noted by MT) per node time?
        {
            double CourierSpeedMetersPerMinute = CourierSpeedKmh * 1000 / 60;
            return (int)(distance / CourierSpeedMetersPerMinute);
        }

        public Point FindPointINotOnCourierPath(Rectangle area)
        {
            Point result = new();
            Rectangle drawArea = area;
            drawArea.Location -= MapOffset.ToPoint();

            bool intersect = false;
            for (int i = -1; i < DisplayUnit.SubordinateUnits.Count; i++)
            {
                MATEUnitInstance unit = (i == -1) ? DisplayUnit : DisplayUnit.SubordinateUnits[i];
                foreach (PointI node in unit.CourierRoute)
                {
                    if (drawArea.Contains(node.ToMGPoint()))
                    {
                        intersect = true;
                        break;
                    }
                }
            }
            if (!intersect)
                result = area.Location;

            if (intersect) // TODO (noted by MT) looks broken, plus shouldn't block highlighted objects
            {
                int X1 = Map.Width;
                int X2 = 0;
                int Y1 = Map.Height;
                int Y2 = 0;

                foreach (PointI node in DisplayUnit.CourierRoute)
                {
                    if (node.X < X1)
                        X1 = node.X;
                    if (node.X > X2)
                        X2 = node.X;
                    if (node.Y < Y1)
                        Y1 = node.Y;
                    if (node.Y > Y2)
                        Y2 = node.Y;
                }

                for (int i = 0; i < DisplayUnit.SubordinateUnits.Count; i++)
                {
                    foreach (PointI node in DisplayUnit.SubordinateUnits[i].CourierRoute)
                    {
                        if (node.X < X1)
                            X1 = node.X;
                        if (node.X > X2)
                            X2 = node.X;
                        if (node.Y < Y1)
                            Y1 = node.Y;
                        if (node.Y > Y2)
                            Y2 = node.Y;
                    }
                }

                X1 += LeftMapOffset;
                X2 += LeftMapOffset;
                Y1 += TopMapOffset;
                Y2 += TopMapOffset;

                result.X = (X2 + X1) / 2;

                if (result.X + area.Width > Map.Width + LeftMapOffset)
                    result.X = X1 - area.Width - 10;

                if (result.X - area.Width < 4)
                    result.X = X2 + area.Width + 10;

                if (Y1 < Y2)
                    result.Y = Y1 - area.Height * 2 - 10;
                else
                    result.Y = Y2 - area.Height * 2 - 10;

                if (result.Y < TopMapOffset)
                {
                    if (Y1 < Y2)
                        result.Y = Y2 + 10;
                    else
                        result.Y = Y1 + 10;
                }
            }

            int mapEdgeGap = 10;
            result.X = Math.Clamp(result.X, LeftMapOffset + mapEdgeGap, LeftMapOffset + Map.Width - area.Width - mapEdgeGap);
            result.Y = Math.Clamp(result.Y, TopMapOffset + mapEdgeGap, TopMapOffset + Map.Height - area.Height - mapEdgeGap);
            return result;
        }

        #endregion

        #region Text Functions

        public string[] JustifyTextStrings(SpriteFont font, string text, float maxLineWidth)
        {
            if (text == null)
                return null;
            string[] words = text.Split(' ', '-');
            StringBuilder sb = new StringBuilder();
            float lineWidth = 0f;
            float spaceWidth = font.MeasureString(" ").X;

            foreach (string word in words)
            {
                Vector2 size = font.MeasureString(word);

                if (word.Contains("\r"))
                {
                    lineWidth = 0f;
                    sb.Append("\r \r");
                }

                if (lineWidth + size.X < maxLineWidth)
                {
                    sb.Append(word + " ");
                    lineWidth += size.X + spaceWidth;
                }

                else
                {
                    if (size.X > maxLineWidth)
                    {
                        if (sb.ToString() == " ")
                        {
                            sb.Append(WrapText(font, word.Insert(word.Length / 2, " ") + " ", maxLineWidth));

                        }
                        else
                        {
                            sb.Append("\n" + WrapText(font, word.Insert(word.Length / 2, " ") + " ", maxLineWidth));

                        }
                    }
                    else
                    {

                        sb.Append("\n" + word + " ");
                        lineWidth = size.X + spaceWidth;

                    }
                }
            }
            string tstring = sb.ToString();
            string[] rString = tstring.Split("\n");

            return rString;
        }

        public string WrapText(SpriteFont font, string text, float maxLineWidth)
        {
            string[] words = text.Split(' ');
            StringBuilder sb = new StringBuilder();
            float lineWidth = 0f;
            float spaceWidth = font.MeasureString(" ").X;

            foreach (string word in words)
            {
                Vector2 size = font.MeasureString(word);

                if (word.Contains("\r"))
                {
                    lineWidth = 0f;
                    sb.Append("\r \r");
                }

                if (lineWidth + size.X < maxLineWidth)
                {
                    sb.Append(word + " ");
                    lineWidth += size.X + spaceWidth;
                }
                else
                {
                    if (size.X > maxLineWidth)
                    {
                        if (sb.ToString() != " ")
                            sb.Append("\n");
                        sb.Append(WrapText(font, word.Insert(word.Length / 2, " ") + " ", maxLineWidth));
                    }
                    else
                    {
                        sb.Append("\n" + word + " ");
                        lineWidth = size.X + spaceWidth;
                    }
                }
            }

            return sb.ToString();
        }

        public string ConvertNumberToWord(int value)
        {
            switch (value)
            {
                case 0: return "Zero";
                case 1: return "One";
                case 2: return "Two";
                case 3: return "Three";
                case 4: return "Four";
                case 5: return "Five";
                case 6: return "Six";
                case 7: return "Seven";
                case 8: return "Eight";
                case 9: return "Nine";
                case 10: return "Ten";
                case 11: return "Eleven";
                default: return "";
            }
        }

        public string TruncateToFirstWord(string text) // TODO (noted by MT) UIElement_TextBox AutoEllipsisMode
        {
            for (int i = 0; i < text.Length; i++)
                if (char.IsWhiteSpace(text[i]))
                    return text.Remove(i);
            return text;
        }

        #endregion

        #region Logging Functions

        public void LoggingWriteUnitCommands()
        {
            string filename = Path.Combine(Logging.LogsFolderForTurn, "UnitCommands.txt");

            try
            {
                Logging.SetCurrentMessageType(Logging.MessageType.UnitCommands);
                using LogsStreamWriter writer = new LogsStreamWriter(filename);

                writer.WriteLine("Analysis Date: " + DateTime.Now.ToString());
                writer.WriteLine("Current Game Time: " + Instance.CurrentGameTime.ToString("hh\\:mm") + ". Current Game Turn: " + Instance.CurrentGameTurn.ToString());
                writer.WriteLine("Scenario: " + Instance.ScenarioName + ". " + Instance.ScenarioDescription);
                writer.WriteLine("Unit Orders Dump");
                writer.WriteLine("________________________________________________________________________________________________");

                for (int i = 0; i < 2; i++)
                {
                    ArmyInstance army = (i == 0) ? Instance.ActiveArmy : Instance.OpponentArmy;

                    writer.WriteLine($"\n{army.Side} Units:");

                    for (int j = 0; j < army.Units.Count; j++)
                    {
                        MATEUnitInstance unit = army.Units[j];

                        string slug = j.ToString("00") + " | ";
                        slug += unit.Orders.Count.ToString();
                        slug += " | " + unit.Name + " " + unit.UnitTypeName;

                        if (unit.Orders.Current != null)
                        {
                            Order command = unit.Orders.Current;
                            slug += " | " + command.ExecuteOrderTime.ToString("hh\\:mm");
                            if (command.OrderObjective.Type == OrderObjective.ObjectiveTypes.Unit)
                                slug += " | " + command.OrderObjective.Unit.Name + " ";
                            else if (command.OrderObjective.Type == OrderObjective.ObjectiveTypes.Place)
                                slug += " | " + UtilityMethods.StripReturn(command.OrderObjective.Place.Name);
                            else if (command.PathToObjective?.Count > 0)
                                slug += " | " + command.PathToObjective[^1].X.ToString() + "/" + command.PathToObjective[^1].Y.ToString();
                            slug += " | " + command.PathToObjective?.Count.ToString();
                            slug += " | " + command.OrderFormation.ToString();
                        }

                        writer.WriteLine(slug);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Write(e, true, "Unable To Write Unit Commands Log",
                    "Unable to write " + filename + "\nIt's probably open in another program");
            }
        }

        #endregion

        #region Draw Game

        public void DrawGame()
        {
            Debug.DrawFrameRate();
            switch (GameState)
            {
                case GameStates.MainMenu:
                    GameState_MainMenu.Draw(this); break;
                case GameStates.LoadingGame:
                    GameState_Loading.Draw(this); break;
                case GameStates.Gameplay:
                    DrawGameplay(); break;
                case GameStates.DebugTool:
                    Debug.DrawToolFullScreen(this); break;
            }
        }

        public void DrawGameplay()
        {
            Matrix zoomMatrix = Matrix.CreateTranslation(new Vector3(-MapOffset.X + 4, -MapOffset.Y + 4, 0)) *
                Matrix.CreateScale(1.0f) *
                Matrix.CreateTranslation(new Vector3(MapOffset.X - 4, MapOffset.Y - 4, 0));

            spriteLayerMap.Begin(rasterizerState: RasterizerStateDefault, samplerState: SamplerState.LinearWrap, transformMatrix: zoomMatrix);
            spriteLayerPaths.Begin(rasterizerState: RasterizerStateClipping, transformMatrix: zoomMatrix);
            spriteLayerBelowUnits.Begin(rasterizerState: RasterizerStateClipping, transformMatrix: zoomMatrix);
            spriteLayerUnits.Begin(rasterizerState: RasterizerStateClipping, transformMatrix: zoomMatrix);
            spriteLayerAboveUnits.Begin(rasterizerState: RasterizerStateClipping, transformMatrix: zoomMatrix);
            spriteLayerTabs.Begin(rasterizerState: RasterizerStateDefault);
            spriteLayerHeaderMenus.Begin(rasterizerState: RasterizerStateDefault);
            spriteLayerControls.Begin(rasterizerState: RasterizerStateDefault);
            spriteLayerTitleScreen.Begin(rasterizerState: RasterizerStateDefault);
            spriteLayerTooltips.Begin(rasterizerState: RasterizerStateDefault);

            HeaderMenus.Draw(this, spriteLayerHeaderMenus);
            Tabs.Draw(this, Instance);

            Input.Draw(this);
            Debug.DrawOverlay(this, Instance);

            UI.UIElement_PathRenderer.BeginDraw();
            DrawMapBackground();
            DrawOnMap();

            if ((GameplayScreen != GameplayScreens.FinishedScenario) ||
                (Tabs.TabSelected == TabNames.Reports && Tabs.TabReports.EventSelected?.EventType != EventTypes.GameCompleted))
                DisplayBanner.Draw(this);
            else
                DrawFinishedScenario();

            UI.UIElement_SpriteLayer.PreDrawActions();

            ScreenResizer.BeginDraw();

            Rectangle originalScissorRectangle = GraphicsDeviceRef.ScissorRectangle;
            GraphicsDeviceRef.ScissorRectangle = new Rectangle(LeftMapOffset, TopMapOffset, Map.Width, Map.Height);

            spriteLayerMap.End();
            spriteLayerPaths.End();
            spriteLayerBelowUnits.End();
            spriteLayerUnits.End();
            spriteLayerAboveUnits.End();

            GraphicsDeviceRef.ScissorRectangle = originalScissorRectangle;

            spriteLayerTabs.End();
            spriteLayerHeaderMenus.End();
            spriteLayerControls.End();
            spriteLayerTitleScreen.End();
            spriteLayerTooltips.End();

            ScreenResizer.EndDraw(OldBackground);
        }

        public void DrawFinishedScenario()
        {
            if (GameplayScreen != GameplayScreens.FinishedScenario)
                return;

            Rectangle r;
            int LeftTabOffset = 268;
            string[] slug;

            r = new Rectangle(LeftMapOffset, TopMapOffset, Map.Width, Map.Height);
            spriteLayerAboveUnits.Draw(VictorianEndScreenBackground, r, Color.White);

            var Headline = EndGameMessages.FirstOrDefault();

            r = new Rectangle(LeftMapOffset + (Map.Width / 2) - 300 - 220, TopMapOffset + 4, 220, 154);
            spriteLayerAboveUnits.Draw(VictorianCannonLeft, r, Color.White);

            r = new Rectangle(LeftMapOffset + (Map.Width / 2) + 300, TopMapOffset + 4, 220, 154);
            spriteLayerAboveUnits.Draw(VictorianCannonRight, r, Color.White);

            Vector2 width = VictorianEndScreenSplashFont.MeasureString(Headline);
            spriteLayerAboveUnits.DrawString(VictorianEndScreenSplashFont, Headline, new Vector2(LeftTabOffset + (Map.Width / 2) - (width.X / 2), 60), Color.Black);

            r = new Rectangle(LeftMapOffset + (Map.Width / 2) - (1052 / 2), 190, 1052, 22);
            spriteLayerAboveUnits.Draw(VictorianRule, r, Color.White);

            width = Baskerville.MeasureString(EndGameMessages[1]);
            spriteLayerAboveUnits.DrawString(Baskerville, EndGameMessages[1], new Vector2(LeftTabOffset + (Map.Width / 2) - (width.X / 2), 206), Color.Black);

            r = new Rectangle(LeftMapOffset + (Map.Width / 2) - (1052 / 2), 238, 1052, 22);
            spriteLayerAboveUnits.Draw(VictorianRule, r, Color.White);

            slug = JustifyTextStrings(CWBookRegular, EndGameMessages[2], 1075);
            
            // Ezra added a little bit of space at top
            int runningY = 252;

            for (int i = 0; i < slug.Length; i++)
            {
                width = CWBookRegular.MeasureString(slug[i]);
                spriteLayerAboveUnits.DrawString(CWBookRegular, slug[i], new Vector2(LeftTabOffset + (Map.Width / 2) - (width.X / 2) + 15, runningY), Color.Black);
                
                // Ezra tightened up leading from 72
                runningY += 52;
            }

            slug = JustifyTextStrings(Baskerville, EndGameMessages[3], 1075);
            runningY += 6;

            for (int i = 0; i < slug.Length; i++)
            {
                width = Baskerville.MeasureString(slug[i]);
                spriteLayerAboveUnits.DrawString(Baskerville, slug[i], new Vector2(LeftTabOffset + (Map.Width / 2) - (width.X / 2) + 15, runningY), Color.Black);
                runningY += 40;
            }

            slug = JustifyTextStrings(Baskerville, EndGameMessages[4], 1075);

            for (int i = 0; i < slug.Length; i++)
            {
                width = Baskerville.MeasureString(slug[i]);
                spriteLayerAboveUnits.DrawString(Baskerville, slug[i], new Vector2(LeftTabOffset + (Map.Width / 2) - (width.X / 2) + 15, runningY), Color.Black);
                runningY += 40;
            }
        }

        #endregion

        #region Draw Map

        public void DrawMapBackground()
        {
            DrawBoxOutline(spriteLayerMap, new Rectangle(0, 0, WindowWidth, WindowHeight - 1), Color.Black, 1);
            Rectangle r = new Rectangle(WindowWidth - Map.Width - ImageBorder, WindowHeight - Map.Height - ImageBorder, MapFrame.Width, MapFrame.Height);
            spriteLayerMap.Draw(MapFrame, r, Color.White);

            if (MapTexture == null)
            {
                byte[] bytes = CurrentSavedGameData.EditorAssetSet.EditorMapData.Asset as byte[];
                if (bytes != null)
                {
                    using MemoryStream stream = new MemoryStream(bytes);
                    MapTexture = Texture2D.FromStream(GraphicsDeviceRef, stream);
                }
            }

            r = new Rectangle(WindowWidth - Map.Width - (ImageBorder / 2), WindowHeight - Map.Height - (ImageBorder / 2), Map.Width, Map.Height);
            if (MapTexture != null)
                spriteLayerMap.Draw(MapTexture, r, Color.White);
        }

        public void DrawOnMap()
        {
            if (ShowGrid)
                DrawOnMapGrid();

            DrawOnMapFogOfWar();

            if (DisplayUnitInfo && (DisplayUnit?.Army == Instance.ActiveArmy))
                DrawOnMapCourierPaths(DisplayUnit);

            UI.UIElement_PathRenderer.EndDraw(this);

            DrawOnMapAllUnits();
            DrawOnMapAllPlaces();

            if (DisplayPlaceInfo)
            {
                PointI point = Instance.AllPlaces[NearestPlaceIndex].Location + MapOffset.ToPointI();
                point.X = Math.Clamp(point.X, LeftMapOffset + 113, WindowWidth - 130);
                point.Y = Math.Clamp(point.Y, TopMapOffset + 42, WindowHeight - 48);

                Rectangle r = new Rectangle(point.X - 113 + 6, point.Y + 6 - 42, 226, 85);
                spriteLayerAboveUnits.Draw(EnemyObservedBoxDropShadow, r, Color.White * 0.5f);

                r = new Rectangle(point.X - 113, point.Y - 42, 226, 85);
                PlaceInstance place = Instance.AllPlaces[NearestPlaceIndex];
                if (place.Side == Sides.Neutral)
                    spriteLayerAboveUnits.Draw(VictorianPlaceNameInfoBoxGreen, r, Color.White);
                else if (place.Side == Sides.Red)
                    spriteLayerAboveUnits.Draw(VictorianPlaceNameInfoBoxRed, r, Color.White);
                else if (place.Side == Sides.Blue)
                    spriteLayerAboveUnits.Draw(VictorianPlaceNameInfoBoxBlue, r, Color.White);

                // Positioning the text inside the box
                string tString = place.Name;
                Vector2 width;

                string[] mString = JustifyTextStrings(UnitInfoNameFont, tString, 120);
                int leading = 0;
                int MaxLines = 2;

                if (mString.Length < 2)
                    MaxLines = mString.Length;

                int YStart = 8;
                if (MaxLines == 2)
                    YStart = 4;
                else if (MaxLines == 1)
                    YStart = 8;

                for (int i = 0; i < MaxLines; i++)
                {
                    width = UnitInfoNameFont.MeasureString(mString[i]);
                    spriteLayerAboveUnits.DrawString(UnitInfoNameFont, mString[i],
                        new Vector2(point.X + 46 - (width.X / 2), point.Y - (width.Y / 2) + YStart + leading - 30), Color.Black);
                    leading += 20;
                }

                YStart = 22 + point.Y;
                int XStart = 40 + point.X;
                tString = place.CaptureValue.ToString() + " Points";
                width = VictorianPlaceNameValueFont.MeasureString(tString);
                spriteLayerAboveUnits.DrawString(VictorianPlaceNameValueFont, tString, new Vector2(XStart - (int)(width.X / 2), YStart - (int)(width.Y / 2)), Color.Black);
            }
        }

        public void DrawOnMapAllUnits()
        {
            MATEUnitInstance mouseOverEnemyUnit = MouseOverUnitSnapshot(Instance.OpponentArmy.Units);

            for (int i = 0; i < Instance.AllArmyUnits.Count; i++)
            {
                MATEUnitInstance unit = Instance.AllArmyUnits[i];
                MATEUnitSnapshot unitSnapshot = unit.SnapshotDisplayed;
                VisibilityState unitVisibilityState = unitSnapshot.VisibilityToArmies[Instance.ActiveArmy.Index];
                unit.IsHighlighted = false;

                int turnDisplayed = Instance.CurrentGameTurn;
                if ((Tabs.TabSelected == TabNames.Reports) && (Tabs.TabReports.EventSelected != null))
                    turnDisplayed = Tabs.TabReports.EventSelected.Turn;

                if (unitSnapshot.IsDead || (unit.EntersOnTurn > turnDisplayed) ||
                    (unitVisibilityState.SightingKnown == false) || unit.IsTempHiddenForGameUI)
                    continue;

                if (unit == DisplayUnit?.GetFromGameInstance(Instance))
                    unit.IsHighlighted = true;

                if ((unit.Army == Instance.OpponentArmy) && (unit == mouseOverEnemyUnit) &&
                    Tabs.TabDirectOrders.State != TabDirectOrders.TabStates.SelectingUnitFormation)
                    unit.IsHighlighted = true;
                if ((Tabs.TabSelected == TabNames.EndTurn) && (GameplayScreen != GameplayScreens.EndingTurn) &&
                    (unit.Army == Instance.ActiveArmy) && (unit.Orders.Count == 0) && (unit.Stance != Stances.Routing))
                    unit.IsHighlighted = true;

                if (Instance.AllArmyUnits[i].IsHighlighted == false)
                    DrawOnMapUnitSnapshot(unitSnapshot, false, false);
            }

            for (int i = 0; i < Instance.AllArmyUnits.Count; i++)
                if (Instance.AllArmyUnits[i].IsHighlighted && Instance.AllArmyUnits[i].IsPulsing == false)
                    DrawOnMapUnitSnapshot(Instance.AllArmyUnits[i].SnapshotDisplayed, true, false);

            for (int i = 0; i < Instance.AllArmyUnits.Count; i++)
                if (Instance.AllArmyUnits[i].IsHighlighted && Instance.AllArmyUnits[i].IsPulsing)
                    DrawOnMapUnitSnapshot(Instance.AllArmyUnits[i].SnapshotDisplayed, true, true);
        }

        public void DrawOnMapUnitInfo(MATEUnitInstance unit)
        {
            if (CurrentSavedGameData.GameModeType == SavedGameData.GameModeTypes.Game)
                return;

            MATEUnitSnapshot unitSnapshot = unit.SnapshotDisplayed;
            MATEUnitSnapshot unitSnapshotForDisplayedChanges = unit.SnapshotForDisplayedChanges;

            Point point = MapOffset.ToPoint() + unitSnapshot.Location.ToMGPoint() + new Point(20);
            Vector2 width;

            if (unit.Army == Instance.ActiveArmy) // looking at friendly
            {
                unit.IsPulsing = true;

                if (ShowRangeOfUnit)
                    DrawOnMapRangeOfUnit(unit);

                Color thermometerHigh = new(0, 193, 0);
                Color thermometerMid = new(255, 255, 0);
                Color thermometerLow = new(255, 94, 94);

                if (unit.UnitType == UnitTypes.HeadQuarters)
                {
                    // HQ Info
                    Rectangle r = new(point.X, point.Y, 226, 88);
                    point = FindPointINotOnCourierPath(r);
                    r.Location = point;

                    spriteLayerAboveUnits.Draw(EnemyObservedBoxDropShadow, point.ToVector2() + new Vector2(6), Color.White * 0.5f);
                    spriteLayerAboveUnits.Draw((unit.Side == Sides.Blue) ? BlueVictorianHQInfoBox : RedVictorianHQInfoBox, point.ToVector2(), Color.White);

                    // Positioning the text inside the box
                    string tString = unit.Name;
                    string[] mString = JustifyTextStrings(UnitInfoNameFont, tString, 125);

                    int leading = 0;
                    int MaxLines = 2;

                    if (mString.Length < 3)
                        MaxLines = mString.Length;

                    int YStart = 5;
                    if (MaxLines == 2)
                        YStart = 17;
                    else if (MaxLines == 1)
                        YStart = 30;

                    for (int i = 0; i < MaxLines; i++)
                    {
                        width = UnitInfoNameFont.MeasureString(mString[i]);
                        spriteLayerAboveUnits.DrawString(UnitInfoNameFont, mString[i],
                            point.ToVector2() + new Vector2(156 - (width.X / 2), -(width.Y / 2) + YStart + leading), Color.Black);
                        leading += 25;
                    }

                    double percent = (double)unitSnapshot.Unit.Leadership * 0.01f;
                    int thermoLength = (int)(percent * 160);
                    Color thermometerColor = thermometerLow;
                    if (percent > 0.50f)
                        thermometerColor = thermometerHigh;
                    else if (percent > 0.25f)
                        thermometerColor = thermometerMid;

                    r = new(point.X + 52, point.Y + 65, thermoLength, 13);
                    spriteLayerAboveUnits.Draw(Thermometer, r, thermometerColor);

                    spriteLayerAboveUnits.DrawString(SmallSmythe, unitSnapshot.Unit.Leadership.ToString() + "%",
                        point.ToVector2() + new Vector2(110, 61), Color.Black);
                }
                else
                {
                    // Unit Info
                    bool strengthChanged = unitSnapshot.Strength != unitSnapshotForDisplayedChanges.Strength;
                    bool ammunitionChanged = unitSnapshot.Ammunition != unitSnapshotForDisplayedChanges.Ammunition;
                    bool extraChangesLine = strengthChanged || ammunitionChanged;
                    Texture2D boxTexture = !extraChangesLine ? UnitInfoBox : UnitInfoBoxLarge;

                    Rectangle r = new(point.X, point.Y, boxTexture.Width, boxTexture.Height);
                    point = FindPointINotOnCourierPath(r);
                    r.Location = point;

                    spriteLayerAboveUnits.Draw(boxTexture, point.ToVector2() + new Vector2(6), Color.Black * 0.5f);
                    spriteLayerAboveUnits.Draw(boxTexture, point.ToVector2(), Color.White);

                    // Positioning the text inside the box
                    string tString = unit.Name;
                    if (unit.Orders[unitSnapshot.OrdersCurrentIndex] != null)
                        tString += " [" + unit.Orders[unitSnapshot.OrdersCurrentIndex].Stance.ToString() + "]";
                    string[] mString = JustifyTextStrings(UnitInfoNameFont, tString, 190);
                    int leading = 0;

                    int YStart;
                    if (mString.Length > 2)
                        YStart = 5;
                    else if (mString.Length == 1)
                        YStart = 35;
                    else
                        YStart = 20;

                    for (int i = 0; i < mString.Length; i++)
                    {
                        width = UnitInfoNameFont.MeasureString(mString[i]);
                        spriteLayerAboveUnits.DrawString(UnitInfoNameFont, mString[i],
                            point.ToVector2() + new Vector2(107 - (width.X / 2), -(width.Y / 2) + leading + YStart), Color.Black);
                        leading += 25;
                    }

                    string valueText;
                    Color valueColor;
                    (string, Color) GetValueText(int value, int lastValue, bool prefixValue = true, string suffixText = "")
                    {
                        string valueText = (prefixValue) ? value + suffixText : "";
                        Color valueColor = Color.Black;
                        int change = value - lastValue;
                        if (change != 0)
                        {
                            valueText += $"{(prefixValue ? " ": "")}({((change > 0) ? "+" : "-")}{Math.Abs(change)}{suffixText})";
                            valueColor = (change > 0) ? new(0, 80, 0) : new(80, 0, 0);
                        }
                        return (valueText, valueColor);
                    }

                    // Unit strength
                    string strengthText = unitSnapshot.Strength.ToString();
                    (valueText, valueColor) = GetValueText(unitSnapshot.Strength, unitSnapshotForDisplayedChanges.Strength, prefixValue: false);                    
                    Vector2 strengthTextSize = Rudyard36.MeasureString(strengthText);
                    spriteLayerAboveUnits.DrawString(Rudyard36, strengthText, point.ToVector2() + new Vector2(55 - (strengthTextSize.X / 2), 54), valueColor);

                    if (extraChangesLine)
                    {
                        if (!strengthChanged)
                            valueText = "-";
                        width = Rudyard36.MeasureString(valueText);
                        spriteLayerAboveUnits.DrawString(Rudyard36, valueText, point.ToVector2() + new Vector2(55 - (width.X / 4), (strengthTextSize.Y) + 56),
                            valueColor, scaleFloat: 0.5f);
                    }

                    // Ammunition
                    string ammunitionText = unitSnapshot.Ammunition.ToString();
                    (valueText, valueColor) = GetValueText(unitSnapshot.Ammunition, unitSnapshotForDisplayedChanges.Ammunition, prefixValue: false);
                    Vector2 ammunitionTextSize = Rudyard36.MeasureString(ammunitionText);
                    spriteLayerAboveUnits.DrawString(Rudyard36, ammunitionText, point.ToVector2() + new Vector2(156 - (ammunitionTextSize.X / 2), 54), valueColor);

                    if (extraChangesLine)
                    {
                        if (!ammunitionChanged)
                            valueText = "-";
                        width = Rudyard36.MeasureString(valueText);
                        spriteLayerAboveUnits.DrawString(Rudyard36, valueText, point.ToVector2() + new Vector2(156 - (width.X / 4), (ammunitionTextSize.Y) + 56),
                                valueColor, scaleFloat: 0.5f);
                        point.Y += 22;
                    }

                    // Leadership
                    double percent = (double)unitSnapshot.Unit.Leadership * 0.01f;
                    int thermoLength = (int)(percent * 180);
                    Color thermometerColor = thermometerLow;
                    if (percent > 0.75f)
                        thermometerColor = thermometerHigh;
                    else if (percent > 0.50f)
                        thermometerColor = thermometerMid;

                    r = new(point.X + 15, point.Y + 79 + 50, thermoLength, 16);
                    spriteLayerAboveUnits.Draw(Thermometer, r, thermometerColor);

                    valueText = unitSnapshot.Unit.Leadership + "%";
                    spriteLayerAboveUnits.DrawString(Smythe16, valueText, point.ToVector2() + new Vector2(95, 130), Color.Black);

                    // Quality
                    percent = (double)unitSnapshot.Unit.Quality * 0.01f;
                    thermoLength = (int)(percent * 180);
                    thermometerColor = thermometerLow;
                    if (percent > 0.75f)
                        thermometerColor = thermometerHigh;
                    else if (percent > 0.50f)
                        thermometerColor = thermometerMid;

                    r = new(point.X + 15, point.Y + 171, thermoLength, 16);
                    spriteLayerAboveUnits.Draw(Thermometer, r, thermometerColor);

                    valueText = unitSnapshot.Unit.Quality + "%";
                    spriteLayerAboveUnits.DrawString(Smythe16, valueText,
                        point.ToVector2() + new Vector2(102 - (int)(Smythe16.MeasureString(valueText).X / 2), 172), Color.Black);

                    // Morale
                    if (unitSnapshot.Stance != Stances.Routing)
                    {
                        percent = (double)unitSnapshot.Morale * 0.01f;
                        thermoLength = (int)(percent * 180);
                        thermometerColor = thermometerLow;
                        if (percent > 0.75f)
                            thermometerColor = thermometerHigh;
                        else if (percent > 0.50f)
                            thermometerColor = thermometerMid;

                        r = new(point.X + 15, point.Y + 212, thermoLength, 16);
                        spriteLayerAboveUnits.Draw(Thermometer, r, thermometerColor);

                        (valueText, valueColor) = GetValueText(unitSnapshot.Morale, unitSnapshotForDisplayedChanges.Morale, suffixText: "%");
                        spriteLayerAboveUnits.DrawString(Smythe16, valueText,
                            point.ToVector2() + new Vector2(102 - (int)(Smythe16.MeasureString(valueText).X / 2), 213), valueColor);
                    }
                    else
                    {
                        // Routing
                        r = new(point.X + 15, point.Y + 212, 180, 16);
                        spriteLayerAboveUnits.Draw(Thermometer, r, thermometerLow);

                        tString = "UNIT ROUTING!";
                        width = Smythe16.MeasureString(tString);
                        spriteLayerAboveUnits.DrawString(Smythe16, tString, point.ToVector2() + new Vector2(100 - (width.X / 2), 213), Color.Black);
                    }
                }
            }
            else
            {
                // Enemy Unit Info
                Rectangle r = new(point.X, point.Y, 226, 85);
                point.X = Math.Clamp(r.X, LeftMapOffset + 10, LeftMapOffset + Map.Width - r.Width - 10);
                point.Y = Math.Clamp(r.Y, TopMapOffset + 10, TopMapOffset + Map.Height - r.Height - 10);
                r.Location = point;

                spriteLayerAboveUnits.Draw(EnemyObservedBoxDropShadow, point.ToVector2() + new Vector2(6), Color.White * 0.5f);
                spriteLayerAboveUnits.Draw((unit.Side == Sides.Blue) ? EnemyObservedBoxBlue : EnemyObservedBoxRed, point.ToVector2(), Color.White);

                VisibilityState visibilityState = unitSnapshot.VisibilityToArmies[Instance.ActiveArmy.Index];
                TimeSpan lastObservedTime = visibilityState.IsVisible  ? Tabs.CurrentGameEventDisplayed.Time : visibilityState.ChangeTime;

                // Positioning text in box
                string tString = "Enemy " + unit.UnitTypeName + " last observed at " +
                    lastObservedTime.ToString("h\\:mm") + ". " + " Strength: " + unit.SnapshotDisplayed.Strength.ToString("N0");

                string[] mString = JustifyTextStrings(ObservedEnemyFont, tString, 135);

                int YStart = 19;
                if (mString.Length > 3)
                    YStart = 10;

                for (int i = 0; i < mString.Length; i++)
                {
                    width = ObservedEnemyFont.MeasureString(mString[i]);
                    spriteLayerAboveUnits.DrawString(ObservedEnemyFont, mString[i],
                        point.ToVector2() + new Vector2(150 - (width.X / 2),  YStart + i * 17), Color.Black);
                   
                }
            }
        }

        public void DrawOnMapUnitSnapshot(MATEUnitSnapshot unitSnapshot, bool highlighted, bool pulsing, bool checkDisplayUnitCollision = false)
        {
            float opacity = 1f;
            VisibilityState visibilityState = unitSnapshot.VisibilityToArmies[Instance.ActiveArmy.Index];
            if (visibilityState.SightingKnown == false)
                return;

            if (visibilityState.IsVisible == false)
            {
                unitSnapshot = new(unitSnapshot, Instance.ActiveArmy);
                double minutesSinceLastObserved = (Instance.CurrentGameTime - visibilityState.ChangeTime).TotalMinutes;
                opacity = ModelLib.MathHelper.ClampedLerp(0.6f, 0.3f, (float)minutesSinceLastObserved / 70f);
            }

            if (checkDisplayUnitCollision)
                if (Vector2.Distance(unitSnapshot.Location.ToMGVector2(), unitSnapshot.Unit.Location.ToMGVector2()) < 50f)
                    return;

            (Texture2D image, Texture2D shadow) = GetSimUnitTexture2D(unitSnapshot, unitSnapshot.Formation);
            Vector2 unitPosition = MapOffset + unitSnapshot.Location.ToVector2();
            float scale = Instance.ScenarioPieceSize * (pulsing ? UnitPulsingScale : 1f);
            if (unitSnapshot.Unit.Index == 0)
                scale *= 1.35f;

            float shadowOffset = 2f;
            float shadowOpacity = 0.25f;
            if (pulsing)
            {
                scale *= 1.4f;
                float shadowPulse = (UnitPulsingScale - 0.75f) / 0.5f;
                shadowOffset += shadowPulse * 12f;
                shadowOpacity *= 1f - (shadowPulse / 1.5f);
            }

            if (highlighted)
            {
                for (int j = 0; j < 8; j++)
                    spriteLayerUnits.Draw(shadow, unitPosition +
                        Vector2.Transform(new Vector2(0, -3), Matrix.CreateRotationZ(ModelLib.MathHelper.ToRadiansFloat(45f * j))), null,
                        SideColors.GetSideColors(unitSnapshot.Unit.Side).highlightedColor.ToMGColor() * opacity * opacity,
                        unitSnapshot.Facing, unitSnapshot.DrawOrigin, scale, SpriteEffects.None, 0f);
            }

            spriteLayerUnits.Draw(shadow, unitPosition + new Vector2(shadowOffset), null, Color.Black * shadowOpacity,
                unitSnapshot.Facing, unitSnapshot.DrawOrigin, scale, SpriteEffects.None, 0f);
            spriteLayerUnits.Draw(image, unitPosition, null, Color.White * opacity,
                unitSnapshot.Facing, unitSnapshot.DrawOrigin, scale, SpriteEffects.None, 0f);

            if (unitSnapshot.Stance == Stances.Routing)
            {
                FormationGroups formationGroup = FormationGroupForFormation(unitSnapshot.Formation);
                Texture2D routingImage = GetSimUnitRoutingTexture2D(unitSnapshot, unitSnapshot.Formation);
                float angle = ModelLib.MathHelper.WrapAngleInDegreesFloat(ModelLib.MathHelper.ToDegreesFloat(unitSnapshot.Facing));
                if ((((formationGroup == FormationGroups.LineGroup) || (formationGroup == FormationGroups.SquareGroup)) && (angle > 90f) && (angle < 270f)) ||
                    ((formationGroup == FormationGroups.ColumnGroup) && (angle < 180f)))
                    angle += 180f;
                float fadeCycle = ModelLib.MathHelper.Clamp((MathF.Sin((float)GameTimeRef.TotalGameTime.TotalSeconds * 4f) + 1.0f), 0f, 1f);
                spriteLayerUnits.Draw(routingImage, unitPosition, null, Color.White * fadeCycle, ModelLib.MathHelper.ToRadiansFloat(angle),
                    unitSnapshot.DrawOrigin, scale, SpriteEffects.None, 0f);
            }
        }

        public void DrawOnMapUnitOrdersSnapshots(MATEUnitInstance unit, int startCommandIndex, int endCommandIndex,
            bool highlighted, bool pulsing, MATEUnitSnapshot? endCommandUnit = null)
        {
            for (int i = startCommandIndex; i <= endCommandIndex; i++)
            {
                Order command = unit.Orders[i];
                if (command.State == Order.OrderState.OrderCalledOff)
                    continue;

                if (command.OrderFormation == Formations.BatteryFire) // TODO (noted by MT)
                {
                    if (i == endCommandIndex)
                    {
                        DrawOnMapUnitSnapshot(command.UnitSnapshotEnd, highlighted, pulsing);
                        if (command.OrderFormation == Formations.BatteryFire)
                            DrawOnMapFireArrow(command.UnitSnapshotFormation.Location,
                                command.OrderObjective.UnitSnapshot.Value.Location);
                    }
                    continue;
                }

                DrawOnMapUnitSnapshot(command.UnitSnapshotFormation, highlighted, false);

                if (i == endCommandIndex)
                    if (endCommandUnit.HasValue)
                        DrawOnMapUnitSnapshot(endCommandUnit.Value, highlighted, pulsing);
                    else
                        DrawOnMapUnitSnapshot(command.UnitSnapshotEnd, highlighted, pulsing);

                switch (FormationGroupForFormation(command.OrderFormation))
                {
                    case FormationGroups.LineGroup:
                    case FormationGroups.SquareGroup:
                        UI.UIElement_PathRenderer.AddArrow(unit.Side, command.PathToObjective[0], command.OrderObjective.Location, Instance.ScenarioPieceWidth, true);
                        break;

                    case FormationGroups.ColumnGroup:
                        UI.UIElement_PathRenderer.AddRoute(unit.Side, command.PathToObjective, 16f, true, 1f);
                        break;
                }
            }
        }

        public void DrawOnMapCourierPaths(MATEUnitInstance unit)
        {
            if (unit.GetFromGameInstance(Instance) == unit.GHQUnit)
                return;

            UI.UIElement_PathRenderer.AddRoute(unit.Side, unit.CourierRoute, 4, false, 1f);
            for (int i = 0; i < unit.SubordinateUnits.Count; i++)
                UI.UIElement_PathRenderer.AddRoute(unit.Side, unit.SubordinateUnits[i].CourierRoute, 4, false, 1f);
            if (unit.CommandingUnit != null)
                UI.UIElement_PathRenderer.AddRoute(unit.Side, unit.CommandingUnit.CourierRoute, 4, false, 1f);

            DrawOnMapCourierIcon(unit);
            for (int i = 0; i < unit.SubordinateUnits.Count; i++)
                DrawOnMapCourierIcon(unit.SubordinateUnits[i]);
        }

        public void DrawOnMapCourierIcon(MATEUnitInstance unit)
        {
            if (unit.GetFromGameInstance(Instance) == unit.GHQUnit)
                return;

            TimeSpan gameTime = Instance.CurrentGameTime;
            if (Tabs.TabSelected == TabNames.Reports)
                gameTime = Tabs.TabReports.EventSelected.Time;
            if ((Tabs.TabSelected == TabNames.EndTurn) && (GameplayScreen == GameplayScreens.EndingTurn))
                gameTime = ProcessTurn.ProgressTime;

            for (int i = unit.SnapshotDisplayed.OrdersCurrentIndex; i < unit.Orders.Count; i++)
            {
                Order command = unit.Orders[i];
                if (command.State == Order.OrderState.CourierDeparted && (gameTime < command.CourierArrivalTime))
                {
                    float amount = (float)Math.Clamp((gameTime - unit.Orders[i].CourierDepartureTime).TotalSeconds /
                        (unit.Orders[i].CourierArrivalTime - unit.Orders[i].CourierDepartureTime).TotalSeconds, 0f, 1f);

                    int nodesCount = unit.CourierRoute.Count;
                    if (unit.CommandingUnit.Index != unit.GHQUnit.Index)
                        nodesCount += unit.CommandingUnit.CourierRoute.Count;
                    int nodeIndex = (int)(amount * (nodesCount - 1));

                    Vector2 GetCourierPosition(MATEUnitInstance unit, int nodeIndex)
                    {
                        Vector2 courierPosition;
                        if (unit.CommandingUnit.Index == unit.GHQUnit.Index)
                            courierPosition = unit.CourierRoute[nodeIndex].ToMGVector2();
                        else
                            if (nodeIndex < unit.CommandingUnit.CourierRoute.Count)
                            courierPosition = unit.CommandingUnit.CourierRoute[nodeIndex].ToMGVector2();
                        else
                            courierPosition = unit.CourierRoute[nodeIndex - unit.CommandingUnit.CourierRoute.Count].ToMGVector2();
                        return courierPosition;
                    }
                    Vector2 courierPosition = GetCourierPosition(unit, nodeIndex);
                    Vector2 lastCourierPosition = (nodeIndex >= 1) ? GetCourierPosition(unit, nodeIndex - 1) : courierPosition;

                    spriteLayerBelowUnits.Draw(UI.UIStyles.Current.MapCourier, MapOffset + courierPosition, null, Color.White, 0f,
                        UI.UIStyles.Current.MapCourier.Bounds.Center.ToVector2(), 0.5f,
                        (courierPosition.X >= lastCourierPosition.X) ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0f);
                }
            }
        }

        public void DrawOnMapFogOfWar()
        {
            MapFogOfWar fogOfWarMap = Instance.ActiveArmy.MapFogOfWar;

            MATEUnitInstance directOrderLOSUnit = Tabs.TabSelected == TabNames.DirectOrder &&
                Tabs.TabDirectOrders.State == TabDirectOrders.TabStates.SettingArtilleryTarget ?
                Tabs.TabDirectOrders.OrderUnit : null;

            if ((fogOfWarMap.FogOfWarType == MapFogOfWar.FogOfWarTypes.None) && (directOrderLOSUnit == null))
                return;

            bool newFogOfWarMap = false;
            if ((fogOfWarTextureInstanceID != Instance.UniqueInstanceID) ||
                (fogOfWarTextureArmyIndex != Instance.ActiveArmy.Index) ||
                (fogOfWarTextureUnit  != directOrderLOSUnit) ||
                (fogOfWarTextureGameEvent != Tabs.CurrentGameEventDisplayed)) 
            {
                newFogOfWarMap = true;
                if (fogOfWarMap.CalculateStarted == false)
                {
                    fogOfWarMap.Calculate(directOrderLOSUnit);
                    if (fogOfWarMap.CalculateStarted)
                    {
                        fogOfWarTextureInstanceID = Instance.UniqueInstanceID;
                        fogOfWarTextureArmyIndex = Instance.ActiveArmy.Index;
                        fogOfWarTextureUnit = directOrderLOSUnit;
                        fogOfWarTextureGameEvent = Tabs.CurrentGameEventDisplayed;
                    }
                }
            }

            Color fogColor = Color.Black * 0.4f;
            if (fogOfWarMap.CalculateStarted && fogOfWarMap.CalculateComplete)
            {
                Color[] Colors = new Color[Map.Width * Map.Height];
                for (int x = 0; x < Map.Width; x++)
                    for (int y = 0; y < Map.Height; y++)
                        Colors[x + (y * Map.Width)] = fogOfWarMap.MapNodes[(x * Map.Height) + y] ? Color.Transparent : fogColor;

                fogOfWarTexture?.Dispose();
                fogOfWarTexture = new Texture2D(GraphicsDeviceRef, Map.Width, Map.Height);
                fogOfWarTexture.SetData(Colors);
                fogOfWarMap.CalculateStarted = false;
            }

            if (fogOfWarTexture != null)
                spriteLayerBelowUnits.Draw(fogOfWarTexture, MapOffset, Color.White);
        }

        public void DrawOnMapGrid()
        {
            for (int x = 0; x < Map.Width; x += 35)
                DrawLine(spriteLayerBelowUnits, new Vector2(x + LeftMapOffset, TopMapOffset),
                    new Vector2(x + LeftMapOffset, TopMapOffset + Map.Height), Color.Gray, 1);

            for (int y = 0; y < Map.Height; y += 35)
                DrawLine(spriteLayerBelowUnits, new Vector2(LeftMapOffset, y + TopMapOffset),
                    new Vector2(LeftMapOffset + Map.Width, y + TopMapOffset), Color.Gray, 1);
        }

        public void DrawOnMapAllPlaces()
        {
            for (int i = 0; i < Instance.AllPlaces.Count; i++)
            {
                Sides placesSide = Instance.AllPlaces[i].SnapshotDisplayed.Side;
                Color placeColor = SideColors.GetSideColors(placesSide).unitColor.ToMGColor();
                spriteLayerUnits.Draw(UI.UIStyles.Current.MapPlace, MapOffset + Instance.AllPlaces[i].Location.ToMGVector2(), null, placeColor,
                    Microsoft.Xna.Framework.MathHelper.ToRadians(-45f), new Vector2(0, UI.UIStyles.Current.MapPlace.Height / 2), 0.5f, SpriteEffects.None, 0f);
            }

            if (!ShowAllPlaces)
                return;

            // We have to do a painter's algorithm sort first
            List<int> SortedList = [];

            for (int i = 0; i < Instance.AllPlaces.Count; i++)
                SortedList.Add(i);

            int temp;

            for (int j = 0; j <= SortedList.Count - 2; j++)
            {
                for (int i = 0; i <= SortedList.Count - 2; i++)
                {
                    if (Instance.AllPlaces[SortedList[i]].Location.Y > Instance.AllPlaces[SortedList[i + 1]].Location.Y)
                    {
                        temp = SortedList[i + 1];
                        SortedList[i + 1] = SortedList[i];
                        SortedList[i] = temp;
                    }
                }
            }

            for (int i = 0; i < SortedList.Count; i++)
            {
                NearestPlaceIndex = SortedList[i];
                PlaceInstance place = Instance.AllPlaces[NearestPlaceIndex];
                int x = place.Location.X + LeftMapOffset;
                int y = place.Location.Y + TopMapOffset;

                if (x < TopMapOffset)
                    x = TopMapOffset + 113;
                if (y < TopMapOffset)
                    y = TopMapOffset + 42;
                if (x > WindowWidth - 119)
                    x = WindowWidth - 130;
                if (y > WindowHeight - 48)
                    y = WindowHeight - 48;

                Rectangle r = new Rectangle(x - 113 + 6, y + 6 - 42, 226, 85);
                spriteLayerAboveUnits.Draw(EnemyObservedBoxDropShadow, r, Color.White * 0.5f);

                r = new Rectangle(x - 113, y - 42, 226, 85);
                Sides placesSide = place.SnapshotDisplayed.Side;
                if (placesSide == Sides.Neutral)
                    spriteLayerAboveUnits.Draw(VictorianPlaceNameInfoBoxGreen, r, Color.White);
                else if (placesSide == Sides.Red)
                    spriteLayerAboveUnits.Draw(VictorianPlaceNameInfoBoxRed, r, Color.White);
                else if (placesSide == Sides.Blue)
                    spriteLayerAboveUnits.Draw(VictorianPlaceNameInfoBoxBlue, r, Color.White);

                // Positioning the text inside the box
                string tString = place.Name;

                Vector2 width;
                string[] mString = JustifyTextStrings(UnitInfoNameFont, tString, 120);
                int leading = 0;
                int MaxLines = 2;

                if (mString.Length < 2)
                    MaxLines = mString.Length;

                int YStart = 8;
                if (MaxLines == 2)
                    YStart = 4;
                else if (MaxLines == 1)
                    YStart = 8;

                for (int j = 0; j < MaxLines; j++)
                {
                    width = UnitInfoNameFont.MeasureString(mString[j]);
                    spriteLayerAboveUnits.DrawString(UnitInfoNameFont, mString[j],
                        new Vector2(x + 46 - (width.X / 2), y - (width.Y / 2) + YStart + leading - 30), Color.Black);
                    leading += 20;
                }

                // draw value here 
                YStart = 22 + y;
                int XStart = 40 + x;
                tString = place.CaptureValue.ToString() + " Points";
                width = VictorianPlaceNameValueFont.MeasureString(tString);
                spriteLayerAboveUnits.DrawString(VictorianPlaceNameValueFont, tString, new Vector2(XStart - (width.X / 2), YStart - (width.Y / 2)), Color.Black);
            }
        }

        public void DrawOnMapAnimateFlag(ArmyInstance armyData, PointI location)
        {
            spriteLayerAboveUnits.Draw(FlagAnimations[armyData.Index + 1][flagAnimationFrameNumber], // TODO (noted by MT) currently flag side index
                new Rectangle(location.X + LeftMapOffset - 4, location.Y + TopMapOffset - 32, 32, 32), Color.White);
        }

        public void DrawOnMapRangeOfUnit(MATEUnitInstance unit)
        {
            Rectangle source;
            float Scale;
            double diameter;
            double radius;
            double resultX;
            double resultY;
            double PercentageOverX = 0;
            double PercentageOverY = 0;
            double XOffset = 0;
            double YOffset = 0;

            /// 1. calculate radius of circle in pixels
            radius = unit.Range / Map.MetersPerPixel;
            diameter = 2 * radius;
            /// 2. subtract radius from unit location
            resultX = unit.Location.X + LeftMapOffset - radius;
            resultY = unit.Location.Y + TopMapOffset - radius;
            /// 3. if result is < LeftMapOffset
            /// 4.     calculate percentage of diameter it is < LeftMapOffset by
            if (resultX < LeftMapOffset)
            {
                XOffset = LeftMapOffset - resultX;
                PercentageOverX = LeftMapOffset - resultX;
                PercentageOverX = diameter / PercentageOverX;
                PercentageOverX = 100 / PercentageOverX;
            }

            if (resultY < TopMapOffset)
            {
                YOffset = TopMapOffset - resultY;
                PercentageOverY = TopMapOffset - resultY;
                PercentageOverY = diameter / PercentageOverY;
                PercentageOverY = 100 / PercentageOverY;
            }

            source = new Rectangle((int)PercentageOverX, (int)PercentageOverY, 100 - (int)PercentageOverX, 100 - (int)PercentageOverY);

            spriteLayerAboveUnits.Draw(BigCircle,
                new Vector2(unit.Location.X
                + LeftMapOffset - (float)radius + (float)XOffset,
                unit.Location.Y
                + TopMapOffset - (float)radius + (float)YOffset)
                , source // rect for source
                , SideColors.GetSideColors(unit.Side).highlightedColor.ToMGColor() * 0.25f // opacity is 25%
                , 0f // rotation
                , Vector2.Zero // rotation center
                , (float)diameter / BigCircle.Width  // scale we need to adjust this for different piece sizes
                , SpriteEffects.None
                , 0.9f // layer depth
            );
        }

        public void DrawOnMapFireArrow(PointI fromMapPoint, PointI toMapPoint)
        {
            Vector2 from = fromMapPoint.ToVector2();
            Vector2 to = toMapPoint.ToVector2();
            float scale = Instance.ScenarioPieceSize * 2f;
            float totalDistance = Math.Max(1f, Vector2.Distance(from, to));
            float totalDistanceWithoutArrowIcon = Math.Max(1f, totalDistance - (FiringArrow.Height * scale));
            float speed = Math.Min(150, totalDistanceWithoutArrowIcon);
            FireArrowDistance += speed * (float)GameTimeRef.ElapsedGameTime.TotalSeconds;
            FireArrowDistance %= totalDistanceWithoutArrowIcon;
            Vector2 position = Vector2.Lerp(from, to, FireArrowDistance / totalDistance);

            spriteLayerAboveUnits.Draw(FiringArrow, position + MapOffset, null, Color.White * 0.75f,
                (float)from.ToPointI().AngleToFacePointInRadiansFloat(to.ToPointI()),
                origin: new Vector2(FiringArrow.Width / 2f, FiringArrow.Height), Instance.ScenarioPieceSize * 2f, SpriteEffects.None, 0f);
        }

        public void DrawOnMapCollisionRect(MATEUnitInstance unit)
        {
            RectangleShape rec = RectangleShape.CalculateActualRect(unit.SnapshotDisplayed);
            System.Numerics.Vector2[] corners = RectangleShape.CalculateCorners(rec, unit.SnapshotDisplayed.Facing);
            DrawBoxOutline(spriteLayerAboveUnits, corners[0] + MapOffset, corners[1] + MapOffset, corners[3] + MapOffset, corners[2] + MapOffset,
                SideColors.GetSideColors(unit.Side).highlightedColor.ToMGColor() * (unit.IsOnBoard ? 1f : 0.3f), 3);
        }

        #endregion

        #region Draw Lines

        public void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, int width, bool snapToPixel = true)
        {
            if (start == end)
                return;

            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            float length = edge.Length();
            if (snapToPixel)
            {
                spriteBatch.Draw(whitePixel, new Rectangle((int)start.X, (int)start.Y, (int)length, width), null, color, angle,
                    new Vector2(0, (width > 1) ? 0.5f : 0f), SpriteEffects.None, 0f);
            }
            else
            {
                float distance = Vector2.Distance(start, end);
                spriteBatch.Draw(whitePixel, start, null, color, angle, new Vector2(0, (width > 1) ? 0.5f : 0f),
                    new Vector2(length, width), SpriteEffects.None, 0f);
            }
        }

        public void DrawBoxOutline(SpriteBatch spriteBatch, Rectangle rectangle, Color color, int width)
        {
            Vector2 topLeft = new Vector2(rectangle.X, rectangle.Y);
            Vector2 topRight = topLeft + new Vector2(rectangle.Width, 0);
            Vector2 bottomLeft = topLeft + new Vector2(0, rectangle.Height);
            Vector2 bottomRight = topLeft + new Vector2(rectangle.Width, rectangle.Height);
            DrawBoxOutline(spriteBatch, topLeft, topRight, bottomLeft, bottomRight, color, width);
        }

        public void DrawBoxOutline(SpriteBatch spriteBatch, Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight, Color color, int width)
        {
            DrawLine(spriteBatch, topLeft, topRight, color, width);
            DrawLine(spriteBatch, topRight, bottomRight, color, width);
            DrawLine(spriteBatch, bottomRight, bottomLeft, color, width);
            DrawLine(spriteBatch, bottomLeft, topLeft, color, width);
        }

        #endregion
    }
}
