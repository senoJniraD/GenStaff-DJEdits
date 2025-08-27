//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Input;
//using ModelLib;
//using TacticalAILib;
//using static GSBPGEMG.Game1;
//using static GSBPGEMG.Tabs;
//using static TacticalAILib.MATEUnitGame;

//namespace GSBPGEMG
//{
//    public static class TabSubordinates
//    {
//        public static bool SubordinateSelected;
//        public static int DisplayCorpsIndex;
//        public static bool SetCorpsTimeClick;

//        public static List<CorpsCommand> SubordinatesScrollContent = new List<CorpsCommand>();
//        public static List<ScrollIndex> IndexIntoSubordinatesScrollList = new List<ScrollIndex>();
//        public static int SubordinatesScrollIndex = 0;

//        public static bool CorpsOrdersGiven;
//        public static bool CorpsOrders;

//        public static void Update()
//        {
//            // Mouse scroll wheel
//            if (Input.mouseScrollWheelChange != 0)
//            {
//                SubordinatesScrollIndex += (Input.mouseScrollWheelChange < 0) ? 1 : -1;
//                SubordinatesScrollIndex = Math.Clamp(SubordinatesScrollIndex, 0, Math.Max(0, IndexIntoSubordinatesScrollList.Count - 36));
//                SubordinateSelected = false;
//            }
//            else if (Input.mouseLeftClick)
//            {
//                // Scroll top
//                if (ScrollUpArea.Contains(Input.mouseMenuPoint))
//                {
//                    SubordinatesScrollIndex = 0;
//                    SubordinateSelected = false;
//                }

//                // Scroll bottom
//                else if (ScrollDownArea.Contains(Input.mouseMenuPoint) && IndexIntoSubordinatesScrollList.Count - 36 > 1)
//                {
//                    if (IndexIntoSubordinatesScrollList.Count - 36 > 1)
//                        SubordinatesScrollIndex = IndexIntoSubordinatesScrollList.Count - 36;
//                    SubordinateSelected = false;
//                }
//            }

//            // Create scroll tab corps
//            (IndexIntoSubordinatesScrollList, SubordinatesScrollContent) = CreateScrollTabSubordinates();

//            // Clicked on a corps
//            if (Input.mouseLeftClick && !CorpsOrdersGiven &&
//                Input.MousePositionWithinArea(Input.MouseCoordinateType.Menu, Input.MouseOriginType.TopLeft,
//                new Point(0, TopMapOffset), new Point(270, WindowHeight - TopMapOffset)))
//            {
//                CorpsOrders = false;
//                for (int i = 0; i < SubordinatesScrollContent.Count; i++)
//                {
//                    SubordinatesScrollContent[i].IsChecked = false;
//                    ActiveArmy.CorpsList[i].IsChecked = false;

//                    if (Input.MousePositionWithinArea(Input.MouseCoordinateType.Menu, Input.MouseOriginType.TopLeft,
//                        new Point(0, SubordinatesScrollContent[i].StartY - (SubordinatesScrollIndex * 20)),
//                        new Point(270, SubordinatesScrollContent[i].EndY - SubordinatesScrollContent[i].StartY)))
//                    {
//                        SubordinateSelected = true;
//                        SubordinatesScrollContent[i].IsChecked = true;
//                        ActiveArmy.CorpsList[i].IsChecked = true;
//                        DisplayCorpsIndex = i;
//                        GivingDirectOrders = true;
//                        MovementGoalClick = false;
//                    }
//                }
//            }

//            if (!GivingDirectOrders)
//            {
//                ClearAllUnitsPulsing();
//                DisplayUnitInfo = MouseOverUnitSnapshot(AllArmyUnits) != null;
//                if (DisplayUnitInfo)
//                    DisplayUnit.IsPulsing = true;
//            }

//            if (SubordinateSelected)
//            {
//                // Will need to check for modifiers, too
//                // Attack, Defend, Recon

//                #region Corps Orders - Line Formation

//                // Left click while giving corps orders
//                if (LegalClick(Input.MouseButtons.Left))
//                {
//                    DisplayPlaceInfo = IsPlaceUnderMouse();
//                    CorpsOrders = true;
//                    CorpsOrdersGiven = true;
//                    Input.MouseCursorIcon = MouseCursor.Wait;

