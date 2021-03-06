﻿using System;
using System.Collections.Generic;
using System.Linq;
using CoC_Bot.API;
using CustomAlgorithmSettings;
using System.Reflection;

[assembly: Addon("SmartFourFingersDeploy", "deploy troops in for sides with human behavior", "Cobratst")]
namespace SmartFourFingersDeploy
{
    [AttackAlgorithm("SmartFourFingersDeploy", "Four Fingers Deploy with advanced settings")]
    public class SmartFourFingersDeploy : BaseAttack
    {
        internal static readonly Version Version = Assembly.GetEntryAssembly().GetName().Version;
        internal static readonly string FormattedVersionString = $"{Version.Major}.{Version.Minor}.{Version.Build}.{Version.Revision}";
        public const string AttackName = "[Smart 4 Fingers Deploy]";

        public SmartFourFingersDeploy(Opponent opponent) : base(opponent)
        {
        }

        public override string ToString()
        {
            return "Smart 4 Fingers Deploy";
        }

        /// <summary>
        /// Returns a Custom Setting's Current Value.  The setting Name must be defined in the DefineSettings Function for this algorithm.
        /// </summary>
        /// <param name="settingName">Name of the setting to Get</param>
        /// <returns>Current Value of the setting.</returns>
        internal int GetCurrentSetting(string settingName)
        {
            return SettingsController.Instance.GetSetting(AttackName, settingName, Opponent.IsDead());
        }
        
        /// <summary>
        /// Returns a list of all current Algorithm Setting Values.
        /// </summary>
        /// <returns>Current Value of the all settings for this algorithm.</returns>
        internal List<AlgorithmSetting> AllCurrentSettings
        {
            get
            {
                return SettingsController.Instance.AllAlgorithmSettings[AttackName].AllSettings;
            }
        }

        /// <summary>
        /// Called from the Bot Framework when the Algorithm is first loaded into memory.
        /// </summary>
        public static void OnInit()
        {
            // On load of the Plug-In DLL, Define the Default Settings for the Algorithm.
            SettingsController.Instance.DefineCustomAlgorithmSettings(DefineSettings());
        }

        internal static AlgorithmSettings DefineSettings()
        {
            var settings = new AlgorithmSettings()
            {
                AlgorithmName = AttackName,
                AlgorithmDescriptionURL = "https://www.raccoonbot.com/forum/topic/24589-dark-push-deploy/"
            };

            // Global Settings.
            var debugMode = new AlgorithmSetting("Debug Mode", "When on, Debug Images will be written out for each attack showing what the algorithm is seeing.", 0, SettingType.Global);
            debugMode.PossibleValues.Add(new SettingOption("Off", 0));
            debugMode.PossibleValues.Add(new SettingOption("On", 1));
            settings.DefineSetting(debugMode);

            var setCollMines = new AlgorithmSetting("Set Exposed Collecotors & Mines", "turn on and off searching for outside elixir collectors and gold mines.", 1, SettingType.ActiveAndDead);
            setCollMines.PossibleValues.Add(new SettingOption("Off", 0));
            setCollMines.PossibleValues.Add(new SettingOption("On", 1));
            settings.DefineSetting(setCollMines);

            // Show These ONLY when Set Exposed Collecotors & Mines is on.
            var minDistance = new AlgorithmSetting("Acceptable Target Range", "the maximun numbers of tiles the collectors and drills can be far from red line", 6, SettingType.ActiveAndDead)
            {
                MinValue = 2,
                MaxValue = 10
            };
            minDistance.HideInUiWhen.Add(new SettingOption("Set Exposed Collecotors & Mines", 0));
            settings.DefineSetting(minDistance);

            var minimElixir = new AlgorithmSetting("Minimum Exposed Colloctors", "Minimum Elixir Colloctores found outside before attack", 3, SettingType.ActiveAndDead)
            {
                MinValue = 0,
                MaxValue = 7
            };
            minimElixir.HideInUiWhen.Add(new SettingOption("Set Exposed Collecotors & Mines", 0));
            settings.DefineSetting(minimElixir);

            var minimGold = new AlgorithmSetting("Minimum Exposed Mines", "Minimum Gold Mines found outside before attack", 3, SettingType.ActiveAndDead)
            {
                MinValue = 0,
                MaxValue = 7
            };
            minimGold.HideInUiWhen.Add(new SettingOption("Set Exposed Collecotors & Mines", 0));
            settings.DefineSetting(minimGold);


            var useSmartZapDrills = new AlgorithmSetting("Smart Zap Drills", "use lighting Drills with smart way to save lighting spells if no need to use (please disable default Lighting drills if you select this option)", 0, SettingType.ActiveAndDead);
            useSmartZapDrills.PossibleValues.Add(new SettingOption("Off", 0));
            useSmartZapDrills.PossibleValues.Add(new SettingOption("On", 1));
            settings.DefineSetting(useSmartZapDrills);

            // Show These ONLY when Smart Zap Drills is on.
            var startZapAfter = new AlgorithmSetting("Start Zap Drills After ?(sec)", "change when bot start to use smart zap , this time start from deployment is done with all troops", 30, SettingType.ActiveAndDead)
            {
                MinValue = 10,
                MaxValue = 60
            };
            startZapAfter.HideInUiWhen.Add(new SettingOption("Smart Zap Drills", 0));
            settings.DefineSetting(startZapAfter);

            var minDrillLvl = new AlgorithmSetting("Min Drill Level", "select minimum level of the drill to be zapped", 3, SettingType.ActiveAndDead)
            {
                MinValue = 1,
                MaxValue = 6
            };
            minDrillLvl.HideInUiWhen.Add(new SettingOption("Smart Zap Drills", 0));
            settings.DefineSetting(minDrillLvl);

            var minDEAmount = new AlgorithmSetting("Min Dark Elixir per Zap", "we will zap only drills that have more than this amount of DE.", 200, SettingType.ActiveAndDead)
            {
                MinValue = 100,
                MaxValue = 600
            };
            minDEAmount.HideInUiWhen.Add(new SettingOption("Smart Zap Drills", 0));
            settings.DefineSetting(minDEAmount);

            var useEQOnDrills = new AlgorithmSetting("Use EarthQuake spell on drills", "use EarthQuake spell to gain DE from drills ", 0, SettingType.ActiveAndDead);
            useEQOnDrills.PossibleValues.Add(new SettingOption("Off", 0));
            useEQOnDrills.PossibleValues.Add(new SettingOption("On", 1));
            useEQOnDrills.HideInUiWhen.Add(new SettingOption("Smart Zap Drills", 0));
            settings.DefineSetting(useEQOnDrills);

            var endBattleAfterZap = new AlgorithmSetting("End Battle after zap ?(sec)", "end battle after this time in sec after Smart Zap is done (0 is disabled)", 10, SettingType.ActiveAndDead)
            {
                MinValue = 0,
                MaxValue = 60
            };
            endBattleAfterZap.HideInUiWhen.Add(new SettingOption("Smart Zap Drills", 0));
            settings.DefineSetting(endBattleAfterZap);

            var deployHeroesAt = new AlgorithmSetting("Deploy Heroes At", "choose where to deploy Heroes", 0, SettingType.ActiveAndDead);
            deployHeroesAt.PossibleValues.Add(new SettingOption("Normal (at the end)", 0));
            deployHeroesAt.PossibleValues.Add(new SettingOption("TownHall Side", 1));
            deployHeroesAt.PossibleValues.Add(new SettingOption("DE storage Side", 1));
            settings.DefineSetting(deployHeroesAt);




            return settings;
        }

