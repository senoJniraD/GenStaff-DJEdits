using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ModelLib;
using TacticalAILib;
using GSBPGEMG.UI;
using System.Linq;

namespace GSBPGEMG
{
    public class TabReports
    {
        public EventBase EventSelected { get; private set; }

        private UIElement_List ReportsList { get; set; } =
            new UIElement_List(new(4, 90), new(245, 720), 90, 6) { lineGap = null };

        private readonly List<EventBase> displayedEvents = [];
        private readonly List<UIElement_TextBox> textBoxes = [];
        private readonly List<bool> turnsVisible = [];
        private readonly List<bool> turnsExpandable = [];

        private bool jumpToEndRequest;
        private bool? jumpToEndDrawnTextMeasured;

        private Vector2 drawLinePosition;

        public void Update(Game1 game)
        {
            GameInstance instance = game.Instance;

            while (turnsVisible.Count < instance.CurrentGameTurn)
            {
                turnsVisible.Add(false);
                turnsExpandable.Add(false);
            }

            if (jumpToEndRequest)
            {
                for (int i = 0; i < turnsVisible.Count; i++)
                {
                    turnsVisible[i] = false;
                    turnsExpandable[i] = false;
                }
                turnsVisible[^1] = true;
            }

            displayedEvents.Clear();
            for (int i = 0; i < instance.CurrentEvents.Count; i++)
            {
                EventBase gameEvent = instance.CurrentEvents[i];
                if (FindArmyWhereReportVisible(gameEvent) != null)
                {
                    int gameEventTurn = Math.Max(0, gameEvent.Turn - 1);
                    if (turnsVisible[gameEventTurn] || gameEvent.EventType == EventTypes.GameTurnChanged)
                        displayedEvents.Add(gameEvent);
                    if (gameEvent.EventType != EventTypes.GameTurnChanged)
                        turnsExpandable[gameEventTurn] = true;
                }
            }

            while (displayedEvents.Count > textBoxes.Count)
                textBoxes.Add(new UIElement_TextBox() { TooltipMode = UIElement_TextBox.TooltipModes.OnOverflow });

            // Find lowest display start
            float maxDisplayStartPosition = displayedEvents.Count - 1;
            float heightRemainingToFill = ReportsList.size.Y;
            for (int i = displayedEvents.Count - 1; i >= 0; i--)
            {
                int reportHeight = GetReportItemHeight(displayedEvents[i], textBoxes[i]);
                heightRemainingToFill -= reportHeight;
                if (heightRemainingToFill <= 0)
                {
                    maxDisplayStartPosition = i + (Math.Abs(heightRemainingToFill) / reportHeight);
                    break;
                }
            }
            // scrolling hang fix 7/10/25 for MT
            if (heightRemainingToFill > 0)
                maxDisplayStartPosition = 0f;

            // Update List
            ReportsList.Update(displayedEvents, true, requestedMaxDisplayStart: maxDisplayStartPosition);
            ReportsList.lerpPosition = Math.Min(ReportsList.lerpPosition, maxDisplayStartPosition);

            // Custom selection code as list items vary in height
            ReportsList.highlighted = false;
            if (Input.MousePositionWithinArea(Input.MouseCoordinateType.Menu, Input.MouseOriginType.TopLeft,
                ReportsList.position.ToPoint(), new((int)ReportsList.size.X - 5, (int)ReportsList.size.Y)))
            {
                int index = (int)ModelLib.MathHelper.Clamp((int)ReportsList.lerpPosition, 0, displayedEvents.Count -1);
                int y = (int)ReportsList.position.Y;
                if ((ReportsList.lerpPosition % 1f) != 0)
                    y -= (int)((ReportsList.lerpPosition % 1f) * GetReportItemHeight(displayedEvents[index], textBoxes[index]));

                while ((y < (ReportsList.size.Y + 60)) && (index < displayedEvents.Count))
                {
                    if ((textBoxes[index].MeasuredSize.Y == 0) && 
                        (displayedEvents[index].EventType != EventTypes.GameTurnChanged))
                        break;

                    if (Input.MousePositionWithinArea(Input.MouseCoordinateType.Menu, Input.MouseOriginType.TopLeft,
                        new((int)ReportsList.position.X, y - 7), new((int)ReportsList.size.X - 5, GetReportItemHeight(displayedEvents[index], textBoxes[index]))))
                    {
                        ReportsList.highlighted = true;
                        ReportsList.selection = index;
                        break;
                    }

                    y += GetReportItemHeight(displayedEvents[index], textBoxes[index]);
                    index++;
                    if (index >= displayedEvents.Count)
                        break;
                }
            }

            int? selectionRequest = null;
            if (Input.mouseLeftClick && ReportsList.highlighted)
            {
                EventBase eventSelected = displayedEvents[ReportsList.selection];
                if ((eventSelected.EventType == EventTypes.GameTurnChanged) && turnsExpandable[eventSelected.Turn - 1] &&
                    (eventSelected == EventSelected))
                    turnsVisible[eventSelected.Turn - 1] = !turnsVisible[eventSelected.Turn - 1];
                if (EventSelected != eventSelected)
                    selectionRequest = ReportsList.selection;
            }
            if (jumpToEndRequest)
            {
                selectionRequest = displayedEvents.Count - 1;
                ReportsList.displayStart = ReportsList.lerpPosition = maxDisplayStartPosition;
                EventSelected = null;
                jumpToEndDrawnTextMeasured ??= false;
            }
            else
            {
                if (game.Tabs.LastTabSelected != TabNames.Reports)
                {
                    int newIndex = displayedEvents.IndexOf(EventSelected);
                    if (newIndex >= 0)
                        selectionRequest = newIndex;
                }
            }

            if (selectionRequest != null)
            {
                ReportsList.selection = selectionRequest.Value;

                if (ReportsList.selection < ReportsList.displayStart)
                {
                    ReportsList.displayStart = ReportsList.selection;
                }
                else if (ReportsList.selection > ReportsList.displayStart)
                {
                    int y = 0;
                    List<int> heights = [];
                    for (int i = (int)ReportsList.displayStart; i <= ReportsList.selection; i++)
                    {
                        heights.Add(GetReportItemHeight(displayedEvents[i], textBoxes[i]));
                        y += heights[^1];
                        if (y > ReportsList.size.Y)
                        {
                            if (i == ReportsList.selection)
                            {
                                while ((heights.Count >= 1) && (y > ReportsList.size.Y))
                                {
                                    ReportsList.displayStart = Math.Min(ReportsList.displayStart + 1, maxDisplayStartPosition);
                                    y -= heights[0];
                                    heights.RemoveAt(0);
                                }
                            }
                            break;
                        }
                    }
                }

                EventSelected = displayedEvents[ReportsList.selection];
                EventBase eventForDisplayedChanges;
                if (EventSelected.EventType == EventTypes.GameTurnChanged)
                    eventForDisplayedChanges = instance.CurrentEvents[instance.CurrentEvents.TurnChangedEventIndexes[Math.Max(0, EventSelected.Turn - 2)]];
                else
                    eventForDisplayedChanges = displayedEvents[Math.Max(0, displayedEvents.IndexOf(EventSelected) - 1)];
                if ((EventSelected.Turn == game.Tabs.CurrentGameEventDisplayed?.Turn) && (EventSelected.Turn == instance.CurrentGameTurn))
                    game.fogOfWarTextureGameEvent = EventSelected;
                game.Tabs.ChangeDisplayedGameEvent(EventSelected, eventForDisplayedChanges);
            }

            if (jumpToEndRequest && (jumpToEndDrawnTextMeasured == true))
            {
                jumpToEndRequest = false;
                jumpToEndDrawnTextMeasured = null;
            }

            TimeSpan bannerTime = EventSelected.Time;
            if (bannerTime < game.Instance.ScenarioStartTime)
                bannerTime = game.Instance.ScenarioStartTime;

            // Added by Ezra 06/22/25
            if((int)game.Instance.ActiveArmy.Side == (int) Sides.Blue)
                game.DisplayBanner.SetMessage($"{game.Instance.ScenarioName} {bannerTime:hh\\:mm} Blue Turn {EventSelected.Turn}.");
            else
                game.DisplayBanner.SetMessage($"{game.Instance.ScenarioName} {bannerTime:hh\\:mm} Red Turn {EventSelected.Turn}.");
        }

