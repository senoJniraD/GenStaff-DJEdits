using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using ModelLib;
using TacticalAILib;

namespace GSBPGEMG
{
    public partial class Game1 // Content Files
    {
        #region Texture2D

        public static Texture2D Game1UnitShadow;
        public static Texture2D Game2UnitShadow;
        public static Texture2D Game3UnitShadow;
        public static Texture2D Game4UnitShadow;
        public static Texture2D Blue1InfGame;
        public static Texture2D Blue2InfGame;
        public static Texture2D Blue3InfGame;
        public static Texture2D Blue4InfGame;
        public static Texture2D Blue1LightInfGame;
        public static Texture2D Blue2LightInfGame;
        public static Texture2D Blue3LightInfGame;
        public static Texture2D Blue4LightInfGame;
        public static Texture2D Blue1CavGame;
        public static Texture2D Blue2CavGame;
        public static Texture2D Blue3CavGame;
        public static Texture2D Blue4CavGame;
        public static Texture2D Blue1LightCavGame;
        public static Texture2D Blue2LightCavGame;
        public static Texture2D Blue3LightCavGame;
        public static Texture2D Blue4LightCavGame;
        public static Texture2D Blue1ArtGame;
        public static Texture2D Blue2ArtGame;
        public static Texture2D Blue3ArtGame;
        public static Texture2D Blue4ArtGame;
        public static Texture2D Blue1HorseArtGame;
        public static Texture2D Blue2HorseArtGame;
        public static Texture2D Blue3HorseArtGame;
        public static Texture2D Blue4HorseArtGame;
        public static Texture2D Red1InfGame;
        public static Texture2D Red2InfGame;
        public static Texture2D Red3InfGame;
        public static Texture2D Red4InfGame;
        public static Texture2D Red1LightInfGame;
        public static Texture2D Red2LightInfGame;
        public static Texture2D Red3LightInfGame;
        public static Texture2D Red4LightInfGame;
        public static Texture2D Red1CavGame;
        public static Texture2D Red2CavGame;
        public static Texture2D Red3CavGame;
        public static Texture2D Red4CavGame;
        public static Texture2D Red1LightCavGame;
        public static Texture2D Red2LightCavGame;
        public static Texture2D Red3LightCavGame;
        public static Texture2D Red4LightCavGame;
        public static Texture2D Red1ArtGame;
        public static Texture2D Red2ArtGame;
        public static Texture2D Red3ArtGame;
        public static Texture2D Red4ArtGame;
        public static Texture2D Red1HorseArtGame;
        public static Texture2D Red2HorseArtGame;
        public static Texture2D Red3HorseArtGame;
        public static Texture2D Red4HorseArtGame;
        public static Texture2D MapFrame;
        public static Texture2D OldBackground;
        public static Texture2D ModernBackground;
        public static Texture2D LancersBottom2;
        public static Texture2D ModernMapFrame;
        public static Texture2D VictorianOrdersBox;
        public static Texture2D tRedLine;
        public static Texture2D tBlueLine;
        public static Texture2D UnitInfoBoxDropShadow;
        public static Texture2D Thermometer;
        public static Texture2D RedMustKIAGraphic;
        public static Texture2D BlueMustKIAGraphic;
        public static Texture2D VictorianBlueMelee;
        public static Texture2D VictorianRedMelee;
        public static Texture2D VictorianRightPointingFinger;
        public static Texture2D VictorianLeftPointingFinger;
        public static Texture2D SplashBackground;
        public static Texture2D VictorianEndScreenBackground;
        public static Texture2D VictorianVictoryGraphic;
        public static Texture2D VictorianCannonLeft;
        public static Texture2D VictorianCannonRight;
        public static Texture2D VictorianRule;
        public static Texture2D ModernSplashLogo;
        public static Texture2D SplashLogo;
        public static Texture2D VictorianSetClock;
        public static Texture2D ModernRightArrow;
        public static Texture2D ModernLeftArrow;
        public static Texture2D VictorianLine;
        public static Texture2D ModernLine;
        public static Texture2D ElevationTerrainBox;
        public static Texture2D RedSimInf;
        public static Texture2D RedSimInfColumn;
        public static Texture2D RedSimInfSquare;
        public static Texture2D RedSimLightInf;
        public static Texture2D RedSimLightInfColumn;
        public static Texture2D RedSimLightInfSquare;
        public static Texture2D RedSimHorseArt;
        public static Texture2D RedSimHorseArtColumn;
        public static Texture2D RedSimHQColumn;
        public static Texture2D RedSimCavColumn;
        public static Texture2D RedSimLightCavColumn;
        public static Texture2D RedSimArt;
        public static Texture2D RedSimArtColumn;
        public static Texture2D RedSimCav;
        public static Texture2D RedSimLightCav;
        public static Texture2D RedSimHQ;
        public static Texture2D BlueSimHQColumn;
        public static Texture2D RedSimSupplies;
        public static Texture2D BlueSimArt;
        public static Texture2D BlueSimHorseArt;
        public static Texture2D BlueSimInf;
        public static Texture2D BlueSimLightInf;
        public static Texture2D BlueSimCav;
        public static Texture2D BlueSimLightCav;
        public static Texture2D BlueSimHQ;
        public static Texture2D BlueSimSupplies;
        public static Texture2D BlueSimInfColumn;
        public static Texture2D BlueSimInfSquare;
        public static Texture2D BlueSimLightInfColumn;
        public static Texture2D BlueSimLightInfSquare;
        public static Texture2D BlueSimCavColumn;
        public static Texture2D BlueSimLightCavColumn;
        public static Texture2D BlueSimArtColumn;
        public static Texture2D BlueSimHorseArtColumn;
        public static Texture2D SimColumnBigShadow;
        public static Texture2D SimColumnSmallShadow;
        public static Texture2D SimUnitShadow;
        public static Texture2D SimSmallUnitShadow;
        public static Texture2D SimSquareShadow;
        public static Texture2D UnitInfoBox;
        public static Texture2D UnitInfoBoxLarge;
        public static Texture2D ModernUnitInfoBox;
        public static Texture2D VictorianViewMenuBox;
        public static Texture2D VictorianViewMenuBoxDropShadow;
        public static Texture2D VictorianFileMenuBox;
        public static Texture2D VictorianFileMenuBoxDropShadow;
        public static Texture2D MenuHollowBox;
        public static Texture2D MenuCheckBox;
        public static Texture2D BigCircle;
        public static Texture2D FiringArrow;
        public static Texture2D ArtilleryFiringBlue;
        public static Texture2D ArtilleryFiringRed;
        public static Texture2D VictorianCorpsOrderRed;
        public static Texture2D EnemyObservedBoxBlue;
        public static Texture2D EnemyObservedBoxRed;
        public static Texture2D EnemyObservedBoxDropShadow;
        public static Texture2D ModernEnemyObservedBox;
        public static Texture2D BlueVictorianHQInfoBox;
        public static Texture2D RedVictorianHQInfoBox;
        public static Texture2D ModernHQInfoBox;
        public static Texture2D VictorianBlueInfantry;
        public static Texture2D VictorianRedInfantry;
        public static Texture2D VictorianCourierBlue;
        public static Texture2D VictorianCourierRed;
        public static Texture2D VictorianRoutingRed;
        public static Texture2D VictorianRoutingBlue;
        public static Texture2D VictorianWoundedBlue;
        public static Texture2D VictorianWoundedRed;
        public static Texture2D VictorianRedObserver;
        public static Texture2D VictorianBlueObserver;
        public static Texture2D VictorianPlaceNameInfoBoxRed;
        public static Texture2D VictorianPlaceNameInfoBoxBlue;
        public static Texture2D VictorianPlaceNameInfoBoxGreen;
        public static Texture2D VictorianUpDownScroll;
        public static Texture2D VictorianBatteredRule;
        public static Texture2D VictorianEndTurnButton1;
        public static Texture2D VictorianEndTurnButton2;
        public static Texture2D VictorianRedArtilleryKIA;
        public static Texture2D VictorianBlueArtilleryKIA;