        /// <summary>
        /// Called by the Bot Framework when This algorithm Row is selected in Attack Options tab
        /// to check to see whether or not this algorithm has Advanced Settings/Options
        /// </summary>
        public static bool ShowAdvancedSettingsButton()
        {
            return true;
        }

        /// <summary>
        /// Called when the Advanced button is clicked in the Bot UI with this algorithm Selected.
        /// </summary>
        public static void OnAdvancedSettingsButtonClicked()
        {
            // Show the Settings Dialog for this Algorithm.
            SettingsController.Instance.ShowSettingsWindow(AttackName);
        }

        /// <summary>
        /// Called from the Bot Framework when the bot is closing.
        /// </summary>
        public static void OnShutdown()
        {
            // Save settings for this algorithm.
            SettingsController.Instance.SaveAlgorithmSettings(AttackName);
        }

        public override IEnumerable<int> AttackRoutine()
        {
            int waveLimit = UserSettings.WaveSize;
            int waveDelay = (int)(UserSettings.WaveDelay * 1000);
            int heroesIndex = -1;

            var core = new PointFT(-0.01f, 0.01f);

            // Points to draw lines in deploy extends area.
            var topLeft = new PointFT((float)GameGrid.MaxX - 2, (float)GameGrid.DeployExtents.MaxY);
            var topRight = new PointFT((float)GameGrid.DeployExtents.MaxX, (float)GameGrid.MaxY - 2);

            var rightTop = new PointFT((float)GameGrid.DeployExtents.MaxX, (float)GameGrid.MinY + 2);
            var rightBottom = new PointFT((float)GameGrid.MaxX - 2, (float)GameGrid.DeployExtents.MinY);

            // Move 8 tiles from bottom corner due to unitsbar.
            var bottomLeft = new PointFT((float)GameGrid.DeployExtents.MinX, (float)GameGrid.MinY + 8);
            var bottomRight = new PointFT((float)GameGrid.MinX + 8, (float)GameGrid.DeployExtents.MinY);

            var leftTop = new PointFT((float)GameGrid.MinX + 2, (float)GameGrid.DeployExtents.MaxY);
            var leftBottom = new PointFT((float)GameGrid.DeployExtents.MinX, (float)GameGrid.MaxY - 2);

            var linesPointsList = new List<PointFT>
            {
                topLeft, topRight,
                rightTop, rightBottom,
                bottomLeft, bottomRight,
                leftBottom, leftTop
            };

            // Main four lines of attack.
            var topRightLine = new Tuple<PointFT, PointFT>(topRight, rightTop);
            var bottomRightLine = new Tuple<PointFT, PointFT>(bottomRight, rightBottom);
            var bottomLeftLine = new Tuple<PointFT, PointFT>(bottomLeft, leftBottom);
            var topLeftLine = new Tuple<PointFT, PointFT>(topLeft, leftTop);

            // List of the four attack lines in clocwise order
            var attackLines = new List<Tuple<PointFT, PointFT>>
            {
                topLeftLine,
                topRightLine,
                bottomRightLine,
                bottomLeftLine
            };

            var deployHeroesAt = GetCurrentSetting("Deploy Heroes At");

            
            var target = SmartFourFingersHelper.GetHeroesTarget(deployHeroesAt);

            var nearestRedPointToTarget = GameGrid.RedPoints.OrderBy(p => p.DistanceSq(target)).FirstOrDefault();
            var nearestLinePoint = linesPointsList.OrderBy(p => p.DistanceSq(nearestRedPointToTarget)).FirstOrDefault();

            heroesIndex = attackLines.FindIndex(u => (u.Item1.X == nearestLinePoint.X && u.Item1.Y == nearestLinePoint.Y) || (u.Item2.X == nearestLinePoint.X && u.Item2.Y == nearestLinePoint.Y));

            var units = Deploy.GetTroops();
            var heroes = units.Extract(x => x.IsHero);
            var cc = units.ExtractOne(u => u.ElementType == DeployElementType.ClanTroops);
            var spells = units.Extract(u => u.ElementType == DeployElementType.Spell);

            units.OrderForDeploy();

            // Set first attack line 
            // Start from the next line to user defined to end with user defined line
            var line = attackLines.NextOf(attackLines[heroesIndex]);
            var index = attackLines.FindIndex(u => u.Item1.X == line.Item1.X && u.Item1.Y == line.Item1.Y);

            Log.Info($"{AttackName} {Version} starts");
            // Start troops deployment on four sides.
            for (var i = 4; i >= 1; i--)
            {
                foreach (var unit in units)
                {
                    if (unit?.Count > 0)
                    {
                        var count = unit.Count / i;
                        var fingers = count % 4 <= 1 ? count : 4;
                        foreach (var t in Deploy.AlongLine(unit, line.Item1, line.Item2, count, fingers, 0, waveDelay))
                            yield return t;
                    }
                }
                if(i != 1)
                {
                    line = attackLines.NextOf(attackLines[index]);
                    index = attackLines.FindIndex(u => u.Item1.X == line.Item1.X && u.Item1.Y == line.Item1.Y);
                }
            }

            if (cc?.Count > 0)
            {
                Log.Info($"{AttackName} Deploy Clan Castle troops");
                foreach (var t in Deploy.AlongLine(cc, line.Item1, line.Item2, 1, 1, 0, waveDelay))
                    yield return t;
            }

            if (heroes.Any())
            {
                Log.Info($"{AttackName} Deploy Heroes");
                foreach (var hero in heroes.Where(u => u.Count > 0))
                {
                    foreach (var t in Deploy.AlongLine(hero, line.Item1, line.Item2, 1, 1, 0, waveDelay))
                        yield return t;
                }
                Deploy.WatchHeroes(heroes, 5000);
            }


            var minDEDrillLevel = GetCurrentSetting("Min Drill Level");

            // start smart zap
            if (GetCurrentSetting("Smart Zap Drills") == 1)
            {
                var waitBeforeSmartZap = GetCurrentSetting("Start Zap Drills After ?(sec)") * 1000;
                var minDEAmount = GetCurrentSetting("Min Dark Elixir per Zap");


                yield return waitBeforeSmartZap;

                foreach (var t in SmartZapping.SmartZap(minDEAmount, minDEDrillLevel, spells))
                    yield return t;
            }

            // start Use EarthQuake spell on drills
            if (GetCurrentSetting("Use EarthQuake spell on drills") == 1)
            {
                foreach (var t in SmartZapping.UseEQOnDrills(minDEDrillLevel, spells))
                    yield return t;
            }

            // end battle
            var endBattleTime = GetCurrentSetting("End Battle after zap ?(sec)");
            foreach (var t in SmartZapping.EndBattle(endBattleTime))
                yield return t;
        }

        public override double ShouldAccept()
        {
            if (!Opponent.MeetsRequirements(BaseRequirements.All))
                return 0;
            if (GetCurrentSetting("Set Exposed Collecotors & Mines") == 1)
            {
                if (!SmartFourFingersHelper.IsBaseMinCollectorsAndMinesOutside(GetCurrentSetting("Acceptable Target Range"), GetCurrentSetting("Minimum Exposed Colloctors"), GetCurrentSetting("Minimum Exposed Mines"), AttackName, GetCurrentSetting("Debug Mode"))) 
                    return 0;
            }
            return 1;
        }
    }
}