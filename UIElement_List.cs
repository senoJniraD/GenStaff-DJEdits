using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GSBPGEMG.UI
{
    public class UIElement_List
    {
        public Vector2 position;
        public Vector2 size;
        public int? lineGap;

        public bool selectionRequired;
        public bool selectionWhenOverMaxOnly;
        public int selection;
        public float displayStart;
        public int displayMax;
        public float lerpPosition;
        public bool highlighted;

        public UIElement_ScrollBar scrollBar;
        public bool scrollBarRequired;
        public bool scrollBarMoveDisplayStart;

        public string lastSelectionStringValue;

        private List<int> drawItemNosRequested;
        public List<Vector2> drawItemPositionsRequested;
        public List<Color> drawItemColorsRequested;

        public SpriteBatch spriteBatch;

        public UIElement_List(Vector2 requestedPosition, Vector2 requestedSize, int? requestedLineGap, int requestedDisplayMax)
        {
            position = requestedPosition;
            size = requestedSize;
            lineGap = requestedLineGap;
            displayMax = requestedDisplayMax;

            scrollBar = new UIElement_ScrollBar(new Vector2(position.X + size.X, position.Y + 1), (int)(size.Y - 9),
                 UIElement_ScrollBar.Orientation.Vertical);
            scrollBarMoveDisplayStart = true;

            drawItemNosRequested = [];
            drawItemPositionsRequested = [];
            drawItemColorsRequested = [];
        }

        public void Update<T>(List<T> list, bool requestedSelectionRequired, float? requestedMaxDisplayStart = null)
        {
            // Selection Input
            selectionRequired = requestedSelectionRequired;

            int pageScrollAmount = lineGap != null ? displayMax : 1;
            float maxDisplayStart = Math.Max(0, requestedMaxDisplayStart ?? (list.Count - pageScrollAmount));

            if (selectionRequired == true && (selectionWhenOverMaxOnly == false || list.Count > pageScrollAmount))
            {
                int lastSelection = selection;
                float requestedDisplayStart = displayStart;

                // Mouse Scroll Wheel
                Vector2 pointerPosition = Input.mouseMenuPosition;
                if (pointerPosition.X >= position.X &&
                    pointerPosition.X <= position.X + size.X &&
                    pointerPosition.Y >= position.Y &&
                    pointerPosition.Y <= position.Y + size.Y &&
                    list.Count > pageScrollAmount)
                {
                    if (Input.mouseScrollWheelChange > 0)
                    {
                        if ((requestedDisplayStart > (int)maxDisplayStart) && (pageScrollAmount == 1))
                            requestedDisplayStart = (int)maxDisplayStart;
                        else
                            requestedDisplayStart -= pageScrollAmount;
                    }
                    if (Input.mouseScrollWheelChange < 0)
                        requestedDisplayStart += pageScrollAmount;
                    if ((Input.mouseScrollWheelChange != 0) && (requestedDisplayStart < (int)maxDisplayStart))
                        requestedDisplayStart = (int)requestedDisplayStart;
                    requestedDisplayStart = MathHelper.Clamp(requestedDisplayStart, 0, maxDisplayStart);
                }

                // Pointer Selection
                highlighted = false;
                if (lineGap != null &&
                    pointerPosition.X >= position.X &&
                    pointerPosition.X <= position.X + size.X &&
                    pointerPosition.Y >= position.Y &&
                    pointerPosition.Y <= position.Y + size.Y &&
                    pointerPosition.X <= scrollBar.position.X - 5)
                {
                    highlighted = true;
                    int displayStartIndex = (int)displayStart;
                    selection = displayStartIndex + (int)((pointerPosition.Y - position.Y) / lineGap);
                    selection = Math.Max(displayStartIndex, Math.Min(selection, displayStartIndex + displayMax - 1));
                }

                // Scroll Bar
                if (scrollBar.itemNoSelected != null)
                    if (scrollBarMoveDisplayStart == true)
                        requestedDisplayStart = (int)scrollBar.itemNoSelected;
                    else
                        selection = (int)scrollBar.itemNoSelected;

                // Display Start Changed
                if (requestedDisplayStart != displayStart)
                {
                    displayStart = Math.Max(0, Math.Min(requestedDisplayStart, maxDisplayStart));
                    //if (selection < displayStart)
                    //    selection = displayStart;
                    //if (selection >= (displayStart + displayMax))
                    //    selection = displayStart + displayMax - 1;
                }
            }

            // Check Selection & Display Start Valid
            selection = Math.Max(0, Math.Min(selection, list.Count - 1));
            //if (selection < displayStart)
            //{
            //    displayStart = selection;
            //    lerpPosition = displayStart;
            //}
            //if ((lineGap != null) && (selection >= (displayStart + displayMax)))
            //{
            //    displayStart = selection - displayMax + 1;
            //    lerpPosition = displayStart;
            //}
            displayStart = Math.Max(0, Math.Min(displayStart, maxDisplayStart));

            // Lerp Position
            if (Math.Abs(lerpPosition - displayStart) >= 0.015f)
                lerpPosition = ModelLib.MathHelper.Lerp(lerpPosition, displayStart, 0.25f);
            else
                lerpPosition = displayStart;

            // Scroll Bar
            if (scrollBarMoveDisplayStart == true)
            {
                scrollBarRequired = (list.Count > displayMax);
                scrollBar.Update(selectionRequired == true && scrollBarRequired == true, (int)Math.Ceiling(displayStart),
                    (int)Math.Ceiling(maxDisplayStart), 0);
            }
            else
            {
                scrollBarRequired = list.Count > 0;
                scrollBar.Update(selectionRequired == true && scrollBarRequired == true, selection, list.Count, list.Count - (int)maxDisplayStart);
            }
        }

        public void Reset()
        {
            selection = 0;
            displayStart = 0f;
            lerpPosition = 0f;
            highlighted = false;

            scrollBar.Reset();

            lastSelectionStringValue = null;

            drawItemNosRequested.Clear();
            drawItemPositionsRequested.Clear();
            drawItemColorsRequested.Clear();
        }

        public void Draw(Game1 game, Action<Game1, SpriteBatch, int, Vector2, bool> drawAction, float fadeIn, int listCount)
        {
            drawItemNosRequested.Clear();
            drawItemPositionsRequested.Clear();
            drawItemColorsRequested.Clear();

            spriteBatch ??= new SpriteBatch(Game1.GraphicsDeviceRef);
            spriteBatch.Begin(rasterizerState: game.RasterizerStateDefault, blendState: BlendState.NonPremultiplied);
            //spriteBatch.Draw(GSBPGEMG.Game1.whitePixel, position, new Rectangle(0, 0, (int)size.X, (int)size.Y), Color.Black * 0.5f * fadeIn); // dev
            //if (scrollBarRequired == true)
            scrollBar.Draw(spriteBatch, fadeIn);
            spriteBatch.End();

            spriteBatch.Begin(rasterizerState: game.RasterizerStateClipping);

            Vector2 linePosition = Vector2.Zero;
            for (int i = 0; i < listCount; i++)
            {
                if (i - lerpPosition <= -1 || lineGap != null && i - lerpPosition >= displayMax)
                    continue;

                drawItemNosRequested.Add(i);
                if (lineGap != null)
                    drawItemPositionsRequested.Add(new Vector2(0, lineGap.Value * i - (int)(lerpPosition * lineGap.Value)));
                else
                    drawItemPositionsRequested.Add(Vector2.Zero);
                if (i == selection && selectionRequired == true && (selectionWhenOverMaxOnly == false || listCount > displayMax))
                    drawItemColorsRequested.Add(new Color(150, 200, 235));
                else
                    drawItemColorsRequested.Add(new Color(255, 255, 255));

                drawAction(game, spriteBatch, i, position + drawItemPositionsRequested[^1],
                    i == selection && selectionRequired == true && (selectionWhenOverMaxOnly == false || listCount > displayMax));

                if (lineGap != null)
                    linePosition += new Vector2(0, lineGap.Value);
            }

            Rectangle originalScissorRectangle = Game1.GraphicsDeviceRef.ScissorRectangle;
            Game1.GraphicsDeviceRef.ScissorRectangle = new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
            spriteBatch.End();
            Game1.GraphicsDeviceRef.ScissorRectangle = originalScissorRectangle;
        }
    }
}
