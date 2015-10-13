using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ColorMS = System.Drawing.Color;
using LeagueSharp;
using LeagueSharp.Common;
using CommonItems = LeagueSharp.Common.Data.ItemData;
using SharpDX;

namespace PippyNami
{
    class PippyNami
    {

        private const string champName = "Nami";
        private static readonly Color namiColor = new Color(37, 223, 223);
        private static readonly ColorMS namiColorMS = ColorMS.FromArgb(37, 223, 223);
        private static readonly string[] HClist = { "Low", "Medium", "High", "Very High" };
        private static readonly string[] EuseWho = { "Only Me", "Most AD Teammate" };

        private static Orbwalking.Orbwalker vaginaOrb;
        private static Spell theQ, theW, theE, theR;
        private static SpellSlot igniteSlot;
        private static Menu NamiMenu;
        private static Items.Item ruined_item, bc_item, youmuu_item;
        private static Obj_AI_Hero _target;
        private static Obj_AI_Hero _targetR;
        private static Obj_AI_Hero meLulz = ObjectManager.Player;

        private static Dictionary<string, float> spellInfo;

        public static void Load(EventArgs args)
        {
            if (meLulz.ChampionName != champName)
            {
                return;
            }

            spellInfo = NamiSpellInfo.GetValues();

            //Spells Init
            theQ = new Spell(SpellSlot.Q, spellInfo["qRange"], TargetSelector.DamageType.Magical);
            theW = new Spell(SpellSlot.W, spellInfo["wRange"], TargetSelector.DamageType.Magical);
            theE = new Spell(SpellSlot.E, spellInfo["eRange"], TargetSelector.DamageType.Magical);
            theR = new Spell(SpellSlot.R, spellInfo["rRange"], TargetSelector.DamageType.Magical);

            //Skillshutz
            theQ.SetSkillshot(spellInfo["qDelay"], spellInfo["qWidth"], spellInfo["qSpeed"], false, SkillshotType.SkillshotCircle);
            theR.SetSkillshot(spellInfo["rDelay"], spellInfo["rWidth"], spellInfo["rSpeed"], false, SkillshotType.SkillshotLine);

            //Ignite
            igniteSlot = ObjectManager.Player.GetSpellSlot("summonerdot");

            //Items
            ruined_item = new Items.Item(CommonItems.Blade_of_the_Ruined_King.Id, CommonItems.Blade_of_the_Ruined_King.Range);
            bc_item = new Items.Item(CommonItems.Bilgewater_Cutlass.Id, CommonItems.Bilgewater_Cutlass.Range);
            youmuu_item = new Items.Item(CommonItems.Youmuus_Ghostblade.Id);

            //Menu
            NamiMenuLoad();

            //Events
            Game.OnUpdate += NamiUpdate;
            Drawing.OnDraw += NamiDraw;
        }