//                    for (int i = 0; i < ActiveArmy.CorpsList.Count; i++)
//                    {
//                        if (ActiveArmy.CorpsList[i].IsChecked)
//                        {
//                            ActiveArmy.CorpsList[i].Formation = Formations.Line;
//                            CorpsTactics.ImplementCorpsOrder(ActiveArmy.CorpsList[i],
//                                Stances.Attack, Input.mouseMapPointI);
//                        }
//                    }

//                    LoggingWriteBooleanStates("Corps Orders - Line Formation");
//                }

//                #endregion

//                #region Corps Orders - Column Formation

//                // Right click while giving corps orders
//                else if (LegalClick(Input.MouseButtons.Right))
//                {
//                    DisplayPlaceInfo = IsPlaceUnderMouse();
//                    CorpsOrders = true;
//                    CorpsOrdersGiven = true;
//                    Input.MouseCursorIcon = MouseCursor.Wait;

//                    for (int i = 0; i < ActiveArmy.CorpsList.Count; i++)
//                    {
//                        if (ActiveArmy.CorpsList[i].IsChecked)
//                        {
//                            ActiveArmy.CorpsList[i].Formation = Formations.Column;
//                            CorpsTactics.ImplementCorpsOrder(ActiveArmy.CorpsList[i], Stances.Attack, Input.mouseMapPointI);
//                        }
//                    }

//                    LoggingWriteBooleanStates("Corps Orders - Column Formation");
//                }

//                #endregion
//            }

//            // Corps Orders Box
//            if (Input.mouseLeftClick
//               && CorpsOrdersGiven && TabSelected == TabNames.Subordinates)
//            {
//                if (SetTimeArea.Contains(Input.mouseMenuPoint))
//                {
//                    SetCorpsTimeClick = true;
//                    int LeadershipCostInMinutes = ActiveArmy.CorpsList[DisplayCorpsIndex].OrdersDelay;
//                    OrderTime = CurrentGameTime + TimeSpan.FromMinutes(LeadershipCostInMinutes);
//                }

//                else if (SetCorpsTimeClick && HourUpArea.Contains(Input.mouseMenuPoint))
//                    HourIncrease = true;
//                else if (SetCorpsTimeClick && MinuteUpArea.Contains(Input.mouseMenuPoint))
//                    MinuteIncrease = true;
//                else if (SetCorpsTimeClick && MinuteDownArea.Contains(Input.mouseMenuPoint))
//                    MinuteDecrease = true;
//                else if (SetCorpsTimeClick && HourDownArea.Contains(Input.mouseMenuPoint))
//                    HourDecrease = true;
//                else if (SetCorpsTimeClick && SetTimeCancelArea.Contains(Input.mouseMenuPoint))
//                {
//                    SetTimeCancel = true;
//                    SetCorpsTimeClick = false;
//                    OrdersTimeHasChanged = false;
//                }

//                else if (SetCorpsTimeClick && SetTimeAcceptArea.Contains(Input.mouseMenuPoint))
//                {
//                    SetTimeAccept = true;
//                    SetCorpsTimeClick = false;
//                    OrdersTimeHasChanged = true;

//                    TempExecuteOrderTime = OrderTime;
//                }

//                // Corps - Cancel Click
//                else if (BackArea.Contains(Input.mouseMenuPoint))
//                {
//                    ResetGivingOrders();
//                }

//                // Corps - Continue Click
//                else if (ContinueArea.Contains(Input.mouseMenuPoint))
//                {
//                    ContinueClick = true;
//                    OrdersTimeHasChanged = false;
//                    CorpsOrdersGiven = true;
//                    MovementFormation = null;
//                    MovementGoalClick = false;
//                    CorpsOrders = true;
//                    // have to update some locations...duh
//                    LoggingWriteBooleanStates("Corps - Continue Click");
//                }

//                // Corps - Courier Click
//                else if (CourierArea.Contains(Input.mouseMenuPoint))
//                {
//                    Tabs.ChangeTab(TabNames.Reports);

//                    ResetGivingOrders();

//                    HeaderMenus.SaveRequired = true;
//                    LoggingWriteBooleanStates("Corps - Courier Click");
//                }
//            }
//        }

//        public static (List<ScrollIndex>, List<CorpsCommand>) CreateScrollTabSubordinates()
//        {
//            List<ScrollIndex> ReportIndex = new List<ScrollIndex>();
//            List<CorpsCommand> ScrollContent = new List<CorpsCommand>();
//            int y = 70;

//            for (int i = 0; i < ActiveArmy.CorpsList.Count; i++)
//            {
//                string[] mString = ActiveArmy.CorpsList[i].LineOfType;

