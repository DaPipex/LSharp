using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace PipexUtils
{
    class Program
    {
        private static Menu myMenu;

        private static int lastPingMeT;
        private static int lastFaggotT;
        private static int lastPingAll;

        private static void ShowInfo(Obj_AI_Hero hero, bool troll = false)
        {
            if (!troll)
            {
                Game.Say("Datos de: " + hero.Name);
                Game.Say("Kills: " + hero.ChampionsKilled);
                Game.Say("Deaths: " + hero.Deaths);
                Game.Say("Assists: " + hero.Assists);
                Game.Say("Muertes del equipo: " + GetDeathsInTeam(hero) + "%");
                Game.Say("Contribucion en el equipo: " + GetContribToTeam(hero) + "%");
                Game.Say("Desempeno Personal: " + GetPerformance(hero) + " puntos");
            }
            else
            {
                Game.Say("Datos (del maricon zamudio) de: " + hero.Name);
                Game.Say("Kills: " + new Random().Next(-110, -54));
                Game.Say("Deaths: " + new Random(DateTime.Now.Millisecond * 2).Next(500, 851));
                Game.Say("Assists: " + 0);
                Game.Say("Contribucion en el equipo: Una real mierda");
                Game.Say("Desempeno Personal: " + -9001 + "%");
            }
        }

        private static float GetContribToTeam(Obj_AI_Hero hero)
        {
            float totalTeamKills = HeroManager.AllHeroes.Where(champ => champ.Team == hero.Team).Sum(champ => champ.ChampionsKilled);

            float myTotal = hero.ChampionsKilled + hero.Assists;

            float percentage = myTotal * 100 / totalTeamKills;

            return (float)Math.Round(percentage, 2);
        }

        private static float GetPerformance(Obj_AI_Hero hero)
        {
            var result = (hero.ChampionsKilled + hero.Assists) / (hero.Deaths == 0 ? 1f : hero.Deaths);

            result = result * 100;

            return (float)Math.Round(result, 2);
        }

        private static float GetDeathsInTeam(Obj_AI_Hero hero)
        {
            float totalTeamDeaths = HeroManager.AllHeroes.Where(champ => champ.Team == hero.Team).Sum(champ => champ.Deaths);

            float myTotal = hero.Deaths;

            float percentage = myTotal * 100 / totalTeamDeaths;

            return (float)Math.Round(percentage, 2);
        }

        private static int ActiveFaggot()
        {
            for (var i = 0; i < 5; i++)
            {
                if (myMenu.Item("faggot" + i).GetValue<bool>())
                {
                    return i;
                }
            }

            return -1;
        }

        private static int ActiveMalo()
        {
            for (var i = 0; i < 5; i++)
            {
                if (myMenu.Item("malo" + i).GetValue<bool>())
                {
                    return i;
                }
            }

            return -1;
        }

        private static void SpamPing(Obj_AI_Hero hero)
        {
            if (Environment.TickCount > lastFaggotT + (myMenu.Item("faggotQuick").GetValue<bool>() ? 500 : 3000))
            {
                var pingToSpam = myMenu.Item("faggotShowMe").GetValue<bool>() ? PingCategory.EnemyMissing : PingCategory.Normal;

                Game.SendPing(pingToSpam, hero);

                lastFaggotT = Environment.TickCount;
            }
        }

        private static void PingPerson(Obj_AI_Hero hero)
        {
            if (Environment.TickCount > lastPingMeT + 1500)
            {
                var radius = new Random().Next(300, 1101);
                const int Count = 5;
                var point = hero.ServerPosition;
                var constant = Math.PI / 2 - Math.PI / Count;

                Game.SendPing(PingCategory.Normal, hero);

                for (var i = 0; i < Count; i++)
                {
                    var v = new Vector2
                    {
                        X = (float)(point.X + radius * Math.Cos(i * 2 * Math.PI / Count + constant)),
                        Y = (float)(point.Y + radius * Math.Sin(i * 2 * Math.PI / Count + constant))
                    };

                    Utility.DelayAction.Add(100, () => Game.SendPing(PingCategory.Normal, v));
                }

                lastPingMeT = Environment.TickCount;
            }
        }

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            myMenu = new Menu("Pipex Utilities", "pipexutils", true);

            var faggotMenu = new Menu("Faggot Ping", "faggotmenu");
            for (var i = 0; i < 5; i++)
            {
                faggotMenu.AddItem(new MenuItem("faggot" + i, HeroManager.Allies[i].ChampionName)).SetValue(false);
            }

            faggotMenu.AddItem(new MenuItem("faggotShowMe", "Show me?")).SetValue(false);
            faggotMenu.AddItem(new MenuItem("faggotQuick", "Quick Spam?")).SetValue(false);
            myMenu.AddSubMenu(faggotMenu);

            var maloMenu = new Menu("Malo culiao", "maloculiao");
            for (var i = 0; i < 5; i++)
            {
                maloMenu.AddItem(new MenuItem("malo" + i, HeroManager.Allies[i].ChampionName)).SetValue(false);
            }
            myMenu.AddSubMenu(maloMenu);

            myMenu.AddItem(new MenuItem("mainCheck", "Enable?")).SetValue(false);
            myMenu.AddItem(new MenuItem("xdPingReq", "Ping on request")).SetValue(false);
            myMenu.AddItem(new MenuItem("xdStats", "Info")).SetValue(false);
            myMenu.AddItem(new MenuItem("pingAll", "Ping everyone consistently")).SetValue(false);

            myMenu.AddItem(new MenuItem("sellAll", "Sell all")).SetValue(false).SetFontStyle(System.Drawing.FontStyle.Bold, Color.Red);
            myMenu.AddItem(new MenuItem("buyAll", "Buy all of first item")).SetValue(false).SetFontStyle(System.Drawing.FontStyle.Bold, Color.Cyan);

            myMenu.AddToMainMenu();

            Game.OnChat += Game_OnChat;
            Game.OnUpdate += Game_OnUpdate;
            Game.OnNotify += Game_OnNotify;
        }

        private static void Game_OnNotify(GameNotifyEventArgs args)
        {
            if (myMenu.Item("mainCheck").GetValue<bool>())
            {
                if (ActiveMalo() != -1)
                {
                    var targetMalo = HeroManager.Allies[ActiveMalo()];

                    if (args.NetworkId == targetMalo.NetworkId && args.EventId == GameEventId.OnChampionKill)
                    {
                        Game.Say("Ya murio el malo culiao de " + targetMalo.ChampionName + " :v");
                    }
                }
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (myMenu.Item("mainCheck").GetValue<bool>())
            {
                if (ActiveFaggot() != -1)
                {
                    var targetFaggot = HeroManager.Allies[ActiveFaggot()];

                    SpamPing(targetFaggot);
                }

                if (myMenu.Item("sellAll").GetValue<bool>())
                {
                    foreach (var item in ObjectManager.Player.InventoryItems)
                    {
                        ObjectManager.Player.SellItem(item.Slot);
                    }

                    myMenu.Item("sellAll").SetValue(false);
                }

                if (myMenu.Item("buyAll").GetValue<bool>())
                {
                    var itemToBuy = ObjectManager.Player.InventoryItems[0];

                    for (int i = 0; i < 5; i++)
                    {
                        ObjectManager.Player.BuyItem(itemToBuy.Id);
                    }

                    myMenu.Item("buyAll").SetValue(false);
                }
                
                if (myMenu.Item("pingAll").GetValue<bool>())
                {
                    if (Environment.TickCount > lastPingAll + 3500)
                    {
                        foreach (var ally in HeroManager.Allies)
                        {
                            Game.SendPing(PingCategory.Normal, ally);
                        }
                        lastPingAll = Environment.TickCount;
                    }
                }
            }
        }

        private static void Game_OnChat(GameChatEventArgs args)
        {
            if (myMenu.Item("mainCheck").GetValue<bool>())
            {
                if (myMenu.Item("xdPingReq").GetValue<bool>())
                {
                    if (args.Sender.Team == ObjectManager.Player.Team && args.Message.ToLower().StartsWith("!pingme"))
                    {
                        PingPerson(args.Sender);
                    }
                }

                if (myMenu.Item("xdStats").GetValue<bool>())
                {
                    if (args.Message.ToLower().StartsWith("!info"))
                    {
                        var equipo = args.Message.ToLower().Contains("abajo")
                                         ? GameObjectTeam.Order
                                         : (args.Message.ToLower().Contains("arriba") ? GameObjectTeam.Chaos : GameObjectTeam.Unknown);

                        var targetChamp =
                                HeroManager.AllHeroes.Find(x => args.Message.ToLower().Contains(x.ChampionName.ToLower()) && x.Team == equipo);

                        if (equipo == GameObjectTeam.Unknown)
                        {
                            Game.Say("Info: Equipo invalido");
                        }
                        else if (targetChamp == null)
                        {
                            Game.Say("Info: Nombre de campeon invalido");
                        }
                        else
                        {
                            if (!args.Message.ToLower().EndsWith("true"))
                            {
                                ShowInfo(targetChamp);
                            }
                            else
                            {
                                ShowInfo(targetChamp, true);
                            }
                        }
                    }
                }
            }
        }
    }
}
