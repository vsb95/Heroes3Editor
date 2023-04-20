using System.Collections.Generic;
using System.Linq;

namespace Heroes3Editor.Models
{
    public class SearchGame
    {
        private static List<SearchPosition> _positions = new();
        private readonly Game _game;

        public SearchGame(Game game)
        {
            _game = game;
        }

        public int Search(int value, int startPosition = 0)
        {
            var endPosition = 0;
            /*if (startPosition != 0)
            {
                startPosition += 100000;
                endPosition = startPosition - 200000;
            }
            else
            {
                startPosition = _game.Bytes.Length;
            }*/
            var newPositions = _game.SearchValue((short)value, startPosition, null, endPosition);

            if (_positions.Count == 0)
            {
                _positions.AddRange(newPositions.Select(x => new SearchPosition() { Absolute = x, Start = startPosition }));
            }
            else
            {
                var prevStart = _positions.First().Start;
                var collision = startPosition - prevStart;
                foreach (var searchPosition in _positions)
                {
                    searchPosition.Relative = searchPosition.Absolute + collision;
                }
                var tt = _positions.Where(x => newPositions.Contains(x.Relative)).ToList();
                _positions = tt;
            }
            return _positions.Count;
        }

        public void SetValue(int value)
        {
            if (_positions.Count != 1)
            {
                return;
            }

            var pos = _positions.First();
            _game.SetValue(pos.Absolute, value); // hero 253719 780, 253719 768  = 253597
        }
    }

    public class SearchPosition
    {
        public int Absolute { get; set; }
        public int Start { get; set; }
        public int Relative { get; set; }

        public override string ToString()
        {
            return $"{Relative}; was: {Absolute} from {Start}";
        }
    }
}
