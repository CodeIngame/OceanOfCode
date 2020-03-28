namespace OceanOfCode.Models
{
    using OceanOfCode.Helpers;

    public class Position
           : PositionScore
    {
        public int X { get; set; } = -1;
        public int Y { get; set; } = -1;

        #region Ctor
        public Position()
        { }

        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Position(Position p) : this(p.X, p.Y)
        { }
        #endregion

        #region Helpers
        /// <summary>
        /// Permet d'obtenir la section de la position
        /// </summary>
        public int Section => this.ToSection();
        /// <summary>
        /// Détermine si la position est connu
        /// </summary>
        public bool Known => X != -1 && Y != -1;
        public string Coordonate => $"{X} {Y}";
        #endregion

        public override string ToString()
        {
            return $"{{{X}:{Y}}}";
        }

        #region Operators
        public override bool Equals(object obj)
        {
            Position p2 = obj as Position;
            if (p2 == null)
                return false;

            return X == p2.X && Y == p2.Y;
        }

        public static bool operator ==(Position a, Position b)
        {
            return a.X.Equals(b?.X) && a.Y.Equals(b?.Y);
        }

        public static bool operator !=(Position a, Position b)
        {
            return !a.X.Equals(b?.X) || !a.Y.Equals(b?.Y);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion
    }

}
