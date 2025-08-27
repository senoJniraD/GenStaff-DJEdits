using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ModelLib;
using static GSBPGEMG.Game1;

namespace GSBPGEMG
{
    public enum Menus { None, File, View, About };

    public class HeaderMenus
    {
        public Menus MenuSelected { get; private set; } = Menus.None;
        public bool SaveRequired { get; set; }

        private Rectangle FileArea = new Rectangle(2, 2, 39, 22);
        private Rectangle ViewArea = new Rectangle(58, 2, 50, 22);
        private Rectangle AboutArea = new Rectangle(112, 2, 75, 22);

        public HeaderMenus(Game1 game)
        {
            ((Form)Control.FromHandle(game.Window.Handle)).FormClosing +=
                (s, e) => { e.Cancel = ExitCancelledOnSaveWarning(game); };
        }

        public void Update(Game1 game)
        {
            if (Input.mouseLeftClick)
            {
                Menus lastMenuSelected = MenuSelected;
                bool clickedOnMenu = false;

                if (FileArea.Contains(Input.mouseMenuPoint) && (MenuSelected != Menus.File))
                {
                    clickedOnMenu = true;
                    MenuSelected = Menus.File;
                }

                if (ViewArea.Contains(Input.mouseMenuPoint) && (MenuSelected != Menus.View))
                {
                    clickedOnMenu = true;
                    MenuSelected = Menus.View;
                }

                if (AboutArea.Contains(Input.mouseMenuPoint) && (MenuSelected != Menus.About))
                {
                    clickedOnMenu = true;
                    MenuSelected = Menus.About;
                }

                if (MenuSelected == lastMenuSelected)
                {
                    if (MenuSelected == Menus.File &&
                        Input.MouseClickWithinArea(Input.MouseButtons.Left, Input.MouseCoordinateType.Menu, Input.MouseOriginType.TopLeft,
                        new Point(10, 30), new Point(190, 105)))
                    {
                        clickedOnMenu = true;
                        if (Input.mouseMenuPoint.Y < 50)
                        {
                            if (game.GameplayScreen != GameplayScreens.EndingTurn)
                            {
                                game.CurrentSavedGameData.UpdateSavedGameData(game, game.Instance);
                                if (game.CurrentSavedGameData.SaveToFile(game))
                                    Logging.Write(showMessageBox: true, title: "General Staff Message", message: "File saved successfully.");
                            }
                            MenuSelected = Menus.None;
                        }
                        else if (Input.mouseMenuPoint.Y < 70)
                        {
                            MenuSelected = Menus.None;
                            if (ExitCancelledOnSaveWarning(game) == false)
                            {
                                Program.Restart = true;
                                game.Exit();
                            }
                        }
                    }

                    if (MenuSelected == Menus.View &&
                        Input.MouseClickWithinArea(Input.MouseButtons.Left, Input.MouseCoordinateType.Menu, Input.MouseOriginType.TopLeft, new Point(70, 30), new Point(234, 120)))
                    {
                        clickedOnMenu = true;
                        if (Input.mouseMenuPoint.Y < 55)
                        {
                            game.ShowAllPlaces = !game.ShowAllPlaces;
                        }
                        else if (Input.mouseMenuPoint.Y < 82)
                        {
                            game.ShowRangeOfUnit = !game.ShowRangeOfUnit;
                        }
                        else if (Input.mouseMenuPoint.Y < 108)
                        {
                            game.ShowGrid = !game.ShowGrid;
                        }
                        else if (Input.mouseMenuPoint.Y < 134)
                        {
                            game.ShowRuler = !game.ShowRuler;
                        }
                        else
                        {
                            game.ShowTerrainVisualizer = !game.ShowTerrainVisualizer;
                        }
                    }
                }

                if (!clickedOnMenu)
                    MenuSelected = Menus.None;
            }

            if (MenuSelected == Menus.View)
                game.DisplayBanner.SetMessage($"Grid lines are {Map.MetersPerPixel * 35:#.#} meters apart. " +
                    $"Battlefield is {Map.Width * Map.MetersPerPixel:0#,0#} meters by {Map.Height * Map.MetersPerPixel:0#,0#} meters.");
        }

        public void Draw(Game1 game, SpriteBatch spriteBatch)
        {
            spriteBatch.DrawString(Ephinol12, "File  View  About", new Vector2(3, 3), Color.Black);

            game.DrawLine(spriteBatch, new Vector2(0, 26), new Vector2(270, 26), Color.Black, 1);
            game.DrawLine(spriteBatch, new Vector2(1, 64), new Vector2(265, 64), Color.Black, 1);

            if (MenuSelected == Menus.File)
                DrawMenuFile(game, spriteBatch);
            if (MenuSelected == Menus.View)
                DrawMenuView(game, spriteBatch);

            if (MenuSelected == Menus.About)
            {
                spriteBatch.Draw(VictorianAboutBox, new Rectangle(Map.Width / 2 - (374 / 2) + game.LeftMapOffset,
                    Map.Height / 2 - (644 / 2), 374, 644), Color.White);

                DateTime buildDate = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).LastWriteTime;
                string BuildDate = buildDate.Date.ToString("yyMMdd");
                string version = "Build " + BuildDate;
                Vector2 width = Smythe22.MeasureString(version);
                spriteBatch.DrawString(Smythe22, version, new Vector2(game.LeftMapOffset + (Map.Width / 2) - (width.X / 2), 614), Color.Black);
            }
        }

