namespace OceanOfCode.Helpers
{
    using OceanOfCode.Enums;
    using OceanOfCode.Models;
    using System.Linq;

    public static class SectionHelpers
    {
        /// <summary>
        /// Retourne la position centrale d'une cellule qui n'est pas un bout d'ile
        /// </summary>
        /// <param name="section"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public static Position ToMidleSectionPosition(this int section, Map map)
        {
            // TODO prendre le milieu accessible
            Position p = null;
            switch (section)
            {
                case 1: p = new Position { X = 2, Y = 2 }; break;
                case 2: p = new Position { X = 7, Y = 2 }; break;
                case 3: p = new Position { X = 12, Y = 2 }; break;
                case 4: p = new Position { X = 2, Y = 7 }; break;
                case 5: p = new Position { X = 7, Y = 7 }; break;
                case 6: p = new Position { X = 12, Y = 7 }; break;
                case 7: p = new Position { X = 2, Y = 12 }; break;
                case 8: p = new Position { X = 7, Y = 12 }; break;
                case 9: p = new Position { X = 12, Y = 12 }; break;
            }

            if (map[p].CellType == CellType.Island)
            {
                var adjs = PathFinderHelpers.GetWalkableAdjacentSquares(p, map, null);
                if (adjs.Count > 0)
                {
                    p = new Position(adjs.First().Position);
                }
            }

            return p;
        }
    }

}