//                CorpsCommand tObj = new CorpsCommand();
//                tObj.StartY = y;
//                ActiveArmy.CorpsList[i].StartY = y;
//                tObj.IsChecked = ActiveArmy.CorpsList[i].IsChecked;

//                for (int j = 0; j < mString.Length; j++)
//                {
//                    ScrollIndex tIndex = new();
//                    tIndex.ScrollObjectNum = i;
//                    tIndex.slug = mString[j];
//                    if (j > 3)
//                        tIndex.DrawIcon = true;

//                    ReportIndex.Add(tIndex);
//                    y += 20;
//                }

//                tObj.EndY = y;
//                ActiveArmy.CorpsList[i].EndY = y;
//                ScrollContent.Add(tObj);

//                y += 20;

//                if (i + 1 < ActiveArmy.CorpsList.Count)
//                    ReportIndex[^1].ScrollObjectNum = i + 1;

//                ScrollIndex uIndex = new ScrollIndex();
//                uIndex.ScrollObjectNum = i;
//                uIndex.slug = "";
//                ReportIndex.Add(uIndex);
//            }

//            return (ReportIndex, ScrollContent);
//        }

//        public static void Draw()
//        {
//            // output reports
//            int x = 85;
//            int y = 70;

//            ClearAllUnitsPulsing();

//            Vector2 width = Klabasto18.MeasureString("SUBORDINATES");
//            spriteLayerTabs.DrawString(Klabasto18, "SUBORDINATES", new Vector2(LeftTabCenter - (width.X / 2), 46 - (width.Y / 2)), Color.Black);

//            Rectangle r = new Rectangle(2, 784, 265, 55);
//            spriteLayerTabs.Draw(VictorianUpDownScroll, r, Color.White);

//            int NumLines = 0;
//            int NumOrders = IndexIntoSubordinatesScrollList[NumLines + SubordinatesScrollIndex].ScrollObjectNum;

//            CorpsCommand corpsCommand = ActiveArmy.CorpsList[NumOrders];
//            if (SubordinateSelected)
//                if (corpsCommand.IsChecked)
//                {
//                    r = new Rectangle(5, y + 80, 75, 50);
//                    spriteLayerTabs.Draw(VictorianHandWithPen, r, Color.White);

//                    // Draw all couriers from HQ commanding this Corps
//                    DisplayUnit = MATEUnitUtil.DeepClone(corpsCommand.Commander);
//                    DrawOnMapCourierRoutes();

//                    // animate units 
//                    for (int i = 0; i < corpsCommand.UnitsInCorps.Count; i++)
//                        corpsCommand.UnitsInCorps[i].IsPulsing = true;
//                }


//            while (NumLines < 35 && NumLines < IndexIntoSubordinatesScrollList.Count
//                && (NumLines + SubordinatesScrollIndex) < IndexIntoSubordinatesScrollList.Count)
//            {
//                if (IndexIntoSubordinatesScrollList[NumLines + SubordinatesScrollIndex].slug == "")
//                {
//                    r = new Rectangle(5, y + 5, 259, 5);
//                    spriteLayerTabs.Draw(VictorianBatteredRule, r, Color.White);

//                    if (NumLines + 1 < IndexIntoSubordinatesScrollList.Count)
//                    {
//                        // to prevent drawing at bottom of tab
//                        if (y < 680)
//                        {
//                            if (NumOrders + 1 < ActiveArmy.CorpsList.Count)
//                                NumOrders++;

//                            corpsCommand = ActiveArmy.CorpsList[NumOrders];
//                            if (SubordinateSelected)
//                                if (corpsCommand.IsChecked)
//                                {
//                                    r = new Rectangle(5, y + 80, 75, 50);
//                                    spriteLayerTabs.Draw(VictorianHandWithPen, r, Color.White);

//                                    // Draw all couriers from HQ commanding this Corps
//                                    DisplayUnit = MATEUnitUtil.DeepClone(corpsCommand.Commander);
//                                    DrawOnMapCourierRoutes();

//                                    // animate units 
//                                    for (int i = 0; i < corpsCommand.UnitsInCorps.Count; i++)
//                                        corpsCommand.UnitsInCorps[i].IsPulsing = true;
//                                }
//                        }
//                    }
//                }
//                else
//                {
//                    spriteLayerTabs.DrawString(VictorianReportsFont, IndexIntoSubordinatesScrollList[NumLines + SubordinatesScrollIndex].slug, new Vector2(x, y), Color.Black);
//                }

