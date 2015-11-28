using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game
{
    public class MarkovGenerator
    {
        private Random _random = new Random();
        private Dictionary<string, List<string>> _data = new Dictionary<string, List<string>>();
        private List<string> _startList = new List<string>();

        public void Feed(IEnumerable<string> data)
        {
            string previous = null;
            foreach (var item in data)
            {
                if (previous == null)
                    _startList.Add(item);
                else
                {
                    if (!_data.ContainsKey(previous))
                        _data[previous] = new List<string>();
                    _data[previous].Add(item);
                }
                previous = item.ToLower();
            }

            if (!_data.ContainsKey(previous))
                _data[previous] = new List<string>();
            _data[previous].Add(null);
        }

        public IEnumerable<string> Get()
        {
            string pointer = _startList[_random.Next(_startList.Count)];
            while (pointer != null)
            {
                yield return pointer;
                pointer = _data[pointer.ToLower()][_random.Next(_data[pointer.ToLower()].Count)];
            }
        }
    }
}
