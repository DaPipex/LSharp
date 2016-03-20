using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;

using SharpDX;

namespace WreckingBall
{
    internal class WreckingBall
    {

        private const string ChampName = "LeeSin";

        private static Obj_AI_Hero leeHero;

        private static Obj_AI_Hero rTarget;

        private static Obj_AI_Hero rTargetSecond;

        private static Spell spellQ, spellQ2, spellW, spellW2, spellE, spellE2, spellR;

        private static SpellSlot igniteSlot, flashSlot;

        private static Menu wbMenu;

        private static readonly string[] qSpellNames = { string.Empty, "BlindMonkQOne", "BlindMonkQTwo" };

        private static readonly string[] wSpellNames = { string.Empty, "BlindMonkWOne", "BlindMonkWTwo" };

        private static readonly string[] eSpellNames = { string.Empty, "BlindMonkEOne", "BlindMonkETwo" };

        private static readonly string rSpellName = "BlindMonkRKick";

        private static int lastWjTick;

        private static bool bubbaKushing;


        private static Items.Item[] wjItems =
            {
                new Items.Item(1408, 600), //Enchanted - Warrior
                new Items.Item(1409, 600), //Enchanted - Cinderhulk 
                new Items.Item(1410, 600), //Enchanted - Runic Echoes  
                new Items.Item(1411, 600), //Enchanted - Devourer
                new Items.Item(2301, 600), //EOT Watchers
                new Items.Item(2302, 600), //EOT Oasis
                new Items.Item(2303, 600), //EOT Equinox
                new Items.Item((int)ItemId.Poachers_Knife, 600),
                new Items.Item((int)ItemId.Ruby_Sightstone, 600),
                new Items.Item((int)ItemId.Sightstone, 600),
                new Items.Item((int)ItemId.Warding_Totem_Trinket, 600),
                new Items.Item((int)ItemId.Vision_Ward, 600), 
            };

        public static void Init()
        {
            CustomEvents.Game.OnGameLoad += WreckingBallLoad;
        }

        private static void WreckingBallLoad(EventArgs args)
        {
            leeHero = ObjectManager.Player;

            if (leeHero.ChampionName != ChampName)
            {
                return;
            }

            ChampInfo.InitSpells();

            LoadMenu();

            spellQ = new Spell(SpellSlot.Q, ChampInfo.Q.Range);
            spellQ2 = new Spell(SpellSlot.Q, ChampInfo.Q2.Range);
            spellW = new Spell(SpellSlot.W, ChampInfo.W.Range);
            spellW2 = new Spell(SpellSlot.W, ChampInfo.W2.Range);
            spellE = new Spell(SpellSlot.E, ChampInfo.E.Range);
            spellE2 = new Spell(SpellSlot.E, ChampInfo.E2.Range);
            spellR = new Spell(SpellSlot.R, ChampInfo.R.Range);

            spellQ.SetSkillshot(
                ChampInfo.Q.Delay,
                ChampInfo.Q.Width,
                ChampInfo.Q.Speed,
                true,
                SkillshotType.SkillshotLine);

            spellE.SetSkillshot(
                ChampInfo.E.Delay,
                ChampInfo.E.Width,
                ChampInfo.E.Speed,
                false,
                SkillshotType.SkillshotCircle);

            igniteSlot = leeHero.GetSpellSlot("summonerdot");
            flashSlot = leeHero.GetSpellSlot("summonerflash");

            Game.OnUpdate += WreckingBallOnUpdate;
            Drawing.OnDraw += WreckingBallOnDraw;
        }