//                y += 20;
//                NumLines++;
//            }

//            /// Above sets up tab and handles clicks
//            /// below outputs results

//            if (CorpsOrders)
//            {
//                for (int i = 0; i < ActiveArmy.CorpsList.Count; i++)
//                {
//                    if (ActiveArmy.CorpsList[i].IsChecked)
//                    {
//                        for (int j = 0; j < ActiveArmy.CorpsList[i].UnitsInCorps.Count; j++)
//                        {
//                            MATEUnitGameSnapshot snapshot = new(ActiveArmy.CorpsList[i].UnitsInCorps[j]);
//                            DrawOnMapUnitOrdersSnapshots(snapshot.Unit, snapshot.CommandIndex, snapshot.Unit.Commands.Count - 1, false);
//                        }
//                    }
//                }
//            }

//            if (CorpsOrdersGiven)
//            {
//                DrawTabConfirmCorpsOrders();
//                DrawTabOrdersBoxCorps();
//            }
//        }

//        public static void DrawOnMapCourierRoutes()
//        {
//            if (DisplayUnit.ArmyData == ActiveArmy)
//                for (int i = 0; i < DisplayUnit.SubordinateUnits.Count; i++)
//                    PathRenderer.AddRoute(DisplayUnit.Side, DisplayUnit.SubordinateUnits[i].CourierRoute, 4, false, 1f);
//        }

//        public static void DrawTabOrdersBoxCorps()
//        {
//            int x = 10;
//            int y = 200;

//            Rectangle r = new Rectangle(2, 187, 263, 592);

//            spriteLayerTabs.Draw(VictorianOrdersBox, Vector2.Zero, r, Color.White, 0f, Vector2.Zero, Vector2.Zero, 0f, 0f);

//            string tString = ActiveArmy.CorpsList[DisplayCorpsIndex].Name;
//            string[] mString = JustifyTextStrings(BigUnitInfoNameFont, tString, 240);
//            Vector2 width;
//            int leading = 0;

//            int MaxLines = 2;
//            if (mString.Length < 2)
//                MaxLines = 1;

//            int YStart = 16;
//            if (MaxLines == 2)
//                YStart = 12;
//            else if (MaxLines == 1)
//                YStart = 16;

//            for (int i = 0; i < MaxLines; i++)
//            {
//                width = BigUnitInfoNameFont.MeasureString(mString[i]);
//                spriteLayerTabs.DrawString(BigUnitInfoNameFont, mString[i], new Vector2(x + 120 - (width.X / 2), y - (width.Y / 2) + YStart + leading), Color.Black);
//                leading += 32;
//            }

//            y += 61;
//            leading = 0;
//            int LeadingIncrement;

//            if (ActiveArmy.CorpsList[DisplayCorpsIndex].LineOfType.Length < 18)
//            {
//                tString = "";
//                for (int i = 3; i < ActiveArmy.CorpsList[DisplayCorpsIndex].LineOfType.Length; i++)
//                    tString += ActiveArmy.CorpsList[DisplayCorpsIndex].LineOfType[i];

//                tString = Strategy.StripReturn(tString);
//                mString = JustifyTextStrings(CorpsOrdersFont, tString, 240);
//                leading = 0;

//                MaxLines = mString.Length;

//                if (MaxLines == 6)
//                    YStart = 10;
//                else if (MaxLines == 5)
//                    YStart = 19;
//                else if (MaxLines == 4)
//                    YStart = 28;
//                else if (MaxLines == 3)
//                    YStart = 37;
//                else if (MaxLines == 2)
//                    YStart = 46;

//                if (MaxLines > 6)
//                    YStart = 10;

//                for (int i = 0; i < MaxLines; i++)
//                {
//                    width = CorpsOrdersFont.MeasureString(mString[i]);
//                    spriteLayerTabs.DrawString(CorpsOrdersFont, mString[i],
//                        new Vector2(x + 120 - (width.X / 2), y - (width.Y / 2) + YStart + leading), Color.Black);
//                    leading += 22;
//                }
//            }
//            else
//            {
//                for (int i = 3; i < ActiveArmy.CorpsList[DisplayCorpsIndex].LineOfType.Length; i++)
//                {
//                    width = CorpsOrdersFont.MeasureString(ActiveArmy.CorpsList[DisplayCorpsIndex].LineOfType[i]);
//                    spriteLayerTabs.DrawString(CorpsOrdersFont, ActiveArmy.CorpsList[DisplayCorpsIndex].LineOfType[i],
//                        new Vector2(x + 120 - (width.X / 2), y + 2 - (width.Y / 2) + leading), Color.Black);
//                    leading += 22;
//                }
//            }