        public static Texture2D ZapfArrow;
        public static Texture2D VictorianHandWithPen;
        public static Texture2D GameBlueInf4;
        public static Texture2D Game4Shadow;
        public static Texture2D GameBlueInfLine3;
        public static Texture2D GameLine3Shadow;
        public static Texture2D SmallBlueCircle;
        public static Texture2D BlueCircle16;
        public static Texture2D RedCircle16;
        public static Texture2D SmallRedCircle;

        public static Texture2D[][] FlagAnimations;

        public static Texture2D BlueRoutingColumn;
        public static Texture2D BlueRoutingLine;
        public static Texture2D RedRoutingColumn;
        public static Texture2D RedRoutingLine;

        public static Texture2D VictorianRightBannerFinger;
        public static Texture2D VictorianLeftBannerFinger;
        public static Texture2D VictorianAboutBox;

        public static Texture2D SimColumnBig; // TODO (noted by MT)
        public static Texture2D SimColumnSmall;
        public static Texture2D SimUnit;
        public static Texture2D SimSmall;

        public static Texture2D whitePixel;
        public static Texture2D blackPixel;
        public static Texture2D transparentPixel;

        #endregion

        #region SpriteFont

        public static SpriteFont Smythe16;
        public static SpriteFont Smythe22;
        public static SpriteFont TopBannerSmythe;
        //SpriteFont Absalom24; // TODO (noted by MT) - remove unused fonts or fix filenames
        public static SpriteFont Amaltea24;
        public static SpriteFont Ephinol12;
        public static SpriteFont Klabasto18;
        public static SpriteFont Phectic36;
        public static SpriteFont Rudyard36;
        public static SpriteFont GeoSlabBold;
        public static SpriteFont Ephinol9;
        public static SpriteFont SplashBannerFont;
        public static SpriteFont ModernSplashFont;
        public static SpriteFont ObservedEnemyFont;
        public static SpriteFont ModernObservedEnemyFont;
        public static SpriteFont UnitInfoNameFont;
        public static SpriteFont BigUnitInfoNameFont;
        public static SpriteFont ModernUnitInfoNameFont;
        public static SpriteFont SmallSmythe;
        public static SpriteFont ModernMenuFont;
        public static SpriteFont ModernScenarioName;
        public static SpriteFont CondensedSansFont;
        public static SpriteFont ModernScenarioDateFont;
        public static SpriteFont ModernArmySlugFont;
        public static SpriteFont ModernBannerFont;
        public static SpriteFont ArialNarrow;
        public static SpriteFont RudyardSmall;
        public static SpriteFont VictorianElevTerFont;
        public static SpriteFont VictorianReportsFont;
        public static SpriteFont VictorianPlaceNameValueFont;
        public static SpriteFont VictorianCopyrightNoticeFont;
        public static SpriteFont VictorianScenarioNameFont;
        public static SpriteFont BlueArmyFont;
        public static SpriteFont CorpsOrdersFont;
        public static SpriteFont VictorianEndScreenSplashFont;
        public static SpriteFont Baskerville;
        public static SpriteFont CWBookRegular;
        public static SpriteFont UnitIDFont;

