using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp.RichText;

namespace GSBPGEMG.UI
{
    public class UIElement_TextBox
    {
        public string Text { get; set; }
        public UIStyle.FontTypes FontType { get; set; }
        public Vector2 Position { get; set; }
        public Color Color { get; set; }

        public enum HorizontalAlignments { Left, Center, Right }
        public HorizontalAlignments HorizontalAlignment { get; set; }

        public enum VerticalAlignments { Top, Center, Bottom }
        public VerticalAlignments VerticalAlignment { get; set; }

        public int? MaxWidth { get; set; }
        public int? MaxHeight { get; set; }

        public enum AutoEllipsisMode { None, Character, Word }
        public AutoEllipsisMode AutoEllipsis { get; set; }

        public Point MeasuredOffset { get => measuredOffset; private set => measuredOffset = value; }
        private Point measuredOffset;
        public Point MeasuredSize { get => measuredSize; private set => measuredSize = value; }
        private Point measuredSize;

        private RichTextLayout textLayout;

        public enum TooltipModes { Never, OnOverflow, Always }
        public TooltipModes TooltipMode { get; set; }
        public string TooltipText { get; set; }
        private UIElement_BorderedTexture tooltipBorderedTexture { get; set; }
        private double? tooltipHoverStartTime;
        private Vector2? tooltipHoverDisplayPosition;
        private Vector2? tooltipHoverStartPosition;
        private RichTextLayout tooltipTextLayout;

        public UIElement_TextBox()
        {
            Color = Color.Black;
            AutoEllipsis = AutoEllipsisMode.Word;
            textLayout = new() { Text = "" };
            tooltipTextLayout = new() { Text = "" };
            tooltipBorderedTexture = new();
        }

        public UIElement_TextBox(UIStyle.FontTypes fontType, string text = "",
            HorizontalAlignments horizontalAlignment = HorizontalAlignments.Left,
            VerticalAlignments verticalAlignment = VerticalAlignments.Top)
        {
            Text = text;
            FontType = fontType;
            Color = Color.Black;
            HorizontalAlignment = horizontalAlignment;
            VerticalAlignment = verticalAlignment;
            textLayout = new() { Text = text };
            tooltipTextLayout = new() { Text = "" };
            tooltipBorderedTexture = new();
        }

        public void Draw(Game1 game, SpriteBatch spriteBatch, UIStyle.FontTypes? fontType = null, string text = null, Vector2? position = null, Color? color = null,
            HorizontalAlignments? horizontalAlignment = null, VerticalAlignments? verticalAlignment = null, Point? thickness = null,
            int? maxWidth = null, int? maxHeight = null, AutoEllipsisMode? autoEllipsis = null)
        {
            if (fontType.HasValue)
                FontType = fontType.Value;
            if (text != null)
                Text = text;
            if (position.HasValue)
                Position = position.Value;
            if (color.HasValue)
                Color = color.Value;

            if (horizontalAlignment.HasValue)
                HorizontalAlignment = horizontalAlignment.Value;
            if (verticalAlignment.HasValue)
                VerticalAlignment = verticalAlignment.Value;
            if (autoEllipsis.HasValue)
                AutoEllipsis = autoEllipsis.Value;

            if (textLayout.Width != maxWidth)
                textLayout.Width = MaxWidth = maxWidth.Value;
            if (textLayout.Height != maxHeight)
                textLayout.Height = MaxHeight = maxHeight.Value;
            textLayout.AutoEllipsisMethod = (AutoEllipsisMethod)(textLayout.Width.HasValue | textLayout.Height.HasValue ?
                AutoEllipsis : AutoEllipsisMode.None);

            if (Text?.Length >= 1 == false)
                return;

            textLayout.Text = Text;

            UIElements_Font font = UIStyles.Current.Fonts[(int)FontType];
            textLayout.Font = font.SpriteFont;

            measuredOffset = Point.Zero;
            measuredSize = textLayout.Measure(MaxWidth);

            if (HorizontalAlignment != HorizontalAlignments.Left)
            {
                measuredOffset.X = measuredSize.X;
                if (MaxWidth.HasValue)
                    measuredOffset.X = Math.Min(measuredOffset.X, MaxWidth.Value);
                if (HorizontalAlignment == HorizontalAlignments.Center)
                    measuredOffset.X /= 2;
                measuredOffset.X *= -1;
            }

            if (VerticalAlignment != VerticalAlignments.Top)
            {
                measuredOffset.Y = textLayout.Lines.Count >= 2 ? measuredSize.Y : font.LineHeight;
                if (MaxHeight.HasValue)
                    measuredOffset.Y = Math.Min(measuredOffset.Y, MaxHeight.Value);
                if (VerticalAlignment == VerticalAlignments.Center)
                    measuredOffset.Y /= 2;
                measuredOffset.Y *= -1;
            }

            Point thicknessOffset = new(1);
            if (thickness.HasValue)
                thicknessOffset = thickness.Value;
            if (font.StyleBold)
                thicknessOffset.X += 1;

            for (int thicknessOffsetX = 0; thicknessOffsetX < thicknessOffset.X; thicknessOffsetX++)
                for (int thicknessOffsetY = 0; thicknessOffsetY < thicknessOffset.Y; thicknessOffsetY++)
                    textLayout.Draw(spriteBatch, Position + new Vector2(
                        font.Offset.X + thicknessOffsetX,
                        font.Offset.Y + measuredOffset.Y + thicknessOffsetY),
                        Color, horizontalAlignment: (TextHorizontalAlignment)HorizontalAlignment);

            if (TooltipMode != TooltipModes.Never)
                DrawTooltip(game, game.spriteLayerTooltips);
        }