        private static void WreckingBallOnUpdate(EventArgs args)
        {
            if (wbMenu.Item("debug1").GetValue<bool>())
            {
                for (int i = 0; leeHero.InventoryItems.Length > i; i++)
                {
                    Game.PrintChat("Item " + i + ": " + leeHero.InventoryItems[i].IData.Id);
                }

                wbMenu.Item("debug1").SetValue(false);
            }

            if (wbMenu.Item("debug2").GetValue<bool>())
            {
                var foundAny = false;

                foreach (Items.Item item in wjItems)
                {
                    if (Items.HasItem(item.Id))
                    {
                        Game.PrintChat("I have a wardjump item: " + item.Id);
                        foundAny = true;
                    }
                }

                if (!foundAny)
                {
                    Game.PrintChat("No wardjump item found :(");
                }

                wbMenu.Item("debug2").SetValue(false);
            }

            //Select most HP target

            List<Obj_AI_Hero> inRangeHeroes =
                HeroManager.Enemies.Where(
                    x =>
                    x.IsValid && !x.IsDead && x.IsVisible
                    && x.Distance(leeHero.ServerPosition) < wbMenu.Item("firstTargetRange").GetValue<Slider>().Value).ToList();

            rTarget = inRangeHeroes.Any() ? ReturnMostHp(inRangeHeroes) : null;

            //Select less HP target
            if (rTarget != null)
            {
                List<Obj_AI_Hero> secondTargets =
                    HeroManager.Enemies.Where(
                        x =>
                        x.IsValid && !x.IsDead && x.IsVisible && x.NetworkId != rTarget.NetworkId
                        && x.Distance(rTarget.ServerPosition)
                        < wbMenu.Item("secondTargetRange").GetValue<Slider>().Value).ToList();

                rTargetSecond = secondTargets.Any() ? ReturnLessHp(secondTargets) : null;
            }
            else
            {
                rTargetSecond = null;
            }

            if (rTarget != null && rTargetSecond != null && wbMenu.Item("bubbaKey").GetValue<KeyBind>().Active)
            {
                if (!bubbaKushing)
                {
                    bubbaKushing = true;
                }

                BubbaKush();
            }
            else
            {
                if (bubbaKushing)
                {
                    bubbaKushing = false;
                }
            }
        }

        private static Obj_AI_Hero ReturnMostHp(List<Obj_AI_Hero> heroList)
        {
            Obj_AI_Hero mostHp = null;

            foreach (var hero in heroList)
            {
                if (mostHp == null)
                {
                    mostHp = hero;
                }

                if (mostHp.Health < hero.Health)
                {
                    mostHp = hero;
                }
            }

            return mostHp;
        }

        private static Obj_AI_Hero ReturnLessHp(List<Obj_AI_Hero> heroList)
        {
            Obj_AI_Hero lessHp = null;

            foreach (var hero in heroList)
            {
                if (lessHp == null)
                {
                    lessHp = hero;
                }

                if (lessHp.Health > hero.Health)
                {
                    lessHp = hero;
                }
            }

            return lessHp;
        }

        private static void BubbaKush()
        {
            if (spellR.Instance.State == SpellState.NotLearned || !spellR.Instance.IsReady())
            {
                return;
            }

            var flashVector = GetFlashVector();

            var doFlash = wbMenu.Item("useFlash").GetValue<bool>();
            var doWardjump = wbMenu.Item("useWardjump").GetValue<bool>();

            if (leeHero.Distance(flashVector) < 400 && CanWardJump() && doWardjump)
            {
                WardJumpTo(flashVector);
                Utility.DelayAction.Add((int)(GetWardjumpTime(flashVector) * 1000),
                    () =>
                        { spellR.CastOnUnit(rTarget); });
            }
            else if (leeHero.Distance(flashVector) < 425 && leeHero.Distance(rTarget) < ChampInfo.R.Range && flashSlot.IsReady() && doFlash)
            {
                spellR.CastOnUnit(rTarget);

                leeHero.Spellbook.CastSpell(flashSlot, flashVector);
            }
            else
            {
                if (leeHero.Distance(flashVector) > 100)
                {
                    leeHero.IssueOrder(GameObjectOrder.MoveTo, flashVector);
                }
                else
                {
                    spellR.CastOnUnit(rTarget);
                }
            }
        }

        private static void WardJumpTo(Vector3 pos)
        {
            var myWard = Items.GetWardSlot();

            if (Environment.TickCount > lastWjTick + 700)
            {
                lastWjTick = Environment.TickCount;

                Items.UseItem((int)myWard.Id, pos);
            }

            var theWard =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(x => x.Name.ToLower().Contains("ward") && pos.Distance(x.ServerPosition) < 700)
                    .OrderBy(x => x.Distance(pos))
                    .FirstOrDefault();

            if (theWard != null)
            {
                spellW.CastOnUnit(theWard);
            }
        }

        private static bool CanWardJump()
        {
            return spellW.Instance.IsReady() && spellW.Instance.Name.ToLower() == wSpellNames[1].ToLower() && Items.GetWardSlot().IsValidSlot();
        }

        private static float GetWardjumpTime(Vector3 pos)
        {
            var distance = leeHero.Distance(pos);
            var speed = ChampInfo.W.Speed;

            var time = distance / speed;

            return time;
        }

