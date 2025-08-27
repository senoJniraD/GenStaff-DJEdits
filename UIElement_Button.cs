using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ModelLib;

namespace GSBPGEMG.UI
{
    public class UIElement_Button
    {
        public UIStyle.ButtonTypes ButtonType { get; set; }
        private UIElement_BorderedTexture buttonTextures;

        public UIStyle.FontTypes FontType;
        private UIElement_TextBox textBox;
        public Color inactiveTextColor;
        public Color activeTextColor;

        public Vector2 Position;
        public Vector2 Size;
        public bool autoListPositioning;

        public bool visible;
        public bool active;
        public bool highlighted;
        public bool lastHighlighted;
        public double highlightedStartTime;
        public bool pressed;
        public bool held;

        public Vector2 pointerOffset;

        public UIElement_Button(string text, UIStyle.ButtonTypes buttonType, UIStyle.FontTypes fontType, Vector2 position, Vector2 size)
        {
            ButtonType = buttonType;
            textBox = new UIElement_TextBox(fontType, text,
                horizontalAlignment: UIElement_TextBox.HorizontalAlignments.Center,
                verticalAlignment: UIElement_TextBox.VerticalAlignments.Center);
            textBox.Text = text;
            FontType = fontType;

            Position = position;
            autoListPositioning = true;
            Size = size;
            visible = true;
            inactiveTextColor = Color.Black * 0.5f;
            activeTextColor = Color.Black;

            buttonTextures = new();

            Reset();
        }

        public void Update(bool requestedActive = true)
        {
            if (!visible)
                return;

            active = requestedActive;
            if (active == true)
            {
                Vector2 pointerPosition = Input.mouseMenuPosition;
                pressed = false;
                held = false;
                highlighted = false;

                if (Math.Abs(pointerPosition.X - Position.X) <= Size.X / 2f &&
                    Math.Abs(pointerPosition.Y - Position.Y) <= Size.Y / 2f)
                {
                    highlighted = true;
                    if (Input.mouseLeftClick == true)
                        pressed = true;
                    if (Input.mouseLeftHeld == true)
                        held = true;
                    Input.MouseCursorIcon = MouseCursor.Hand;
                }
            }
        }

        public void Highlight(double totalTime, bool pointerInput)
        {
            highlighted = true;
            highlightedStartTime = totalTime;
        }

        public void Unhighlight()
        {
            highlighted = false;
            highlightedStartTime = double.MinValue;
        }

        public void Reset()
        {
            Unhighlight();
            pressed = false;
            held = false;
        }

        public void Draw(Game1 game, SpriteBatch spriteBatch, string text = null, float fadeIn = 1f)
        {
            if (!visible)
                return;

            // Background
            Texture2D background = UIStyles.Current.Buttons[(int)ButtonType].Background;
            Texture2D border = UIStyles.Current.Buttons[(int)ButtonType].Border;
            Texture2D mask = UIStyles.Current.Buttons[(int)ButtonType].Mask;
            buttonTextures.Size = Size;
            buttonTextures.CornerSize = new(87, 82); //new(76, 42);
            if (ButtonType == UIStyle.ButtonTypes.MainMenuButton2)
                buttonTextures.CornerSize = new(5, 10);
            buttonTextures.Position = Position - new Vector2((int)(Size.X / 2), (int)(Size.Y / 2));
            Color buttonColor = Color.White * fadeIn;
            
            if (highlighted)
            {
                buttonTextures.Position += new Vector2(1);
                buttonTextures.Draw(spriteBatch, background, border, mask, colorRequest: Color.White * 0.15f);
                buttonTextures.Position += new Vector2(1);
                buttonTextures.Draw(spriteBatch, background, border, mask, colorRequest: Color.White * 0.15f);
                buttonTextures.Position += new Vector2(-2);
            }
            else
            {
                buttonColor *= 0.9f;
            }
            buttonTextures.Draw(spriteBatch, background, border, mask, colorRequest: buttonColor);

            // Text
            if (text != null)
                textBox.Text = text;
            Color textColor = inactiveTextColor * fadeIn;
            if (highlighted == true && active == true)
                textColor = activeTextColor * fadeIn;
            if (highlighted)
                textBox.Draw(game, spriteBatch, position: Position + new Vector2(2, 3), color: Color.Black * 0.2f);
            textBox.Draw(game, spriteBatch, position: Position + new Vector2(1, 2), color: textColor);
        }
    }
}
