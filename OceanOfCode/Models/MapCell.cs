namespace OceanOfCode.Models
{
    using OceanOfCode.Enums;
    using OceanOfCode.Helpers;

    public class MapCell
    {
        /// <summary>
        /// La position de la cellule
        /// </summary>
        public Position Position { get; set; }
        /// <summary>
        /// Le caractére qui correspond é la cellule
        /// </summary>
        public char Cell { get; set; }

        /// <summary>
        /// Pour la recherche de chemin il faut un parent
        /// </summary>
        public MapCell Parent;

        public bool CanGoHere => !Visited && CellType == CellType.Empty;
        /// <summary>
        /// On ne doit pas se déplacer sur une cellule qu'on a déjà visité
        /// Sauf dans le cas où on refait surface
        /// </summary>
        public bool Visited { get; set; } = false;
        /// <summary>
        /// Le type de cellule
        /// </summary>
        public CellType CellType => Cell.ToCellType();
    }
}
