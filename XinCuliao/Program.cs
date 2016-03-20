using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace XinCuliao
{
    class Program
    {
        private static Menu semenMenu;

        private static int lastT;

        private static Random myRandom;

        private static readonly string[] mensajeList =
            {
                "OH ME ENCANTA EL PICO GILES CULIAOS IMBECILES DE MIERDA",
                "TU MAMA ENTERA BUENA PAL PICO AWEONAO",
                "Y SI ME CHUPAI LA CORNETA MEJOR XEDDXDXD",
                "A NARUTO LE CORTAN LA TULA PORQUE SE LA METIO MUCHO A TU MAMA HAHAHAHAAJAJJAAJAJAJJAJAJ",
                "VENGAN A GANKEARME MEJOR IMBECILES CULIAOS CHUPENLA",
                "MIJITO VENGA A CHUPARME EL PICO MEJOR AJKASKJDASJKAJKASDJKJKASDJK",
                "SI SUPIERAS LINCE COMO LE TIRO LOS CORTES A TU MAMA POR DEBAJO DE LA MESA ASJJASJAS",
                "TE PILLO TE CULEO QUE BUEN MOMO",
                "ASJDASJJ TE GUSTA POR EL CHICO MARICON ZAMUDIO TE AGARRO A BATAZOS EQURISDAUUXD"
            };

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnLoad;
        }

        private static void OnLoad(EventArgs args)
        {
            semenMenu = new Menu("Chupa el pico", "pene", true);
            semenMenu.AddItem(new MenuItem("sexo", "Chupa el sexo")).SetValue(new KeyBind("p".ToCharArray()[0], KeyBindType.Press));
            semenMenu.AddItem(new MenuItem("sexoanal", "PICO")).SetValue(false);
            semenMenu.AddItem(new MenuItem("patodos", "Decirlo pa todos los culiaos?")).SetValue(true);
            semenMenu.AddToMainMenu();

            myRandom = new Random(DateTime.Now.Millisecond);

            Game.OnUpdate += Game_OnUpdate;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (semenMenu.Item("sexo").GetValue<KeyBind>().Active)
            {
                if (Environment.TickCount > lastT + 1250)
                {
                    var elegir = myRandom.Next(0, mensajeList.Length);

                    if (semenMenu.Item("patodos").GetValue<bool>())
                    {
                        Game.Say("/all " + mensajeList[elegir]);
                    }
                    else
                    {
                        Game.Say(mensajeList[elegir]);
                    }

                    lastT = Environment.TickCount;
                }
            }

            if (semenMenu.Item("sexoanal").GetValue<bool>())
            {
                if (Environment.TickCount > lastT + 1000)
                {
                    Game.Say("/all PICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICOPICO");
                    lastT = Environment.TickCount;
                }
            }
        }
    }
}