        public static SpriteFont Amaltea18;
        public static SpriteFont TPTCCWBookBlackRegular;
        public static SpriteFont TPTCCWBrassFramesRegular;
        public static SpriteFont AllHands;

        #endregion

        #region SoundEffect

        public static SoundEffect BigButtonClick;
        public static SoundEffectInstance BigButtonClickInstance;

        #endregion

        public void LoadContentFiles()
        {
            #region Texture2D

            MapFrame = Content.Load<Texture2D>("MapFrame");
            OldBackground = Content.Load<Texture2D>("Old Paper 3");
            LancersBottom2 = Content.Load<Texture2D>("Bottom Lancers");
            ModernBackground = Content.Load<Texture2D>("Brushed Aluminum");
            ModernMapFrame = Content.Load<Texture2D>("ModernMapFrame");
            VictorianLeftPointingFinger = Content.Load<Texture2D>("LeftPointingFinger");
            VictorianRightPointingFinger = Content.Load<Texture2D>("RightPointingFinger");
            VictorianHandWithPen = Content.Load<Texture2D>("RightHandHoldingPen");
            ModernRightArrow = Content.Load<Texture2D>("RightArrowModern");
            ModernLeftArrow = Content.Load<Texture2D>("LeftArrowModern");
            VictorianLine = Content.Load<Texture2D>("VictorianLine");
            ModernLine = Content.Load<Texture2D>("ModernLine");
            VictorianSetClock = Content.Load<Texture2D>("VictorianSetTimeClock");
            VictorianUpDownScroll = Content.Load<Texture2D>("VictorianScrollUpDown");
            VictorianBatteredRule = Content.Load<Texture2D>("VictorianBatteredRule");
            VictorianViewMenuBox = Content.Load<Texture2D>("VictorianViewMenuBox");
            VictorianViewMenuBoxDropShadow = Content.Load<Texture2D>("VictorianViewMenuBoxDropShadow");
            VictorianFileMenuBox = Content.Load<Texture2D>("VictorianFileMenuBox");
            VictorianFileMenuBoxDropShadow = Content.Load<Texture2D>("VictorianFileMenuBoxDropShadow");
            VictorianBlueMelee = Content.Load<Texture2D>("BlueMelee");
            VictorianRedMelee = Content.Load<Texture2D>("BlueMelee");
            VictorianEndTurnButton1 = Content.Load<Texture2D>("Victorian End Turn Button 1");
            VictorianEndTurnButton2 = Content.Load<Texture2D>("Victorian End Turn Button 2");

            VictorianRoutingBlue = Content.Load<Texture2D>("VictorianRoutingBlue");
            VictorianRoutingRed = Content.Load<Texture2D>("VictorianRoutingRed");

            BlueSimInf = Content.Load<Texture2D>("SimBlueInfLine");
            BlueSimLightInf = Content.Load<Texture2D>("SimBlueLightInfLine");
            BlueSimCav = Content.Load<Texture2D>("SimBlueCavLine");
            BlueSimLightCav = Content.Load<Texture2D>("BlueSimLightCav");
            BlueSimArt = Content.Load<Texture2D>("SimBlueArtLine");
            BlueSimHorseArt = Content.Load<Texture2D>("SimBlueHorseArtLine");
            BlueSimHQ = Content.Load<Texture2D>("SimBlueHQ");
            BlueSimHQColumn = Content.Load<Texture2D>("BlueSimHQColumn");
            BlueSimSupplies = Content.Load<Texture2D>("BlueSimSupplies");

            BlueSimInfColumn = Content.Load<Texture2D>("BlueSimInfColumn");
            BlueSimLightInfColumn = Content.Load<Texture2D>("BlueSimLightInfColumn");
            BlueSimLightInfSquare = Content.Load<Texture2D>("BlueLightInfSquare");
            BlueSimInfSquare = Content.Load<Texture2D>("BlueInfSquare");

            BlueSimCavColumn = Content.Load<Texture2D>("BlueSimCavColumn");
            BlueSimLightCavColumn = Content.Load<Texture2D>("BlueSimLightCavColumn");
            BlueSimArtColumn = Content.Load<Texture2D>("BlueSimArtColumn");
            BlueSimHorseArtColumn = Content.Load<Texture2D>("BlueSimHorseArtColumn");

            RedSimInf = Content.Load<Texture2D>("RedSimInf");
            RedSimLightInf = Content.Load<Texture2D>("RedSimLightInf");
            RedSimLightInfSquare = Content.Load<Texture2D>("RedLightInfSquare");
            RedSimInfSquare = Content.Load<Texture2D>("RedInfSquare");
            RedSimCav = Content.Load<Texture2D>("RedSimCav");
            RedSimLightCav = Content.Load<Texture2D>("RedSimLightCav");
            RedSimArt = Content.Load<Texture2D>("RedSimArt");
            RedSimHorseArt = Content.Load<Texture2D>("RedSimHorseArt");
            RedSimHQ = Content.Load<Texture2D>("SimRedHQ");
            RedSimHQColumn = Content.Load<Texture2D>("RedSimHQColumn");
            RedSimSupplies = Content.Load<Texture2D>("RedSimSupplies");

            RedSimInfColumn = Content.Load<Texture2D>("RedSimInfColumn");
            RedSimLightInfColumn = Content.Load<Texture2D>("RedSimLightInfColumn");
            RedSimCavColumn = Content.Load<Texture2D>("RedSimCavColumn");
            RedSimLightCavColumn = Content.Load<Texture2D>("RedSimLightCavColumn");
            RedSimArtColumn = Content.Load<Texture2D>("RedSimArtColumn");
            RedSimHorseArtColumn = Content.Load<Texture2D>("RedSimHorseArtColumn");

            Blue1InfGame = Content.Load<Texture2D>("BlueInf1Game");
            Blue2InfGame = Content.Load<Texture2D>("BlueInf2Game");
            Blue3InfGame = Content.Load<Texture2D>("BlueInf3Game");
            Blue4InfGame = Content.Load<Texture2D>("BlueInf4Game");
            Blue1LightInfGame = Content.Load<Texture2D>("BlueLightInf1Game");
            Blue2LightInfGame = Content.Load<Texture2D>("BlueLightInf2Game");
            Blue3LightInfGame = Content.Load<Texture2D>("BlueLightInf3Game");
            Blue4LightInfGame = Content.Load<Texture2D>("BlueLightInf4Game");
            Blue1CavGame = Content.Load<Texture2D>("BlueCav1Game");
            Blue2CavGame = Content.Load<Texture2D>("BlueCav2Game");
            Blue3CavGame = Content.Load<Texture2D>("BlueCav3Game");
            Blue4CavGame = Content.Load<Texture2D>("BlueCav4Game");
            Blue1LightCavGame = Content.Load<Texture2D>("BlueLightCav1Game");
            Blue2LightCavGame = Content.Load<Texture2D>("BlueLightCav2Game");
            Blue3LightCavGame = Content.Load<Texture2D>("BlueLightCav3Game");
            Blue4LightCavGame = Content.Load<Texture2D>("BlueLightCav4Game");
            Blue1ArtGame = Content.Load<Texture2D>("BlueArt1Game");
            Blue2ArtGame = Content.Load<Texture2D>("BlueArt2Game");
            Blue3ArtGame = Content.Load<Texture2D>("BlueArt3Game");
            Blue4ArtGame = Content.Load<Texture2D>("BlueArt4Game");
            Blue1HorseArtGame = Content.Load<Texture2D>("BlueHorseArt1Game");
            Blue2HorseArtGame = Content.Load<Texture2D>("BlueHorseArt2Game");
            Blue3HorseArtGame = Content.Load<Texture2D>("BlueHorseArt3Game");
            Blue4HorseArtGame = Content.Load<Texture2D>("BlueHorseArt4Game");

            Red1InfGame = Content.Load<Texture2D>("RedInf1Game");
            Red2InfGame = Content.Load<Texture2D>("RedInf2Game");
            Red3InfGame = Content.Load<Texture2D>("RedInf3Game");
            Red4InfGame = Content.Load<Texture2D>("RedInf4Game");
            Red1LightInfGame = Content.Load<Texture2D>("RedLightInf1Game");
            Red2LightInfGame = Content.Load<Texture2D>("RedLightInf2Game");
            Red3LightInfGame = Content.Load<Texture2D>("RedLightInf3Game");
            Red4LightInfGame = Content.Load<Texture2D>("RedLightInf4Game");
            Red1CavGame = Content.Load<Texture2D>("RedCav1Game");
            Red2CavGame = Content.Load<Texture2D>("RedCav2Game");
            Red3CavGame = Content.Load<Texture2D>("RedCav3Game");
            Red4CavGame = Content.Load<Texture2D>("RedCav4Game");
            Red1LightCavGame = Content.Load<Texture2D>("RedLightCav1Game");
            Red2LightCavGame = Content.Load<Texture2D>("RedLightCav2Game");
            Red3LightCavGame = Content.Load<Texture2D>("RedLightCav3Game");
            Red4LightCavGame = Content.Load<Texture2D>("RedLightCav4Game");
            Red1ArtGame = Content.Load<Texture2D>("RedArt1Game");
            Red2ArtGame = Content.Load<Texture2D>("RedArt2Game");
            Red3ArtGame = Content.Load<Texture2D>("RedArt3Game");
            Red4ArtGame = Content.Load<Texture2D>("RedArt4Game");
            Red1HorseArtGame = Content.Load<Texture2D>("RedHorseArt1Game");
            Red2HorseArtGame = Content.Load<Texture2D>("RedHorseArt2Game");
            Red3HorseArtGame = Content.Load<Texture2D>("RedHorseArt3Game");
            Red4HorseArtGame = Content.Load<Texture2D>("RedHorseArt4Game");


            RedMustKIAGraphic = Content.Load<Texture2D>("RedMustKIAG");
            BlueMustKIAGraphic = Content.Load<Texture2D>("BlueMustKIAG");
            ElevationTerrainBox = Content.Load<Texture2D>("ElevationTerrainBox");
            SplashBackground = Content.Load<Texture2D>("SplashBackground");
            SplashLogo = Content.Load<Texture2D>("SplashLogoVictorian");
            ModernBackground = Content.Load<Texture2D>("Brushed Aluminum");
            ModernSplashLogo = Content.Load<Texture2D>("Modern Splash Logo");
            UnitInfoBox = Content.Load<Texture2D>("UnitInfoPaper");
            UnitInfoBoxLarge = Content.Load<Texture2D>("UnitInfoPaperLarge");
            ModernUnitInfoBox = Content.Load<Texture2D>("Modern Unit Info Paper");
            UnitInfoBoxDropShadow = Content.Load<Texture2D>("UnitInfoPaperDropShadow");
            
            // These got swapped, reversed Ezra 06/28/25
            EnemyObservedBoxRed = Content.Load<Texture2D>("Observed Enemy Box");
            EnemyObservedBoxBlue = Content.Load<Texture2D>("Observed Enemy Box-red");
            FiringArrow = Content.Load<Texture2D>("FiringArrow");

            ModernEnemyObservedBox = Content.Load<Texture2D>("ModernObservedEnemy");
            EnemyObservedBoxDropShadow = Content.Load<Texture2D>("Observed Enemy Box Shadow");
            BlueVictorianHQInfoBox = Content.Load<Texture2D>("CommanderBlue");
            RedVictorianHQInfoBox = Content.Load<Texture2D>("CommanderRed");
            ModernHQInfoBox = Content.Load<Texture2D>("ModernHQInfoBox");
            SmallBlueCircle = Content.Load<Texture2D>("SmallBlueCircle");
            BlueCircle16 = Content.Load<Texture2D>("BlueCircle16");
            RedCircle16 = Content.Load<Texture2D>("RedCircle16");
            SmallRedCircle = Content.Load<Texture2D>("SmallRedCircle");
            Thermometer = Content.Load<Texture2D>("Thermometer");
            VictorianOrdersBox = Content.Load<Texture2D>("Orders Box");
            ArtilleryFiringBlue = Content.Load<Texture2D>("ArtilleryFire");
            ArtilleryFiringRed = Content.Load<Texture2D>("Victorian Artillery Red");

            BigCircle = Content.Load<Texture2D>("BigCircle");

            VictorianBlueInfantry = Content.Load<Texture2D>("VictorianBlueInfantry");
            VictorianRedInfantry = Content.Load<Texture2D>("VictorianBlueInfantry");
            VictorianCourierRed = Content.Load<Texture2D>("VictorianCourierRed");
            VictorianCourierBlue = Content.Load<Texture2D>("VictorianCourier");
            VictorianBlueObserver = Content.Load<Texture2D>("VictorianBlueEnemyObserver");
            VictorianRedObserver = Content.Load<Texture2D>("VictorianRedEnemyObserver");
            VictorianWoundedBlue = Content.Load<Texture2D>("Wounded");
            VictorianWoundedRed = Content.Load<Texture2D>("Wounded");
            VictorianCorpsOrderRed = Content.Load<Texture2D>("VictorianCorpsOrderRed");
            VictorianPlaceNameInfoBoxRed = Content.Load<Texture2D>("PlaceNameInfoBoxRed");
            VictorianPlaceNameInfoBoxBlue = Content.Load<Texture2D>("PlaceNameInfoBoxBlue");
            VictorianPlaceNameInfoBoxGreen = Content.Load<Texture2D>("PlaceNameInfoBoxGreen");
            ZapfArrow = Content.Load<Texture2D>("Zapf Arrow");
            MenuHollowBox = Content.Load<Texture2D>("MenuHollowBox");
            MenuCheckBox = Content.Load<Texture2D>("MenuCheckBox");

            VictorianEndScreenBackground = Content.Load<Texture2D>("EndScreenSplash");
            VictorianVictoryGraphic = Content.Load<Texture2D>("Victorian Victory");
            VictorianCannonLeft = Content.Load<Texture2D>("Victorian End Cannon Left");
            VictorianCannonRight = Content.Load<Texture2D>("Victorian End Cannon Right");
            VictorianRule = Content.Load<Texture2D>("LongCivilWarRule");

            FlagAnimations = new Texture2D[3][];
            FlagAnimations[(int)Sides.Blue] = new Texture2D[6];
            FlagAnimations[(int)Sides.Red] = new Texture2D[6];
            for (int i = 0; i < 6; i++)
            {
                FlagAnimations[(int)Sides.Blue][i] = Content.Load<Texture2D>(Sides.Blue + "Flag" + (i + 1));
                FlagAnimations[(int)Sides.Red][i] = Content.Load<Texture2D>(Sides.Red + "Flag" + (i + 1));
            }

            BlueRoutingColumn = Content.Load<Texture2D>("BlueRoutingColumn");
            BlueRoutingLine = Content.Load<Texture2D>("BlueRoutingLine");
            RedRoutingColumn = Content.Load<Texture2D>("RedRoutingColumn");
            RedRoutingLine = Content.Load<Texture2D>("RedRoutingLine");

            VictorianRightBannerFinger = Content.Load<Texture2D>("LeftBannerFinger");
            VictorianLeftBannerFinger = Content.Load<Texture2D>("RightBannerFinger");
            VictorianAboutBox = Content.Load<Texture2D>("Victorian About Box 1");

            #endregion

            #region SpriteFont

            Smythe16 = new SpriteFont(Content, "Smythe");
            Smythe22 = new SpriteFont(Content, "Smythe22");
            //Absalom24 = new SpriteFont(Content, "Absalom"); // TODO (noted by MT) - remove unused fonts or fix filenames
            Amaltea24 = new SpriteFont(Content, "Amaltea");
            Ephinol12 = new SpriteFont(Content, "Ephinol");
            //Monastic28 = new SpriteFont(Content, "K22 Monastic");
            Klabasto18 = new SpriteFont(Content, "Klabasto");
            Phectic36 = new SpriteFont(Content, "Phectic");
            Rudyard36 = new SpriteFont(Content, "Rudyard");
            GeoSlabBold = new SpriteFont(Content, "GeoSlabBold");
            Ephinol9 = new SpriteFont(Content, "Ephinol9");
            //Herschel1Per10 = new SpriteFont(Content, "Herschel1Percent10");
            //HerschelButter14 = new SpriteFont(Content, "HerschelButter14");
            //Amaltea16 = new SpriteFont(Content, "Amaltea 16");
            SplashBannerFont = new SpriteFont(Content, "Ephinol 56");
            ModernSplashFont = new SpriteFont(Content, "GeoBold56");
            //SmallAvantGarde = new SpriteFont(Content, "SmallAvantGarde");
            ObservedEnemyFont = new SpriteFont(Content, "AbsalomSmall");
            ModernObservedEnemyFont = new SpriteFont(Content, "ModernObservedEnemyFont");
            UnitInfoNameFont = new SpriteFont(Content, "UnitInfoNameFont");
            BigUnitInfoNameFont = new SpriteFont(Content, "BigUnitInfoNameFont");
            ModernUnitInfoNameFont = new SpriteFont(Content, "ModernUnitInfoNameFont");
            TopBannerSmythe = new SpriteFont(Content, "TopBannerSmythe");
            SmallSmythe = new SpriteFont(Content, "SmallSmythe");
            ModernMenuFont = new SpriteFont(Content, "ModernMenuFont");
            ModernScenarioName = new SpriteFont(Content, "ModernScenarioName");
            CondensedSansFont = new SpriteFont(Content, "CondensedSans");
            //ModernScenarioDateFont = new SpriteFont(Content, "ModernScenarioDateFont");
            ModernArmySlugFont = new SpriteFont(Content, "ModernArmySlugFont");
            ModernBannerFont = new SpriteFont(Content, "ModernBannerFont");
            ArialNarrow = new SpriteFont(Content, "ArialNarrow");
            RudyardSmall = new SpriteFont(Content, "SmallRudyard");
            VictorianElevTerFont = new SpriteFont(Content, "VictorianEleTerBoxFont");
            VictorianReportsFont = new SpriteFont(Content, "VictorianReportsFont");
            VictorianPlaceNameValueFont = new SpriteFont(Content, "VictorianPlaceNameValueFont");
            VictorianCopyrightNoticeFont = new SpriteFont(Content, "VictorianCopyrightFont");
            VictorianScenarioNameFont = new SpriteFont(Content, "VictorianScenarioNameFont");
            BlueArmyFont = new SpriteFont(Content, "BlueArmyFont");
            //TreadgearBook = new SpriteFont(Content, "TreadgearBook");
            //TreadgearBookItalic = new SpriteFont(Content, "TreadgearBookItalic");
            CorpsOrdersFont = new SpriteFont(Content, "CorpsOrdersFont");
            VictorianEndScreenSplashFont = new SpriteFont(Content, "EndScreenBanner");
            Baskerville = new SpriteFont(Content, "Baskerville");
            CWBookRegular = new SpriteFont(Content, "CW Book Regular");
            UnitIDFont = new SpriteFont(Content, "UnitIDType");

            Amaltea18 = new SpriteFont(Content, "Amaltea");
            TPTCCWBookBlackRegular = new SpriteFont(Content, "TPTCCWBookBlackRegular");
            TPTCCWBrassFramesRegular = new SpriteFont(Content, "TPTCCWBrassFramesRegular");
            AllHands = new SpriteFont(Content, "AllHands");

            #endregion

            #region SoundEffect

            BigButtonClick = Content.Load<SoundEffect>("Mechanical Gears");
            BigButtonClickInstance = BigButtonClick.CreateInstance();

            #endregion
        }

