using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ModelLib;
using TacticalAILib;
using static GSBPGEMG.Game1;

namespace GSBPGEMG
{
    public class TabEndTurn
    {
        public void Update(Game1 game, ProcessTurn processTurn)
        {
            GameInstance instance = game.Instance;
            SavedGameData currentSavedGameData = game.CurrentSavedGameData;

            if (processTurn.LocalEndTurnAvailable(instance) &&
                Input.MouseClickWithinArea(Input.MouseButtons.Left, Input.MouseCoordinateType.Menu, Input.MouseOriginType.TopLeft,
                new Point(5, 701), new Point(game.LeftMapOffset - 5, 150 - game.TopMapOffset)))
            {
                game.GameplayScreen = GameplayScreens.EndingTurn;
                currentSavedGameData.LocalPlayerArmy.SetPlannedEvents(instance);
            }

            if ((processTurn.NoOfOpponentsNotTakenTurn(instance, currentSavedGameData) >= 1) &&
                (currentSavedGameData.GameOpponentType == SavedGameData.GameOpponentTypes.PlayByEmail))
            {
                // Default Email Application
                if (Input.MouseClickWithinArea(Input.MouseButtons.Left, Input.MouseCoordinateType.Menu, Input.MouseOriginType.Center,
                    new Point(134, 370), new Point(game.LeftMapOffset - 10, 50)))
                {
                    try
                    {
                        string file = Path.Combine(Path.GetTempPath(),
                            $"{instance.ScenarioName} (From {instance.ActiveArmy.Side} Turn {instance.CurrentGameTurn}).{SavedGameData.FileExtension}");
                        currentSavedGameData.UpdateSavedGameData(game, instance);
                        currentSavedGameData.SaveToFile(game, file);
                        System.Windows.Clipboard.SetFileDropList([file]);

                        string command = "mailto:\"";

                        string emailAddress = currentSavedGameData.SavedArmies[instance.OpponentArmy.Index].Email;
                        command += ((emailAddress != null) ? emailAddress : "") + "?";

                        string emailSubject = "GSBP My Turn";
                        emailSubject = Uri.EscapeDataString(emailSubject);
                        command += "subject=" + emailSubject;

                        string emailBody = $"TO DO - write a template email body\nInsert details about current game like turn number, in-game time etc.\n\n\n" +
                            "\uD83D\uDCCB \u21B7 \uD83D\uDCCE "+
                            "IMPORTANT - Highlight this entire line and press Ctrl+V to paste in the saved game from the clipboard " +
                            "\uD83D\uDCCB \u21B7 \uD83D\uDCCE";
                        emailBody = Uri.EscapeDataString(emailBody);
                        command += "&body=" + emailBody + "\"";

                        Process.Start(new ProcessStartInfo("cmd", "/c start " + command) { CreateNoWindow = true });
                    }
                    catch
                    {
                        System.Windows.MessageBox.Show("Unable to start default email application.", "Sending Email Failed", System.Windows.MessageBoxButton.OK);
                    }
                }

                // Choose A File Location
                if (Input.MouseClickWithinArea(Input.MouseButtons.Left, Input.MouseCoordinateType.Menu, Input.MouseOriginType.Center,
                    new Point(134, 460), new Point(game.LeftMapOffset - 10, 50)))
                {
                    System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog()
                    {
                        Title = "Save GSBP Saved Game As",
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        FileName = $"{instance.ScenarioName} (From {instance.ActiveArmy.Side} Turn {instance.CurrentGameTurn})",
                        DefaultExt = SavedGameData.FileExtension,
                        Filter = "GSBP Saved Game|*.gsbp-save"
                    };
                    if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        currentSavedGameData.UpdateSavedGameData(game, instance);
                        currentSavedGameData.SaveToFile(game);
                        currentSavedGameData.SaveToFile(game, saveFileDialog.FileName);

                        try { Process.Start("explorer.exe", $"/select, \"{saveFileDialog.FileName}\""); }
                        catch { }
                    }
                }

                // Open Received Saved Game Data
                if (Input.MouseClickWithinArea(Input.MouseButtons.Left, Input.MouseCoordinateType.Menu, Input.MouseOriginType.Center,
                    new Point(134, 580), new Point(game.LeftMapOffset - 10, 50)))
                    SavedGamesCollection.SelectEmailAttachment();
            }
        }

