using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GSBPGEMG.UI
{
    public class UIElement_BorderedTexture
    {
        public Vector2 Position;
        public Vector2 Size;
        public Point CornerSize = new(4, 4);
        public Color Color;
        public bool Shadow;

        RenderTarget2D renderTarget;

        public UIElement_BorderedTexture() { }

        // round edges
        // scaling
        // set values in draw

        public void GenerateTexture(GraphicsDevice graphicsDevice, Texture2D background, Texture2D border, Texture2D mask, Point destinationSize)
        {
            Point requiredSize = destinationSize;
            if (border != null)
                requiredSize = new Point(Math.Max(requiredSize.X, border.Width), Math.Max(requiredSize.Y, border.Height));

            if (renderTarget == null || renderTarget.Bounds.Size != requiredSize)
            {
                renderTarget?.Dispose();
                renderTarget = new RenderTarget2D(Game1.GraphicsDeviceRef, requiredSize.X, requiredSize.Y, false,
                    graphicsDevice.PresentationParameters.BackBufferFormat,
                    Game1.GraphicsDeviceRef.PresentationParameters.DepthStencilFormat,
                    Game1.GraphicsDeviceRef.PresentationParameters.MultiSampleCount, RenderTargetUsage.PreserveContents);

                UIElement_SpriteLayer.GenerateTextureActions.Add(() =>
                {
                    graphicsDevice.SetRenderTarget(renderTarget);
                    graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1f, 0);

                    if (mask != null)
                    {
                        UICommon.RenderTargetAlphaTestEffect.Projection = Matrix.CreateOrthographicOffCenter(0, renderTarget.Width, renderTarget.Height, 0, 0, 1);
                        UICommon.RenderTargetSpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque,
                                depthStencilState: UICommon.StencilWrite, effect: UICommon.RenderTargetAlphaTestEffect);
                        UICommon.RenderTargetSpriteBatch.Draw(mask, Vector2.Zero, Color.White);
                        UICommon.RenderTargetSpriteBatch.End();

                        UICommon.RenderTargetSpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque,
                            depthStencilState: UICommon.StencilRead, effect: UICommon.RenderTargetAlphaTestEffect);
                    }
                    else
                    {
                        UICommon.RenderTargetSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                    }

                    if (background != null)
                        UICommon.RenderTargetSpriteBatch.Draw(background, Vector2.Zero, Color.White);
                    if (border != null)
                        UICommon.RenderTargetSpriteBatch.Draw(border, Vector2.Zero, Color.White);

                    UICommon.RenderTargetSpriteBatch.End();
                    graphicsDevice.BlendState = BlendState.AlphaBlend;
                    graphicsDevice.DepthStencilState = DepthStencilState.Default;
                });
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D backgroundTexture = null, Texture2D borderTexture = null, Texture2D maskTexture = null,
            Vector2? positionRequest = null, Vector2? sizeRequest = null, Color? colorRequest = null, bool? shadowRequest = null)
        {
            if (positionRequest.HasValue)
                Position = positionRequest.Value;
            if (sizeRequest.HasValue)
                Size = sizeRequest.Value;
            if (colorRequest.HasValue)
                Color = colorRequest.Value;
            if (shadowRequest.HasValue)
                Shadow = shadowRequest.Value;

            Point point = Position.ToPoint();
            Point cornerSize = borderTexture != null ? CornerSize : Point.Zero;
            Point size = new((int)Math.Max(Size.X, cornerSize.X * 2 + 1), (int)Math.Max(Size.Y, cornerSize.Y * 2 + 1));
            Color color = colorRequest ?? Color.White;

            GenerateTexture(Game1.GraphicsDeviceRef, backgroundTexture, borderTexture, maskTexture, size);

            if (Shadow)
                spriteBatch.Draw(renderTarget, Position + new Vector2(6, 6), new Rectangle(0, 0, size.X, size.Y), Color.Black * 0.35f);

            int destinationOuterLeft = point.X;
            int destinationOuterTop = point.Y;
            int destinationInnerLeft = point.X + cornerSize.X;
            int destinationInnerTop = point.Y + cornerSize.Y;
            int destinationInnerRight = point.X + size.X - cornerSize.X;
            int destinationInnerBottom = point.Y + size.Y - cornerSize.Y;
            int destinationInnerWidth = destinationInnerRight - destinationInnerLeft;
            int destinationInnerHeight = destinationInnerBottom - destinationInnerTop;

            int sourceOuterLeft = 0;
            int sourceOuterTop = 0;
            int sourceInnerLeft = cornerSize.X;
            int sourceInnerTop = cornerSize.Y;
            int sourceInnerRight = renderTarget.Width - cornerSize.X;
            int sourceInnerBottom = renderTarget.Height - cornerSize.Y;
            int sourceInnerWidth = Math.Min(size.X, sourceInnerRight - sourceInnerLeft);
            int sourceInnerHeight = Math.Min(size.Y, sourceInnerBottom - sourceInnerTop);

            // Top-Left
            spriteBatch.Draw(renderTarget, new Rectangle(destinationOuterLeft, destinationOuterTop, cornerSize.X, cornerSize.Y),
                new Rectangle(sourceOuterLeft, sourceOuterTop, cornerSize.X, cornerSize.Y), color);

            // Top-Center
            spriteBatch.Draw(renderTarget, new Rectangle(destinationInnerLeft, destinationOuterTop, destinationInnerWidth, cornerSize.Y),
                new Rectangle(sourceInnerLeft, sourceOuterTop, sourceInnerWidth, cornerSize.Y), color);

            // Top-Right
            spriteBatch.Draw(renderTarget, new Rectangle(destinationInnerRight, destinationOuterTop, cornerSize.X, cornerSize.Y),
                new Rectangle(sourceInnerRight, sourceOuterTop, cornerSize.X, cornerSize.Y), color);

            // Mid-Left
            spriteBatch.Draw(renderTarget, new Rectangle(destinationOuterLeft, destinationInnerTop, cornerSize.X, destinationInnerHeight),
                new Rectangle(sourceOuterLeft, sourceInnerTop, cornerSize.X, sourceInnerHeight), color);

            // Mid-Center
            spriteBatch.Draw(renderTarget, new Rectangle(destinationInnerLeft, destinationInnerTop, destinationInnerWidth, destinationInnerHeight),
                new Rectangle(sourceInnerLeft, sourceInnerTop, sourceInnerWidth, sourceInnerHeight), color);

            // Mid-Right
            spriteBatch.Draw(renderTarget, new Rectangle(destinationInnerRight, destinationInnerTop, cornerSize.X, destinationInnerHeight),
                new Rectangle(sourceInnerRight, sourceInnerTop, cornerSize.X, sourceInnerHeight), color);

            // Bottom-Left
            spriteBatch.Draw(renderTarget, new Rectangle(destinationOuterLeft, destinationInnerBottom, cornerSize.X, cornerSize.Y),
                new Rectangle(sourceOuterLeft, sourceInnerBottom, cornerSize.X, cornerSize.Y), color);

            // Bottom-Center
            spriteBatch.Draw(renderTarget, new Rectangle(destinationInnerLeft, destinationInnerBottom, destinationInnerWidth, cornerSize.Y),
                new Rectangle(sourceInnerLeft, sourceInnerBottom, sourceInnerWidth, cornerSize.Y), color);

            // Bottom-Right
            spriteBatch.Draw(renderTarget, new Rectangle(destinationInnerRight, destinationInnerBottom, cornerSize.X, cornerSize.Y),
                new Rectangle(sourceInnerRight, sourceInnerBottom, cornerSize.X, cornerSize.Y), color);
        }
    }
}
