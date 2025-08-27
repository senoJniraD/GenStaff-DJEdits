using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;

namespace GSBPGEMG
{
    public class SpriteFont
    {
        public static bool useFontTTF = true;
        public static bool useSnapToPixel = true;

        public Microsoft.Xna.Framework.Graphics.SpriteFont spriteFontXNB { get; }

        public DynamicSpriteFont spriteFontTTF { get; }
        public FontSystem spriteFontTTFSystem { get; }
        public float spriteFontTTFSpacing { get; }

        public SpriteFont(ContentManager content, string fileName)
        {
            spriteFontXNB = content.Load<Microsoft.Xna.Framework.Graphics.SpriteFont>(fileName);

            string fontFileName = null;
            int fontSize = 0;
            string fontStyle = null;
            float fontSpacing = 0;

            if (fileName == "GeoSlabBold") // TODO (noted by MT) - fix spelling mistakes in filenames / mgcb
                fileName = "GeoSlapBold";

            string[] spriteFontTextLines = File.ReadAllLines(Path.Combine("Content", fileName + ".spritefont"));
            for (int i = 0; i < spriteFontTextLines.Length; i++)
            {
                if (spriteFontTextLines[i].Contains("<FontName>"))
                    fontFileName = spriteFontTextLines[i].Split("<FontName>")[1].Split("</FontName>")[0];
                if (spriteFontTextLines[i].Contains("<Size>"))
                    fontSize = Convert.ToInt32(spriteFontTextLines[i].Split("<Size>")[1].Split("</Size>")[0]);
                if (spriteFontTextLines[i].Contains("<Style>"))
                    fontStyle = spriteFontTextLines[i].Split("<Style>")[1].Split("</Style>")[0];
                if (spriteFontTextLines[i].Contains("<Spacing>"))
                    fontSpacing = Convert.ToSingle(spriteFontTextLines[i].Split("<Spacing>")[1].Split("</Spacing>")[0]);
            }

            fontFileName = Path.Combine("Content", fontFileName).ToString();
            fontFileName += (File.Exists(fontFileName + ".ttf") ? ".ttf" : ".otf");
            spriteFontTTFSystem = new FontSystem();
            spriteFontTTFSystem.AddFont(File.ReadAllBytes(fontFileName));
            spriteFontTTF = spriteFontTTFSystem.GetFont((int)Math.Round(fontSize * (4f / 3f)));
            spriteFontTTFSpacing = fontSpacing;
        }

        public Vector2 MeasureString(string text, Vector2? scale = null, float characterSpacing = 0f, float lineSpacing = 0f,
            FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0)
        {
            if (useFontTTF == true)
                return new Vector2(spriteFontTTF.MeasureString(text).X, /*spriteFontTTF.LineHeight*/ /*spriteFontXNB*/spriteFontTTF.MeasureString(text).Y); // TODO (noted by MT)
            else
                return spriteFontXNB.MeasureString(text);
        }
    }

    public static class SpriteBatchExtensionMethod
    {
        public static double DrawString(this SpriteBatch spriteBatch, SpriteFont spriteFont, string text, Vector2 position,
            Color color, float rotation = 0f, Vector2 origin = default(Vector2), float? scaleFloat = null, Vector2? scaleVector2 = null, float layerDepth = 0f,
            float characterSpacing = 0f, float lineSpacing = 0f, TextStyle textStyle = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0,
            Point? thickness = null)
        {
            if (SpriteFont.useSnapToPixel == true)
                position = new Vector2((float)Math.Round(position.X), (float)Math.Round(position.Y));

            Vector2 scale = Vector2.One;
            if (scaleFloat is float?)
                scaleVector2 = new Vector2((float)scaleFloat);
            if (scaleVector2 is Vector2?)
                scale = (Vector2)scaleVector2;

            Vector2 thicknessOffset = (thickness != null) ? ((Point)thickness).ToVector2() : Vector2.One;

            for (int x = 0; x < thicknessOffset.X; x++)
            {
                for (int y = 0; y < thicknessOffset.Y; y++)
                {
                    if ((SpriteFont.useFontTTF == true) && (spriteFont.spriteFontTTF != null))
                        spriteBatch.DrawString(spriteFont.spriteFontTTF, text, position + new Vector2(x, y), color, rotation, origin, scale, layerDepth,
                            spriteFont.spriteFontTTFSpacing, lineSpacing, textStyle, effect, effectAmount);
                    else
                        spriteBatch.DrawString(spriteFont.spriteFontXNB, text, position + new Vector2(x, y), color, rotation, origin, scale,
                            SpriteEffects.None, layerDepth);
                }
            }
            return 0d;
        }

        public static double DrawString2(this SpriteBatch spriteBatch, UI.UIElements_Font spriteFont, string text, Vector2 position,
            Color color, float rotation = 0f, Vector2 origin = default, float? scaleFloat = null, Vector2? scaleVector2 = null, float layerDepth = 0f,
            float characterSpacing = 0f, float lineSpacing = 0f, TextStyle textStyle = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None,
            int effectAmount = 0, Point? thickness = null)
        {
            position = new Vector2((float)Math.Round(position.X), (float)Math.Round(position.Y));

            Vector2 scale = Vector2.One;
            if (scaleFloat is float?)
                scaleVector2 = new Vector2((float)scaleFloat);
            if (scaleVector2 is Vector2?)
                scale = (Vector2)scaleVector2;

            Vector2 thicknessOffset = (thickness != null) ? ((Point)thickness).ToVector2() : Vector2.One;

            for (int x = 0; x < thicknessOffset.X; x++)
                for (int y = 0; y < thicknessOffset.Y; y++)
                    spriteBatch.DrawString(spriteFont.SpriteFont, text, position + new Vector2(x, y), color, rotation, origin, scale, layerDepth,
                        characterSpacing, lineSpacing, textStyle, effect, effectAmount);

            return 0d;
        }
    }
}
