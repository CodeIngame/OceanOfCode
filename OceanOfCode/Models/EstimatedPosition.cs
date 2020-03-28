namespace OceanOfCode.Models
{
    public class EstimatedPosition
        : Position
    {

        public int XPrecision { get; set; } = -1;
        public int YPrecision { get; set; } = -1;

        #region Ctor
        public EstimatedPosition()
        { }

        public EstimatedPosition(Position p) : base(p)
        { }

        public EstimatedPosition(EstimatedPosition p) : base(p)
        {
            XPrecision = p.XPrecision;
            YPrecision = p.YPrecision;
        }
        #endregion

        public override string ToString()
        {
            if (XPrecision != -1 && YPrecision != -1)
                return $"{{{X}:{Y}}} precision: {XPrecision}:{YPrecision}";

            return base.ToString();
        }
    }
}