//            // Display order
//            tString = "Your Orders";
//            width = Amaltea24.MeasureString(tString);
//            spriteLayerTabs.DrawString(Amaltea24, tString, new Vector2(x + 120 - (width.X / 2), y - (width.Y / 2) + YStart - 16), Color.Black);

//            TimeSpan TTime;
//            if (!OrdersTimeHasChanged)
//                TTime = CurrentGameTime + TimeSpan.FromMinutes(ActiveArmy.CorpsList[DisplayCorpsIndex].OrdersDelay);
//            else
//                TTime = OrderTime;
//            tString = "At " + TTime.ToString("h\\:mm") + " ";

//            if (ActiveArmy.CorpsList[DisplayCorpsIndex].Formation == Formations.Column)
//                tString += "move in column formation taking advantage of the road network as shown on the map.";
//            else if (ActiveArmy.CorpsList[DisplayCorpsIndex].Formation == Formations.Line)
//                tString += "move in line formation as shown on the map.";

//            mString = JustifyTextStrings(CorpsOrdersFont, tString, 240);
//            leading = 0;

//            MaxLines = mString.Length;

//            if (MaxLines == 6)
//                YStart = 15;
//            else if (MaxLines == 5)
//                YStart = 24;
//            else if (MaxLines == 4)
//                YStart = 33;
//            else if (MaxLines == 3)
//                YStart = 42;
//            else if (MaxLines == 2)
//                YStart = 21;

//            if (MaxLines > 6)
//                YStart = 10;

//            y += 170;
//            LeadingIncrement = 22;

//            for (int i = 0; i < MaxLines; i++)
//            {
//                width = CorpsOrdersFont.MeasureString(mString[i]);
//                spriteLayerTabs.DrawString(CorpsOrdersFont, mString[i], new Vector2(x + 120 - (width.X / 2), y - (width.Y / 2) + YStart + leading), Color.Black);
//                leading += LeadingIncrement;
//            }

//            tString = "{Set|Time}";
//            width = Phectic36.MeasureString(tString);
//            spriteLayerTabs.DrawString(Phectic36, tString, new Vector2(x + 124 - (width.X / 2), 600 - (width.Y / 2)), Color.Black);

//            y += 40;
//            tString = "{Back}";
//            width = Phectic36.MeasureString(tString);
//            spriteLayerTabs.DrawString(Phectic36, tString, new Vector2(x + 124 - (width.X / 2), 650 - (width.Y / 2)), Color.Black);

//            y += 40;
//            tString = "{Continue}";
//            width = Phectic36.MeasureString(tString);
//            spriteLayerTabs.DrawString(Phectic36, tString, new Vector2(x + 124 - (width.X / 2), 700 - (width.Y / 2)), Color.Black);

//            y += 40;
//            tString = "{Courier}";
//            width = Phectic36.MeasureString(tString);
//            spriteLayerTabs.DrawString(Phectic36, tString, new Vector2(x + 124 - (width.X / 2), 750 - (width.Y / 2)), Color.Black);

//            if (SetCorpsTimeClick)
//            {
//                r = new Rectangle(9, 616, 250, 154);
//                spriteLayerTabs.Draw(VictorianSetClock, r, Color.White);

//                TimeSpan TeaTime = CurrentGameTime + TimeSpan.FromMinutes(ActiveArmy.CorpsList[DisplayCorpsIndex].OrdersDelay);

//                if (!OrdersTimeHasChanged)
//                {
//                    if (HourIncrease)
//                        OrderTime += TimeSpan.FromHours(1);
//                    else if (MinuteIncrease)
//                        OrderTime += TimeSpan.FromMinutes(1);
//                    else if (HourDecrease)
//                        OrderTime += TimeSpan.FromHours(-1);
//                    else if (MinuteDecrease)
//                        OrderTime += TimeSpan.FromMinutes(-1);

//                    if (OrderTime < TeaTime)
//                        OrderTime = TeaTime;
//                }

//                tString = OrderTime.ToString("hh\\:mm");

//                width = Rudyard36.MeasureString(tString);
//                spriteLayerTabs.DrawString(Rudyard36, tString, new Vector2(LeftTabCenter - (width.X / 2), 710 - (width.Y / 2)), Color.Black);

