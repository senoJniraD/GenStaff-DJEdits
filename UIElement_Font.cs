using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using FontStashSharp;
using ModelLib;

namespace GSBPGEMG.UI
{
    public class UIElements_Font
    {
        public FontSystem FontSystem { get; private set; }
        public DynamicSpriteFont SpriteFont { get; private set; }

        public string FileName { get; private set; }
        public string StyleName { get; private set; }
        public int SizeY { get; private set; }
        public Vector2 Offset { get; private set; }
        public int OffsetNextLineY { get; private set; }

        public int LineHeight { get; private set; }
        public bool StyleBold { get; private set; }

        public UIElements_Font(string directoryName, UIStyle.FontTypes fontType, string[] fontsConfig)
        {
            try
            {
                FileName = default;
                StyleName = "Regular";
                SizeY = 0;
                Offset = Vector2.Zero;
                OffsetNextLineY = 0;

                foreach (string line in fontsConfig)
                {
                    if (line.StartsWith(fontType.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        string[] options = line.Split(',');
                        for (int i = 0; i < options.Length; i++)
                            options[i] = options[i].Trim();

                        FileName = options[1];
                        StyleName = options[2];
                        SizeY = Convert.ToInt32(options[3]);
                        Offset = new(Convert.ToInt32(options[4]), Convert.ToInt32(options[5]));
                        OffsetNextLineY = Convert.ToInt32(options[6]);
                        break;
                    }
                }

                string filePath = Path.Combine(directoryName, FileName).ToString();
                FontSystem = new FontSystem();
                FontSystem.AddFont(File.ReadAllBytes(filePath));
                SpriteFont = FontSystem.GetFont((int)Math.Round(SizeY * (4f / 3f)));

                LineHeight = (int)SpriteFont.FontSize + OffsetNextLineY;
                StyleBold = StyleName.Contains("Bold", StringComparison.InvariantCultureIgnoreCase);
            }
            catch(Exception e)
            {
                // TODO (noted by MT) load default font
                FontSystem = new FontSystem();
                StyleBold = false;

                //Logging.Write(e, true, "Font failed to load", $"Font Type: {fontType}\nDirectory: {directoryName}");
            }
        }

        public Vector2 MeasureString(string text, Vector2? scale = null, float characterSpacing = 0f, float lineSpacing = 0f,
            FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0)
        {
            return new Vector2(SpriteFont.MeasureString(text).X, LineHeight);
        }
    }
}