        private static void NamiMenuLoad()
        {
            NamiMenu = new Menu("Pippy Nami", "pippynami", true);

            //OrbWalker
            var orbMenu = new Menu("Orbwalking", "orbw");
            vaginaOrb = new Orbwalking.Orbwalker(orbMenu);
            NamiMenu.AddSubMenu(orbMenu);

            var tsMenu = new Menu("Target Selector", "ts");
            TargetSelector.AddToMenu(tsMenu);
            NamiMenu.AddSubMenu(tsMenu);

            var comboMenu = new Menu("Combo Settings", "combo");
            NamiMenu.AddSubMenu(comboMenu);


            var itemsMenu = new Menu("Items Settings", "items");
            itemsMenu.AddItem(new MenuItem("useBC", "Use Bilgewater Cutlass")).SetValue(true);
            itemsMenu.AddItem(new MenuItem("useBOTRK", "Use Ruined King")).SetValue(true);
            itemsMenu.AddItem(new MenuItem("useYoumuu", "Use Youmuu's Ghostblade")).SetValue(true);
            comboMenu.AddSubMenu(itemsMenu);

            var qComboMenu = new Menu("Q Settings", "qcombo");
            qComboMenu.AddItem(new MenuItem("qComboUse", "Use in combo")).SetValue(true);
            qComboMenu.AddItem(new MenuItem("qComboHC", "Min Hitchance")).SetValue(new StringList(HClist, 3));
            comboMenu.AddSubMenu(qComboMenu);

            var wComboMenu = new Menu("W Settings", "wcombo");
            wComboMenu.AddItem(new MenuItem("wComboAttack", "Use W in combo")).SetValue(false);
            wComboMenu.AddItem(new MenuItem("wComboHeal", "Use W to heal (self)")).SetValue(true);
            wComboMenu.AddItem(new MenuItem("wComboHealSlider", "-->At % min HP")).SetFontStyle(System.Drawing.FontStyle.Regular, namiColor).SetValue(new Slider(40, 0, 100));
            comboMenu.AddSubMenu(wComboMenu);

            var eComboMenu = new Menu("E Settings", "ecombo");
            eComboMenu.AddItem(new MenuItem("eComboUse", "Use E in combo")).SetValue(true);
            eComboMenu.AddItem(new MenuItem("eComboUseWho", "--> On who?")).SetFontStyle(System.Drawing.FontStyle.Regular, namiColor).SetValue(new StringList(EuseWho));
            comboMenu.AddSubMenu(eComboMenu);

            var rComboMenu = new Menu("R Settings", "rcombo");
            rComboMenu.AddItem(new MenuItem("rComboUse", "Use R in Combo")).SetValue(false);
            rComboMenu.AddItem(new MenuItem("rComboHC", "Min Hitchance")).SetValue(new StringList(HClist, 2));
            comboMenu.AddSubMenu(rComboMenu);


            var harassMenu = new Menu("Harass Settings", "harass");
            harassMenu.AddItem(new MenuItem("qHarassUse", "Use Q in harass")).SetValue(true);
            harassMenu.AddItem(new MenuItem("eHarassUse", "Use E in harass")).SetValue(false);
            harassMenu.AddItem(new MenuItem("harassMana", "Min Mana % to Harass")).SetValue(new Slider(55, 0, 100));
            NamiMenu.AddSubMenu(harassMenu);

            var healingMenu = new Menu("Healing Settings", "healing");
            var allyList = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly).ToList();
            foreach (var ally in allyList)
            {
                healingMenu.AddItem(new MenuItem("heal" + ally.ChampionName, "Heal " + ally.ChampionName)).SetValue(false);
                healingMenu.AddItem(new MenuItem("healSlider" + ally.ChampionName, "--> Min % HP")).SetFontStyle(System.Drawing.FontStyle.Regular, namiColor).SetValue(new Slider(30, 0, 100));
            }
            NamiMenu.AddSubMenu(healingMenu);

            var drawMenu = new Menu("Drawing Settings", "drawing");
            drawMenu.AddItem(new MenuItem("qDrawRange", "Draw Q Range")).SetValue(new Circle(true, ColorMS.Tomato, spellInfo["qRange"]));
            drawMenu.AddItem(new MenuItem("wDrawRange", "Draw W Range")).SetValue(new Circle(false, ColorMS.Tomato, spellInfo["wRange"]));
            drawMenu.AddItem(new MenuItem("eDrawRange", "Draw E Range")).SetValue(new Circle(false, ColorMS.Tomato, spellInfo["eRange"]));
            drawMenu.AddItem(new MenuItem("rDrawRange", "Draw R Range")).SetValue(new Circle(true, ColorMS.Tomato, spellInfo["rRange"]));
            drawMenu.AddItem(new MenuItem("targetDraw", "Draw Forced Target")).SetValue(new Circle(true, ColorMS.Red, 200));
            NamiMenu.AddSubMenu(drawMenu);