        private static Vector3 GetFlashVector()
        {
            var secondTargetPredPos = Prediction.GetPrediction(
                rTargetSecond,
                GetTimeBetweenTargets() + ChampInfo.R.Delay);

            var fVector = new Vector3(secondTargetPredPos.UnitPosition.ToArray()).Extend(rTarget.ServerPosition, rTarget.Distance(rTargetSecond) + 200);

            return fVector;
        }

        private static float GetTimeBetweenTargets()
        {
            var distance = rTarget.Distance(rTargetSecond);
            var speed = ChampInfo.R.Speed;

            var time = distance / speed;

            return time;
        }

        private static void WreckingBallOnDraw(EventArgs args)
        {
            if (wbMenu.Item("disableAllDraw").GetValue<bool>() || leeHero.IsDead)
            {
                return;
            }

            var heroPos = Drawing.WorldToScreen(leeHero.Position);
            var offsetValue = -30;
            var offsetValueInfo = -50;
            var offSet = new Vector2(heroPos.X, heroPos.Y - offsetValue);
            var offSetInfo = new Vector2(heroPos.X, heroPos.Y - offsetValueInfo);

            var simpleCircles = wbMenu.Item("simpleCircles").GetValue<bool>();

            if (wbMenu.Item("bubbaKey").GetValue<KeyBind>().Active)
            {
                Drawing.DrawText(offSet.X, offSet.Y, wbMenu.Item("textcolor").GetValue<Circle>().Color, "Bubba Kush Active!");
            }
            else
            {
                Drawing.DrawText(offSet.X, offSet.Y, System.Drawing.Color.Red, "Bubba Kush Inactive!");
            }

            if (wbMenu.Item("bestTarget").GetValue<Circle>().Active)
            {
                if (rTarget != null)
                {
                    if (!simpleCircles)
                    {
                        Drawing.DrawCircle(rTarget.Position, 200, wbMenu.Item("bestTarget").GetValue<Circle>().Color);
                    }
                    else
                    {
                        Render.Circle.DrawCircle(
                            rTarget.Position,
                            200,
                            wbMenu.Item("bestTarget").GetValue<Circle>().Color);
                    }
                }
            }

            if (wbMenu.Item("secondTarget").GetValue<Circle>().Active)
            {
                if (rTargetSecond != null)
                {
                    if (!simpleCircles)
                    {
                        Drawing.DrawCircle(
                            rTargetSecond.Position,
                            150,
                            wbMenu.Item("secondTarget").GetValue<Circle>().Color);
                    }
                    else
                    {
                        Render.Circle.DrawCircle(
                            rTargetSecond.Position,
                            150,
                            wbMenu.Item("secondTarget").GetValue<Circle>().Color);
                    }
                }
            }

            if (wbMenu.Item("traceLine").GetValue<Circle>().Active)
            {
                if (rTarget != null && rTargetSecond != null)
                {
                    Drawing.DrawLine(Drawing.WorldToScreen(rTarget.Position), Drawing.WorldToScreen(rTargetSecond.Position), 5, wbMenu.Item("traceLine").GetValue<Circle>().Color);
                }
            }

            if (wbMenu.Item("myRangeDraw").GetValue<Circle>().Active)
            {
                if (!simpleCircles)
                {
                    Drawing.DrawCircle(
                        leeHero.Position,
                        wbMenu.Item("firstTargetRange").GetValue<Slider>().Value,
                        wbMenu.Item("myRangeDraw").GetValue<Circle>().Color);
                }
                else
                {
                    Render.Circle.DrawCircle(
                        leeHero.Position,
                        wbMenu.Item("firstTargetRange").GetValue<Slider>().Value,
                        wbMenu.Item("myRangeDraw").GetValue<Circle>().Color);
                }
            }

            if (wbMenu.Item("rTargetDraw").GetValue<Circle>().Active)
            {
                if (rTarget != null)
                {
                    if (!simpleCircles)
                    {
                        Drawing.DrawCircle(
                            rTarget.Position,
                            wbMenu.Item("secondTargetRange").GetValue<Slider>().Value,
                            wbMenu.Item("rTargetDraw").GetValue<Circle>().Color);
                    }
                    else
                    {
                        Render.Circle.DrawCircle(
                            rTarget.Position,
                            wbMenu.Item("secondTargetRange").GetValue<Slider>().Value,
                            wbMenu.Item("rTargetDraw").GetValue<Circle>().Color);
                    }
                }
            }

            if (bubbaKushing)
            {
                if (spellR.Instance.State == SpellState.NotLearned)
                {
                    Drawing.DrawText(offSetInfo.X, offSetInfo.Y, System.Drawing.Color.AliceBlue, "R is not leveled up yet");
                }
                else
                {
                    if (!spellR.Instance.IsReady())
                    {
                        Drawing.DrawText(offSetInfo.X, offSetInfo.Y, System.Drawing.Color.AliceBlue, "R is not ready yet");
                    }
                }
            }

            if (rTarget != null && rTargetSecond != null)
            {
                if (wbMenu.Item("predVector").GetValue<Circle>().Active)
                {
                    var flashVector = GetFlashVector();

                    Render.Circle.DrawCircle(flashVector, 50, wbMenu.Item("predVector").GetValue<Circle>().Color);
                }
            }
        }