        public void GenerateRuntimeContent()
        {
            UI.UIElement_PathRenderer.Load();

            SimUnitShadow = GenerateUnitShadow(BlueSimInf);
            SimSmallUnitShadow = GenerateUnitShadow(BlueSimArt);
            SimColumnBigShadow = GenerateUnitShadow(BlueSimInfColumn);
            SimColumnSmallShadow = GenerateUnitShadow(BlueSimArtColumn);
            SimSquareShadow = GenerateUnitShadow(BlueSimInfSquare);
            Game1UnitShadow = GenerateUnitShadow(Blue1InfGame);
            Game2UnitShadow = GenerateUnitShadow(Blue2InfGame);
            Game3UnitShadow = GenerateUnitShadow(Blue3InfGame);
            Game4UnitShadow = GenerateUnitShadow(Blue4InfGame);

            // create 1x1 texture for line drawing
            whitePixel = new Texture2D(GraphicsDevice, 1, 1);
            whitePixel.SetData([Color.White]);
            blackPixel = new Texture2D(GraphicsDevice, 1, 1);
            blackPixel.SetData([Color.Black]);
            transparentPixel = new Texture2D(GraphicsDevice, 1, 1);
            transparentPixel.SetData([Color.Transparent]);
        }

        Texture2D GenerateUnitShadow(Texture2D image)
        {
            Color[] data = new Color[image.Width * image.Height];
            image.GetData(data);
            for (int i = 0; i < data.Length; i++)
                data[i] = (data[i].A > 0) ? Color.White : Color.Transparent;
            Texture2D shadow = new Texture2D(GraphicsDevice, image.Width, image.Height);
            shadow.SetData(data);
            return shadow;
        }

