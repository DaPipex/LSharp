using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace PippyDeveloper
{
    class DeveloperInfo
    {

        private static Menu DevMenu;
        private static Obj_AI_Hero meLulz;
        private static int _lastCastedTime = 0;
        //private static float _lastCastedResult = 0f;

        public static void DeveloperLoad(EventArgs args)
        {
            meLulz = ObjectManager.Player;

            DeveloperMenu();

            Game.OnUpdate += DeveloperUpdate;
            Obj_AI_Base.OnProcessSpellCast += DeveloperProcessSpell;
            GameObject.OnCreate += DeveloperObjectCreate;
            Drawing.OnDraw += DeveloperDraw;
            //Obj_AI_Base.OnBuffAdd += DeveloperBuffAdd;
            //Obj_AI_Base.OnBuffUpdateCount += DeveloperBuffUpdate;
            //Obj_AI_Base.OnBuffRemove += DeveloperBuffRemove;
        }

        private static void DeveloperMenu()
        {
            DevMenu = new Menu("Developer Menu", "devmenu", true);

            DevMenu.AddItem(new MenuItem("champInfo", "Show Champ Info")).SetValue(false);
            DevMenu.AddItem(new MenuItem("qInfo", "Show Q Info")).SetValue(false);
            //DevMenu.AddItem(new MenuItem("qRange", "Draw Q Range")).SetValue(new Circle(false, System.Drawing.Color.Red, SpellData.GetSpellData(0).CastRange == 0
            //    ? SpellData.GetSpellData(0).CastRangeDisplayOverride : SpellData.GetSpellData(0).CastRange));
            DevMenu.AddItem(new MenuItem("wInfo", "Show W Info")).SetValue(false);
            //DevMenu.AddItem(new MenuItem("wRange", "Draw W Range")).SetValue(new Circle(false, System.Drawing.Color.Red, SpellData.GetSpellData(1).CastRange == 0
            //    ? SpellData.GetSpellData(1).CastRangeDisplayOverride : SpellData.GetSpellData(1).CastRange));
            DevMenu.AddItem(new MenuItem("eInfo", "Show E Info")).SetValue(false);
            //DevMenu.AddItem(new MenuItem("eRange", "Draw E Range")).SetValue(new Circle(false, System.Drawing.Color.Red, SpellData.GetSpellData(2).CastRange == 0
            //    ? SpellData.GetSpellData(2).CastRangeDisplayOverride : SpellData.GetSpellData(2).CastRange));
            DevMenu.AddItem(new MenuItem("rInfo", "Show R Info")).SetValue(false);
            //DevMenu.AddItem(new MenuItem("rRange", "Draw R Range")).SetValue(new Circle(false, System.Drawing.Color.Red, SpellData.GetSpellData(3).CastRange == 0
            //   ? SpellData.GetSpellData(3).CastRangeDisplayOverride : SpellData.GetSpellData(3).CastRange));
            //DevMenu.AddItem(new MenuItem("spellCastInfo", "Show Spell Info on Cast")).SetValue(true);
            DevMenu.AddItem(new MenuItem("rangeDraw", "Draw Custom Circle")).SetValue(new Circle(false, System.Drawing.Color.PaleVioletRed));
            DevMenu.AddItem(new MenuItem("rangeDrawSlider", "--> Range Slider")).SetValue(new Slider(200, 0, 2500));

            DevMenu.AddItem(new MenuItem("separator1", "-----------------"));

            DevMenu.AddItem(new MenuItem("buffMeCreate", "Buff me - Create")).SetValue(true);
            DevMenu.AddItem(new MenuItem("buffMeUpdate", "Buff me - Update")).SetValue(true);
            DevMenu.AddItem(new MenuItem("buffMeDelete", "Buff me - Delete")).SetValue(true);

            DevMenu.AddItem(new MenuItem("separator2", "-----------------"));

            DevMenu.AddItem(new MenuItem("buffAllyCreate", "Buff ally - Create")).SetValue(false);
            DevMenu.AddItem(new MenuItem("buffAllyUpdate", "Buff ally - Update")).SetValue(false);
            DevMenu.AddItem(new MenuItem("buffAllyDelete", "Buff ally - Delete")).SetValue(false);

            DevMenu.AddItem(new MenuItem("separator3", "-----------------"));

            DevMenu.AddItem(new MenuItem("buffEnemyCreate", "Buff enemy - Create")).SetValue(false);
            DevMenu.AddItem(new MenuItem("buffEnemyUpdate", "Buff enemy - Update")).SetValue(false);
            DevMenu.AddItem(new MenuItem("buffEnemyDelete", "Buff enemy - Delete")).SetValue(false);

            //DevMenu.AddItem(new MenuItem("separator4", "-----------------"));

            //DevMenu.AddItem(new MenuItem("timeInfo", "Last Cast Time Info")).SetValue(false);

            DevMenu.AddToMainMenu();
        }

        private static void DeveloperUpdate(EventArgs args)
        {

            var spellQInfo = meLulz.Spellbook.GetSpell(SpellSlot.Q);
            var spellWInfo = meLulz.Spellbook.GetSpell(SpellSlot.W);
            var spellEInfo = meLulz.Spellbook.GetSpell(SpellSlot.E);
            var spellRInfo = meLulz.Spellbook.GetSpell(SpellSlot.R);

            if (DevMenu.Item("champInfo").GetValue<bool>())
            {
                Game.PrintChat("Champion Name: " + meLulz.ChampionName);
                Game.PrintChat("Auto Attack Range: " + meLulz.AttackRange.ToString());
                foreach (var buff in meLulz.Buffs)
                {
                    Game.PrintChat("I have buff: " + buff.Name);
                }

                DevMenu.Item("champInfo").SetValue(false);
            }

            if (DevMenu.Item("qInfo").GetValue<bool>())
            {
                Game.PrintChat("Q Spell Name: " + spellQInfo.Name);
                Game.PrintChat("Q Spell State: " + spellQInfo.State);
                Game.PrintChat("Q Spell Range: " + spellQInfo.SData.CastRange);
                Game.PrintChat("Q Spell Range Display Override: " + spellQInfo.SData.CastRangeDisplayOverride);
                Game.PrintChat("Q Spell Range Array: " + spellQInfo.SData.CastRangeArray[0]);
                Game.PrintChat("Q Spell Speed: " + spellQInfo.SData.MissileSpeed);
                Game.PrintChat("Q Spell Delay: " + (spellQInfo.SData.SpellCastTime) / 1000);
                Game.PrintChat("Q Spell Delay (Override): " + spellQInfo.SData.OverrideCastTime);
                Game.PrintChat("Q Spell Width (Line): " + spellQInfo.SData.LineWidth);
                Game.PrintChat("Q Spell Width (Radius): " + spellQInfo.SData.CastRadius);
                Game.PrintChat("Q Spell Channel Duration: " + spellQInfo.SData.ChannelDuration);


                DevMenu.Item("qInfo").SetValue(false);
            }

            if (DevMenu.Item("wInfo").GetValue<bool>())
            {
                Game.PrintChat("W Spell Name: " + spellWInfo.Name);
                Game.PrintChat("W Spell State: " + spellWInfo.State);
                Game.PrintChat("W Spell Range: " + spellWInfo.SData.CastRange);
                Game.PrintChat("W Spell Range Display Override: " + spellWInfo.SData.CastRangeDisplayOverride);
                Game.PrintChat("W Spell Range Array: " + spellWInfo.SData.CastRangeArray[0]);
                Game.PrintChat("W Spell Speed: " + spellWInfo.SData.MissileSpeed);
                Game.PrintChat("W Spell Delay: " + (spellWInfo.SData.SpellCastTime) / 1000);
                Game.PrintChat("W Spell Delay (Override): " + spellWInfo.SData.OverrideCastTime);
                Game.PrintChat("W Spell Width (Line): " + spellWInfo.SData.LineWidth);
                Game.PrintChat("W Spell Width (Radius): " + spellWInfo.SData.CastRadius);
                Game.PrintChat("W Spell Channel Duration: " + spellWInfo.SData.ChannelDuration);

                DevMenu.Item("wInfo").SetValue(false);
            }

            if (DevMenu.Item("eInfo").GetValue<bool>())
            {
                Game.PrintChat("E Spell Name: " + spellEInfo.Name);
                Game.PrintChat("E Spell State: " + spellEInfo.State);
                Game.PrintChat("E Spell Range: " + spellEInfo.SData.CastRange);
                Game.PrintChat("E Spell Range Display Override: " + spellEInfo.SData.CastRangeDisplayOverride);
                Game.PrintChat("E Spell Range Array: " + spellEInfo.SData.CastRangeArray[0]);
                Game.PrintChat("E Spell Speed: " + spellEInfo.SData.MissileSpeed);
                Game.PrintChat("E Spell Delay: " + (spellEInfo.SData.SpellCastTime) / 1000);
                Game.PrintChat("E Spell Delay (Override): " + spellEInfo.SData.OverrideCastTime);
                Game.PrintChat("E Spell Width (Line): " + spellEInfo.SData.LineWidth);
                Game.PrintChat("E Spell Width (Radius): " + spellEInfo.SData.CastRadius);
                Game.PrintChat("E Spell Channel Duration: " + spellEInfo.SData.ChannelDuration);

                DevMenu.Item("eInfo").SetValue(false);
            }

            if (DevMenu.Item("rInfo").GetValue<bool>())
            {
                Game.PrintChat("R Spell Name: " + spellRInfo.Name);
                Game.PrintChat("R Spell State: " + spellRInfo.State);
                Game.PrintChat("R Spell Range: " + spellRInfo.SData.CastRange);
                Game.PrintChat("R Spell Range Display Override: " + spellRInfo.SData.CastRangeDisplayOverride);
                Game.PrintChat("R Spell Range Array: " + spellRInfo.SData.CastRangeArray[0]);
                Game.PrintChat("R Spell Speed: " + spellRInfo.SData.MissileSpeed);
                Game.PrintChat("R Spell Delay: " + spellRInfo.SData.SpellCastTime);
                Game.PrintChat("R Spell Delay (Override): " + spellRInfo.SData.OverrideCastTime);
                Game.PrintChat("R Spell Delay (Backup): " + (spellRInfo.SData.CastFrame / 30));
                Game.PrintChat("R Spell Width (Line): " + spellRInfo.SData.LineWidth);
                Game.PrintChat("R Spell Width (Radius): " + spellRInfo.SData.CastRadius);
                Game.PrintChat("R Spell Channel Duration: " + spellRInfo.SData.ChannelDuration);

                DevMenu.Item("rInfo").SetValue(false);
            }
        }

        private static void DeveloperProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }

             _lastCastedTime = Utils.GameTimeTickCount;
        }

        private static void DeveloperObjectCreate(GameObject sender, EventArgs args)
        {
            var missile = (MissileClient)sender;

            Game.PrintChat(missile.SData.Name);

            if (_lastCastedTime != 0)
            {
                if (missile.SData.Name == "namiqmissile" && missile.Position != missile.StartPosition)
                {
                    Game.PrintChat("Q Delay: " + (Utils.GameTimeTickCount - _lastCastedTime).ToString());
                    _lastCastedTime = 0;
                }

                if (missile.SData.Name == "NamiRMissile" && missile.Position != missile.StartPosition)
                {
                    Game.PrintChat("R Delay: " + (Utils.GameTimeTickCount - _lastCastedTime).ToString());
                    _lastCastedTime = 0;
                }
            }
        }

        private static void DeveloperDraw(EventArgs args)
        {
            var circleStuff = DevMenu.Item("rangeDraw").GetValue<Circle>();

            if (circleStuff.Active)
            {
                Render.Circle.DrawCircle(meLulz.Position, DevMenu.Item("rangeDrawSlider").GetValue<Slider>().Value, circleStuff.Color);
            }
        }
    }
}
