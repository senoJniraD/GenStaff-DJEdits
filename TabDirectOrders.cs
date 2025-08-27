using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ModelLib;
using TacticalAILib;
using static GSBPGEMG.Game1;
using static GSBPGEMG.UI.UIElement_PathRenderer;
using static TacticalAILib.MATEUnitInstance;
using static TacticalAILib.MapPathfinding;
using Microsoft.Xna.Framework.Graphics;

namespace GSBPGEMG
{
    public class TabDirectOrders
    {
        public enum TabStates
        {
            SelectingUnitFormation,
            SettingObjective,
            SettingArtilleryTarget,
            SettingOrderTime,
            ConfirmingOrder
        }
        public TabStates State { get; private set; }
        public bool SetOrdersInProgress => OrderUnit != null;

        public MATEUnitInstance OrderUnit { get; private set; }
        private Order newOrder;
        private TimeSpan newOrderTime;

        private List<PointI> path;
        private PointI pathStartPoint = new(-1);
        private PointI pathEndPoint = new(-1);
        private PointI pathLastEndPoint = new(-1);
        private FormationGroups pathFormation;
        private (PointI point, string bannerMessage)? pathBlockedDetails;

        private List<PointI> drawPath;
        private double drawPathFadeTime;

        private PointI? fireArrowErrorPoint = null;

        private bool continueButtonAvailable =>
            newOrder != null &&
            newOrder.Stance != Stances.Attack && newOrder.Stance != Stances.Defend &&
            newOrder.OrderFormation != Formations.Skirmish && newOrder.OrderFormation != Formations.BatteryFire;

        private Rectangle continueArea = new(16, 681, 224, 24);
        private Rectangle courierArea = new(16, 727, 224, 28);
        private Rectangle backArea = new(16, 630, 224, 28);

        public void Update(Game1 game)
        {
            Rectangle currentBackArea = continueButtonAvailable ? backArea : continueArea;
            if (Input.MouseClickWithinArea(Input.MouseButtons.Left, Input.MouseCoordinateType.Menu, Input.MouseOriginType.TopLeft,
                currentBackArea.Location, currentBackArea.Size) ||
                Input.KeyIsPressed(Keys.Escape))
            {
                Reset(true);
            }

            if (State != TabStates.SelectingUnitFormation)
                SetMovementPaths(game, game.Instance);

            switch (State)
            {
                case TabStates.SelectingUnitFormation:
                    UpdateSelectingUnitFormation(game, game.Instance); break;
                case TabStates.SettingObjective:
                    UpdateSettingObjective(game, game.Instance, game.DisplayBanner); break;
                case TabStates.SettingArtilleryTarget:
                    UpdateSettingArtilleryTarget(game, game.Instance, game.DisplayBanner); break;
                case TabStates.SettingOrderTime:
                    UpdateSettingOrderTime(game, game.Instance, game.DisplayBanner); break;
                case TabStates.ConfirmingOrder:
                    UpdateConfirmingOrder(game, game.Instance); break;
            }
        }

        private void UpdateSelectingUnitFormation(Game1 game, GameInstance instance)
        {
            if (Input.MousePositionWithinMap() == false)
                return;

            MATEUnitInstance selectedUnit = OrderUnit ?? game.DisplayUnit;
            if (selectedUnit?.Army != instance.ActiveArmy)
                return;

            if (selectedUnit.UnitType == UnitTypes.HeadQuarters || selectedUnit.UnitType == UnitTypes.Supplies)
                game.DisplayBanner.SetMessage("HQs and Supply units can only move in column formation.");
            else if (selectedUnit.IsArtilleryType)
                game.DisplayBanner.SetMessage("Left-click for targeting. Right-click for movement in column formation.");
            else
                game.DisplayBanner.SetMessage("Left-click for movement in line formation. Right-click for fastest route in column formation.");

            FormationGroups? formationRequested = null;

            if (CheckMapClickPointIsValid(Input.MouseButtons.Left)
                && (Input.KeyIsHeld(Keys.Tab) == false)
                && selectedUnit.UnitType != UnitTypes.HeadQuarters
                && selectedUnit.UnitType != UnitTypes.Supplies)
                formationRequested = FormationGroups.LineGroup;

            else if (CheckMapClickPointIsValid(Input.MouseButtons.Right))
                formationRequested = FormationGroups.ColumnGroup;

            else if (CheckMapClickPointIsValid(Input.MouseButtons.Left)
                && Input.KeyIsHeld(Keys.Tab)
                && (selectedUnit.IsInfantryType))
                formationRequested = FormationGroups.SquareGroup;

            if (formationRequested == null)
                return;

            pathFormation = (FormationGroups)formationRequested;
            pathStartPoint = pathEndPoint = new PointI(-1);

            if (OrderUnit == null)
            {
                OrderUnit = selectedUnit.Clone(instance);
                OrderUnit.IsPulsing = true;
                newOrderTime = instance.CurrentGameTime + TimeSpan.FromMinutes(OrderUnit.OrderDelayTimeFromGHQ);
#if DEBUG
                if (Debug.SettingsInstantCouriers)
                    newOrderTime = instance.CurrentGameTime;
#endif
            }
            else
            {
                newOrderTime = OrderUnit.Orders[^1].ExecuteOrderTime;
            }

            newOrder = new(OrderUnit) { ExecuteOrderTime = newOrderTime };
            if (pathFormation == FormationGroups.ColumnGroup)
                newOrder.OrderObjective = new(pathEndPoint);

            State = (pathFormation == FormationGroups.LineGroup && OrderUnit?.IsArtilleryType == true) ?
                TabStates.SettingArtilleryTarget : TabStates.SettingObjective;
        }

