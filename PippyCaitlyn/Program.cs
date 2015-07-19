using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;

namespace PippyCaitlyn
{
    class Program
    {

        private const string champName = "Caitlyn";
        private const Color pippyCaitColor = Color.FromArgb(60, 222, 203);
        private const string[] hcList = { "Low", "Medium", "High", "Very High" };
        private const string[] dtList = { "Physical", "Magical" };

        private static Spell theQ, theW, theE, theR;
        private static Items.Item botrk, bc, youmuu, gunblade;
        private static SpellSlot ignite;
        private static Menu CaitMenu;
        private static Orbwalking.Orbwalker penisOrb;
        private static Obj_AI_Hero target;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += LoadStuff;
        }

        static SpellData SpellInfo(uint hash)
        {
            return SpellData.GetSpellData(hash);
        }

        static void LoadStuff(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != champName)
            {
                return;
            }

            //#NeverForgetPrintChat
            Notifications.AddNotification("Pippy Caitlyn Loaded", 6).SetTextColor(pippyCaitColor);

            //Spell Init
            theQ = new Spell(SpellSlot.Q, 1300f);
            theW = new Spell(SpellSlot.W, 800f, TargetSelector.DamageType.Magical);
            theE = new Spell(SpellSlot.E, 950f, TargetSelector.DamageType.Magical);
            theR = new Spell(SpellSlot.R, 2000f);

            //Skillshot stuff
            theQ.SetSkillshot(SpellInfo(0).SpellCastTime, SpellInfo(0).LineWidth, SpellInfo(0).MissileSpeed, false, SkillshotType.SkillshotLine);
            theE.SetSkillshot(SpellInfo(2).SpellCastTime, SpellInfo(2).LineWidth, SpellInfo(2).MissileSpeed, true, SkillshotType.SkillshotLine);
            theR.SetTargetted(SpellInfo(3).SpellCastTime, SpellInfo(3).MissileSpeed);

            //Do we have ignite? Well, not me, since you are the one playing
            ignite = ObjectManager.Player.GetSpellSlot("summonerdot");

            //Items init
            botrk = new Items.Item(3153, 550f);
            bc = new Items.Item(3144, 550f);
            youmuu = new Items.Item(3142, ObjectManager.Player.AttackRange - 50f);
            gunblade = new Items.Item(3146, 700f);

            //I believe this is the menu
            CaitMenuKappa();

