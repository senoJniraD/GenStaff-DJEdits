using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TacticalAILib;
using static GSBPGEMG.UI.UIStyles;

namespace GSBPGEMG.UI
{
    public class UIStyle
    {
        public string Name { get; protected set; }
        public string FolderPath => Path.Combine("Content", Name);

        public Texture2D TitleLogo { get; protected set; }

        public Texture2D WindowBackground1 { get; protected set; }
        public Texture2D WindowBorder1 { get; protected set; }

        public Texture2D WindowBackground2 { get; protected set; }
        public Texture2D WindowBorder2 { get; protected set; }

        public enum ButtonTypes
        {
            MainMenuButton1,
            MainMenuButton2,
            TabButton1
        }
        public (Texture2D Background, Texture2D Border, Texture2D Mask)[] Buttons { get; private set; } = [];

        public Texture2D ScrollBarBackground { get; protected set; }
        public Texture2D ScrollBarSelector { get; protected set; }
        public Texture2D ListSelector { get; protected set; }

        public Texture2D Divider1 { get; protected set; }
        public Texture2D Divider2 { get; protected set; }

        public Dictionary<string, Texture2D> ReportIcons { get; private set; } = [];

        public Texture2D MapCourier { get; protected set; }
        public Texture2D MapPlace { get; protected set; }

        public enum FontTypes
        {
            MainMenuButton1,
            MainMenuButton2,
            MainMenuHeading,
            MainMenuScenarioName,
            MainMenuScenarioDescription,
            Tooltip,

            HeaderMenu,
            TabTitle1,
            TabTitle2,

            TabTextSmall,
            TabTextMedium,
            TabTextLarge,

            TabReportTurnNumber,
            TabScenarioArmyName,

            Divider1,
            Divider2,

            ReportText
        }
        public UIElements_Font[] Fonts { get; private set; } = [];

        public UIStyle(ContentManager contentManager, GraphicsDevice graphicsDevice)
        {
            Name = ((Styles)ContentStylesList.Count).ToString();

            TitleLogo = LoadTexture(graphicsDevice, nameof(TitleLogo));
            WindowBackground1 = LoadTexture(graphicsDevice, nameof(WindowBackground1));
            WindowBorder1 = LoadTexture(graphicsDevice, nameof(WindowBorder1));
            WindowBackground2 = LoadTexture(graphicsDevice, nameof(WindowBackground2));
            WindowBorder2 = LoadTexture(graphicsDevice, nameof(WindowBorder2));
            ScrollBarBackground = LoadTexture(graphicsDevice, nameof(ScrollBarBackground));
            ScrollBarSelector = LoadTexture(graphicsDevice, nameof(ScrollBarSelector));
            ListSelector = LoadTexture(graphicsDevice, nameof(ListSelector));
            Divider1 = LoadTexture(graphicsDevice, nameof(Divider1));
            Divider2 = LoadTexture(graphicsDevice, nameof(Divider2));

            Buttons = new (Texture2D, Texture2D, Texture2D)[Enum.GetNames(typeof(ButtonTypes)).Length];
            for (int i = 0; i < Buttons.Length; i++)
                Buttons[i] =
                    (LoadTexture(graphicsDevice, (ButtonTypes)i + "Background"),
                     LoadTexture(graphicsDevice, (ButtonTypes)i + "Border"),
                     LoadTexture(graphicsDevice, (ButtonTypes)i + "Mask"));

            LoadReportIcons(graphicsDevice, Event_InitialCourierReport.ReportIconTypes);
            LoadReportIcons(graphicsDevice, Event_UnitVisibilityChanged.ReportIconTypes);
            LoadReportIcons(graphicsDevice, Event_ReinforcementsArrive.ReportIconTypes);
            LoadReportIcons(graphicsDevice, Event_DirectOrdersSent.ReportIconTypes);
            LoadReportIcons(graphicsDevice, Event_CorpsOrdersSent.ReportIconTypes);
            LoadReportIcons(graphicsDevice, Event_MeleeCombat.ReportIconTypes);
            LoadReportIcons(graphicsDevice, Event_RangedCombat.ReportIconTypes);
            LoadReportIcons(graphicsDevice, Event_UnitCasualties.ReportIconTypes);
            LoadReportIcons(graphicsDevice, Event_UnitRouted.ReportIconTypes);
            LoadReportIcons(graphicsDevice, Event_UnitStoppedRouting.ReportIconTypes);
            LoadReportIcons(graphicsDevice, Event_UnitKIA.ReportIconTypes);
            LoadReportIcons(graphicsDevice, Event_PlaceCaptured.ReportIconTypes);

            MapCourier = LoadTexture(graphicsDevice, "MapCourier");
            MapPlace = LoadTexture(graphicsDevice, "MapPlace");

            Fonts = new UIElements_Font[Enum.GetNames(typeof(FontTypes)).Length];
            string[] fontsConfig = File.ReadAllLines(Path.Combine(FolderPath, "FontsConfig.csv"));
            for (int i = 0; i < Fonts.Length; i++)
            {
                Fonts[i] = new UIElements_Font(FolderPath, (FontTypes)i, fontsConfig);
                //Fonts[i] = new UIElements_Font(contentManager, Path.Combine(
                //    Path.Combine(FolderPath.Split(Path.DirectorySeparatorChar).Skip(1).ToArray()),
                //    "Font" + ((FontTypes)i).ToString()));
            }
        }

        public Texture2D LoadTexture(GraphicsDevice graphicsDevice, string fileName)
        {
            fileName = Path.Combine(FolderPath, fileName + ".png");
            return File.Exists(fileName) ? Texture2D.FromFile(graphicsDevice, fileName) : null;
        }

        public void LoadReportIcons(GraphicsDevice graphicsDevice, string[] fileNames)
        {
            for (int i = 0; i < fileNames.Length; i++)
            {
                for (int j = 0; j < Enum.GetNames(typeof(ModelLib.Sides)).Length; j++)
                {
                    Texture2D icon = LoadTexture(graphicsDevice, Path.Combine("ReportIcons", fileNames[i] + (ModelLib.Sides)j));
                    if (icon != null)
                        ReportIcons.Add($"{Name}_{fileNames[i]}_{(ModelLib.Sides)j}", icon);
                }
            }
        }
    }

    public static class UIStyles
    {
        public enum Styles
        {
            Victorian,
            Modern
        }
        public static Styles Style { get; set; }
        public static UIStyle Current => ContentStylesList[(int)Style];

        public static List<UIStyle> ContentStylesList { get; private set; } = [];

        public static void Load(ContentManager contentManager, GraphicsDevice graphicsDevice)
        {
            for (int i = 0; i < Enum.GetNames(typeof(Styles)).Length; i++)
                ContentStylesList.Add(new UIStyle(contentManager, graphicsDevice));

            Style = Styles.Victorian;
        }
    }
}
