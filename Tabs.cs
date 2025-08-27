using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ModelLib;
using TacticalAILib;
//using static GSBPGEMG.Game1;

namespace GSBPGEMG
{
    public enum TabNames { Reports, Scenario, DirectOrder, Subordinates, EndTurn }

    public class Tabs
    {
        public TabReports TabReports { get; private set; }
        public TabScenario TabScenario { get; private set; }
        public TabDirectOrders TabDirectOrders { get; private set; }
        //public TabSubordinates TabSubordinates { get; private set; }
        public TabEndTurn TabEndTurn { get; private set; }

        public List<TabNames> VisibleTabs = [];

        public int TabsEndIndex = Enum.GetNames(typeof(TabNames)).Length - 1;

        public TabNames TabSelected { get; private set; } = TabNames.Reports;
        public TabNames LastTabSelected { get; private set; }

        public EventBase CurrentGameEventDisplayed { get; private set; }

        public int LeftTabCenter = 134;
        public Rectangle LeftArrowArea = new Rectangle(5, 32, 56, 28);
        public Rectangle RightArrowArea = new Rectangle(208, 32, 56, 28);

        public Tabs()
        {
            TabReports = new();
            TabScenario = new();
            TabDirectOrders = new();
            //TabSubordinates = new();
            TabEndTurn = new();
        }

        public void Update(Game1 game, GameInstance instance)
        {
            LastTabSelected = TabSelected;

            // Set visible tabs
            SetTabsVisible(game);

            // Existing tab no longer available
            if (VisibleTabs.Contains(TabSelected) == false)
                TabSelected = VisibleTabs[0];

            //Select tab
            if (game.HeaderMenus.MenuSelected == Menus.None)
            {
                bool tabLeft = false, tabRight = false;

                if (Input.mouseLeftClick)
                {
                    if (LeftArrowArea.Contains(Input.mouseMenuPoint))
                        tabLeft = true;
                    if (RightArrowArea.Contains(Input.mouseMenuPoint))
                        tabRight = true;
                }

                if ((Input.mouseScrollWheelChange != 0) &&
                    Input.MousePositionWithinArea(Input.MouseCoordinateType.Menu, Input.MouseOriginType.TopLeft,
                    new Point(LeftArrowArea.X, LeftArrowArea.Y), new Point(RightArrowArea.X + RightArrowArea.Width, RightArrowArea.Y)))
                    if (Input.mouseScrollWheelChange > 0)
                        tabLeft = true;
                    else
                        tabRight = true;

                if (tabLeft || tabRight)
                {
                    int tabIndex = VisibleTabs.IndexOf(TabSelected);
                    if (tabLeft)
                        tabIndex = (tabIndex >= 1) ? tabIndex - 1 : VisibleTabs.Count - 1;
                    if (tabRight)
                        tabIndex = (tabIndex < (VisibleTabs.Count - 1)) ? tabIndex + 1 : 0;
                    TabSelected = VisibleTabs[tabIndex];
                }

                if (Input.KeyIsHeld(Keys.LeftShift) || Input.KeyIsHeld(Keys.RightShift))
                {
                    if (Input.KeyIsPressed(Keys.R))
                        ChangeTab(TabNames.Reports);
                    if (Input.KeyIsPressed(Keys.C))
                        ChangeTab(TabNames.Scenario);
                    if (Input.KeyIsPressed(Keys.D))
                        ChangeTab(TabNames.DirectOrder);
                    if (Input.KeyIsPressed(Keys.S))
                        ChangeTab(TabNames.Subordinates);
                    if (Input.KeyIsPressed(Keys.E))
                        ChangeTab(TabNames.EndTurn);
                }

                // Select display unit
                //if ((TabSelected != TabNames.DirectOrder || !TabDirectOrders.GivingDirectOrders) &&
                //    (TabSelected != TabNames.Subordinates || !TabDirectOrders.GivingCorpsOrders))
                //{
                game.ClearAllUnitsPulsing(instance);
                MATEUnitInstance unit = game.MouseOverUnitSnapshot(instance.AllArmyUnits);
                if (unit != null)
                {
                    if ((game.DisplayUnit?.Army != unit.Army) || (game.DisplayUnit?.Index != unit.Index))
                        game.DisplayUnit = unit.Clone(instance);
                    game.DisplayUnitInfo = true;
                    game.DisplayUnit.IsPulsing = true;
                }
                else
                {
                    game.DisplayUnit = null;
                    game.DisplayUnitInfo = false;
                }
                //}
            }

            // Tab just changed
            if ((TabSelected != LastTabSelected) ||
                (Input.KeyIsPressed(Keys.Escape) && (TabSelected == TabNames.DirectOrder) && (TabDirectOrders.State != TabDirectOrders.TabStates.SelectingUnitFormation)))
            {
                game.ClearAllUnitsPulsing(instance);
                if (TabSelected != TabNames.Reports)
                {
                    CurrentGameEventDisplayed = instance.CurrentEvents[^1];
                    instance.SetAllDisplayedValuesToEvent(CurrentGameEventDisplayed, applyVisibilityStatesForArmy: instance.ActiveArmy);
                }
            }

            // Update current tab
            if (game.HeaderMenus.MenuSelected == Menus.None)
            {
                switch (TabSelected)
                {
                    case TabNames.Reports: TabReports.Update(game); break;
                    case TabNames.Scenario: TabScenario.Update(game); break;
                    case TabNames.DirectOrder: TabDirectOrders.Update(game); break;
                    //case TabNames.Subordinates: TabSubordinates.Update(); break;
                    case TabNames.EndTurn: TabEndTurn.Update(game, game.ProcessTurn); break;
                }
            }
        }

