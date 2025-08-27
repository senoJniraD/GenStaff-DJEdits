using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ModelLib;
using TacticalAILib;

namespace GSBPGEMG.UI
{
    public static class UIElement_PathRenderer
    {
        private static RenderTarget2D renderTarget;
        private static SpriteBatch spriteBatch;
        private static VertexPositionColor[] vertices;
        private static int verticesCount;
        private static BasicEffect basicEffect;

        public enum RenderType { Route, Arrow, Cross }
        public static List<(RenderType renderType, bool outline, int numberOfElements)> paths;

        private static List<(Vector2 position, float diameter, Color color)> routeCircles;
        private static List<Vector2> routePositions;
        private static Texture2D routeCircleImage;

        public static void Load()
        {
            GraphicsDevice device = Game1.GraphicsDeviceRef;
            renderTarget = new RenderTarget2D(device, Map.Width, Map.Height, false,
                device.PresentationParameters.BackBufferFormat,
                device.PresentationParameters.DepthStencilFormat,
                device.PresentationParameters.MultiSampleCount, RenderTargetUsage.PreserveContents);

            spriteBatch = new SpriteBatch(Game1.GraphicsDeviceRef);

            int size = 32;
            int halfSize = size / 2;
            routeCircleImage = new Texture2D(Game1.GraphicsDeviceRef, size, size);
            Color[] pixels = new Color[routeCircleImage.Width * routeCircleImage.Height];
            for (int x = 0; x < routeCircleImage.Width; x++)
                for (int y = 0; y < routeCircleImage.Height; y++)
                    pixels[x + (y * routeCircleImage.Width)] =
                        Color.White * Math.Clamp(1f - (Vector2.Distance(new Vector2(x, y), new Vector2(halfSize, halfSize)) / halfSize), 0f, 1f);
            routeCircleImage.SetData(pixels);

            basicEffect = new BasicEffect(device);
            basicEffect.VertexColorEnabled = true;
            basicEffect.World = Matrix.Identity;
            vertices = new VertexPositionColor[10000];

            paths = new List<(RenderType, bool, int)>();
            routeCircles = new List<(Vector2, float, Color)>(100000);
            routePositions = new List<Vector2>(10000);
            vertices = new VertexPositionColor[10000];
        }

        internal static void AddRoute(Sides side, IEnumerable<MapPathfinding.MapNode> mapNodes, float width, bool outline, float fadeIn)
        {
            foreach (MapPathfinding.MapNode mapNode in mapNodes)
                routePositions.Add(mapNode.Position);
            AddRoute(side, width, outline, fadeIn);
        }

        internal static void AddRoute(Sides side, IEnumerable<PointI> points, float width, bool outline, float fadeIn)
        {
            foreach (PointI point in points)
                routePositions.Add(point.ToVector2());
            AddRoute(side, width, outline, fadeIn);
        }

        internal static void AddRoute(Sides side, IEnumerable<Vector2> positions, float width, bool outline, float fadeIn)
        {
            foreach (Vector2 position in positions)
                routePositions.Add(position);
            AddRoute(side, width, outline, fadeIn);
        }

        private static void AddRoute(Sides side, float width, bool outline, float fadeIn)
        {
            paths.Add((RenderType.Route, outline, routePositions.Count));
            foreach (Vector2 position in routePositions)
                routeCircles.Add((position, width, SideColors.GetSideColors(side).highlightedColor.ToMGColor() * fadeIn));
            routePositions.Clear();
        }

        internal static void AddArrow(Sides side, PointI startPoint, PointI endPoint, float pixelWidth, bool outline)
        {
            if (startPoint == endPoint)
                return;

            Vector2 startPosition = startPoint.ToVector2();
            Vector2 endPosition = endPoint.ToVector2();
            float arrowLength = Vector2.Distance(startPosition, endPosition);
            float triangleLength = Math.Min(25, arrowLength);
            Vector2 trianglePosition = endPosition - (triangleLength * Vector2.Normalize(endPosition - startPosition));
            int lastVerticesCount = verticesCount;

            if (outline)
            {
                if (arrowLength > triangleLength)
                    AddRectangle(startPosition, trianglePosition, Color.Transparent, pixelWidth, 3);
                AddTriangle(trianglePosition, endPosition, Color.Transparent, pixelWidth, 3f);
            }

            Color color = SideColors.GetSideColors(side).highlightedColor.ToMGColor();
            if (arrowLength > triangleLength)
                AddRectangle(startPosition, trianglePosition, color, pixelWidth, 0f);
            AddTriangle(trianglePosition, endPosition, color, pixelWidth, 0f);

            paths.Add((RenderType.Arrow, outline, verticesCount - lastVerticesCount));
        }

        internal static void AddCross(PointI point, float size, Color color, Color outlineColor, float lineSize)
        {
            Vector2 position = point.ToVector2();
            int lastVerticesCount = verticesCount;
            AddRectangle(position + new Vector2(-size, -size), position + new Vector2(size, size), color, lineSize, 4f);
            AddRectangle(position + new Vector2(-size, size), position + new Vector2(size, -size), color, lineSize, 4f);
            AddRectangle(position + new Vector2(-size, -size), position + new Vector2(size, size), outlineColor, lineSize, 0f);
            AddRectangle(position + new Vector2(-size, size), position + new Vector2(size, -size), outlineColor, lineSize, 0f);
            paths.Add((RenderType.Cross, true, verticesCount - lastVerticesCount));
        }