        private void UpdateSettingObjective(Game1 game, GameInstance instance, DisplayBanner displayBanner)
        {
            if ((Input.MousePositionWithinMap() == false) || pathBlockedDetails.HasValue)
            {
                if (pathBlockedDetails.HasValue)
                    displayBanner.SetMessage(pathBlockedDetails.Value.bannerMessage);
                return;
            }

            if ((pathFormation == FormationGroups.LineGroup) || (pathFormation == FormationGroups.SquareGroup))
                displayBanner.SetMessage("Left-click to move to a location in " +
                    ((pathFormation == FormationGroups.LineGroup) ? "line" : "square") + " " +
                    "formation. Left-click on enemy unit to attack. Left Shift-click to defend.");
            if (pathFormation == FormationGroups.ColumnGroup)
                displayBanner.SetMessage("Right-click for fastest route to a location in column formation.");

            bool lineClickValid = CheckMapClickPointIsValid(Input.MouseButtons.Left)
                && ((pathFormation == FormationGroups.LineGroup) || (pathFormation == FormationGroups.SquareGroup));
            bool columnClickValid = CheckMapClickPointIsValid(Input.MouseButtons.Right)
                && (pathFormation == FormationGroups.ColumnGroup)
                && (pathStartPoint != pathEndPoint);
            if (!lineClickValid && !columnClickValid)
                return;

            if (game.DisplayPlaceInfo)
                newOrder.OrderObjective = new(instance.AllPlaces[game.NearestPlaceIndex]);
            else
                newOrder.OrderObjective = new(pathEndPoint);

            switch (pathFormation)
            {
                case FormationGroups.LineGroup: newOrder.OrderFormation = Formations.Line; break;
                case FormationGroups.ColumnGroup: newOrder.OrderFormation = Formations.Column; break;
                case FormationGroups.SquareGroup: newOrder.OrderFormation = Formations.Square; break;
            }

            MATEUnitInstance moveOverOpponentUnit = game.MouseOverUnitSnapshot(instance.OpponentArmy.Units);

            // Defend
            if ((Input.KeyIsHeld(Keys.LeftShift) || Input.KeyIsHeld(Keys.RightShift))
                && (pathFormation == FormationGroups.LineGroup)
                && (moveOverOpponentUnit == null))
            {
                newOrder.Stance = Stances.Defend;
            }

            // Skirmish / Reconnaissance
            if ((Input.KeyIsHeld(Keys.LeftControl) || Input.KeyIsHeld(Keys.RightControl))
                && (pathFormation == FormationGroups.LineGroup)
                && (OrderUnit.CanSkirmish != Skirmishing.Never))
            {
                newOrder.OrderFormation = Formations.Skirmish;
                if (moveOverOpponentUnit != null)
                    newOrder.OrderObjective = new(moveOverOpponentUnit);
            }

            // Attack
            else if (moveOverOpponentUnit != null
                && ((OrderUnit.CanSkirmish != Skirmishing.Always)
                || (pathFormation == FormationGroups.ColumnGroup)))
            {
                newOrder.Stance = Stances.Attack;
                newOrder.OrderObjective = new(moveOverOpponentUnit);
            }

            // Move
            else
            {
                newOrder.Stance = Stances.None;
            }

            State = TabStates.ConfirmingOrder;
            Rectangle buttonArea = continueButtonAvailable ? continueArea : courierArea;
            Input.MouseAutoJump(new Point(buttonArea.X + (buttonArea.Width / 2), buttonArea.Y + (buttonArea.Height / 2)));
        }