        private static void LoadMenu()
        {
            wbMenu = new Menu("Wrecking Ball (Bubba Kush)", "wreckingball", true);

            var mainMenu = new Menu("Main Settings", "mainsettings");
            mainMenu.AddItem(new MenuItem("bubbaKey", "Bubba Kush Key"))
                .SetValue(new KeyBind(84, KeyBindType.Toggle, false))
                .SetTooltip("Toggle mode");
            mainMenu.AddItem(new MenuItem("useFlash", "Use Flash to get to Kick pos")).SetValue(true);
            mainMenu.AddItem(new MenuItem("useWardjump", "Use Wardjump to get to Kick pos")).SetValue(true);
            mainMenu.AddItem(new MenuItem("useQ", "Use Q skill to gapclose to Kick pos"))
                .SetValue(false)
                .SetTooltip("Not ready yet!")
                .FontColor = Color.Red;
            mainMenu.AddItem(new MenuItem("firstTargetRange", "Range to check for most HP enemy"))
                .SetValue(new Slider(1000, 0, 2000));
            mainMenu.AddItem(new MenuItem("secondTargetRange", "Range to check for second target")).SetValue(new Slider(600, 0, 650));
            wbMenu.AddSubMenu(mainMenu);

            var drawingsMenu = new Menu("Draw Settings", "drawing");
            drawingsMenu.AddItem(new MenuItem("simpleCircles", "Use Simple Circles")).SetValue(true);
            drawingsMenu.AddItem(new MenuItem("textcolor", "Active Text color"))
                .SetValue(new Circle(false, System.Drawing.Color.Lime));
            drawingsMenu.AddItem(new MenuItem("info1", "    ^ Turning button on/off doesn't do anything"));
            drawingsMenu.AddItem(new MenuItem("bestTarget", "Draw Circle around R target"))
                .SetValue(new Circle(false, System.Drawing.Color.Red));
            drawingsMenu.AddItem(new MenuItem("secondTarget", "Draw Circle around second target"))
                .SetValue(new Circle(false, System.Drawing.Color.DeepSkyBlue));
            drawingsMenu.AddItem(new MenuItem("traceLine", "Draw Line from first to second target"))
                .SetValue(new Circle(false, System.Drawing.Color.BlueViolet));
            drawingsMenu.AddItem(new MenuItem("predVector", "Draw predicted Kick Vector"))
                .SetValue(new Circle(false, System.Drawing.Color.Blue));
            drawingsMenu.AddItem(new MenuItem("info2", "Ranges:"));
            drawingsMenu.AddItem(new MenuItem("myRangeDraw", "Draw R target search range"))
                .SetValue(new Circle(false, System.Drawing.Color.Yellow));
            drawingsMenu.AddItem(new MenuItem("rTargetDraw", "Draw second target search range"))
                .SetValue(new Circle(false, System.Drawing.Color.Orange));

            drawingsMenu.AddItem(new MenuItem("disableAllDraw", "Disable all drawings")).SetValue(false);
            wbMenu.AddSubMenu(drawingsMenu);

            var debugMenu = new Menu("Debug", "debugmenu");
            debugMenu.AddItem(new MenuItem("debug1", "Show info 1")).SetValue(false);
            debugMenu.AddItem(new MenuItem("debug2", "Has wardjump item?")).SetValue(false);
            wbMenu.AddSubMenu(debugMenu);

            wbMenu.AddToMainMenu();
        }
    }
}
