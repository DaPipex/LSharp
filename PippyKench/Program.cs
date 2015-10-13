using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace PippyKench
{
    class Program
    {

        //Assembly Info
        private const string champName = "TahmKench";
        private const Color pippyKenchColor = Color.FromArgb(225, 172, 50);
        private const string thirdStrikeBuff = "InsertBuffHere";

        //Champ Info
        private static Spell theQ, theW, theE;
        private static Items.Item tiamat_item, ravenous_item, titanic_item, youmuu_item, hourglass_item, bc_item, ruined_item;
        private static SpellSlot igniteSlot;
        private static Menu TammyMenu;
        private static Orbwalking.Orbwalker vaginaOrb;
        private static Obj_AI_Hero _target;

        //Champ Spell Info
        private static Dictionary<string, float> spellInfo;


        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += TammyLoad;
        }
    }
}