        private void UpdateSettingArtilleryTarget(Game1 game, GameInstance instance, DisplayBanner displayBanner)
        {
            if (Input.MousePositionWithinMap() == false)
                return;

            newOrder.OrderFormation = Formations.BatteryFire;
            newOrder.Stance = Stances.None;
            fireArrowErrorPoint = null;

            MATEUnitInstance opponentUnit = game.MouseOverUnitSnapshot(instance.OpponentArmy.Units);
            if (opponentUnit == null)
            {
                newOrder.OrderObjective = new(Input.mouseMapPointI);
                OrderUnit.Facing = newOrder.UnitSnapshotFormation.Facing;
                return;
            }

            newOrder.OrderObjective = new(opponentUnit);
            double distance = Map.EuclideanDistance(pathStartPoint, opponentUnit.Location);
            distance *= Map.MetersPerPixel;
            if (UtilityMethods.Bresenham3D(OrderUnit.Location, opponentUnit.Location, out PointI intersectPoint) == true)
            {
                if (distance <= OrderUnit.Range)
                {
                    int percent = (int)Math.Clamp((distance / OrderUnit.Range) * 100d, 0d, (OrderUnit.Accuracies.Count - 1));
                    double accuracy = OrderUnit.Accuracies[percent] * 100.0;
                    displayBanner.SetMessage($"Distance = {distance.ToString("N0")} meters. Accuracy = {accuracy:N1}%", alert: true);

                    if (Input.mouseLeftClick)
                    {
                        newOrder.OrderObjective = new(opponentUnit);
                        newOrder.Stance = Stances.Attack;
                        newOrder.ExecuteOrderTime = instance.CurrentGameTime + TimeSpan.FromMinutes(OrderUnit.OrderDelayTimeFromGHQ);

                        State = TabStates.ConfirmingOrder;
                        Input.MouseAutoJump(continueArea.Location + new Point(continueArea.Width / 2, continueArea.Height / 2));
                    }
                }
                else
                {
                    fireArrowErrorPoint = opponentUnit.Location;
                    displayBanner.SetMessage($"Distance = {distance.ToString("N0")} meters. Distance exceeds range.", alert: true);
                }
            }
            else
            {
                fireArrowErrorPoint = intersectPoint;
                displayBanner.SetMessage("Obstructed 3D Line of Sight to target. Cannot fire.", alert: true);
            }
        }

        private void UpdateSettingOrderTime(Game1 game, GameInstance instance, DisplayBanner displayBanner)
        {
            displayBanner.SetMessage("Use up arrows to increase hours and minutes. Use down arrows to decrease hours and minutes.");

            if (Input.mouseLeftClick == false)
                return;

            // Adjust Time
            if (new Rectangle(107, 667, 18, 15).Contains(Input.mouseMenuPoint))
                newOrderTime += TimeSpan.FromHours(1);
            if (new Rectangle(111, 731, 13, 46).Contains(Input.mouseMenuPoint))
                newOrderTime += TimeSpan.FromHours(-1);
            if (new Rectangle(142, 667, 18, 15).Contains(Input.mouseMenuPoint))
                newOrderTime += TimeSpan.FromMinutes(1);
            if (new Rectangle(146, 731, 14, 46).Contains(Input.mouseMenuPoint))
                newOrderTime += TimeSpan.FromMinutes(-1);
            TimeSpan earliestTime = instance.CurrentGameTime + TimeSpan.FromMinutes(OrderUnit.OrderDelayTimeFromGHQ);
#if DEBUG
            if (Debug.SettingsInstantCouriers)
                earliestTime = instance.CurrentGameTime;
#endif
            if (newOrderTime < earliestTime)
                newOrderTime = earliestTime;

            // Cancel
            if (new Rectangle(23, 739, 54, 26).Contains(Input.mouseMenuPoint))
                State = TabStates.ConfirmingOrder;

            // Confirm
            if (new Rectangle(183, 739, 53, 26).Contains(Input.mouseMenuPoint))
            {
                State = TabStates.ConfirmingOrder;
                newOrder.ExecuteOrderTime = newOrderTime;
            }
        }

        private void UpdateConfirmingOrder(Game1 game, GameInstance instance)
        {
            game.DisplayBanner.SetMessage("Set Time to change time of orders. Continue to add more orders. Cancel to exit without orders. Courier to dispatch courier.");

            if (Input.mouseLeftClick == false)
                return;

            // Set Time
            Rectangle setTimeArea = continueButtonAvailable ? new Rectangle(16, 570, 224, 44) : backArea;
            if (setTimeArea.Contains(Input.mouseMenuPoint))
            {
                State = TabStates.SettingOrderTime;
                newOrderTime = newOrder.ExecuteOrderTime;
                return;
            }

            // Confirm
            bool continueClicked = continueButtonAvailable && new Rectangle(16, 681, 224, 24).Contains(Input.mouseMenuPoint);
            bool courierClicked = new Rectangle(16, 727, 224, 28).Contains(Input.mouseMenuPoint);
            if ((continueClicked || courierClicked) &&
                (newOrder.PathToObjective != null) && (pathBlockedDetails == null))
            {
                newOrder.CourierDepartureTime = instance.CurrentGameTime;
                newOrder.CourierArrivalTime = instance.CurrentGameTime + TimeSpan.FromMinutes(OrderUnit.OrderDelayTimeFromGHQ);
#if DEBUG
                if (Debug.SettingsInstantCouriers)
                    newOrder.CourierArrivalTime = instance.CurrentGameTime + TimeSpan.FromSeconds(1);
#endif
                OrderUnit.Orders.AddOrder(newOrder);

                // Continue Orders
                if (continueClicked)
                {
                    State = TabStates.SelectingUnitFormation;
                    OrderUnit.Location = newOrder.UnitSnapshotEnd.Location;
                    OrderUnit.Facing = newOrder.UnitSnapshotEnd.Facing;
                    OrderUnit.Formation = newOrder.UnitSnapshotEnd.Formation;
                    Input.MouseAutoJump(game.MapOffset.ToPoint() + newOrder.OrderObjective.Location.ToMGPoint());
                    Reset(false);
                    return;
                }

                // Courier Orders
                if (courierClicked)
                {
                    List<Order> newCommands = [];
                    for (int i = OrderUnit.GetFromGameInstance(instance).Orders.Count; i < OrderUnit.Orders.Count; i++)
                    {
                        OrderUnit.Orders[i].ExecuteOrderTime = newOrderTime;
                        newCommands.Add(OrderUnit.Orders[i]);
                    }
                    instance.CurrentEvents.AddEvent(new Event_DirectOrdersSent(newCommands, sentByCourier: true));

                    game.Tabs.ChangeTab(TabNames.Reports);
                    game.Tabs.RefreshForChanges();
                    game.HeaderMenus.SaveRequired = true;
                    Reset(true);
                }
            }
        }