        public int GetReportItemHeight(EventBase reportEvent, UIElement_TextBox textBox)
        {
            if (reportEvent.EventType == EventTypes.GameTurnChanged)
                return 160;
            else
                return Math.Max(120, textBox.MeasuredSize.Y + 10);
        }

        private ArmyInstance FindArmyWhereReportVisible(EventBase reportEvent)
        {
            GameInstance instance = reportEvent.GameInstance;
            ArmyInstance army = instance.ActiveArmy;

            if (reportEvent.VisibleOnTabReports())
                return instance.ActiveArmy;

            if (Debug.SettingsShowAllReports)
            {
                for (int i = 0; i < instance.AllArmies.Count; i++)
                    if ((i != instance.ActiveArmy.Index) && (reportEvent.VisibleOnTabReports(instance.AllArmies[i])))
                        return instance.AllArmies[i];
                return instance.ActiveArmy;
            }

            return null;
        }

        public void RefreshForChanges() => jumpToEndRequest = true;

        public void Draw(Game1 game, Tabs tabs, UIElement_SpriteLayer spriteBatch)
        {
            Vector2 width = Game1.Klabasto18.MeasureString("REPORTS");
            spriteBatch.DrawString(Game1.Klabasto18, "REPORTS",
                new Vector2(tabs.LeftTabCenter - (width.X / 2), 46 - (width.Y / 2)), Color.Black);

            drawLinePosition = Vector2.Zero;
            spriteBatch.postEndActions.Add(() =>
            { ReportsList.Draw(game, DrawReportItem, 1f, displayedEvents.Count); });

            if (jumpToEndRequest)
                jumpToEndDrawnTextMeasured = true;

            if (EventSelected == null)
                return;

            EventBase eventToDraw = EventSelected;
            if (eventToDraw.EventType == EventTypes.UnitCasualties)
            {
                for (int i = EventSelected.Index; i >= game.Instance.CurrentEvents.TurnChangedEventIndexes[EventSelected.Turn - 1]; i--)
                {
                    if ((game.Instance.CurrentEvents[i].EventType == EventTypes.MeleeCombat) ||
                        (game.Instance.CurrentEvents[i].EventType == EventTypes.RangedCombat))
                    {
                        eventToDraw = game.Instance.CurrentEvents[i];
                        break;
                    }
                }
            }

            switch (eventToDraw.EventType)
            {
                case EventTypes.UnitVisibilityChanged:
                    Event_UnitVisibilityChanged eventUnitVisibilityChanged = (Event_UnitVisibilityChanged)eventToDraw;
                    if ((Game1.GameTimeRef.TotalGameTime.TotalSeconds % 1.5f) < 0.75f)
                        game.DrawOnMapUnitSnapshot(eventUnitVisibilityChanged.Unit.SnapshotDisplayed, true, false);
                    game.DrawOnMapUnitSnapshot(eventUnitVisibilityChanged.VisibilityState.FromPerspectiveOfUnit.SnapshotDisplayed, true, false);
                    eventUnitVisibilityChanged.Unit.IsTempHiddenForGameUI = true;
                    eventUnitVisibilityChanged.VisibilityState.FromPerspectiveOfUnit.IsTempHiddenForGameUI = true;
                    break;

                case EventTypes.DirectOrdersSent:
                    Event_DirectOrdersSent eventDirectOrdersSent = (Event_DirectOrdersSent)eventToDraw;
                    Order command = eventDirectOrdersSent.ObjectiveCommand;
                    game.DrawOnMapUnitOrdersSnapshots(eventDirectOrdersSent.Unit,
                        eventDirectOrdersSent.Unit.SnapshotDisplayed.OrdersCurrentIndex, command.Index, true, false);
                    eventDirectOrdersSent.Unit.IsTempHiddenForGameUI = true;
                    break;

                case EventTypes.MeleeCombat:
                    Event_MeleeCombat eventMeleeCombat = (Event_MeleeCombat)eventToDraw;
                    game.DrawOnMapUnitSnapshot(eventMeleeCombat.AttackingUnitSnapshot, true, false);
                    game.DrawOnMapUnitSnapshot(eventMeleeCombat.DefendingUnitSnapshot, true, false);
                    eventMeleeCombat.AttackingUnitSnapshot.Unit.IsTempHiddenForGameUI = true;
                    eventMeleeCombat.DefendingUnitSnapshot.Unit.IsTempHiddenForGameUI = true;
                    break;

                case EventTypes.RangedCombat:
                    Event_RangedCombat eventRangedCombat = (Event_RangedCombat)eventToDraw;
                    game.DrawOnMapUnitSnapshot(eventRangedCombat.FiringUnitSnapshot, true, false);
                    game.DrawOnMapUnitSnapshot(eventRangedCombat.TargetUnitSnapshot, true, false);
                    eventRangedCombat.FiringUnitSnapshot.Unit.IsTempHiddenForGameUI = true;
                    eventRangedCombat.TargetUnitSnapshot.Unit.IsTempHiddenForGameUI = true;
                    if (eventRangedCombat.FiringUnitSnapshot.VisibilityToArmies[game.Instance.ActiveArmy.Index].IsVisible)
                        game.DrawOnMapFireArrow(eventRangedCombat.FiringUnitSnapshot.Location,
                            eventRangedCombat.TargetUnitSnapshot.Location);
                    break;

                case EventTypes.UnitRouted:
                    Event_UnitRouted eventUnitRouted = (Event_UnitRouted)eventToDraw;
                    game.DrawOnMapUnitSnapshot(eventUnitRouted.RoutedUnitSnapshot, true, false);
                    game.DrawOnMapUnitSnapshot(eventUnitRouted.InitiatingUnitSnapshot, true, false);
                    eventUnitRouted.RoutedUnitSnapshot.Unit.IsTempHiddenForGameUI = true;
                    eventUnitRouted.InitiatingUnitSnapshot.Unit.IsTempHiddenForGameUI = true;
                    break;

                case EventTypes.UnitStoppedRouting:
                    Event_UnitStoppedRouting eventUnitStoppedRouting = (Event_UnitStoppedRouting)eventToDraw;
                    game.DrawOnMapUnitSnapshot(eventUnitStoppedRouting.RoutedUnitSnapshot, true, false);
                    eventUnitStoppedRouting.RoutedUnitSnapshot.Unit.IsTempHiddenForGameUI = true;
                    break;

                case EventTypes.UnitKIA:
                    Event_UnitKIA event_UnitKIA = (Event_UnitKIA)eventToDraw;
                    if ((Game1.GameTimeRef.TotalGameTime.TotalSeconds % 1.5f) < 0.75f)
                        game.DrawOnMapUnitSnapshot(event_UnitKIA.UnitSnapshot, true, false);
                    else
                        game.DrawOnMapAnimateFlag(event_UnitKIA.UnitSnapshot.Unit.EnemyArmy, event_UnitKIA.UnitSnapshot.Location + new PointI(2));
                    UIElement_PathRenderer.AddCross(event_UnitKIA.UnitSnapshot.Location, 8, Color.Black, Color.Red, 3);
                    break;

                case EventTypes.PlaceCaptured:
                    Event_PlaceCaptured eventPlaceCaptured = (Event_PlaceCaptured)eventToDraw;
                    game.DrawOnMapAnimateFlag(eventPlaceCaptured.Army, eventPlaceCaptured.Place.Location);
                    break;
            }
        }

