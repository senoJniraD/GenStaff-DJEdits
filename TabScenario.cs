using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ModelLib;
using TacticalAILib;

namespace GSBPGEMG
{
    public class TabScenario
    {
        public void Update(Game1 game)
        {
        }

        public void Draw(Game1 game, GameInstance instance, Tabs tabs, SpriteBatch spriteBatch)
        {
            Vector2 width = Game1.Klabasto18.MeasureString("SCENARIO");
            game.spriteLayerTabs.DrawString(Game1.Klabasto18, "SCENARIO", new Vector2(tabs.LeftTabCenter - (width.X / 2), 46 - (width.Y / 2)), Color.Black);

            string scenarioNameDisplay = instance.ScenarioName;
            width = Game1.VictorianScenarioNameFont.MeasureString(scenarioNameDisplay);
            if (width.X > game.LeftMapOffset - 10)
            {
                scenarioNameDisplay = game.TruncateToFirstWord(scenarioNameDisplay);
                width = Game1.VictorianScenarioNameFont.MeasureString(scenarioNameDisplay);
            }
            spriteBatch.DrawString(Game1.VictorianScenarioNameFont, scenarioNameDisplay, new Vector2(tabs.LeftTabCenter - (width.X / 2), 80 - (width.Y / 2)), Color.Black);

            if (instance.ScenarioDate != null)
            {
                width = Game1.Smythe22.MeasureString(instance.ScenarioDate);
                spriteBatch.DrawString(Game1.Smythe22, instance.ScenarioDate, new Vector2(tabs.LeftTabCenter - (width.X / 2), 106 - (width.Y / 2)), Color.Black);
            }

            string tString = instance.CurrentGameTime.ToString("h\\:mm");
            width = Game1.Rudyard36.MeasureString(tString);
            // Changed Y from 146 - Ezra 6/22/25
            spriteBatch.DrawString(Game1.Rudyard36, tString, new Vector2(tabs.LeftTabCenter - (width.X / 2), 139 - (width.Y / 2)), Color.Black);

            tString = "Turn " + instance.CurrentGameTurn.ToString() + " of " + instance.ScenarioNumTurns.ToString();
            width = Game1.TopBannerSmythe.MeasureString(tString);

            spriteBatch.DrawString(Game1.TopBannerSmythe, tString, new Vector2(tabs.LeftTabCenter - (width.X / 2), 175 - (width.Y / 2)), Color.Black);

            Rectangle r = new Rectangle(tabs.LeftTabCenter - 234 / 2, 188, 234, 18);
            spriteBatch.Draw(Game1.VictorianLine, r, Color.White);

            for (int i = 0; i < 2; i++)
            {
                ArmyInstance army = (i == 0) ? instance.ActiveArmy : instance.OpponentArmy;
                ArmyGainsLossesSnapshot armyGainsLosses = army.GainsLossesSnapshot;

                int leading = (i == 0) ? 310 : 610;
                r = new Rectangle(10, leading, 26, 167);
                spriteBatch.Draw((army.Side == Sides.Blue) ? Game1.BlueMustKIAGraphic : Game1.RedMustKIAGraphic, r, Color.White);
                leading -= 80;

                tString = army.Side + " Army";
                width = Game1.BlueArmyFont.MeasureString(tString);
                spriteBatch.DrawString(Game1.BlueArmyFont, tString, new Vector2(tabs.LeftTabCenter - (width.X / 2), leading - (width.Y / 2)),
                    SideColors.GetSideColors(army.Side).highlightedColor.ToMGColor());
                leading += 30;

                tString = "Controls " + armyGainsLosses.MyVPIControl.ToString() + " Victory Points";
                width = Game1.Smythe16.MeasureString(tString);
                spriteBatch.DrawString(Game1.Smythe16, tString, new Vector2(tabs.LeftTabCenter - (width.X / 2), leading - (width.Y / 2)), Color.Black);
                leading += 25;

                tString = "Needs " + armyGainsLosses.MyVP2Win.ToString() + " Victory Points to Win";
                width = Game1.Smythe16.MeasureString(tString);
                spriteBatch.DrawString(Game1.Smythe16, tString, new Vector2(tabs.LeftTabCenter - (width.X / 2), leading - (width.Y / 2)), Color.Black);
                leading += 25;

                tString = "Must capture " + armyGainsLosses.numEnemyInfantry2KIA.ToString() + ". Have captured " + armyGainsLosses.numEnemyKIAInfantry.ToString() + ".";
                spriteBatch.DrawString(Game1.Smythe16, tString, new Vector2(45, leading), Color.Black);
                leading += 25;

                tString = "Must capture " + armyGainsLosses.numEnemyLightInfantry2KIA.ToString() + ". Have captured " + armyGainsLosses.numEnemyKIALightInfantry.ToString() + ".";
                spriteBatch.DrawString(Game1.Smythe16, tString, new Vector2(45, leading), Color.Black);
                leading += 25;

                tString = "Must capture " + armyGainsLosses.numEnemyCavalry2KIA.ToString() + ". Have captured " + armyGainsLosses.numEnemyKIACavalry.ToString() + ".";
                spriteBatch.DrawString(Game1.Smythe16, tString, new Vector2(45, leading), Color.Black);
                leading += 25;

                tString = "Must capture " + armyGainsLosses.numEnemyLightCavalry2KIA.ToString() + ". Have captured " + armyGainsLosses.numEnemyKIALightCavalry.ToString() + ".";
                spriteBatch.DrawString(Game1.Smythe16, tString, new Vector2(45, leading), Color.Black);
                leading += 25;

                tString = "Must capture " + armyGainsLosses.numEnemyArtillery2KIA.ToString() + ". Have captured " + armyGainsLosses.numEnemyKIAArtillery.ToString() + ".";
                spriteBatch.DrawString(Game1.Smythe16, tString, new Vector2(45, leading), Color.Black);
                leading += 25;

                tString = "Must capture " + armyGainsLosses.numEnemyHorseArtillery2KIA.ToString() + ". Have captured " + armyGainsLosses.numEnemyKIAHorseArtillery.ToString() + ".";
                spriteBatch.DrawString(Game1.Smythe16, tString, new Vector2(45, leading), Color.Black);
                leading += 25;

                tString = "Must capture " + armyGainsLosses.numEnemySupplies2KIA.ToString() + ". Have captured " + armyGainsLosses.numEnemyKIASupplies.ToString() + ".";
                spriteBatch.DrawString(Game1.Smythe16, tString, new Vector2(45, leading), Color.Black);

                if (i == 0)
                {
                    spriteBatch.Draw(Game1.VictorianLine, new Rectangle(tabs.LeftTabCenter - 234 / 2, 488, 234, 18), Color.White);
                    spriteBatch.Draw(Game1.VictorianLine, new Rectangle(tabs.LeftTabCenter - 234 / 2, 188, 234, 18), Color.White);
                }
            }

            r = new Rectangle(6, 800, 260, 37);
            spriteBatch.Draw(Game1.ElevationTerrainBox, r, Color.White);

            width = Game1.Smythe16.MeasureString("Elevation:");
            spriteBatch.DrawString(Game1.VictorianElevTerFont, "Elevation:", new Vector2(75 - (width.X / 2), 786), Color.Black);

            width = Game1.Smythe16.MeasureString("Terrain:");
            spriteBatch.DrawString(Game1.VictorianElevTerFont, "Terrain:", new Vector2(180 - (width.X / 2), 786), Color.Black);

            width = Game1.Smythe16.MeasureString(game.ElevationString);
            spriteBatch.DrawString(Game1.Smythe16, game.ElevationString, new Vector2(78 - (width.X / 2), 810), Color.Black);

            width = Game1.Smythe16.MeasureString(game.TerrainString);
            spriteBatch.DrawString(Game1.Smythe16, game.TerrainString, new Vector2(183 - (width.X / 2), 810), Color.Black);

            if (!game.DisplayUnitInfo && !game.ShowGrid)
                game.DisplayBanner.SetMessage(instance.ScenarioDescription);
        }
    }
}
