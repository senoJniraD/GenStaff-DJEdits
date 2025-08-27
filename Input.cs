using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ModelLib;
using static GSBPGEMG.Game1;

namespace GSBPGEMG
{
    public static class Input
    {
        private static bool gameActive;
        private static bool lastGameActive;

        private static KeyboardState keyboardState;
        private static KeyboardState lastKeyboardState;
        private static MouseState mouseState;
        private static MouseState lastMouseState;

        public enum MouseButtons { Left, Middle, Right, X1, X2 }
        public enum MouseCoordinateType { Menu, Map }
        public enum MouseOriginType { TopLeft, Center }

        public static Vector2 mouseMenuPosition { get; private set; }
        public static Point mouseMenuPoint { get; private set; }
        public static PointI mouseMenuPointI { get; private set; }

        public static Vector2 mouseMapPosition { get; private set; }
        public static Point mouseMapPoint { get; private set; }
        public static PointI mouseMapPointI { get; private set; }

        public static bool mouseLeftClick { get; private set; }
        public static bool mouseLeftHeld { get; private set; }
        public static bool mouseMiddleClick { get; private set; }
        public static bool mouseMiddleHeld { get; private set; }
        public static bool mouseRightClick { get; private set; }
        public static bool mouseRightHeld { get; private set; }
        public static bool mouseX1Click { get; private set; }
        public static bool mouseX1Held { get; private set; }
        public static bool mouseX2Click { get; private set; }
        public static bool mouseX2Held { get; private set; }
        public static int mouseScrollWheelChange { get; private set; }

        public static bool autoMouseJumpEnabled = true;
        private static bool autoMouseJumpInProgress;
        private static Point autoMouseJumpStart;
        private static Point autoMouseJumpEnd;
        private static double autoMouseJumpTime;
        private static bool autoMouseJumpSetComplete;

        public static MouseCursor MouseCursorIcon;

        public static void Initialize()
        {
            keyboardState = lastKeyboardState = default;
            mouseState = lastMouseState = default;
            MouseCursorIcon = MouseCursor.Wait;
        }

        public static void Update(Game1 game)
        {
            lastGameActive = gameActive;
            lastKeyboardState = keyboardState;
            lastMouseState = mouseState;

            gameActive = false;
            try
            {
                if (GameRef.IsActive && (System.Windows.Forms.Form.ActiveForm?.Handle == GameRef.Window.Handle))
                    gameActive = true;
            }
            catch { }

            if (gameActive)
            {
                keyboardState = Keyboard.GetState();
                mouseState = Mouse.GetState();
            }
            else
            {
                keyboardState = default;
                mouseState = default;
            }
            Debug.SetInputs(game, ref gameActive, ref keyboardState, ref mouseState);

            mouseMenuPoint = ((mouseState.Position - game.ScreenResizer.offset).ToVector2() / game.ScreenResizer.scaling).ToPoint();
            mouseMenuPosition = mouseMenuPoint.ToVector2();
            mouseMenuPointI = mouseMenuPoint.ToPointI();
            mouseMapPoint = mouseMenuPoint - game.MapOffset.ToPoint();
            mouseMapPosition = mouseMapPoint.ToVector2();
            mouseMapPointI = mouseMapPoint.ToPointI();

            mouseLeftHeld = mouseState.LeftButton == ButtonState.Pressed;
            mouseLeftClick = mouseLeftHeld && (lastMouseState.LeftButton == ButtonState.Released);
            mouseMiddleHeld = mouseState.MiddleButton == ButtonState.Pressed;
            mouseMiddleClick = mouseMiddleHeld && (lastMouseState.MiddleButton == ButtonState.Released);
            mouseRightHeld = mouseState.RightButton == ButtonState.Pressed;
            mouseRightClick = mouseRightHeld && (lastMouseState.RightButton == ButtonState.Released);
            mouseX1Held = mouseState.XButton1 == ButtonState.Pressed;
            mouseX1Click = mouseX1Held && (lastMouseState.XButton1 == ButtonState.Released);
            mouseX2Held = mouseState.XButton2 == ButtonState.Pressed;
            mouseX2Click = mouseX2Held && (lastMouseState.XButton2 == ButtonState.Released);

            if (!gameActive && lastGameActive)
            {
                mouseScrollWheelChange = 0;
                autoMouseJumpInProgress = false;
                return;
            }

            mouseScrollWheelChange = mouseState.ScrollWheelValue - lastMouseState.ScrollWheelValue;

            if (autoMouseJumpEnabled && autoMouseJumpInProgress)
            {
                if (autoMouseJumpSetComplete == false)
                {
                    float jumpLerp = Math.Clamp((float)(GameTimeRef.TotalGameTime.TotalSeconds - autoMouseJumpTime) / 0.2f, 0f, 1f);
                    Point jumpPoint = Vector2.Lerp(autoMouseJumpStart.ToVector2(), autoMouseJumpEnd.ToVector2(), jumpLerp).ToPoint();
                    jumpPoint = game.ScreenResizer.offset + (jumpPoint.ToVector2() * game.ScreenResizer.scaling).ToPoint();
                    if (!Debug.SetMousePosition(game, jumpPoint))
                        Mouse.SetPosition(jumpPoint.X, jumpPoint.Y);
                    if (jumpPoint == autoMouseJumpEnd)
                        autoMouseJumpSetComplete = true;
                }

                if ((GameTimeRef.TotalGameTime.TotalSeconds - autoMouseJumpTime) >= 0.5f)
                    autoMouseJumpInProgress = false;
            }

            MouseSetIcon(game);
        }