            var miscMenu = new Menu("Misc Settings", "misc");
            miscMenu.AddItem(new MenuItem("iKS", "KS with Ignite")).SetValue(true);
            miscMenu.AddItem(new MenuItem("forceR", "Force Manual R")).SetValue(new KeyBind(65, KeyBindType.Press));
            miscMenu.AddItem(new MenuItem("forceRHC", "Force R - Hitchance")).SetValue(new StringList(HClist));
            NamiMenu.AddSubMenu(miscMenu);

            NamiMenu.AddToMainMenu();
        }

        private static void NamiUpdate(EventArgs args)
        {
            //Checks
            _target = (TargetSelector.SelectedTarget == null) ? TargetSelector.GetTarget(spellInfo["qRange"], TargetSelector.DamageType.Physical) : TargetSelector.SelectedTarget;
            _targetR = (TargetSelector.SelectedTarget == null) ? TargetSelector.GetTarget(spellInfo["rRange"], TargetSelector.DamageType.Physical) : TargetSelector.SelectedTarget;

            HealingManager();


            //Modes
            switch (vaginaOrb.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    Items();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    //Clear(); Soon
                    break;
            }

            //KS Ignite - Calling it like this because of ignite only at the moment
            if (NamiMenu.Item("iKS").GetValue<bool>())
            {
                KillSteal();
            }

            if (NamiMenu.Item("forceR").GetValue<KeyBind>().Active)
            {
                if (_targetR != null && theR.IsReady())
                {
                    theR.CastIfHitchanceEquals(_targetR, (HitChance)(NamiMenu.Item("forceRHC").GetValue<StringList>().SelectedIndex + 3));
                }
                Game.PrintChat((NamiMenu.Item("forceRHC").GetValue<StringList>().SelectedIndex + 3).ToString());
            }
        }

        private static void Combo()
        {

            if (_target == null || meLulz.IsDead)
            {
                return;
            }

            if (NamiMenu.Item("qComboUse").GetValue<bool>())
            {
                if (theQ.IsReady())
                {
                    theQ.CastIfHitchanceEquals(_target, (HitChance)(NamiMenu.Item("qComboHC").GetValue<StringList>().SelectedIndex + 3));
                }
            }

            if (NamiMenu.Item("wComboAttack").GetValue<bool>())
            {
                if (theW.IsReady() && meLulz.Distance(_target) < spellInfo["wRange"])
                {
                    theW.CastOnUnit(_target);
                }
            }

            if (NamiMenu.Item("wComboHeal").GetValue<bool>())
            {
                if (theW.IsReady())
                {
                    if (meLulz.HealthPercent < NamiMenu.Item("wComboHealSlider").GetValue<Slider>().Value)
                    {
                        theW.CastOnUnit(meLulz);
                    }
                }
            }

            if (NamiMenu.Item("eComboUse").GetValue<bool>())
            {
                if (theE.IsReady())
                {
                    switch (NamiMenu.Item("eComboUseWho").GetValue<StringList>().SelectedIndex)
                    {
                        case 0:
                            if (meLulz.Distance(_target) < meLulz.AttackRange)
                            {
                                theE.CastOnUnit(meLulz);
                            }
                            break;
                        case 1:
                            //Feel like Hoes, just needed 2 more .Where()
                            theE.CastOnUnit(ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly)
                                .Where(ally => ally.Distance(meLulz) < spellInfo["eRange"] && ally.Distance(_target) < ally.AttackRange + 100)
                                .OrderByDescending(ally => ally.GetAutoAttackDamage(_target)).FirstOrDefault());
                            break;
                    }
                }
            }

