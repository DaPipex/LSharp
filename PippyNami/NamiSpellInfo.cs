using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PippyNami
{
    class NamiSpellInfo
    {
        public static Dictionary<string, float> GetValues()
        {
            var _spellStuff = new Dictionary<string, float>();

            //_spellStuff.Add("aaRange", 0f);

            _spellStuff.Add("qRange", 850f);
            _spellStuff.Add("qSpeed", 1750f);
            _spellStuff.Add("qDelay", 0.25f);
            _spellStuff.Add("qWidth", 200f);

            _spellStuff.Add("wRange", 725f);

            _spellStuff.Add("eRange", 800f);

            _spellStuff.Add("rRange", 2200f);
            _spellStuff.Add("rSpeed", 1200f);
            _spellStuff.Add("rDelay", 0.5f);
            _spellStuff.Add("rWidth", 210f);

            return _spellStuff;
        }
    }
}