        public void SetTabsVisible(Game1 game)
        {
            VisibleTabs.Clear();
            if (game.GameplayScreen != Game1.GameplayScreens.EndingTurn)
            {
                VisibleTabs.Add(TabNames.Reports);
                VisibleTabs.Add(TabNames.Scenario);
            }
            if (game.GameplayScreen == Game1.GameplayScreens.Playing)
            {
                VisibleTabs.Add(TabNames.DirectOrder);
                //VisibleTabs.Add(TabNames.Subordinates); // TODO (noted by MT) subordinates tab disabled
            }
            VisibleTabs.Add(TabNames.EndTurn);
        }

        public bool ChangeTab(TabNames tabRequested)
        {
            if (VisibleTabs.Contains(tabRequested))
            {
                TabSelected = tabRequested;
                return true;
            }
            return false;
        }

        public void ChangeDisplayedGameEvent(EventBase gameEvent, EventBase gameEventForDisplayedChanges = null)
        {
            CurrentGameEventDisplayed = gameEvent;
            gameEvent.GameInstance.SetAllDisplayedValuesToEvent(CurrentGameEventDisplayed, gameEventForDisplayedChanges,
                applyVisibilityStatesForArmy: gameEvent.GameInstance.ActiveArmy);
        }

        public void RefreshForChanges()
        {
            TabReports.RefreshForChanges();
        }

        public void Draw(Game1 game, GameInstance instance)
        {
            Color color = Color.White * (game.GameplayScreen != Game1.GameplayScreens.EndingTurn ? 1f : 0.25f);
            game.spriteLayerTabs.Draw(Game1.VictorianLeftPointingFinger, LeftArrowArea, color);
            game.spriteLayerTabs.Draw(Game1.VictorianRightPointingFinger, RightArrowArea, color);

            switch (TabSelected)
            {
                case TabNames.Reports: TabReports.Draw(game, this, game.spriteLayerTabs); break;
                case TabNames.Scenario: TabScenario.Draw(game, game.Instance, this, game.spriteLayerTabs); break;
                case TabNames.DirectOrder: TabDirectOrders.Draw(game, game.Instance, game.spriteLayerTabs); break;
                //case TabNames.Subordinates: TabSubordinates.Draw(); break;
                case TabNames.EndTurn: TabEndTurn.Draw(game, game.Instance, this, game.ProcessTurn, game.spriteLayerTabs); break;
            }

            // Cursor over unit, display info about unit
            if (game.DisplayUnitInfo && /*!TabDirectOrders.ContinueClick && !TabDirectOrders.MovementError &&*/ // TODO (noted by MT)
                ((TabSelected == TabNames.Reports) || (TabSelected == TabNames.Subordinates) ||
                (TabSelected == TabNames.DirectOrder && TabDirectOrders.SetOrdersInProgress == false)))
            {
                if ((game.DisplayUnit.Army == instance.ActiveArmy) && (game.DisplayUnit.Orders.Current != null))
                {
                    game.DrawOnMapUnitOrdersSnapshots(game.DisplayUnit, game.DisplayUnit.SnapshotDisplayed.OrdersCurrentIndex, game.DisplayUnit.Orders.Count - 1, true, false);
                    game.DisplayUnit.GetFromGameInstance(instance).IsTempHiddenForGameUI = true;
                    instance.AllArmies[game.DisplayUnit.Army.Index].Units[game.DisplayUnit.Index].IsTempHiddenForGameUI = true;
                }
                game.DrawOnMapUnitInfo(game.DisplayUnit);
            }

            if (Input.MousePositionWithinMap())
            {
                game.TerrainString = Map.ReturnTerrainStringAtPoint(Input.mouseMapPointI);
                game.ElevationString = Map.Elevations[Input.mouseMapPoint.X, Input.mouseMapPoint.Y].ToString("0.00");
                game.DisplayPlaceInfo = game.IsPlaceUnderMouse();
            }
            else
            {
                if (((TabSelected == TabNames.DirectOrder) || (TabSelected == TabNames.Subordinates))
                    && ((TabDirectOrders.State == TabDirectOrders.TabStates.SettingOrderTime) || (TabDirectOrders.State == TabDirectOrders.TabStates.ConfirmingOrder)))
                    return;

                game.TerrainString = game.ElevationString = "";
                game.DisplayPlaceInfo = false;
            }
        }
    }
}