            if (NamiMenu.Item("rComboUse").GetValue<bool>())
            {
                if (theR.IsReady())
                {
                    theR.CastIfHitchanceEquals(_target, (HitChance)(NamiMenu.Item("rComboHC").GetValue<StringList>().SelectedIndex + 3));
                }
            }
        }

        private static void Items()
        {
            var bcUse = NamiMenu.Item("useBC").GetValue<bool>();
            var botrkUse = NamiMenu.Item("useBOTRK").GetValue<bool>();
            var youmuuUse = NamiMenu.Item("useYoumuu").GetValue<bool>();

            if ((!bcUse && !botrkUse && !youmuuUse) || _target == null)
            {
                return;
            }

            if (bcUse && bc_item.IsReady())
            {
                bc_item.Cast(_target);
            }

            if (botrkUse && ruined_item.IsReady())
            {
                ruined_item.Cast(_target);
            }

            if (youmuuUse && youmuu_item.IsReady() && meLulz.Distance(_target) < (meLulz.AttackRange - 50))
            {
                youmuu_item.Cast();
            }
        }

        private static void Harass()
        {
            if (_target == null || meLulz.IsDead)
            {
                return;
            }

            if (meLulz.ManaPercent > NamiMenu.Item("harassMana").GetValue<Slider>().Value)
            {
                if (NamiMenu.Item("qHarassUse").GetValue<bool>() && meLulz.Distance(_target) < spellInfo["qRange"])
                {
                    if (theQ.IsReady())
                    {
                        theQ.CastIfHitchanceEquals(_target, HitChance.Medium);
                    }
                }

                if (NamiMenu.Item("eHarassUse").GetValue<bool>() && meLulz.Distance(_target) < meLulz.AttackRange)
                {
                    if (theE.IsReady())
                    {
                        theE.CastOnUnit(meLulz);
                    }
                }
            }
        }

        private static void Clear()
        {

        }

        private static void HealingManager()
        {
            if (meLulz.IsDead || meLulz.IsRecalling())
            {
                return;
            }

            var allyList = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly).ToList();

            foreach (var ally in allyList)
            {
                if (NamiMenu.Item("heal" + ally.ChampionName).GetValue<bool>())
                {
                    if (ally.HealthPercent <= NamiMenu.Item("healSlider" + ally.ChampionName).GetValue<Slider>().Value)
                    {
                        if (meLulz.Distance(ally) <= spellInfo["wRange"])
                        {
                            theW.CastOnUnit(ally);
                        }
                    }
                }
            }
        }

        private static void NamiDraw(EventArgs args)
        {
            var qDraw = NamiMenu.Item("qDrawRange").GetValue<Circle>();
            var wDraw = NamiMenu.Item("wDrawRange").GetValue<Circle>();
            var eDraw = NamiMenu.Item("eDrawRange").GetValue<Circle>();
            var rDraw = NamiMenu.Item("rDrawRange").GetValue<Circle>();
            var tDraw = NamiMenu.Item("targetDraw").GetValue<Circle>();

            if (meLulz.IsDead) return;

            if (qDraw.Active)
            {
                Render.Circle.DrawCircle(meLulz.Position, qDraw.Radius, theQ.IsReady() ? namiColorMS : ColorMS.Tomato);
            }

            if (wDraw.Active)
            {
                Render.Circle.DrawCircle(meLulz.Position, wDraw.Radius, theW.IsReady() ? namiColorMS : ColorMS.Tomato);
            }

            if (eDraw.Active)
            {
                Render.Circle.DrawCircle(meLulz.Position, eDraw.Radius, theE.IsReady() ? namiColorMS : ColorMS.Tomato);
            }

            if (rDraw.Active)
            {
                Render.Circle.DrawCircle(meLulz.Position, rDraw.Radius, theR.IsReady() ? namiColorMS : ColorMS.Tomato);
            }

            if (tDraw.Active && TargetSelector.SelectedTarget != null)
            {
                Render.Circle.DrawCircle(TargetSelector.SelectedTarget.Position, tDraw.Radius, tDraw.Color);
            }
        }

        private static void KillSteal()
        {
            var enemyList = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy).ToList();

            foreach (var enemy in enemyList)
            {
                if (meLulz.Distance(enemy) <= 600 && igniteSlot != SpellSlot.Unknown && enemy.Health <= meLulz.GetSummonerSpellDamage(_target, Damage.SummonerSpell.Ignite))
                {
                    meLulz.Spellbook.CastSpell(igniteSlot, enemy);
                }
            }
        }
    }
}
