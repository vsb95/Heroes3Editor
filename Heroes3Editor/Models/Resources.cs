using System;

namespace Heroes3Editor.Models
{
    public class Resources
    {
        private readonly Game _game;

        public int Wood
        {
            get => _game.GetIntValue(_woodPosition);
            set => _game.SetValue(_woodPosition, value);
        }

        public int Mercury
        {
            get => _game.GetIntValue(MercuryPosition);
            set => _game.SetValue(MercuryPosition, value);
        }

        public int Ore
        {
            get => _game.GetIntValue(OrePosition);
            set => _game.SetValue(OrePosition, value);
        }

        public int Sulfur
        {
            get => _game.GetIntValue(SulfurPosition);
            set => _game.SetValue(SulfurPosition, value);
        }
        public int Crystal
        {
            get => _game.GetIntValue(CrystalPosition);
            set => _game.SetValue(CrystalPosition, value);
        }
        public int Gems
        {
            get => _game.GetIntValue(GemsPosition);
            set => _game.SetValue(GemsPosition, value);
        }
        public int Gold
        {
            get => _game.GetIntValue(GoldPosition);
            set => _game.SetValue(GoldPosition, value);
        }

        private int _woodPosition;

        private int MercuryPosition => _woodPosition + 4;

        private int OrePosition => MercuryPosition + 4;

        private int SulfurPosition => OrePosition + 4;

        private int CrystalPosition => SulfurPosition + 4;

        private int GemsPosition => CrystalPosition + 4;

        private int GoldPosition => GemsPosition + 4;

        public Resources(Game game)
        {
            _game = game;
        }

        public event EventHandler IndexChanged;

        public void SetIndex(int indexValue)
        {
            _woodPosition = indexValue;
            IndexChanged?.Invoke(this, null!);
        }
    }
}