        private static void AddRectangle(Vector2 startPosition, Vector2 endPosition, Color color, float width, float outlineSize)
        {
            Vector2 axisY = Vector2.Normalize(endPosition - startPosition);
            Vector2 axisX = new Vector2(-axisY.Y, axisY.X);
            Vector2 halfWidth = ((width / 2f) + outlineSize) * axisX;
            startPosition -= (outlineSize * axisY);
            endPosition += (outlineSize * axisY);

            Vector2 rectangleStartLeft = startPosition - halfWidth;
            Vector2 rectangleStartRight = startPosition + halfWidth;
            Vector2 rectangleEndLeft = endPosition - halfWidth;
            Vector2 rectangleEndRight = endPosition + halfWidth;

            AddVertex(rectangleStartRight, color);
            AddVertex(rectangleStartLeft, color);
            AddVertex(rectangleEndRight, color);
            AddVertex(rectangleEndLeft, color);
            AddVertex(rectangleEndRight, color);
            AddVertex(rectangleStartLeft, color);
        }

        private static void AddTriangle(Vector2 basePosition, Vector2 tipPosition, Color color, float baseWidth, float outlineSize)
        {
            if (basePosition == tipPosition)
                return;

            Vector2 axisY = Vector2.Normalize(tipPosition - basePosition);
            Vector2 axisX = new Vector2(-axisY.Y, axisY.X);
            float triangleHeight = Vector2.Distance(basePosition, tipPosition);
            baseWidth += ((baseWidth / triangleHeight) * outlineSize * 2);

            Vector2 triangleTip = tipPosition + (outlineSize * axisY);
            Vector2 triangleBase = basePosition - (outlineSize * axisY);
            Vector2 triangleBaseLeft = triangleBase - (baseWidth * axisX);
            Vector2 triangleBaseRight = triangleBase + (baseWidth * axisX);

            AddVertex(triangleTip, color);
            AddVertex(triangleBaseRight, color);
            AddVertex(triangleBaseLeft, color);
        }

        private static void AddVertex(Vector2 position, Color color)
        {
            vertices[verticesCount] = new VertexPositionColor() { Position = new Vector3(position.X, position.Y, 0.5f), Color = color };
            verticesCount++;
        }

        internal static void BeginDraw()
        {
            GraphicsDevice graphicsDevice = Game1.GraphicsDeviceRef;
            graphicsDevice.SetRenderTarget(renderTarget);
            graphicsDevice.Clear(ClearOptions.Target, Color.Transparent, 1f, 0);

            int circlesOffset = 0;
            int verticesOffset = 0;
            for (int i = 0; i < paths.Count; i++)
            {
                if ((paths[i].renderType == RenderType.Arrow) || (paths[i].renderType == RenderType.Cross))
                {
                    graphicsDevice.BlendState = BlendState.Opaque;

                    basicEffect.View = Matrix.CreateLookAt(
                        new Vector3(graphicsDevice.Viewport.Width / 2, graphicsDevice.Viewport.Height / 2, graphicsDevice.Viewport.MinDepth),
                        new Vector3(graphicsDevice.Viewport.Width / 2, graphicsDevice.Viewport.Height / 2, graphicsDevice.Viewport.MaxDepth), Vector3.Down);
                    basicEffect.Projection = Matrix.CreateOrthographic(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height,
                        graphicsDevice.Viewport.MinDepth, graphicsDevice.Viewport.MaxDepth);
                    basicEffect.CurrentTechnique.Passes[0].Apply();

                    graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, verticesOffset, paths[i].numberOfElements / 3);
                    verticesOffset += paths[i].numberOfElements;
                }

                if (paths[i].renderType == RenderType.Route)
                {
                    Vector2 origin = new Vector2(routeCircleImage.Width / 2);

                    if (paths[i].outline)
                    {
                        spriteBatch.Begin(blendState: BlendState.Opaque);
                        for (int j = circlesOffset; j < circlesOffset + paths[i].numberOfElements; j++)
                            spriteBatch.Draw(routeCircleImage, routeCircles[j].position, null, routeCircles[j].color * 0.5f, 0f, origin,
                                (routeCircles[j].diameter + 1) / routeCircleImage.Width, SpriteEffects.None, 0f);
                        spriteBatch.End();
                    }

                    spriteBatch.Begin(blendState: BlendState.AlphaBlend);
                    for (int j = circlesOffset; j < circlesOffset + paths[i].numberOfElements; j++)
                        spriteBatch.Draw(routeCircleImage, routeCircles[j].position, null, routeCircles[j].color, 0f, origin,
                            routeCircles[j].diameter / routeCircleImage.Width, SpriteEffects.None, 0f);
                    spriteBatch.End();

                    circlesOffset += paths[i].numberOfElements;
                }
            }

            graphicsDevice.BlendState = BlendState.AlphaBlend;
            paths.Clear();
            routeCircles.Clear();
            verticesCount = 0;
        }

        internal static void EndDraw(Game1 game)
        {
            game.spriteLayerPaths.Draw(renderTarget, game.MapOffset, Color.White * 0.6f);
        }
    }
}