        private void SetMovementPaths(Game1 game, GameInstance instance)
        {
            bool objectiveSet = (State == TabStates.ConfirmingOrder) || (State == TabStates.SettingOrderTime);

            PointI? fireOrderPoint = null;
            if ((State == TabStates.SettingArtilleryTarget) && (newOrder?.OrderObjective?.Type == OrderObjective.ObjectiveTypes.Unit))
                fireOrderPoint = newOrder.OrderObjective.Unit.Location;

            if (!objectiveSet && Input.MousePositionWithinMap())
            {
                OrderUnit.Location = pathStartPoint = OrderUnit.Orders.LatestObjectiveSnapshot.Location;
                if (fireOrderPoint == null)
                    OrderUnit.Facing = OrderUnit.FacingForCommand(OrderUnit.Orders.LatestObjectiveSnapshot,
                        Input.mouseMapPointI, pathFormation);
                else
                    OrderUnit.Facing = OrderUnit.Location.AngleToFacePointInRadiansFloat(fireOrderPoint.Value);

                switch (pathFormation)
                {
                    case FormationGroups.ColumnGroup: OrderUnit.Formation = Formations.Column; break;
                    case FormationGroups.LineGroup: OrderUnit.Formation = Formations.Line; break;
                    case FormationGroups.SquareGroup: OrderUnit.Formation = Formations.Square; break;
                }
            }

            if ((pathFormation == FormationGroups.LineGroup)
                || (pathFormation == FormationGroups.SquareGroup))
            {
                if (!objectiveSet && Input.MousePositionWithinMap())
                {
                    pathEndPoint = Input.mouseMapPointI;
                    if (fireOrderPoint != null)
                        pathEndPoint = (PointI)fireOrderPoint;

                    CheckMapLineMovementIsValid();
                    if (pathBlockedDetails.HasValue || (fireArrowErrorPoint != null))
                        Input.MouseCursorIcon = MouseCursor.No;

                    newOrder.PathToObjective = UtilityMethods.BresenhamLine(pathStartPoint, pathEndPoint);
                }
            }

            if (pathFormation == FormationGroups.ColumnGroup)
            {
                if (!objectiveSet)
                    pathEndPoint = Input.mouseMapPointI;
                if ((pathStartPoint != pathEndPoint) &&
                    (!objectiveSet || (newOrder.PathToObjective == null)))
                {
                    if ((Math.Abs(pathEndPoint.X - pathLastEndPoint.X) < 5) &&
                        (Math.Abs(pathEndPoint.Y - pathLastEndPoint.Y) < 5))
                        pathEndPoint = pathLastEndPoint;

                    path = OrderUnit.Army.MapPathfinding.GetPathUsingBackgroundThread(pathStartPoint, pathEndPoint, OrderUnit, WallTypes.ROIOpponentArmy,
                        Game1.GameTimeRef.ElapsedGameTime.TotalSeconds, true);

                    if (path != null)
                    {
                        if ((path.First() == pathStartPoint) && (path.Last() == pathEndPoint))
                        {
                            newOrder.PathToObjective = path;
                            pathBlockedDetails = null;
                            //List<PointI> tPathList = Defense.ConvertPathNodes2PointList(OrderPath); // TODO (noted by MT) - needed?
                            //if (tPathList.Count >= 2)
                            //    TempUnitFacing = (float)tPathList[^(Math.Min(tPathList.Count, 15))].AngleToFacePointInRadians(tPathList[^1]);
                            //else
                            //    TempUnitFacing = (float)OrderStartPoint.AngleToFacePointInRadians(tPathList[^1]);
                        }
                        else
                        {
                            newOrder.PathToObjective = null;
                            pathBlockedDetails = (pathEndPoint, "No route available to reach location.");

                            if (Input.MousePositionWithinMap())
                                Input.MouseCursorIcon = MouseCursor.No;
                        }
                    }
                    else
                    {
                        newOrder.PathToObjective = null;
                        pathBlockedDetails = null;
                        if (Input.MousePositionWithinMap() ||
                            continueArea.Contains(Input.mouseMenuPoint) || courierArea.Contains(Input.mouseMenuPoint))
                            Input.MouseCursorIcon = MouseCursor.Wait;
                    }
                }
            }

            pathLastEndPoint = pathEndPoint;

            // Goal already set while pathfinding was still running
            // Afterwards if no routes were found then unset the goal click
            if (pathFormation == FormationGroups.ColumnGroup && objectiveSet && pathBlockedDetails.HasValue)
                State = TabStates.SettingObjective;
        }