//                HourIncrease = false;
//                MinuteIncrease = false;
//                HourDecrease = false;
//                MinuteDecrease = false;
//            }
//        }

//        public static void DrawTabConfirmCorpsOrders()
//        {
//            Vector2 width = Klabasto18.MeasureString("ORDERS");
//            spriteLayerTabs.DrawString(Klabasto18, "ORDERS", new Vector2(LeftTabCenter - (width.X / 2), 46 - (width.Y / 2)), Color.Black);

//            string scenarioNameDisplay = ScenarioName;
//            width = VictorianScenarioNameFont.MeasureString(scenarioNameDisplay);
//            if (width.X > LeftMapOffset - 10)
//            {
//                scenarioNameDisplay = TruncateToFirstWord(scenarioNameDisplay);
//                width = VictorianScenarioNameFont.MeasureString(scenarioNameDisplay);
//            }
//            spriteLayerTabs.DrawString(VictorianScenarioNameFont, scenarioNameDisplay, new Vector2(LeftTabCenter - (width.X / 2), 80 - (width.Y / 2)), Color.Black);

//            if (ScenarioDate != null)
//            {
//                width = Smythe22.MeasureString(ScenarioDate);
//                spriteLayerTabs.DrawString(Smythe22, ScenarioDate, new Vector2(LeftTabCenter - (width.X / 2), 106 - (width.Y / 2)), Color.Black);
//            }

//            string tString = CurrentGameTime.ToString("h\\:mm");

//            width = Rudyard36.MeasureString(tString);
//            spriteLayerTabs.DrawString(Rudyard36, tString, new Vector2(LeftTabCenter - (width.X / 2), 146 - (width.Y / 2)), Color.Black);


//            tString = "Turn " + CurrentGameTurn.ToString() + " of " + NumTurnsInScenario.ToString();
//            width = Smythe16.MeasureString(tString);
//            spriteLayerTabs.DrawString(Smythe16, tString, new Vector2(LeftTabCenter - (width.X / 2), 175 - (width.Y / 2)), Color.Black);

//            Rectangle r = new Rectangle(LeftTabCenter - 234 / 2, 180, 234, 18);
//            spriteLayerTabs.Draw(VictorianLine, r, Color.White);


//            r = new Rectangle(6, 800, 260, 37);
//            spriteLayerTabs.Draw(ElevationTerrainBox, r, Color.White);

//            width = Smythe16.MeasureString("Elevation:");
//            spriteLayerTabs.DrawString(VictorianElevTerFont, "Elevation:", new Vector2(75 - (width.X / 2), 786), Color.Black);

//            width = Smythe16.MeasureString("Terrain:");
//            spriteLayerTabs.DrawString(VictorianElevTerFont, "Terrain:", new Vector2(180 - (width.X / 2), 786), Color.Black);

//            width = Smythe16.MeasureString(ElevationString);
//            spriteLayerTabs.DrawString(Smythe16, ElevationString, new Vector2(78 - (width.X / 2), 810), Color.Black);

//            width = Smythe16.MeasureString(TerrainString);

//            spriteLayerTabs.DrawString(Smythe16, TerrainString, new Vector2(183 - (width.X / 2), 810), Color.Black);

//            for (int j = 0; j < ActiveArmy.CorpsList[DisplayCorpsIndex].UnitsInCorps.Count; j++)
//            {
//                MATEUnitGameSnapshot snapshot = new(ActiveArmy.CorpsList[DisplayCorpsIndex].UnitsInCorps[j]);
//                DrawOnMapUnitOrdersSnapshots(snapshot.Unit, snapshot.CommandIndex, snapshot.Unit.Commands.Count - 1, false);
//            }
//        }
//    }

//    public class ScrollIndex
//    {
//        public bool DrawIcon
//        {
//            get
//            {
//                return _DrawIcon;
//            }
//            set
//            {

//                _DrawIcon = value;
//            }
//        }
//        private bool _DrawIcon;

//        public int ScrollObjectNum
//        {
//            get
//            {
//                return _ScrollObjectNum;
//            }
//            set
//            {

//                _ScrollObjectNum = value;
//            }
//        }
//        private int _ScrollObjectNum;

//        public string slug
//        {
//            get
//            {
//                return _slug;
//            }
//            set
//            {

//                _slug = value;
//            }
//        }
//        private string _slug;
//    }
//}