using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ModelLib;
using static GSBPGEMG.Game1;

namespace GSBPGEMG
{
    public class DisplayBanner
    {
        private string messageSet;
        private bool alertSet;

        private int animatedFingerIncrement = 80;

        public void Update(Game1 game)
        {
            messageSet = null;
            alertSet = false;

            int millisecondsPerFingerAnimation = 5;
            if (game.timeSinceLastFrame > millisecondsPerFingerAnimation)
            {
                animatedFingerIncrement -= 2;
                if (animatedFingerIncrement < -80)
                    animatedFingerIncrement = 80;
            }
        }

        public void SetMessage(string message, bool alert = false)
        {
            if (!alert && alertSet)
                return;

            messageSet = message;
            alertSet = alert;
        }

        public void Draw(Game1 game)
        {
            if ((messageSet == null) || (game.GameplayScreen == GameplayScreens.FinishedScenario))
                SetMessage($"{game.Instance.ScenarioName} {game.Instance.CurrentGameTime:hh\\:mm} Turn {game.Instance.CurrentGameTurn}");

            
            Vector2 position = new Vector2(268, 0);
            Vector2 width = TopBannerSmythe.MeasureString(messageSet);
            game.spriteLayerTabs.DrawString(TopBannerSmythe, messageSet, position + new Vector2((Map.Width / 2) - (width.X / 2), 13 - (width.Y / 2)), Color.Black);

            if (alertSet)
            {
                int localIncrementer = animatedFingerIncrement;
                if (localIncrementer < 0)
                    localIncrementer = 0;

                Rectangle r = new(0, (int)(position.Y + 14 - (width.Y / 2)), 37, 17);

                r.X = (int)(position.X + (Map.Width / 2) - (width.X / 2) - 37) - localIncrementer;
                game.spriteLayerTabs.Draw(VictorianLeftBannerFinger, r, Color.White);

                r.X = (int)(position.X + (Map.Width / 2) + (width.X / 2) + 1 + localIncrementer);
                game.spriteLayerTabs.Draw(VictorianRightBannerFinger, r, Color.White);
            }

            messageSet = null;
        }
    }
}