        private bool CheckMapClickPointIsValid(Input.MouseButtons mouseButton)
        {
            if (Input.MouseClickWithinArea(mouseButton, Input.MouseCoordinateType.Map, Input.MouseOriginType.TopLeft,
                Point.Zero, new Point(Map.Width, Map.Height)) == false)
                return false;

            if (State == TabStates.SelectingUnitFormation)
                return true;

            WallTypes nodeWallType = GetWallTypeForTerrainType((Terrains)Map.Terrains[Input.mouseMapPoint.X, Input.mouseMapPoint.Y]);
            WallTypes unitBlockingWallTypes = GetWallTypesForUnitType((UnitTypes)OrderUnit.UnitType);
            return (nodeWallType & unitBlockingWallTypes) == 0;
        }

        private void CheckMapLineMovementIsValid()
        {
            if (pathEndPoint.X < 0 || pathEndPoint.X >= Map.Width ||
                pathEndPoint.Y < 0 || pathEndPoint.Y >= Map.Height)
            {
                pathBlockedDetails = (new PointI(-1), string.Empty);
                return;
            }

            List<PointI> path = UtilityMethods.BresenhamLine(pathStartPoint, pathEndPoint);
            foreach (PointI point in path)
            {
                Terrains terrainType = (Terrains)Map.Terrains[point.X, point.Y];

                if ((terrainType == Terrains.Water)
                    && (OrderUnit.IsInfantryType || OrderUnit.IsCavalryType))
                {
                    pathBlockedDetails = (point, "Units can not cross water. Units can cross fords and bridges only in column formation.");
                    return;
                }

                if ((terrainType == Terrains.Woods || terrainType == Terrains.Swamp)
                    && OrderUnit.IsCavalryType)
                {
                    pathBlockedDetails = (point, "Cavalry can not enter woods or swamp in line formation.");
                    return;
                }
            }

            pathBlockedDetails = null;
            return;
        }

        private void Reset(bool fullyResetOrderUnit)
        {
            //if (GivingCorpsOrders) // TODO (noted by MT)
            //{
            //    ActiveArmy.ReportList.RemoveRange(
            //        ActiveArmy.ReportList.Count - ActiveArmy.CorpsList[TabSubordinates.DisplayCorpsIndex].UnitsInCorps.Count,
            //        ActiveArmy.CorpsList[TabSubordinates.DisplayCorpsIndex].UnitsInCorps.Count);
            //    CorpsTactics.CancelCorpsOrders();
            //}
            //CorpsOrders = false;
            //CorpsOrdersGiven = false;

            State = TabStates.SelectingUnitFormation;

            newOrder = null;
            newOrderTime = default;

            path = null;
            pathStartPoint = new PointI(-1);
            pathEndPoint = new PointI(-1);
            pathLastEndPoint = new PointI(-1);
            pathFormation = default;
            pathBlockedDetails = null;

            drawPath = null;
            drawPathFadeTime = 0d;

            if (fullyResetOrderUnit)
                OrderUnit = null;
        }