        public void Draw(Game1 game, GameInstance instance, Tabs tabs, ProcessTurn processTurn, SpriteBatch spriteBatch)
        {
            Vector2 width = Klabasto18.MeasureString("END TURN");
            spriteBatch.DrawString(Klabasto18, "END TURN", new Vector2(tabs.LeftTabCenter - (width.X / 2), 46 - (width.Y / 2)), Color.Black);

            SavedGameData currentSavedGameData = game.CurrentSavedGameData;

            int y = 100;

            string tString = "Players";
            width = VictorianReportsFont.MeasureString(tString);
            spriteBatch.DrawString(VictorianReportsFont, tString, new Vector2(134 - (width.X / 2), y - (width.Y / 2)), Color.Black);
            y += 30;

            DrawPlayer(game, instance, currentSavedGameData.LocalPlayerArmy, spriteBatch, y, 125);
            y += 30;

            for (int i = 0; i < currentSavedGameData.SavedArmies.Count; i++)
            {
                if (i != currentSavedGameData.LocalPlayerArmy.Index)
                {
                    DrawPlayer(game, instance, currentSavedGameData.SavedArmies[i], spriteBatch, y, 125);
                    y += 30;
                }
            }

            string[] mString;

            if (game.GameplayScreen == GameplayScreens.EndingTurn)
            {
                if (processTurn.DisplayCalculatingTurn)
                    tString = "Calculating Turn. Percentage calculated: " + processTurn.ProgressPercentage.ToString() + "%";
                else
                    tString = "Turn calculations are complete.";
                mString = game.JustifyTextStrings(VictorianReportsFont, tString, 220);
                y = 280;
                int leading = 0;
                for (int i = 0; i < mString.Length; i++)
                {
                    width = VictorianReportsFont.MeasureString(mString[i]);
                    spriteBatch.DrawString(VictorianReportsFont, mString[i], new Vector2(134 - (width.X / 2), y - (width.Y / 2) + leading),
                        Color.Black * (processTurn.DisplayCalculatingTurn ? 0.5f : 1.0f));
                    leading += 20;
                }

                if (currentSavedGameData.GameModeType == SavedGameData.GameModeTypes.Simulation)
                {
                    if (processTurn.DisplayCalculatingCourierRoutes)
                        tString = "Calculating Courier Routes.";
                    else
                        tString = "Courier Route calculations are complete.";
                    mString = game.JustifyTextStrings(VictorianReportsFont, tString, 220);
                    y += 50;
                    leading = 0;
                    for (int i = 0; i < mString.Length; i++)
                    {
                        width = VictorianReportsFont.MeasureString(mString[i]);
                        spriteBatch.DrawString(VictorianReportsFont, mString[i], new Vector2(134 - (width.X / 2), y - (width.Y / 2) + leading),
                            Color.Black * (processTurn.DisplayCalculatingCourierRoutes ? 0.5f : 1.0f));
                        leading += 20;
                    }
                }

                if (currentSavedGameData.GameOpponentType == SavedGameData.GameOpponentTypes.AI)
                {
                    if (processTurn.DisplayCalculatingAI)
                        tString = "AI is being calculated.";
                    else
                        tString = "AI calculations are complete.";
                    mString = game.JustifyTextStrings(VictorianReportsFont, tString, 220);
                    y += 50;
                    leading = 0;
                    for (int i = 0; i < mString.Length; i++)
                    {
                        width = VictorianReportsFont.MeasureString(mString[i]);
                        spriteBatch.DrawString(VictorianReportsFont, mString[i], new Vector2(134 - (width.X / 2), y - (width.Y / 2) + leading),
                            Color.Black * (processTurn.DisplayCalculatingAI ? 0.5f : 1.0f));
                        leading += 20;
                    }
                }

                if (Debug.SettingsAnimateEndTurn)
                {
                    y += 50;
                    leading = 0;
                    tString = game.ProcessTurn.ProgressTime.ToString(@"hh\:mm\:ss");
                    width = Rudyard36.MeasureString("00:00:00");
                    spriteBatch.DrawString(Rudyard36, tString,
                        new Vector2(134 - (width.X / 2), y - (width.Y / 2) + leading),
                        Color.Black * (processTurn.DisplayCalculatingAI ? 0.5f : 1.0f));
                }
            }

            if (game.GameplayScreen == GameplayScreens.Playing)
            {
                tString = "Highlighted units are without orders";
                mString = game.JustifyTextStrings(VictorianReportsFont, tString, 220);
                y = 650;
                int leading = 0;
                for (int i = 0; i < mString.Length; i++)
                {
                    width = VictorianReportsFont.MeasureString(mString[i]);
                    spriteBatch.DrawString(VictorianReportsFont, mString[i], new Vector2(134 - (width.X / 2), y - (width.Y / 2) + leading), Color.Black);
                    leading += 20;
                }
            }

            Rectangle r = new Rectangle(1, 706, 265, 132);
            if (game.GameplayScreen == GameplayScreens.Playing)
                spriteBatch.Draw(VictorianEndTurnButton1, r, Color.White);
            else if (game.GameplayScreen == GameplayScreens.EndingTurn)
                spriteBatch.Draw(VictorianEndTurnButton2, r, Color.White * 0.5f);

            if ((processTurn.NoOfOpponentsNotTakenTurn(instance, currentSavedGameData) >= 1) &&
                (currentSavedGameData.GameOpponentType == SavedGameData.GameOpponentTypes.PlayByEmail) &&
                ((game.GameplayScreen == GameplayScreens.Playing) || (game.GameplayScreen == GameplayScreens.WaitingForOpponent)))
            {
                y = 315;

                tString = "Send";
                width = VictorianScenarioNameFont.MeasureString(tString);
                spriteBatch.DrawString(VictorianScenarioNameFont, tString, new Vector2(134 - (width.X / 2), y), Color.Black);
                y += 35;

                game.DrawBoxOutline(spriteBatch, new Rectangle(6, y - 7, 255, 50), Color.Black, 1);
                tString = "Email Saved Game Through";
                width = VictorianReportsFont.MeasureString(tString);
                spriteBatch.DrawString(VictorianReportsFont, tString, new Vector2(134 - (width.X / 2), y), Color.Black);
                y += 20;

                tString = "Default Email Application";
                width = VictorianReportsFont.MeasureString(tString);
                spriteBatch.DrawString(VictorianReportsFont, tString, new Vector2(134 - (width.X / 2), y), Color.Black);
                y += 35;

                tString = "OR";
                width = VictorianReportsFont.MeasureString(tString);
                spriteBatch.DrawString(VictorianReportsFont, tString, new Vector2(134 - (width.X / 2), y), Color.Black);
                y += 35;

                game.DrawBoxOutline(spriteBatch, new Rectangle(6, y - 7, 255, 50), Color.Black, 1);
                tString = "Choose A File Location And";
                width = VictorianReportsFont.MeasureString(tString);
                spriteBatch.DrawString(VictorianReportsFont, tString, new Vector2(134 - (width.X / 2), y), Color.Black);
                y += 20;

                tString = "Handle Transfer Myself";
                width = VictorianReportsFont.MeasureString(tString);
                spriteBatch.DrawString(VictorianReportsFont, tString, new Vector2(134 - (width.X / 2), y), Color.Black);
                y += 65;

                //tString = "To Do";
                //width = ModernArmySlugFont.MeasureString(tString);
                //spriteLayerTabs.DrawString(ModernArmySlugFont, tString, new Vector2(87 + 30, y - 30), Color.Red,
                //    rotation: ModelLib.MathHelper.ToRadiansFloat(35));

                tString = "Receive";
                width = VictorianScenarioNameFont.MeasureString(tString);
                spriteBatch.DrawString(VictorianScenarioNameFont, tString, new Vector2(134 - (width.X / 2), y), Color.Black);
                y += 35;

                game.DrawBoxOutline(spriteBatch, new Rectangle(6, y - 7, 255, 50), Color.Black, 1);
                tString = "Open Email Attachment";
                width = VictorianReportsFont.MeasureString(tString);
                spriteBatch.DrawString(VictorianReportsFont, tString, new Vector2(134 - (width.X / 2), y), Color.Black);
                y += 20;

                tString = "(Or Drag & Drop On Window)";
                width = VictorianReportsFont.MeasureString(tString);
                spriteBatch.DrawString(VictorianReportsFont, tString, new Vector2(134 - (width.X / 2), y), Color.Black);
            }
        }