        public void DrawReportItem(Game1 game, SpriteBatch spriteBatch, int index, Vector2 position, bool selected)
        {
            if ((index >= displayedEvents.Count) || (index >= textBoxes.Count))
                return;

            EventBase reportEvent = displayedEvents[index];
            UIElement_TextBox reportTextBox = textBoxes[index];
            GameInstance instance = reportEvent.GameInstance;

            if (drawLinePosition == Vector2.Zero)
                drawLinePosition = new Vector2(position.X,
                    position.Y - ((ReportsList.lerpPosition % 1f) * GetReportItemHeight(reportEvent, reportTextBox)));

            if (reportEvent.EventType != EventTypes.GameTurnChanged)
            {
                ArmyInstance army = FindArmyWhereReportVisible(reportEvent) ?? reportEvent.GameInstance.ActiveArmy;
                string iconFileName = $"{UIStyles.Current.Name}_{reportEvent.DisplayIconFileName(army)}";
                Texture2D icon = UIStyles.Current.ReportIcons.GetValueOrDefault(iconFileName);
                if (icon != null)
                    spriteBatch.Draw(icon, drawLinePosition, Color.White);
            }
            else
            {
                DrawTurnButton(instance, spriteBatch, drawLinePosition, reportEvent);
            }

            reportTextBox.Text = reportEvent.Time.ToString("hh\\:mm") + " " + reportEvent.DisplayText();

#if DEBUG
            reportTextBox.TooltipMode = UIElement_TextBox.TooltipModes.Always;
            reportTextBox.TooltipText = reportEvent.EventType + "\r\n";
            for (int i = 0; i < reportEvent.GameObjectsChanges.Count; i++)
                reportTextBox.TooltipText += reportEvent.GameObjectsChanges[i].ToString() + "\r\n";
#endif

            if (reportEvent.EventType != EventTypes.GameTurnChanged)
                reportTextBox.Draw(game, spriteBatch, fontType: UIStyle.FontTypes.ReportText,
                    color: selected && ReportsList.highlighted ? Color.Black : Color.Black * 0.7f,
                    position: drawLinePosition + new Vector2(80, 0), maxWidth: 165,
                    autoEllipsis: UIElement_TextBox.AutoEllipsisMode.Character);

            if (reportEvent == EventSelected)
                if (reportEvent.EventType != EventTypes.GameTurnChanged)
                    spriteBatch.Draw(UIStyles.Current.ListSelector, drawLinePosition + new Vector2(16, 78), Color.White);
                else
                    spriteBatch.Draw(UIStyles.Current.ListSelector, drawLinePosition + new Vector2(40, 72), Color.White);

            drawLinePosition.Y += GetReportItemHeight(reportEvent, reportTextBox);
            spriteBatch.Draw(Game1.VictorianBatteredRule, drawLinePosition + new Vector2(0, -10), Color.White);
        }