        public void Draw(Game1 game, GameInstance instance, SpriteBatch spriteBatch)
        {
            MATEUnitInstance selectedUnit = OrderUnit ?? game.DisplayUnit;

            Vector2 width = Klabasto18.MeasureString("DIRECTLY  ORDER");
            spriteBatch.DrawString(Klabasto18, "DIRECTLY  ORDER", new Vector2(game.Tabs.LeftTabCenter - (width.X / 2), 46 - (width.Y / 2)), Color.Black);

            Rectangle r = new Rectangle(6, 800, 260, 37);
            spriteBatch.Draw(ElevationTerrainBox, r, Color.White);

            width = Smythe16.MeasureString("Elevation:");
            spriteBatch.DrawString(VictorianElevTerFont, "Elevation:", new Vector2(75 - (width.X / 2), 786), Color.Black);

            width = Smythe16.MeasureString("Terrain:");
            spriteBatch.DrawString(VictorianElevTerFont, "Terrain:", new Vector2(180 - (width.X / 2), 786), Color.Black);

            width = Smythe16.MeasureString(game.ElevationString);
            spriteBatch.DrawString(Smythe16, game.ElevationString, new Vector2(78 - (width.X / 2), 810), Color.Black);

            width = Smythe16.MeasureString(game.TerrainString);

            spriteBatch.DrawString(Smythe16, game.TerrainString, new Vector2(183 - (width.X / 2), 810), Color.Black);

            string tString = "Left-click on a unit for line formation. " +
                "At goal: left-click + CTRL for skirmish. Left-click + Shift for defend. " +
                "Right-click on a unit & goal to order it to move in column formation. " +
                "Left-click to target artillery. ";

            string[] mString = game.JustifyTextStrings(VictorianReportsFont, tString, 250);
            int leading = 0;
            int MaxLines;
            int x = 10;
            int y = 67;

            MaxLines = mString.Length;

            for (int i = 0; i < MaxLines; i++)
            {
                width = VictorianReportsFont.MeasureString(mString[i]);
                spriteBatch.DrawString(VictorianReportsFont, mString[i], new Vector2(x + 124 - (width.X / 2), y  +  leading), Color.Black);
                leading += 17;
            }

            if (OrderUnit == null)
                return;

            if (OrderUnit.Orders.Count > OrderUnit.SnapshotsByTurn[^1].OrdersCurrentIndex)
                game.DrawOnMapUnitOrdersSnapshots(OrderUnit, OrderUnit.Orders.CurrentIndex, OrderUnit.Orders.Count - 1, true, true,
                    (State != TabStates.SelectingUnitFormation) ? new(OrderUnit) : null);
            else
                game.DrawOnMapUnitSnapshot(new(OrderUnit), true, true);
            OrderUnit.GetFromGameInstance(instance).IsTempHiddenForGameUI = true;

            if ((Input.MousePositionWithinMap() || State == TabStates.ConfirmingOrder || State == TabStates.SettingOrderTime) &&
                (pathStartPoint.X >= 0) && (pathEndPoint.X >= 0))
            {
                if ((pathFormation == FormationGroups.LineGroup)
                    || (pathFormation == FormationGroups.SquareGroup))
                {
                    if (OrderUnit.IsArtilleryType)
                        DrawFireOrder(game, instance, game.spriteLayerAboveUnits);
                    else
                        AddArrow(OrderUnit.Side, pathStartPoint, pathBlockedDetails?.point ?? pathEndPoint, instance.ScenarioPieceWidth, true);
                }

                if (pathFormation == FormationGroups.ColumnGroup)
                {
                    if (path != drawPath)
                    {
                        drawPath = path;
                        drawPathFadeTime = GameTimeRef.TotalGameTime.TotalSeconds;
                    }
                    float pathFade = (float)Math.Clamp((GameTimeRef.TotalGameTime.TotalSeconds - drawPathFadeTime) * 3f, 0f, 1f);

                    if (drawPath != null)
                        AddRoute(OrderUnit.Side, drawPath, 16, true, pathFade);

                    if ((drawPath == null) || (pathFade < 1f))
                    {
                        Vector2 start = pathStartPoint.ToVector2();
                        float distance = Vector2.Distance(start, pathEndPoint.ToVector2());
                        Vector2 direction = Vector2.Normalize(pathEndPoint.ToVector2() - start);
                        Vector2 lastPosition = new Vector2(-1, -1);
                        List<Vector2> dotPositions = new List<Vector2>();
                        for (int i = 0; i <= (int)distance; i += 32)
                        {
                            Vector2 position = start + (i * direction);
                            if (position != lastPosition)
                                dotPositions.Add(position);
                            lastPosition = position;
                        }
                        AddRoute(OrderUnit.Side, dotPositions, 16, false, (drawPath != null) ? (1f - pathFade) : 1f);
                    }
                }

                if (pathBlockedDetails.HasValue)
                    AddCross(pathBlockedDetails.Value.point, 12, Color.White, Color.DarkRed, 10);
            }

            if (State != TabStates.SelectingUnitFormation)
                DrawOrdersBox(game, instance, spriteBatch);
        }

