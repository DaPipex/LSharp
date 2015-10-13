using LeagueSharp.Common;

namespace PippyDeveloper
{
    class Program
    {
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += DeveloperInfo.DeveloperLoad;
        }
    }
}