        public void DrawPlayer(Game1 game, GameInstance instance, SavedGameArmyData savedArmy, SpriteBatch spriteBatch, int y, int maxWidth)
        {
            string tString = savedArmy.PlayerName(instance);
            Vector2 width = VictorianReportsFont.MeasureString(tString);

            int length = tString.Length;
            while (width.X > maxWidth)
            {
                tString = tString.Substring(0, tString.Length - 1);
                width = VictorianReportsFont.MeasureString(tString);
            }
            if (tString.Length < length)
                tString += "...";

            spriteBatch.DrawString(VictorianReportsFont, tString, new Vector2(20, y - (width.Y / 2)),
                SideColors.GetSideColors(instance.AllArmies[savedArmy.Index].Side).highlightedColor.ToMGColor());

            bool turnTaken = (instance.AllArmies[savedArmy.Index].TurnsTaken == instance.CurrentGameTurn);
            if ((game.CurrentSavedGameData.GameOpponentType == SavedGameData.GameOpponentTypes.SteamPlayer) && (savedArmy.SteamID == 0))
                tString = "Invite Still Open";
            else
                tString = turnTaken ? "Turn Taken" : "Playing Turn";
            width = VictorianReportsFont.MeasureString(tString);
            spriteBatch.DrawString(VictorianReportsFont, tString, new Vector2(260 - width.X, y - (width.Y / 2)), Color.Black * (turnTaken ? 1.0f : 0.5f));
        }
    }
}