        private void DrawOrdersBox(Game1 game, GameInstance instance, SpriteBatch spriteBatch)
        {
            int x = 12;
            int y = 200;
            string tString;
            string[] mString = null;

            Rectangle r = new Rectangle(2, 187, 263, 592);
            spriteBatch.Draw(VictorianOrdersBox, r, Color.White);

            tString = OrderUnit.Name;

            Vector2 width = BigUnitInfoNameFont.MeasureString(tString);

            mString = game.JustifyTextStrings(BigUnitInfoNameFont, tString, 235);
            int leading = 0;
            int MaxLines = 2;

            if (mString.Length < 2)
                MaxLines = mString.Length;

            int YStart = 16;

            if (MaxLines == 2)
                YStart = 8;
            else if (MaxLines == 1)
                YStart = 16;

            for (int i = 0; i < MaxLines; i++)
            {
                width = BigUnitInfoNameFont.MeasureString(mString[i]);
                spriteBatch.DrawString(BigUnitInfoNameFont, mString[i], new Vector2(x + 120 - (width.X / 2), y - (width.Y / 2) + YStart + leading), Color.Black);
                leading += 32;
            }

            if (OrderUnit.GetFromGameInstance(instance) != OrderUnit.Army.GHQ)
            {
                // was 71
                y += 58;
                if (OrderUnit.IsHQ)
                {
                    tString = $"Couriers from your General HQ to this HQ will travel {(int)OrderUnit.CourierRouteDistanceGHQ:N0} meters " +
                        $"taking {OrderUnit.OrderDelayTimeFromGHQ} minutes at {game.CourierSpeedKmh.ToString("0.00")} KmPH.";
                }
                else
                {
                    tString = $"Couriers from {instance.ActiveArmy.Units[0].Name} to {OrderUnit.CommandingUnit.Name} and then to the unit, " +
                        $"factoring in Leadership Cost Delays, will arrive no sooner than " +
                        (instance.CurrentGameTime + TimeSpan.FromMinutes(OrderUnit.OrderDelayTimeFromGHQ)).ToString("h\\:mm");
                }

                mString = game.JustifyTextStrings(RudyardSmall, tString, 255);
                leading = 0;
                MaxLines = mString.Length;

                YStart = 0;
                if (MaxLines == 5)
                    YStart = 1;
                if (MaxLines == 4)
                    YStart = 9;
                else if (MaxLines == 3)
                    YStart = 18;
                else if (MaxLines == 2)
                    YStart = 27;
            }

            for (int i = 0; i < MaxLines; i++)
            {
                width = RudyardSmall.MeasureString(mString[i]);
                spriteBatch.DrawString(RudyardSmall, mString[i], new Vector2(x + 124 - (width.X / 2), y /*- (width.Y / 2)*/ + YStart + leading), Color.Black);
                leading += 22;
            }

            if (State == TabStates.SettingObjective || State == TabStates.SettingArtilleryTarget)
                return;

            y += 26;
            // Display order
            tString = "Your Orders";
            width = Amaltea24.MeasureString(tString);
            spriteBatch.DrawString(Amaltea24, tString, new Vector2(x + 120 - (width.X / 2), y /* - (width.Y / 2)*/ + YStart + leading - 16), Color.Black);

            tString = "At " + newOrder.ExecuteOrderTime.ToString("h\\:mm") + " ";

            string unitTypeString = (newOrder.OrderObjective.Type == OrderObjective.ObjectiveTypes.Unit) ? newOrder.OrderObjective.Unit.UnitTypeName : "";
            string formationString = newOrder.OrderFormation.ToString().ToLower() + " formation";
            if (newOrder.OrderFormation == Formations.Skirmish)
                formationString = $"skirmish line formation";
            string objectiveString = newOrder.OrderObjective.Place?.Name ??
                $"({(int)newOrder.OrderObjective.Location.X}/{(int)newOrder.OrderObjective.Location.Y})";

            if (newOrder.OrderFormation == Formations.BatteryFire)
                tString += $"fire battery at enemy {unitTypeString} at {objectiveString}";
            else if (newOrder.Stance == Stances.Attack)
                tString += $"attack enemy {unitTypeString} in {formationString}";
            else if (newOrder.Stance == Stances.Defend)
                tString += $"defend the position at {objectiveString}";
            else if ((newOrder.OrderFormation == Formations.Column)
                || (newOrder.OrderFormation == Formations.Line)
                || (newOrder.OrderFormation == Formations.Square)
                || (newOrder.OrderFormation == Formations.Skirmish))
                tString += $"move in {formationString} to {objectiveString}";
            tString += " as shown on the map.";

            //TempReportString = tString;

            mString = game.JustifyTextStrings(Ephinol9, tString, 235);
            leading = 0;

            MaxLines = mString.Length;
            int LeadingIncrement;

            if (MaxLines == 6)
                YStart = 15;
            else if (MaxLines == 5)
                YStart = 24;
            else if (MaxLines == 4)
                YStart = 43;
            else if (MaxLines == 3)
                YStart = 42;
            else if (MaxLines == 2)
                YStart = 21; // TODO (noted by MT) looks broken

            YStart = 48;

            if (MaxLines == 6)
                LeadingIncrement = 20;
            else
                LeadingIncrement = 28;

            y += 120;

            for (int i = 0; i < MaxLines; i++)
            {
                width = Ephinol9.MeasureString(mString[i]);
                spriteBatch.DrawString(Ephinol9, mString[i], new Vector2(x + 120 - (width.X / 2), y + YStart + leading), Color.Black);
                leading += LeadingIncrement;
            }

            bool continueAvailable = newOrder.Stance != Stances.Attack && newOrder.Stance != Stances.Defend
                && newOrder.OrderFormation != Formations.Skirmish && newOrder.OrderFormation != Formations.BatteryFire;

            y = 600;
            if (!continueAvailable)
                y += 40;

            tString = "{Set|Time}";
            width = Phectic36.MeasureString(tString);
            spriteBatch.DrawString(Phectic36, tString, new Vector2(x + 124 - (width.X / 2), y - (width.Y / 2)), Color.Black);

            y += 50;
            tString = "{Back}";
            width = Phectic36.MeasureString(tString);
            spriteBatch.DrawString(Phectic36, tString, new Vector2(x + 124 - (width.X / 2), y - (width.Y / 2)), Color.Black);

            float threadWaitFadeIn = ((newOrder.PathToObjective != null) && (pathBlockedDetails == null)) ? 1f : 0.5f;

            if (continueAvailable)
            {
                y += 50;
                tString = "{Continue}";
                width = Phectic36.MeasureString(tString);
                spriteBatch.DrawString(Phectic36, tString, new Vector2(x + 124 - (width.X / 2), y - (width.Y / 2)),
                    Color.Black * threadWaitFadeIn);
            }

            y += 50;
            tString = "{Courier}";
            width = Phectic36.MeasureString(tString);
            spriteBatch.DrawString(Phectic36, tString, new Vector2(x + 124 - (width.X / 2), y - (width.Y / 2)), Color.Black * threadWaitFadeIn);

            if (State == TabStates.SettingOrderTime)
            {
                r = new Rectangle(9, 616, 250, 154);
                spriteBatch.Draw(VictorianSetClock, r, Color.White);

                tString = newOrderTime.ToString("hh\\:mm");
                width = Rudyard36.MeasureString(tString);
                spriteBatch.DrawString(Rudyard36, tString, new Vector2(game.Tabs.LeftTabCenter - (width.X / 2), 710 - (width.Y / 2)), Color.Black);
            }

            game.TerrainString = Map.ReturnTerrainStringAtPoint(newOrder.OrderObjective.Location);
            game.ElevationString = Map.Elevations[newOrder.OrderObjective.Location.X, newOrder.OrderObjective.Location.Y].ToString("0.00");
            game.DisplayPlaceInfo = newOrder.OrderObjective.Type == OrderObjective.ObjectiveTypes.Place;
            return;
        }

