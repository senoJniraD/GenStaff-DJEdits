using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GSBPGEMG.UI
{
    public class UIElement_ScrollBar
    {
        public Vector2 position;
        public int size;
        public Vector2? loadedPosition;
        public int? loadedSize;

        public enum Orientation
        {
            Vertical,
            Horizontal
        }
        public Orientation orientation;

        public int borderSize;

        public bool visible;

        public List<Rectangle> backgroundDestinationRectangles;
        public List<Rectangle> backgroundSourceRectangles;

        public Vector2 selectorPosition;
        public bool selectorHighlighted;

        public bool itemNoSelectionLatch;
        public Vector2 itemNoSelectionPointerOffset;
        public int itemNoSelectionPointerExtraWidth;
        public int? itemNoSelected;

        public Texture2D textureBackground => UIStyles.Current.ScrollBarBackground;
        public Texture2D textureSelector => UIStyles.Current.ScrollBarSelector;

        public UIElement_ScrollBar(Vector2 newPosition, int newSize, Orientation newOrientation)
        {
            borderSize = 49;
            visible = true;
            itemNoSelectionPointerExtraWidth = 10;

            backgroundDestinationRectangles = new List<Rectangle>(2);
            backgroundSourceRectangles = new List<Rectangle>(2);

            Load(newPosition, newSize, newOrientation);
        }

        public void Load(Vector2 newPosition, int newSize, Orientation newOrientation)
        {
            if (newPosition == loadedPosition && newSize == loadedSize)
                return;

            backgroundDestinationRectangles.Clear();
            backgroundSourceRectangles.Clear();

            position = newPosition;
            size = newSize;
            size = Math.Min(size, textureBackground.Height);
            orientation = newOrientation;

            if (size < textureBackground.Height)
            {
                // Background Top
                if (orientation == Orientation.Vertical)
                    backgroundDestinationRectangles.Add(new Rectangle((int)position.X, (int)position.Y,
                        textureBackground.Width, size - borderSize));
                else
                    backgroundDestinationRectangles.Add(new Rectangle((int)position.X,
                        (int)(position.Y + textureBackground.Width),
                        textureBackground.Width, size - borderSize));
                backgroundSourceRectangles.Add(new Rectangle(0, 0, textureBackground.Width, size - borderSize));

                // Background Bottom
                if (orientation == Orientation.Vertical)
                    backgroundDestinationRectangles.Add(new Rectangle((int)position.X, (int)(position.Y + size - borderSize),
                        textureBackground.Width, borderSize));
                else
                    backgroundDestinationRectangles.Add(new Rectangle((int)(position.X + size - borderSize),
                        (int)(position.Y + textureBackground.Width), textureBackground.Width, borderSize));
                backgroundSourceRectangles.Add(new Rectangle(0, textureBackground.Height - borderSize,
                    textureBackground.Width, borderSize));
            }
            else
            {
                // Background
                if (orientation == Orientation.Vertical)
                    backgroundDestinationRectangles.Add(new Rectangle((int)position.X, (int)position.Y,
                        textureBackground.Width, size));
                else
                    backgroundDestinationRectangles.Add(new Rectangle((int)position.X,
                        (int)(position.Y + textureBackground.Width),
                        textureBackground.Width, size));
                backgroundSourceRectangles.Add(new Rectangle(0, 0, textureBackground.Width, size));
            }

            // Selector
            if (orientation == Orientation.Vertical)
                selectorPosition.X = position.X + textureBackground.Width / 2f;
            else
                selectorPosition.Y = position.Y + textureBackground.Width / 2f;

            // Record Loaded Values
            loadedPosition = position;
            loadedSize = size;
        }

        public void Update(bool active, int itemNo, int maxValue, int totalReduction)
        {
            if (!visible)
                return;

            // Selector Position
            int offset = 0;
            if (maxValue - totalReduction > 0)
            {
                int freeSpace = size - 2 * borderSize;
                offset = (int)((float)itemNo / (maxValue - totalReduction) * freeSpace);
                if (itemNo >= 1 && offset == 0)
                    offset = 1;
                if (itemNo == maxValue - 1 && offset == freeSpace)
                    offset = freeSpace - 1;
                if (itemNo >= maxValue)
                    offset = freeSpace;
                offset = Math.Max(0, Math.Min(freeSpace, offset));
            }
            offset += borderSize;

            if (orientation == Orientation.Vertical)
                selectorPosition.Y = position.Y + offset;
            else
                selectorPosition.X = position.X + offset;

            // Scroll Bar Pointer Input
            itemNoSelected = null;
            if (active == true)
            {
                Vector2 pointerPosition = Input.mouseMenuPosition - itemNoSelectionPointerOffset;

                selectorHighlighted = Input.MousePositionWithinArea(Input.MouseCoordinateType.Menu,
                     Input.MouseOriginType.Center, selectorPosition.ToPoint(), textureSelector.Bounds.Size) || itemNoSelectionLatch;

                if (Input.mouseLeftHeld == false)
                    itemNoSelectionLatch = false;
                if (orientation == Orientation.Vertical)
                {
                    if (Input.mouseLeftClick == true &&
                        pointerPosition.X - position.X >= -(textureBackground.Width / 2) - 10 &&
                        pointerPosition.X - position.X <= textureBackground.Width / 2 + 10 &&
                        pointerPosition.Y - position.Y >= 0f &&
                        pointerPosition.Y - position.Y <= size ||
                        itemNoSelectionLatch == true)
                    {
                        itemNoSelectionLatch = true;
                        itemNoSelected = (int)Math.Round((pointerPosition.Y - position.Y - borderSize) / (size - 2 * borderSize) * (maxValue - totalReduction));
                        itemNoSelected = Math.Max(0, Math.Min((int)itemNoSelected, maxValue - totalReduction));
                    }
                }
                if (orientation == Orientation.Horizontal)
                {
                    if (Input.mouseLeftClick == true &&
                        pointerPosition.X - position.X >= 0f &&
                        pointerPosition.X - position.X <= size &&
                        pointerPosition.Y - position.Y >= -(textureBackground.Width / 2) - 10 &&
                        pointerPosition.Y - position.Y <= textureBackground.Width / 2 + 10 ||
                        itemNoSelectionLatch == true)
                    {
                        itemNoSelectionLatch = true;
                        itemNoSelected = (int)Math.Round((pointerPosition.X - position.X - borderSize) / (size - 2 * borderSize) * (maxValue - totalReduction));
                        itemNoSelected = Math.Max(0, Math.Min((int)itemNoSelected, maxValue - totalReduction));
                    }
                }
            }
        }

        public void Reset()
        {
            selectorHighlighted = false;
            itemNoSelectionLatch = false;
            itemNoSelected = null;
        }

        public void Draw(SpriteBatch spriteBatch, float fadeIn)
        {
            if (visible == true)
            {
                // Background
                if (orientation == Orientation.Vertical)
                    for (int i = 0; i < backgroundDestinationRectangles.Count; i++)
                        spriteBatch.Draw(textureBackground, backgroundDestinationRectangles[i], backgroundSourceRectangles[i],
                            Color.White * fadeIn, MathHelper.ToRadians(0f), Vector2.Zero, SpriteEffects.None, 0f);
                else
                    for (int i = 0; i < backgroundDestinationRectangles.Count; i++)
                        spriteBatch.Draw(textureBackground, backgroundDestinationRectangles[i], backgroundSourceRectangles[i],
                            Color.White * fadeIn, MathHelper.ToRadians(270f), Vector2.Zero, SpriteEffects.None, 0f);

                // Selector
                Vector2 selectorOrigin = new Vector2(textureSelector.Width, textureSelector.Height) / 2f;
                if (selectorHighlighted)
                    spriteBatch.Draw(textureSelector, selectorPosition + new Vector2(5, 5), null, Color.Black * fadeIn * 0.35f, 0f,
                        new Vector2(textureSelector.Width, textureSelector.Height) / 2f, 1f, SpriteEffects.None, 0f);
                spriteBatch.Draw(textureSelector, selectorPosition, null, Color.White * fadeIn, 0f,
                    selectorOrigin, selectorHighlighted ? 1f : 0.8f, SpriteEffects.None, 0f);
            }
        }
    }
}
