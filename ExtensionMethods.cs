using System;
using Microsoft.Xna.Framework;
using ModelLib;

namespace GSBPGEMG
{
    public static class ExtensionMethods
    {
        public static Vector2 ToMGVector2(this PointI pointI)
        {
            return new Vector2(pointI.X, pointI.Y);
        }

        public static Point ToMGPoint(this PointI pointI)
        {
            return new Point(pointI.X, pointI.Y);
        }

        public static Point ToDrawingPoint(this System.Drawing.Point point)
        {
            return new Point(point.X, point.Y);
        }

        public static PointI ToPointI(this Point point)
        {
            return new PointI(point.X, point.Y);
        }

        public static PointI ToPointI(this Vector2 vector2)
        {
            return new PointI((int)vector2.X, (int)vector2.Y);
        }

        public static PointI ToPointIRounded(this Vector2 vector2)
        {
            return new PointI((int)Math.Round(vector2.X), (int)Math.Round(vector2.Y));
        }

        public static Color ToMGColor(this System.Drawing.Color color)
        {
            return new Color(color.R, color.G, color.B, color.A);
        }

        public static System.Drawing.Color ToDrawingColor(this Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static Color ToMGColor(this System.Windows.Media.Color color)
        {
            return new Color(color.R, color.G, color.B, color.A);
        }

        public static System.Windows.Media.Color ToWindowsMediaColor(this Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}