        private void DrawFireOrder(Game1 game, GameInstance instance, SpriteBatch spriteBatch)
        {
            Vector2 lineStart = pathStartPoint.ToMGVector2();
            Vector2 target = pathEndPoint.ToMGVector2();
            Vector2 direction = Vector2.Normalize(target - lineStart);
            Vector2 perpendicularDirection = new Vector2(-direction.Y, direction.X);
            float unitRangeInPixels = (float)(OrderUnit.Range / Map.MetersPerPixel);
            Vector2 lineEnd = lineStart + (direction * unitRangeInPixels);

            game.DrawLine(spriteBatch, lineStart + game.MapOffset, lineEnd + game.MapOffset, Color.Black * 0.8f, 2);

            double nextAccuracyMarker = 0.9d;
            for (int i = 1; i <= OrderUnit.Accuracies.Count; i++)
            {
                double accuracy = (i < OrderUnit.Accuracies.Count) ? OrderUnit.Accuracies[i] : 0d;
                if (accuracy <= nextAccuracyMarker)
                {
                    float accuracyMarkerDistance = ((float)i / OrderUnit.Accuracies.Count) * unitRangeInPixels;
                    Vector2 accuracyMarkerPosition = lineStart + (direction * accuracyMarkerDistance);
                    Vector2 accuracyMarkerSize = ((nextAccuracyMarker % 0.5) < 0.01) ? new Vector2(8, 2) : new Vector2(4, 1);
                    game.DrawLine(spriteBatch, accuracyMarkerPosition + (perpendicularDirection * -accuracyMarkerSize.X) + game.MapOffset,
                        accuracyMarkerPosition + (perpendicularDirection * accuracyMarkerSize.X) + game.MapOffset,
                        Color.Black * 0.8f, (int)accuracyMarkerSize.Y, false);
                    nextAccuracyMarker -= 0.1d;
                }
            }

            Vector2 crossPosition = Vector2.Zero;
            if (fireArrowErrorPoint != null)
            {
                crossPosition = ((PointI)fireArrowErrorPoint).ToMGVector2();
                if (Vector2.DistanceSquared(lineStart, crossPosition) >
                    Vector2.DistanceSquared(lineStart, lineEnd))
                    crossPosition = lineEnd;
                AddCross(crossPosition.ToPointIRounded(), 18, Color.White, Color.DarkRed, 15);
            }

            if (newOrder?.OrderObjective?.Unit != null)
                game.DrawOnMapFireArrow(lineStart.ToPointI(), ((fireArrowErrorPoint == null) ? target : crossPosition).ToPointI());
            else
                game.FireArrowDistance = 0;
        }
    }
}
