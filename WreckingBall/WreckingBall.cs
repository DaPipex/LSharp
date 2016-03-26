using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;

using CommonCollision = LeagueSharp.Common.Collision;

using SharpDX;

namespace WreckingBall
{
    internal class WreckingBall
    {

        private enum PriorityMode
        {
            Wardjump = 0,
            Flash = 1,
        }

        private const string ChampName = "LeeSin";

        private const string Version = "1.7.5";

        private static Obj_AI_Hero leeHero;

        private static Obj_AI_Hero rTarget;

        private static Obj_AI_Hero rTargetSecond;

        private static Obj_AI_Minion mostRecentWard;

        private static Spell spellQ, spellQ2, spellW, spellW2, spellE, spellE2, spellR;

        private static SpellSlot igniteSlot, flashSlot;

        private static Menu wbMenu;

        private static readonly string[] QSpellNames = { string.Empty, "BlindMonkQOne", "BlindMonkQTwo" };

        private static readonly string[] WSpellNames = { string.Empty, "BlindMonkWOne", "BlindMonkWTwo" };

        private static readonly string[] ESpellNames = { string.Empty, "BlindMonkEOne", "BlindMonkETwo" };

        private static readonly string RSpellName = "BlindMonkRKick";

        private static int lastWjTick;

        private static int lastWjTickMenu;

        private static int lastFlashTick;

        private static int lastFlashTickMenu;

        private static int lastSwitchT;

        private static int distTargetKickPos;

        private static int distLeeKickPos;

        private static int distLeeToWardjump;

        private static int finishWardJumpTimes = 0;

        private static bool bubbaKushing;

        private static int mainMode;

        private static PriorityMode bubbaPriorityMode;


        private static readonly Items.Item[] WjItems =
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

            bubbaPriorityMode = (PriorityMode)wbMenu.Item("modePrio").GetValue<StringList>().SelectedIndex;

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
            GameObject.OnCreate += WreckingBallOnCreate;
        }

        private static void WreckingBallOnUpdate(EventArgs args)
        {
            //Select most HP target

            List<Obj_AI_Hero> inRangeHeroes =
                HeroManager.Enemies.Where(
                    x =>
                    x.IsValid && !x.IsDead && x.IsVisible
                    && x.Distance(leeHero.ServerPosition) < wbMenu.Item("firstTargetRange").GetValue<Slider>().Value).ToList();

            rTarget = inRangeHeroes.Any() ? (mainMode == 0 ? ReturnMostHp(inRangeHeroes) : ReturnClosest(inRangeHeroes)) : null;

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

            switch (wbMenu.Item("modePrio").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    if (bubbaPriorityMode == PriorityMode.Flash)
                    {
                        bubbaPriorityMode = PriorityMode.Wardjump;
                    }
                    break;
                case 1:
                    if (bubbaPriorityMode == PriorityMode.Wardjump)
                    {
                        bubbaPriorityMode = PriorityMode.Flash;
                    }
                    break;
            }

            //Get some menu values if they change
            if (distTargetKickPos != wbMenu.Item("distanceToKick").GetValue<Slider>().Value)
            {
                distTargetKickPos = wbMenu.Item("distanceToKick").GetValue<Slider>().Value;
            }

            if (distLeeKickPos != wbMenu.Item("distanceLeeKick").GetValue<Slider>().Value)
            {
                distLeeKickPos = wbMenu.Item("distanceLeeKick").GetValue<Slider>().Value;
            }

            if (distLeeToWardjump != wbMenu.Item("distanceToWardjump").GetValue<Slider>().Value)
            {
                distLeeToWardjump = wbMenu.Item("distanceToWardjump").GetValue<Slider>().Value;
            }

            if (mainMode != wbMenu.Item("mainMode").GetValue<StringList>().SelectedIndex)
            {
                mainMode = wbMenu.Item("mainMode").GetValue<StringList>().SelectedIndex;
            }

            if (wbMenu.Item("switchKey").GetValue<KeyBind>().Active && Environment.TickCount > lastSwitchT + 450)
            {
                switch (wbMenu.Item("mainMode").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        wbMenu.Item("mainMode")
                            .SetValue(new StringList(new[] { "Most MaxHP >> Less HP", "Closest >> Less HP" }, 1));
                        lastSwitchT = Environment.TickCount;
                        break;
                    case 1:
                        wbMenu.Item("mainMode")
                            .SetValue(new StringList(new[] { "Most MaxHP >> Less HP", "Closest >> Less HP" }, 0));
                        lastSwitchT = Environment.TickCount;
                        break;
                }
            }

            /*if (wbMenu.Item("debug3").GetValue<KeyBind>().Active)
            {
                WardJumpTo(Game.CursorPos);
            }*/
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

                if (mostHp.MaxHealth < hero.MaxHealth)
                {
                    mostHp = hero;
                }
            }

