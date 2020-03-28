namespace OceanOfCode.Helpers
{
    using OceanOfCode.Enums;
    public static class CellTypeHelpers
    {
        /// <summary>
        /// Permet de passer du caractére au type de cellule
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static CellType ToCellType(this char c)
        {
            switch (c)
            {
                case 'x': return CellType.Island;
                case '.': return CellType.Empty;
                default: return CellType.Unknow;
            }
        }
        public static string ToText(this CellType cellType)
        {
            switch (cellType)
            {

                case CellType.Island: return "Island";
                case CellType.Empty: return "Empty";
            }
            return "Unknow";
        }
    }
}