        private void DrawTurnButton(GameInstance instance, SpriteBatch spriteBatch, Vector2 position, EventBase gameEvent)
        {
            // Draw the big frame
            Vector2 width = Game1.TPTCCWBrassFramesRegular.MeasureString("B");
            spriteBatch.DrawString(Game1.TPTCCWBrassFramesRegular, "B", position + new Vector2(10, -31), Color.Black); // 20, -30

            // Draw "Start of Turn"
            width = Game1.Amaltea18.MeasureString("Start of Turn");
            spriteBatch.DrawString(Game1.Amaltea18, "Start of Turn", position + new Vector2(40, 27), Color.Black);

            // Draw the number of the turn
            UIElements_Font turnNumberFont= UIStyles.Current.Fonts[(int)UIStyle.FontTypes.TabReportTurnNumber];
            width = turnNumberFont.MeasureString(gameEvent.Turn.ToString());
            turnNumberFont.SpriteFont.DrawText(spriteBatch, gameEvent.Turn.ToString(), position + new Vector2(124 - (int)(width.X / 2), 45), Color.Black);

            // This draws the pointy finger pointing to the left/right
            if ((gameEvent == EventSelected) && turnsExpandable[gameEvent.Turn - 1])
                spriteBatch.DrawString(Game1.AllHands, turnsVisible[gameEvent.Turn - 1] ? ";" : "C", position + new Vector2(165, 67), Color.Black);
        }
    }
}