        public static (Texture2D unitImage, Texture2D shadowImage) GetSimUnitTexture2D(MATEUnitSnapshot unitSnapshot, Formations? formationOverride = null)
        {
            MATEUnitInstance unit = unitSnapshot.Unit;
            Formations formation = (formationOverride == null) ? unitSnapshot.Formation : (Formations)formationOverride;
            if (unit.UnitType == UnitTypes.HeadQuarters)
                formation = Formations.None;

            if (unit.Side == Sides.Blue) // Blue
            {
                switch (MATEUnitInstance.FormationGroupForFormation(formation))
                {
                    case MATEUnitInstance.FormationGroups.ColumnGroup:
                        switch (unit.UnitType)
                        {
                            case UnitTypes.Infantry: return (BlueSimInfColumn, SimColumnBigShadow);
                            case UnitTypes.LightInfantry: return (BlueSimLightInfColumn, SimColumnBigShadow);
                            case UnitTypes.Cavalry: return (BlueSimCavColumn, SimColumnBigShadow);
                            case UnitTypes.LightCavalry: return (BlueSimLightCavColumn, SimColumnBigShadow);
                            case UnitTypes.Artillery: return (BlueSimArtColumn, SimColumnSmallShadow);
                            case UnitTypes.HorseArtillery: return (BlueSimHorseArtColumn, SimColumnSmallShadow);
                        }
                        break;

                    case MATEUnitInstance.FormationGroups.LineGroup:
                        switch (unit.UnitType)
                        {
                            case UnitTypes.Infantry: return (BlueSimInf, SimUnitShadow);
                            case UnitTypes.LightInfantry: return (BlueSimLightInf, SimUnitShadow);
                            case UnitTypes.Cavalry: return (BlueSimCav, SimUnitShadow);
                            case UnitTypes.LightCavalry: return (BlueSimLightCav, SimUnitShadow);
                            case UnitTypes.Artillery: return (BlueSimArt, SimSmallUnitShadow);
                            case UnitTypes.HorseArtillery: return (BlueSimHorseArt, SimSmallUnitShadow);
                            case UnitTypes.HeadQuarters: return (BlueSimHQ, SimSmallUnitShadow);
                            case UnitTypes.Supplies: return (BlueSimSupplies, SimUnitShadow);
                        }
                        break;

                    case MATEUnitInstance.FormationGroups.SquareGroup:
                        switch (unit.UnitType)
                        {
                            case UnitTypes.Infantry: return (BlueSimInfSquare, SimSquareShadow);
                            case UnitTypes.LightInfantry: return (BlueSimLightInfSquare, SimSquareShadow);
                        }
                        break;
                }
            }
            else // Red
            {
                switch (MATEUnitInstance.FormationGroupForFormation(formation))
                {
                    case MATEUnitInstance.FormationGroups.ColumnGroup:
                        switch (unit.UnitType)
                        {
                            case UnitTypes.Infantry: return (RedSimInfColumn, SimColumnBigShadow);
                            case UnitTypes.LightInfantry: return (RedSimLightInfColumn, SimColumnBigShadow);
                            case UnitTypes.Cavalry: return (RedSimCavColumn, SimColumnBigShadow);
                            case UnitTypes.LightCavalry: return (RedSimLightCavColumn, SimColumnBigShadow);
                            case UnitTypes.Artillery: return (RedSimArtColumn, SimColumnSmallShadow);
                            case UnitTypes.HorseArtillery: return (RedSimHorseArtColumn, SimColumnSmallShadow);
                        }
                        break;

                    case MATEUnitInstance.FormationGroups.LineGroup:
                        switch (unit.UnitType)
                        {
                            case UnitTypes.Infantry: return (RedSimInf, SimUnitShadow);
                            case UnitTypes.LightInfantry: return (RedSimLightInf, SimUnitShadow);
                            case UnitTypes.Cavalry: return (RedSimCav, SimUnitShadow);
                            case UnitTypes.LightCavalry: return (RedSimLightCav, SimUnitShadow);
                            case UnitTypes.Artillery: return (RedSimArt, SimSmallUnitShadow);
                            case UnitTypes.HorseArtillery: return (RedSimHorseArt, SimSmallUnitShadow);
                            case UnitTypes.HeadQuarters: return (RedSimHQ, SimSmallUnitShadow);
                            case UnitTypes.Supplies: return (RedSimSupplies, SimUnitShadow);
                        }
                        break;

                    case MATEUnitInstance.FormationGroups.SquareGroup:
                        switch (unit.UnitType)
                        {
                            case UnitTypes.Infantry: return (RedSimInfSquare, SimSquareShadow);
                            case UnitTypes.LightInfantry: return (RedSimLightInfSquare, SimSquareShadow);
                        }
                        break;
                }
            }

            return (null, null);
        }