            //The happenings - Haha, get it? Happening, Event. HAHAHAHAHAHHAHAHAHA
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Interrupter2.OnInterruptableTarget += OnPossibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += OnGapCloser;

        }

        static void OnUpdate(EventArgs args)
        {
            target = TargetSelector.GetTarget(theQ.Range, CurrentDamageType());

            Killstealing();
            CheckRRange();

            if (CaitMenu.Item("comboKey").GetValue<KeyBind>().Active)
            {
                TeCombeoPapi();
            }

            if (CaitMenu.Item("harassKey").GetValue<KeyBind>().Active)
            {
                TeHarraseoPapi();
            }

            if (CaitMenu.Item("laneclearKey").GetValue<KeyBind>().Active)
            {
                TeLimpioLaWea();
            }

            if (CaitMenu.Item("ultKey").GetValue<KeyBind>().Active)
            {
                ForceUlti();
            }
        }

        static void CaitMenuKappa()
        {

            CaitMenu = new Menu("Pippy Caitlyn", "pippycaitlyn", true);

            var orbMenu = new Menu("Orbwalking", "orbwalking");
            penisOrb = new Orbwalking.Orbwalker(orbMenu);
            CaitMenu.AddSubMenu(orbMenu);

            var tsMenu = new Menu("Target Selector", "ts");
            TargetSelector.AddToMenu(tsMenu);
            CaitMenu.AddSubMenu(tsMenu);

            var keysMenu = new Menu("Keybinds", "keys");
            keysMenu.AddItem(new MenuItem("comboKey", "Combo")).SetValue(new KeyBind(32, KeyBindType.Press));
            keysMenu.AddItem(new MenuItem("harassKey", "Harass")).SetValue(new KeyBind(67, KeyBindType.Press));
            keysMenu.AddItem(new MenuItem("laneclearKey", "Laneclear")).SetValue(new KeyBind(86, KeyBindType.Press));
            keysMenu.AddItem(new MenuItem("ultKey", "Force Ultimate on Target")).SetValue(new KeyBind(65, KeyBindType.Press));
            CaitMenu.AddSubMenu(keysMenu);

            var comboMenu = new Menu("Combo Settings", "combo");
            comboMenu.AddItem(new MenuItem("comboQ", "Use Q")).SetValue(true);
            comboMenu.AddItem(new MenuItem("comboW", "Use W near Target")).SetValue(false);
            comboMenu.AddItem(new MenuItem("comboE", "Use E offensively")).SetValue(true);

            var harassMenu = new Menu("Harass Settings", "harass");
            harassMenu.AddItem(new MenuItem("harassQ", "Use Q")).SetValue(true);
            harassMenu.AddItem(new MenuItem("harassE", "Use E offensively")).SetValue(true);
            harassMenu.AddItem(new MenuItem("minManaHarass", "Min Mana %")).SetValue(new Slider(40));
            CaitMenu.AddSubMenu(harassMenu);

            var hitChanceMenu = new Menu("Hitchance Settings", "hitchance");
            hitChanceMenu.AddItem(new MenuItem("hcQcombo", "Q Hitchance - Combo")).SetValue(new StringList(hcList, 2));
            hitChanceMenu.AddItem(new MenuItem("hcEcombo", "E Hitchance - Combo")).SetValue(new StringList(hcList, 1));
            hitChanceMenu.AddItem(new MenuItem("hcQharass", "Q Hitchance - Harass")).SetValue(new StringList(hcList, 2));
            hitChanceMenu.AddItem(new MenuItem("hcEharass", "E Hitchance - Harass")).SetValue(new StringList(hcList, 1));
            CaitMenu.AddSubMenu(hitChanceMenu);

            /*var waveClearMenu = new Menu("Wave Clear Settings", "waveclear");
            waveClearMenu.AddItem(new MenuItem("clearQ", "Clear with Q")).SetValue(false);
            CaitMenu.AddSubMenu(waveClearMenu);
            */

            var killstealMenu = new Menu("Killsteal Settings", "killsteal");
            killstealMenu.AddItem(new MenuItem("ksI", "Killsteal with Ignite")).SetValue(true);
            killstealMenu.AddItem(new MenuItem("ksQ", "Killsteal with Q")).SetValue(true);
            killstealMenu.AddItem(new MenuItem("ksE", "Killsteal with E")).SetValue(true);
            killstealMenu.AddItem(new MenuItem("ksR", "Killsteal with R")).SetValue(true);

            var drawMenu = new Menu("Drawing Settings", "drawing");
            drawMenu.AddItem(new MenuItem("drawQ", "Draw Q Range")).SetValue(new Circle(true, pippyCaitColor));
            drawMenu.AddItem(new MenuItem("drawW", "Draw W Range")).SetValue(new Circle(false, pippyCaitColor));
            drawMenu.AddItem(new MenuItem("drawE", "Draw E Range")).SetValue(new Circle(true, pippyCaitColor));
            drawMenu.AddItem(new MenuItem("drawR", "Draw R Range")).SetValue(new Circle(true, pippyCaitColor));
            drawMenu.AddItem(new MenuItem("drawTarget", "Draw current Target")).SetValue(new Circle(true, Color.Red));
            drawMenu.AddItem(new MenuItem("drawKillableR", "Draw Killable with R")).SetValue(new Circle(true, Color.BlueViolet));
            CaitMenu.AddSubMenu(drawMenu);

            CaitMenu.AddItem(new MenuItem("dmgType", "Damage Mode:")).SetValue(new StringList(dtList));

            CaitMenu.AddToMainMenu();

        }

        static void TeCombeoPapi()
        {

            if(target == null)
            { 
                return;
            }

            if (CaitMenu.Item("comboQ").GetValue<bool>())
            {
                if (theQ.IsReady())
                {
                    if (target.IsValid && ObjectManager.Player.Distance(target.ServerPosition) < theQ.Range)
                    {
                        var qOutput = theQ.GetPrediction(target);
                        if (qOutput.Hitchance >= (HitChance)CaitMenu.Item("hcQcombo").GetValue<StringList>().SelectedIndex + 2)
                        {
                            theQ.Cast(qOutput.CastPosition);
                        }
                    }
                }
            }

            if (CaitMenu.Item("comboE").GetValue<bool>())
            {
                if(theE.IsReady())
                {
                    if (target.IsValid && ObjectManager.Player.Distance(target.ServerPosition) < theE.Range)
                    {
                        var eOutput = theE.GetPrediction(target);
                        if (eOutput.Hitchance >= (HitChance)CaitMenu.Item("hcEcombo").GetValue<StringList>().SelectedIndex + 2)
                        {
                            theE.Cast(eOutput.CastPosition);
                        }
                    }
                }
            }

            if (CaitMenu.Item("comboW").GetValue<bool>())
            {
                if (theW.IsReady())
                {
                    if (target.IsValid && ObjectManager.Player.Distance(target.ServerPosition) < theW.Range)
                    {
                        theW.Cast(target.ServerPosition);
                    }
                }
            }
        }

        static void TeHarraseoPapi()
        {
            if(target == null)
            {
                return;
            }

            if(CaitMenu.Item("harassQ").GetValue<bool>())
            {
                if(theQ.IsReady())
                {

                }
            }
        }

        static TargetSelector.DamageType CurrentDamageType()
        {
            if(CaitMenu.Item("dmgType").GetValue<StringList>().SelectedIndex == 0)
            {
                return TargetSelector.DamageType.Physical;
            }
            else
            {
                return TargetSelector.DamageType.Magical;
            }
        }
    }
}
