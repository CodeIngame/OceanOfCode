namespace OceanOfCode.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class Map
    {
        /// <summary>
        /// La largeur de la carte
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// La hauteur de la carte
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// La configuration de la carte
        /// </summary>
        public List<List<MapCell>> Maze2D { get; set; } = new List<List<MapCell>>();

        #region Helpers
        public MapCell this[int x, int y] => Maze2D[y][x];
        public MapCell this[Position p] => Maze2D[p.Y][p.X];

        #endregion

        /// <summary>
        /// Permet d'afficher la carte
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var mapTxt = new StringBuilder();
            Maze2D.ForEach(row =>
            {
                row.ForEach(c =>
                {
                    mapTxt.Append($"{c.Cell}");
                });
                mapTxt.AppendLine("");
            });
            return mapTxt.ToString();
        }


    }
}