        public static Texture2D GetSimUnitRoutingTexture2D(MATEUnitSnapshot unitSnapshot, Formations? formationOverride = null)
        {
            MATEUnitInstance unit = unitSnapshot.Unit;
            Formations formation = (formationOverride == null) ? unitSnapshot.Formation : (Formations)formationOverride;
            if (unit.UnitType == UnitTypes.HeadQuarters || unit.UnitType == UnitTypes.Supplies)
                formation = Formations.None;

            if (unit.Side == Sides.Blue)
                return (formation == Formations.Column) ? BlueRoutingColumn : BlueRoutingLine;
            else if (unit.Side == Sides.Red)
                return (formation == Formations.Column) ? RedRoutingColumn : RedRoutingLine;
            return null;
        }

        public static (Texture2D unitImage, Texture2D shadowImage) GetGameUnitTexture2D(MATEUnitInstance unit)
        {
            if (unit.Side == Sides.Blue)
            {
                switch (unit.UnitType)
                {
                    case UnitTypes.Infantry:
                        switch (unit.GameStrength)
                        {
                            case 1: return (Blue1InfGame, Game1UnitShadow);
                            case 2: return (Blue2InfGame, Game2UnitShadow);
                            case 3: return (Blue3InfGame, Game3UnitShadow);
                            case 4: return (Blue4InfGame, Game4UnitShadow);
                        }
                        break;

                    case UnitTypes.LightInfantry:
                        switch (unit.GameStrength)
                        {
                            case 1: return (Blue1LightInfGame, Game1UnitShadow);
                            case 2: return (Blue2LightInfGame, Game2UnitShadow);
                            case 3: return (Blue3LightInfGame, Game3UnitShadow);
                            case 4: return (Blue4LightInfGame, Game4UnitShadow);
                        }
                        break;

                    case UnitTypes.Cavalry:
                        switch (unit.GameStrength)
                        {
                            case 1: return (Blue1CavGame, Game1UnitShadow);
                            case 2: return (Blue2CavGame, Game2UnitShadow);
                            case 3: return (Blue3CavGame, Game3UnitShadow);
                            case 4: return (Blue4CavGame, Game4UnitShadow);
                        }
                        break;

                    case UnitTypes.LightCavalry:
                        switch (unit.GameStrength)
                        {
                            case 1: return (Blue1LightCavGame, Game1UnitShadow);
                            case 2: return (Blue2LightCavGame, Game2UnitShadow);
                            case 3: return (Blue3LightCavGame, Game3UnitShadow);
                            case 4: return (Blue4LightCavGame, Game4UnitShadow);
                        }
                        break;

                    case UnitTypes.Artillery:
                        switch (unit.GameStrength)
                        {
                            case 1: return (Blue1ArtGame, Game1UnitShadow);
                            case 2: return (Blue2ArtGame, Game2UnitShadow);
                            case 3: return (Blue3ArtGame, Game3UnitShadow);
                            case 4: return (Blue4ArtGame, Game4UnitShadow);
                        }
                        break;

                    case UnitTypes.HorseArtillery:
                        switch (unit.GameStrength)
                        {
                            case 1: return (Blue1HorseArtGame, Game1UnitShadow);
                            case 2: return (Blue2HorseArtGame, Game2UnitShadow);
                            case 3: return (Blue3HorseArtGame, Game3UnitShadow);
                            case 4: return (Blue4HorseArtGame, Game4UnitShadow);
                        }
                        break;
                }
            }
            else if (unit.Side == Sides.Red)
            {
                switch (unit.UnitType)
                {
                    case UnitTypes.Infantry:
                        switch (unit.GameStrength)
                        {
                            case 1: return (Red1InfGame, Game1UnitShadow);
                            case 2: return (Red2InfGame, Game2UnitShadow);
                            case 3: return (Red3InfGame, Game3UnitShadow);
                            case 4: return (Red4InfGame, Game4UnitShadow);
                        }
                        break;

                    case UnitTypes.LightInfantry:
                        switch (unit.GameStrength)
                        {
                            case 1: return (Red1LightInfGame, Game1UnitShadow);
                            case 2: return (Red2LightInfGame, Game2UnitShadow);
                            case 3: return (Red3LightInfGame, Game3UnitShadow);
                            case 4: return (Red4LightInfGame, Game4UnitShadow);
                        }
                        break;

                    case UnitTypes.Cavalry:
                        switch (unit.GameStrength)
                        {
                            case 1: return (Red1CavGame, Game1UnitShadow);
                            case 2: return (Red2CavGame, Game2UnitShadow);
                            case 3: return (Red3CavGame, Game3UnitShadow);
                            case 4: return (Red4CavGame, Game4UnitShadow);
                        }
                        break;

                    case UnitTypes.LightCavalry:
                        switch (unit.GameStrength)
                        {
                            case 1: return (Red1LightCavGame, Game1UnitShadow);
                            case 2: return (Red2LightCavGame, Game2UnitShadow);
                            case 3: return (Red3LightCavGame, Game3UnitShadow);
                            case 4: return (Red4LightCavGame, Game4UnitShadow);
                        }
                        break;

                    case UnitTypes.Artillery:
                        switch (unit.GameStrength)
                        {
                            case 1: return (Red1ArtGame, Game1UnitShadow);
                            case 2: return (Red2ArtGame, Game2UnitShadow);
                            case 3: return (Red3ArtGame, Game3UnitShadow);
                            case 4: return (Red4ArtGame, Game4UnitShadow);
                        }
                        break;

                    case UnitTypes.HorseArtillery:
                        switch (unit.GameStrength)
                        {
                            case 1: return (Red1HorseArtGame, Game1UnitShadow);
                            case 2: return (Red2HorseArtGame, Game2UnitShadow);
                            case 3: return (Red3HorseArtGame, Game3UnitShadow);
                            case 4: return (Red4HorseArtGame, Game4UnitShadow);
                        }
                        break;
                }
            }

            return (null, null);
        }
    }
}