        public void DrawTooltip(Game1 game, SpriteBatch spriteBatch)
        {
            Point hoverSize = MeasuredSize;
            if (MaxHeight.HasValue)
                hoverSize.Y = MaxHeight.Value;

            if (TooltipMode == TooltipModes.Never ||
                Input.MousePositionWithinArea(Input.MouseCoordinateType.Menu, Input.MouseOriginType.TopLeft,
                Position.ToPoint() + measuredOffset, hoverSize) == false ||
                measuredSize == Point.Zero ||
                ((TooltipMode == TooltipModes.OnOverflow) &&
                (textLayout.Lines[^1].Chunks.Count == 0 ||
                (textLayout.Lines[^1].Chunks[^1] as TextChunk).Text.EndsWith(textLayout.AutoEllipsisString) == false)))
            {
                tooltipHoverStartTime = null;
                tooltipHoverStartPosition = null;
                tooltipHoverDisplayPosition = null;
                return;
            }

            tooltipHoverStartPosition ??= Input.mouseMenuPosition;
            if (tooltipHoverDisplayPosition == null &&
                Vector2.DistanceSquared(Input.mouseMenuPosition, tooltipHoverStartPosition.Value) >= 25)
            {
                tooltipHoverStartTime = null;
                tooltipHoverStartPosition = null;
                return;
            }

            tooltipHoverStartTime ??= Game1.GameTimeRef.TotalGameTime.TotalSeconds;
            if (Game1.GameTimeRef.TotalGameTime.TotalSeconds < tooltipHoverStartTime + 0.35d)
                return;

            tooltipHoverDisplayPosition ??= Input.mouseMenuPosition;

            tooltipTextLayout.Text = TooltipText ?? Text;
            tooltipTextLayout.Font = UIStyles.Current.Fonts[(int)UIStyle.FontTypes.Tooltip].SpriteFont;
            tooltipTextLayout.Width = 490;
            tooltipBorderedTexture.Size = tooltipTextLayout.Measure(tooltipTextLayout.Width).ToVector2() + new Vector2(20);
            tooltipBorderedTexture.Size.X = Math.Min(tooltipBorderedTexture.Size.X, 512);
            tooltipBorderedTexture.Size.Y = Math.Min(tooltipBorderedTexture.Size.Y, 512);
            tooltipBorderedTexture.Position = tooltipHoverDisplayPosition.Value + new Vector2(0, 20);
            tooltipBorderedTexture.Position.X = (int)Math.Min(tooltipBorderedTexture.Position.X, game.WindowWidth - tooltipBorderedTexture.Size.X - 10);
            tooltipBorderedTexture.Position.Y = (int)Math.Max(0, Math.Min(tooltipBorderedTexture.Position.Y, game.WindowHeight - tooltipBorderedTexture.Size.Y - 10));

            tooltipBorderedTexture.Draw(spriteBatch, UIStyles.Current.WindowBackground2, UIStyles.Current.WindowBorder2, shadowRequest: true);
            tooltipTextLayout.Draw(spriteBatch, tooltipBorderedTexture.Position + new Vector2(10), Color.Black);
        }
    }
}
