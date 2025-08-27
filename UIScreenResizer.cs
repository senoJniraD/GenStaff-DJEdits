using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GSBPGEMG
{
    public class UIScreenResizer
    {
        public RenderTarget2D renderTarget { get; private set; }
        public float scaling { get; private set; }
        public Point offset { get; private set; }

        private SpriteBatch spriteBatch;

        public UIScreenResizer(Game1 game)
        {
            spriteBatch = new SpriteBatch(game.GraphicsDevice);
            game.Window.ClientSizeChanged += (s, e) => OnSizeChanged(game.Window);
            Update(game);
        }

        public void Update(Game1 game)
        {
            SetRenderTargetSize(game.WindowWidth, game.WindowHeight);

            float widthScaling = (float)Game1.GraphicsDeviceManagerRef.PreferredBackBufferWidth / renderTarget.Width;
            float heightScaling = (float)Game1.GraphicsDeviceManagerRef.PreferredBackBufferHeight / renderTarget.Height;
            scaling = Math.Min(widthScaling, heightScaling);

            Point newOffset = Point.Zero;
            if (widthScaling > heightScaling)
                newOffset.X = (int)((Game1.GraphicsDeviceManagerRef.PreferredBackBufferWidth - (renderTarget.Width * scaling)) / 2f);
            if (heightScaling > widthScaling)
                newOffset.Y = (int)((Game1.GraphicsDeviceManagerRef.PreferredBackBufferHeight - (renderTarget.Height * scaling)) / 2f);
            offset = newOffset;
        }

        public void SetRenderTargetSize(int requiredWidth, int requiredHeight)
        {
            bool createRenderTargets = false;
            if (renderTarget == null)
                createRenderTargets = true;
            if (renderTarget != null)
            {
                if ((renderTarget.Width != requiredWidth) || (renderTarget.Height != requiredHeight))
                {
                    createRenderTargets = true;
                    renderTarget.Dispose();
                }
            }

            if (createRenderTargets == true)
                renderTarget = new RenderTarget2D(Game1.GraphicsDeviceRef, requiredWidth, requiredHeight, false,
                    Game1.GraphicsDeviceRef.PresentationParameters.BackBufferFormat,
                    Game1.GraphicsDeviceRef.PresentationParameters.DepthStencilFormat,
                    Game1.GraphicsDeviceRef.PresentationParameters.MultiSampleCount, RenderTargetUsage.PreserveContents);
        }

        public void OnSizeChanged(GameWindow gameWindow)
        {
            if ((gameWindow.ClientBounds.Width > 0) && (gameWindow.ClientBounds.Height > 0))
            {
                int minWidth = 720;
                int minHeight = 480;
                if ((gameWindow.ClientBounds.Width < minWidth) || (gameWindow.ClientBounds.Height < minHeight))
                {
                    Game1.GraphicsDeviceManagerRef.PreferredBackBufferWidth = Math.Max(gameWindow.ClientBounds.Width, minWidth);
                    Game1.GraphicsDeviceManagerRef.PreferredBackBufferHeight = Math.Max(gameWindow.ClientBounds.Height, minHeight);
                    Game1.GraphicsDeviceManagerRef.ApplyChanges();
                }
            }
        }

        public void BeginDraw()
        {
            Game1.GraphicsDeviceRef.SetRenderTarget(renderTarget);
            Game1.GraphicsDeviceRef.Clear(ClearOptions.Target, Color.Transparent, 1f, 0);
        }

        public void EndDraw(Texture2D background)
        {
            // Clear screen
            Game1.GraphicsDeviceRef.SetRenderTarget(null);
            Game1.GraphicsDeviceRef.Clear(ClearOptions.Target, (background != null) ? Color.Transparent : new Color(240, 230, 215), 1f, 0);

            // Game background
            if (background != null)
            {
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None);
                spriteBatch.Draw(background,
                    new Rectangle(0, 0, Game1.GraphicsDeviceManagerRef.PreferredBackBufferWidth, Game1.GraphicsDeviceManagerRef.PreferredBackBufferHeight),
                    new Rectangle(0, 0, Game1.GraphicsDeviceManagerRef.PreferredBackBufferWidth, Game1.GraphicsDeviceManagerRef.PreferredBackBufferHeight),
                    Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0f);
                spriteBatch.End();
            }
            
            // Resized render target
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None);
            spriteBatch.Draw(renderTarget, new Rectangle(offset.X, offset.Y,
                (int)Math.Round(renderTarget.Width * scaling), (int)Math.Round(renderTarget.Height * scaling)),
                new Rectangle(0, 0, renderTarget.Width, renderTarget.Height), Color.White);
            spriteBatch.End();
        }
    }
}