            return mostHp;
        }

        private static Obj_AI_Hero ReturnClosest(List<Obj_AI_Hero> herolist)
        {
            Obj_AI_Hero closest = null;

            foreach (var hero in herolist)
            {
                if (closest == null)
                {
                    closest = hero;
                }

                if (closest.Distance(leeHero) > hero.Distance(leeHero))
                {
                    closest = hero;
                }
            }

            return closest;
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
            var gpUnit = GetClosestDirectEnemyUnitToPos(flashVector);

            var doFlash = wbMenu.Item("useFlash").GetValue<bool>();
            var doWardjump = wbMenu.Item("useWardjump").GetValue<bool>();
            var doQ = wbMenu.Item("useQ").GetValue<bool>();

            var qPredHc = wbMenu.Item("useQpred").GetValue<StringList>().SelectedIndex + 4;

            if (doQ)
            {
                if (leeHero.Distance(flashVector) > 425 && leeHero.Distance(flashVector) < ChampInfo.Q.Range + distLeeToWardjump
                    && CanQ1())
                {
                    if (gpUnit != null)
                    {
                        var pred = spellQ.GetPrediction(gpUnit);

                        if (pred.Hitchance >= (HitChance)qPredHc)
                        {
                            spellQ.Cast(pred.CastPosition);
                        }
                    }
                }

                if (spellQ2.Instance.IsReady() && spellQ2.Instance.Name.ToLower() == QSpellNames[2].ToLower())
                {
                    spellQ2.Cast();
                }
            }

            if (bubbaPriorityMode == PriorityMode.Wardjump)
            {
                if (leeHero.Distance(flashVector) < distLeeToWardjump && CanWardJump() && doWardjump)
                {
                    WardJumpTo(flashVector);
                }
                else if (leeHero.Distance(flashVector) < 425 && leeHero.Distance(rTarget) < ChampInfo.R.Range
                         && flashSlot.IsReady() && doFlash && Environment.TickCount > lastWjTickMenu + 2000)
                {
                    var castedR = spellR.CastOnUnit(rTarget);

                    if (castedR)
                    {
                        leeHero.Spellbook.CastSpell(flashSlot, flashVector);
                    }
                }
                else
                {
                    if (leeHero.Distance(flashVector) > distLeeKickPos)
                    {
                        if (wbMenu.Item("moveItself").GetValue<bool>())
                        {
                            leeHero.IssueOrder(
                                GameObjectOrder.MoveTo,
                                wbMenu.Item("moveItselfMode").GetValue<StringList>().SelectedIndex == 0
                                    ? flashVector
                                    : Game.CursorPos);
                        }
                    }
                    else
                    {
                        spellR.CastOnUnit(rTarget);
                    }
                }
            }
            else if (bubbaPriorityMode == PriorityMode.Flash)
            {
                if (leeHero.Distance(flashVector) < 425 && leeHero.Distance(rTarget) < ChampInfo.R.Range
                    && flashSlot.IsReady() && doFlash)
                {
                    var castedR = spellR.CastOnUnit(rTarget);

                    if (castedR)
                    {
                        leeHero.Spellbook.CastSpell(flashSlot, flashVector);
                        lastFlashTick = Environment.TickCount;
                    }
                }
                else if (leeHero.Distance(flashVector) < distLeeToWardjump && CanWardJump() && doWardjump
                         && Environment.TickCount > lastFlashTickMenu + 2000)
                {
                    WardJumpTo(flashVector);
                }
                else
                {
                    if (leeHero.Distance(flashVector) > distLeeKickPos)
                    {
                        if (wbMenu.Item("moveItself").GetValue<bool>())
                        {
                            leeHero.IssueOrder(
                                GameObjectOrder.MoveTo,
                                wbMenu.Item("moveItselfMode").GetValue<StringList>().SelectedIndex == 0
                                    ? flashVector
                                    : Game.CursorPos);
                        }
                    }
                    else
                    {
                        spellR.CastOnUnit(rTarget);
                    }
                }
            }
        }

        private static Obj_AI_Base GetClosestDirectEnemyUnitToPos(Vector3 pos)
        {
            List<Obj_AI_Base> possibleHeroes =
                HeroManager.Enemies.Where(x => x.IsValidTarget() && pos.Distance(x.ServerPosition) < distLeeToWardjump && x.Health > spellQ.GetDamage(x)).ToList().ConvertAll(x => (Obj_AI_Base)x);

            List<Obj_AI_Base> possibleMinions =
                MinionManager.GetMinions(pos, distLeeToWardjump).Where(x => x.Health > spellQ.GetDamage(x)).ToList();

            List<Obj_AI_Base> allPossible = possibleHeroes.Concat(possibleMinions).ToList();

            allPossible = allPossible.OrderBy(unit => unit.Distance(pos)).ToList();

            Obj_AI_Base bestUnit = null;

            foreach (var candidate in allPossible)
            {
                var collisionList = new List<Vector3> { leeHero.ServerPosition, candidate.ServerPosition };

                var predInput = new PredictionInput();
                predInput.Speed = ChampInfo.Q.Speed;
                predInput.Range = ChampInfo.Q.Range;
                predInput.Delay = ChampInfo.Q.Delay;
                predInput.Radius = ChampInfo.Q.Width;
                predInput.Collision = true;
                predInput.Type = SkillshotType.SkillshotLine;
                predInput.CollisionObjects = new[]
                                                 {
                                                     CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                                     CollisionableObjects.YasuoWall
                                                 };

                var collInput = CommonCollision.GetCollision(collisionList, predInput);

                var realCollList = new List<Obj_AI_Base>();

                if (collInput.Any())
                {
                    realCollList.AddRange(collInput.Where(unit => unit.NetworkId != leeHero.NetworkId && unit.NetworkId != candidate.NetworkId));
                }

                if (realCollList.Any())
                {
                    continue;
                }

                bestUnit = candidate;
                break;
            }

            return bestUnit;
        }

        private static void WardJumpTo(Vector3 pos)
        {
            var myWard = Items.GetWardSlot();

            if (Environment.TickCount > lastWjTick + 200)
            {
                if (Items.UseItem((int)myWard.Id, pos))
                {
                    lastWjTick = Environment.TickCount;

                    Utility.DelayAction.Add(
                        80,
                        () =>
                            {
                                spellW.CastOnUnit(mostRecentWard);
                            });
                }
            }
        }

        private static void WreckingBallOnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Name.ToLower().Contains("ward") && !(sender is Obj_GeneralParticleEmitter))
            {
                var ward = (Obj_AI_Base)sender;

                //Game.PrintChat(ward.Buffs[0].SourceName);

                Utility.DelayAction.Add(
                    5,
                    () =>
                        {
                            if (ward.Buffs.Any(x => x.SourceName == "Lee Sin"))
                            {
                                mostRecentWard = (Obj_AI_Minion)ward;
                            }
                        });
            }
        }

        private static bool CanWardJump()
        {
            return spellW.Instance.IsReady() && spellW.Instance.Name.ToLower() == WSpellNames[1].ToLower() && Items.GetWardSlot().IsValidSlot();
        }

        private static bool CanQ1()
        {
            return spellQ.Instance.IsReady() && spellQ.Instance.Name.ToLower() == QSpellNames[1].ToLower();
        }

        /*private static float GetWardjumpTime(Vector3 pos)
        {
            var distance = leeHero.Distance(pos);
            var speed = ChampInfo.W.Speed;

            var time = distance / speed;

            return time;
        }*/

        private static Vector3 GetFlashVector(bool forDraws = false)
        {
            var secondTargetPredPos = Prediction.GetPrediction(
                rTargetSecond,
                GetTimeBetweenTargets() + ChampInfo.R.Delay);

            var fVector = new Vector3(secondTargetPredPos.UnitPosition.ToArray()).Extend(rTarget.ServerPosition, rTarget.Distance(secondTargetPredPos.UnitPosition) + distTargetKickPos);
            
            var fVectorDraw = new Vector3(secondTargetPredPos.UnitPosition.ToArray()).Extend(rTarget.Position, rTarget.Distance(secondTargetPredPos.UnitPosition) + distTargetKickPos);

            return !forDraws ? fVector : fVectorDraw;
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
            var textDimension = Drawing.GetTextExtent("Bubba Kush Active!");
            const int OffsetValue = -30;
            const int OffsetValueInfo = -50;
            var offSet = new Vector2(heroPos.X, heroPos.Y - OffsetValue);
            var offSetInfo = new Vector2(heroPos.X, heroPos.Y - OffsetValueInfo);

            var simpleCircles = wbMenu.Item("simpleCircles").GetValue<bool>();

            if (wbMenu.Item("bubbaKey").GetValue<KeyBind>().Active)
            {
                Drawing.DrawText(offSet.X - textDimension.Width / 2f, offSet.Y, wbMenu.Item("textcolor").GetValue<Circle>().Color, "Bubba Kush Active!");
            }
            else
            {
                Drawing.DrawText(offSet.X - textDimension.Width / 2f, offSet.Y, System.Drawing.Color.Red, "Bubba Kush Inactive!");
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
                    var flashVector = GetFlashVector(true);

                    Render.Circle.DrawCircle(flashVector, 50, wbMenu.Item("predVector").GetValue<Circle>().Color);
                }
            }
        }

        private static void LoadMenu()
        {
            wbMenu = new Menu("Wrecking Ball (Bubba Kush)", "wreckingball", true);
            wbMenu.AddItem(new MenuItem("info4", "Version: " + Version));

            var mainMenu = new Menu("Main Settings", "mainsettings");
            mainMenu.AddItem(new MenuItem("bubbaKey", "Bubba Kush Key"))
                .SetValue(new KeyBind(84, KeyBindType.Toggle, false))
                .SetTooltip("Toggle mode");
            mainMenu.AddItem(new MenuItem("mainMode", "Bubba Kush Mode"))
                .SetValue(new StringList(new[] { "Most MaxHP >> Less HP", "Closest >> Less HP" }));
            mainMenu.AddItem(new MenuItem("switchKey", "Switch mode Key")).SetValue(new KeyBind(90, KeyBindType.Press));
            mainMenu.AddItem(new MenuItem("useFlash", "Use Flash to get to Kick pos")).SetValue(true);
            mainMenu.AddItem(new MenuItem("useWardjump", "Use Wardjump to get to Kick pos")).SetValue(true);
            mainMenu.AddItem(new MenuItem("modePrio", "Kick pos method priority:"))
                .SetValue(new StringList(new[] { "Wardjump", "Flash" }));
            mainMenu.AddItem(new MenuItem("useQ", "Use Q skill to gapclose to Kick pos"))
                .SetValue(false);
            mainMenu.AddItem(new MenuItem("useQpred", "Q Min Prediction"))
                .SetValue(new StringList(new[] { "Medium", "High", "Very High" }));
            mainMenu.AddItem(new MenuItem("moveItself", "Move to Kick pos...")).SetValue(true);
            mainMenu.AddItem(new MenuItem("moveItselfMode", "If ^ true then move:"))
                .SetValue(new StringList(new[] { "Automatically", "Manually" }, 1));
            mainMenu.AddItem(new MenuItem("firstTargetRange", "Range to check for most HP enemy"))
                .SetValue(new Slider(1000, 0, 2000));
            mainMenu.AddItem(new MenuItem("secondTargetRange", "Range to check for second target")).SetValue(new Slider(600, 0, 650));

            var extraMenu = new Menu("Extra Settings", "extrasettings");
            extraMenu.AddItem(new MenuItem("info3", "Adjust if needed"));
            extraMenu.AddItem(new MenuItem("distanceToKick", "Distance from first target to Kick Pos"))
                .SetValue(new Slider(150, 100, 200));
            extraMenu.AddItem(new MenuItem("distanceLeeKick", "Min distance from Lee to Kick Pos"))
                .SetValue(new Slider(75, 10, 150));
            extraMenu.AddItem(new MenuItem("distanceToWardjump", "Min distance to Kick Pos for Wardjump"))
                .SetValue(new Slider(400, 300, 650));

            mainMenu.AddSubMenu(extraMenu);
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

            /*var debugMenu = new Menu("Debug", "debugmenu");
            debugMenu.AddItem(new MenuItem("debug1", "Show info 1")).SetValue(false);
            debugMenu.AddItem(new MenuItem("debug2", "Has wardjump item?")).SetValue(false);
            debugMenu.AddItem(new MenuItem("debug3", "Wardjump to mouse test"))
                .SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press));
            wbMenu.AddSubMenu(debugMenu);*/

            wbMenu.AddToMainMenu();
        }
    }
}
