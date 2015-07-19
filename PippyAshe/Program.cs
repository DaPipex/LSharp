using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace PippyAshe
{
    class Program
    {

        static Obj_AI_Hero playerChar = ObjectManager.Player;

        static Spell spellQ, spellW, spellE, spellR;

        static Orbwalking.Orbwalker daOrbwalker;

        static Menu AshyMenu;

        static string[] hcList = { "Low", "Medium", "High" };

        static List<int> autoLevelSequenceInt = new List<int>(18) { 2, 1, 3, 2, 2, 4, 2, 1, 2, 1, 4, 1, 1, 3, 3, 4, 3, 3 };

        static List<SpellSlot> autoLevelSequence = new List<SpellSlot>(18);

        static SpellSlot igniteslot = SpellSlot.Unknown;

        static bool PipexDebug = true;


        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += AshyLoad;
        }

        private static void AshyLoad(EventArgs args)
        {
            if (playerChar.ChampionName != "Ashe") return;

            spellQ = new Spell(SpellSlot.Q);
            spellW = new Spell(SpellSlot.W, 1100f);
            spellW.SetSkillshot(0.25f, 24.32f, 902f, true, SkillshotType.SkillshotCone);
            spellE = new Spell(SpellSlot.E);
            spellR = new Spell(SpellSlot.R);
            spellR.SetSkillshot(0.25f, 130f, 1600f, false, SkillshotType.SkillshotLine);

            if (ObjectManager.Player.GetSpellSlot("SummonerIgnite") != SpellSlot.Unknown)
            {
                igniteslot = ObjectManager.Player.GetSpellSlot("SummonerIgnite");
            }

            AutoLevel demLevels = new AutoLevel(autoLevelSequence);

            foreach (int slotSequence in autoLevelSequenceInt)
            {
                switch (slotSequence)
                {
                    case 1:
                        autoLevelSequence.Add(SpellSlot.Q);
                        break;
                    case 2:
                        autoLevelSequence.Add(SpellSlot.W);
                        break;
                    case 3:
                        autoLevelSequence.Add(SpellSlot.E);
                        break;
                    case 4:
                        autoLevelSequence.Add(SpellSlot.R);
                        break;
                }
            }

            if (PipexDebug)
            {
                foreach (SpellSlot slotSlot in autoLevelSequence)
                {
                    Console.WriteLine(slotSlot.ToString());
                }
            }
            


            AshyMenu = new Menu("Pippy Ashe", "pippyashe", true);

            var ashyKeys = new Menu("Keys", "keys");
            ashyKeys.AddItem(new MenuItem("comboKey", "Combo Key:")).SetValue(new KeyBind(32, KeyBindType.Press));
            ashyKeys.AddItem(new MenuItem("harassKey", "Harass Key")).SetValue(new KeyBind(67, KeyBindType.Press));
            //ashyKeys.AddItem(new MenuItem("ultKey", "Ult fire Key:")).SetValue(new KeyBind(65, KeyBindType.Press));
            AshyMenu.AddSubMenu(ashyKeys);

            var ashyTS = new Menu("Target Selector", "tsmenu");
            TargetSelector.AddToMenu(ashyTS);
            AshyMenu.AddSubMenu(ashyTS);

            var ashyOrb = new Menu("Orbwalker", "orbmenu");
            daOrbwalker = new Orbwalking.Orbwalker(ashyOrb);
            AshyMenu.AddSubMenu(ashyOrb);

            var ashyCombo = new Menu("Combo Settings", "combomenu");
            ashyCombo.AddItem(new MenuItem("useQcombo", "Use Q against champs")).SetValue(true);
            ashyCombo.AddItem(new MenuItem("useWcombo", "Use W in combo")).SetValue(true);
            ashyCombo.AddItem(new MenuItem("useRcombo", "Use R in combo")).SetValue(false);
            AshyMenu.AddSubMenu(ashyCombo);

            var ashyHarass = new Menu("Harass Settings", "harassmenu");
            ashyHarass.AddItem(new MenuItem("useWharass", "Use W in harass")).SetValue(true);
            AshyMenu.AddSubMenu(ashyHarass);

            var ashyHitchance = new Menu("Hitchance Settings", "hcmenu");
            ashyHitchance.AddItem(new MenuItem("wCombo", "W - Combo")).SetValue(new StringList(hcList, 1));
            ashyHitchance.AddItem(new MenuItem("rCombo", "R - Combo")).SetValue(new StringList(hcList, 1));
            ashyHitchance.AddItem(new MenuItem("wHarass", "W - Harass")).SetValue(new StringList(hcList, 1));
            ashyHitchance.AddItem(new MenuItem("rHelper", "R - Helper")).SetValue(new StringList(hcList, 1));
            AshyMenu.AddSubMenu(ashyHitchance);

            var ashyRhelper = new Menu("Ulti Helper", "ultmenu");
            ashyRhelper.AddItem(new MenuItem("ultKey", "Fire ult key")).SetValue(new KeyBind(65, KeyBindType.Press));
            ashyRhelper.AddItem(new MenuItem("info1", "Range to check: 1500"));
            AshyMenu.AddSubMenu(ashyRhelper);

            var ashyKS = new Menu("Killsteal Settings", "ksmenu");
            ashyKS.AddItem(new MenuItem("ksI", "KS with Ignite")).SetValue(true);
            ashyKS.AddItem(new MenuItem("ksW", "KS with W")).SetValue(true);
            ashyKS.AddItem(new MenuItem("ksR", "KS with R")).SetValue(false);
            AshyMenu.AddSubMenu(ashyKS);

            var ashyDraw = new Menu("Drawings", "draw");
            ashyDraw.AddItem(new MenuItem("wRange", "Draw W Range")).SetValue(new Circle(true, Color.Turquoise));
            ashyDraw.AddItem(new MenuItem("eRange", "Draw E Range")).SetValue(new Circle(false, Color.Turquoise));
            ashyDraw.AddItem(new MenuItem("eRangeMM", "Draw E Range (Minimap)")).SetValue(new Circle(true, Color.White));
            ashyDraw.AddItem(new MenuItem("rKsRange", "Draw R KS Range")).SetValue(new Circle(false, Color.Red));
            ashyDraw.AddItem(new MenuItem("rInterRange", "Draw R Interrupt Range")).SetValue(new Circle(false, Color.Red));
            AshyMenu.AddSubMenu(ashyDraw);

            var ashyMisc = new Menu("Misc Settings", "misc");
            ashyMisc.AddItem(new MenuItem("autoLevel", "Auto Level (W-Q-E)")).SetValue(false);
            ashyMisc.AddItem(new MenuItem("interG", "Interrupt with R")).SetValue(true);
            ashyMisc.AddItem(new MenuItem("info2", "More stuff soon!"));
            AshyMenu.AddSubMenu(ashyMisc);

            var ashyInfo = new Menu("Information", "info");
            ashyInfo.AddItem(new MenuItem("info3", "Author: DaPipex"));
            ashyInfo.AddItem(new MenuItem("info4", "Version: 1.0.0.0"));
            AshyMenu.AddSubMenu(ashyInfo);

            AshyMenu.AddToMainMenu();


            Game.OnUpdate += Ashy_OnUpdate;
            //Drawing.OnDraw += Ashy_OnDraw;
            //Interrupter2.OnInterruptableTarget += Ashy_OnInterrupt;


            Notifications.AddNotification("Pippy Ashe by DaPipex Loaded", 6000);
        }

        private static void Ashy_OnUpdate(EventArgs args)
        {
            Killsteal();
            AutoLeveler();

            /*if (AshyMenu.Item("comboKey").GetValue<bool>())
            {
                //Combo();
                //Items();
            }
            if (AshyMenu.Item("harassKey").GetValue<bool>())
            {
                //Harass();
            }
            if (AshyMenu.Item("ultKey").GetValue<bool>())
            {
                //UltHelper();
            }*/
        }

        private static void AutoLeveler()
        {

            if (AshyMenu.Item("autoLevel").GetValue<bool>())
            {
                AutoLevel.Enabled(true);
            }
            else
            {
                AutoLevel.Enabled(false);
            }
        }

        private static void Killsteal()
        {
            var ksIbool = AshyMenu.Item("ksI").GetValue<bool>();
            var ksWbool = AshyMenu.Item("ksW").GetValue<bool>();
            var ksRbool = AshyMenu.Item("ksR").GetValue<bool>();

            if (!ksIbool && !ksWbool && !ksRbool)
            {
                return;
            }

            if (ksIbool)
            {
                if ((igniteslot != SpellSlot.Unknown) && igniteslot.IsReady())
                {
                    
                }
            }
        }
    }
}