        public static bool MouseClick(MouseButtons mouseButton)
        {
            switch(mouseButton)
            {
                case MouseButtons.Left: return mouseLeftClick;
                case MouseButtons.Middle: return mouseMiddleClick;
                case MouseButtons.Right: return mouseRightClick;
                case MouseButtons.X1: return mouseX1Click;
                case MouseButtons.X2: return mouseX2Click;
                default: return false;
            }
        }

        public static bool MouseClickWithinArea(MouseButtons mouseButton, MouseCoordinateType mouseCoordinateType, MouseOriginType mouseOriginType, Point point, Point areaSize)
        {
            return MouseClick(mouseButton) && MousePositionWithinArea(mouseCoordinateType, mouseOriginType, point, areaSize);
        }

        public static bool MousePositionWithinArea(MouseCoordinateType mouseCoordinateType, MouseOriginType mouseOriginType,
            Point point, Point areaSize, double? rotation = null)
        {
            Point mousePosition = default;
            switch (mouseCoordinateType)
            {
                case MouseCoordinateType.Menu: mousePosition = mouseMenuPoint; break;
                case MouseCoordinateType.Map: mousePosition = mouseMapPoint; break;
            }
            Point topLeftPosition = default;
            switch (mouseOriginType)
            {
                case MouseOriginType.TopLeft: topLeftPosition = point; break;
                case MouseOriginType.Center: topLeftPosition = point - new Point(areaSize.X / 2, areaSize.Y / 2); break;
            }
            if (rotation != null)
                mousePosition = point + Vector2.Transform((mousePosition - point).ToVector2(), Matrix.CreateRotationZ(-(float)rotation)).ToPoint();
            return new Rectangle(topLeftPosition.X, topLeftPosition.Y, areaSize.X, areaSize.Y).Contains(mousePosition);
        }

        public static bool MousePositionWithinMap()
        {
            return new Rectangle(0, 0, Map.Width - 1, Map.Height - 1).Contains(mouseMapPoint);
        }

        public static void MouseAutoJump(Point jumpPoint)
        {
            if (autoMouseJumpEnabled == false)
                return;

            autoMouseJumpStart = !autoMouseJumpInProgress ? mouseMenuPoint : autoMouseJumpEnd;
            autoMouseJumpEnd = jumpPoint;
            autoMouseJumpTime = GameTimeRef.TotalGameTime.TotalSeconds;
            autoMouseJumpInProgress = true;
            autoMouseJumpSetComplete = false;
        }

        public static void MouseSetIcon(Game1 game)
        {
            // Set to the selected icon at the end of the last frame (after any game code overrides)
            Mouse.SetCursor(MouseCursorIcon);

            // Setup default icon for cursor's screen position (before any game code overrides)
            MouseCursorIcon = MouseCursor.Arrow;
            if (MousePositionWithinArea(MouseCoordinateType.Menu, MouseOriginType.TopLeft,
                new Point(2, 2), new Point(game.WindowWidth - 4, game.WindowHeight - 4)))
            {
                if (game.loadingThread != null)
                    MouseCursorIcon = MouseCursor.WaitArrow;
                else if (mouseMapPosition.X >= 0 && mouseMapPosition.Y >= 0 &&
                    mouseMapPosition.X < Map.Width && mouseMapPosition.Y < Map.Height)
                    MouseCursorIcon = MouseCursor.Crosshair;
                else
                    MouseCursorIcon = MouseCursor.Hand;
            }
        }

        public static void MouseClearButtons()
        {
            mouseLeftClick = false;
            mouseLeftHeld = false;
            mouseMiddleClick = false;
            mouseMiddleHeld = false;
            mouseRightClick = false;
            mouseRightHeld = false;
            mouseX1Click = false;
            mouseX1Held = false;
            mouseX2Click = false;
            mouseX2Held = false;
            mouseScrollWheelChange = 0;
        }

        public static bool KeyIsPressed(Keys key)
        {
            return keyboardState.IsKeyDown(key) && lastKeyboardState.IsKeyUp(key);
        }

        public static bool KeyIsHeld(Keys key)
        {
            return keyboardState.IsKeyDown(key);
        }

        public static void Draw(Game1 game)
        {
            if (autoMouseJumpEnabled && autoMouseJumpInProgress)
            {
                float pointerLerp = Math.Clamp((float)(GameTimeRef.TotalGameTime.TotalSeconds - autoMouseJumpTime) / 0.2f, 0f, 1f);
                Vector2 pointerPoint = Vector2.Lerp(autoMouseJumpStart.ToVector2(), autoMouseJumpEnd.ToVector2(), pointerLerp);
                float trailLerp = Math.Clamp((float)(GameTimeRef.TotalGameTime.TotalSeconds - (autoMouseJumpTime + 0.3d)) / 0.2f, 0f, 1f);
                Vector2 trailPoint = Vector2.Lerp(autoMouseJumpStart.ToVector2(), autoMouseJumpEnd.ToVector2(), trailLerp);
                game.DrawLine(game.spriteLayerControls, pointerPoint, trailPoint, Color.Black * 0.1f, 10);
            }
        }
    }
}