        public void DrawMenuFile(Game1 game, SpriteBatch spriteBatch)
        {
            int leading = 20;
      //      spriteBatch.Draw(VictorianViewMenuBoxDropShadow, new Vector2(6 + 4, 25 + 4), Color.White * 0.25f);
      //      spriteBatch.Draw(VictorianViewMenuBox, new Vector2(6, 25), Color.White);

            spriteBatch.Draw(VictorianFileMenuBoxDropShadow, new Vector2(6 + 4, 25 + 4), Color.White * 0.25f);
            spriteBatch.Draw(VictorianFileMenuBox, new Vector2(6, 25), Color.White);


            //      game.DrawLine(spriteBatch, new Vector2(14, 51 + leading), new Vector2(228, 51 + leading), Color.Black, 1);

            string tString = "Save Scenario";
            spriteBatch.DrawString(Ephinol12, tString, new Vector2(15, 10 + leading),
                Color.Black * (game.GameplayScreen != GameplayScreens.EndingTurn ? 1f : 0.5f));
            if (SaveRequired && (game.GameplayScreen != GameplayScreens.EndingTurn))
            {
                float lerp = ((float)Math.Sin(GameTimeRef.TotalGameTime.TotalSeconds * ModelLib.MathHelper.TwoPi) + 1f) / 2f;
                spriteBatch.DrawString(Ephinol12, "*", new Vector2(17 + (int)Ephinol12.MeasureString(tString).X, 28 + leading),
                    Color.Lerp(Color.DarkGray, Color.Black, lerp), scaleFloat: 1.5f);
            }

            leading += 34;
            spriteBatch.DrawString(Ephinol12, "Go To Main Menu", new Vector2(15, leading), Color.Black);
        }

        public void DrawMenuView(Game1 game, SpriteBatch spriteBatch)
        {
            int leading = 26;
            spriteBatch.Draw(VictorianViewMenuBoxDropShadow, new Vector2(63 + 4, 25 + 4), Color.White * 0.25f);
            spriteBatch.Draw(VictorianViewMenuBox, new Vector2(63, 25), Color.White);

            if (game.ShowAllPlaces)
                spriteBatch.Draw(MenuCheckBox, new Vector2(71, 32), Color.White);
            else
                spriteBatch.Draw(MenuHollowBox, new Vector2(71, 32), Color.White);

            spriteBatch.DrawString(Ephinol12, "Show All Places", new Vector2(90, 31), Color.Black);
            game.DrawLine(spriteBatch, new Vector2(70, 55), new Vector2(300, 55), Color.Black, 1);

            if (game.ShowRangeOfUnit)
                spriteBatch.Draw(MenuCheckBox, new Vector2(71, 32 + leading), Color.White);
            else
                spriteBatch.Draw(MenuHollowBox, new Vector2(71, 32 + leading), Color.White);

            spriteBatch.DrawString(Ephinol12, "Show Range of Unit", new Vector2(90, 31 + leading), Color.Black);
            game.DrawLine(spriteBatch, new Vector2(70, 55 + leading), new Vector2(300, 55 + leading), Color.Black, 1);

            leading += 26;

            if (game.ShowGrid)
                spriteBatch.Draw(MenuCheckBox, new Vector2(71, 32 + leading), Color.White);
            else
                spriteBatch.Draw(MenuHollowBox, new Vector2(71, 32 + leading), Color.White);

            spriteBatch.DrawString(Ephinol12, "Show Grid", new Vector2(90, 31 + leading), Color.Black);
            game.DrawLine(spriteBatch, new Vector2(70, 55 + leading), new Vector2(300, 55 + leading), Color.Black, 1);

            leading += 26;
            if (game.ShowRuler)
                spriteBatch.Draw(MenuCheckBox, new Vector2(71, 32 + leading), Color.White);
            else
                spriteBatch.Draw(MenuHollowBox, new Vector2(71, 32 + leading), Color.White);

            spriteBatch.DrawString(Ephinol12, "Range of Influence", new Vector2(90, 31 + leading), Color.Gray);
            game.DrawLine(spriteBatch, new Vector2(70, 55 + leading), new Vector2(300, 55 + leading), Color.Black, 1);

            leading += 26;
            if (game.ShowTerrainVisualizer)
                spriteBatch.Draw(MenuCheckBox, new Vector2(71, 32 + leading), Color.White);
            else
                spriteBatch.Draw(MenuHollowBox, new Vector2(71, 32 + leading), Color.White);

            spriteBatch.DrawString(Ephinol12, "Terrain Visualizer", new Vector2(90, 31 + leading), Color.Gray);
        }

        public bool ExitCancelledOnSaveWarning(Game1 game)
        {
            if (SaveRequired && (game.GameState == GameStates.Gameplay || game.GameplayScreen == GameplayScreens.EndingTurn))
                if (System.Windows.MessageBox.Show("Are you sure you want to exit without saving?", "Exit without saving?",
                    System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.No)
                    return true;
            return false;
        }
    }
}
