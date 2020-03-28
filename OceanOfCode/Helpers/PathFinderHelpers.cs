namespace OceanOfCode.Helpers
{
    using OceanOfCode.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class PathFinderHelpers
    {
        public static List<MapCell> FindPath(this Position start, Position end, Map map, bool useVisited = true)
        {
            // reset de la précédante 
            map.Maze2D.ForEach(row =>
            {
                row.ForEach(c =>
                {
                    c.Parent = null;
                    c.Position.F = 0;
                    c.Position.G = 0;
                    c.Position.H = 0;

                });
            });

            MapCell current = null;
            var openList = new List<MapCell>();
            var closedList = new List<MapCell>();
            int g = 0;
            Console.Error.Write($" Path finding with start:{start} - target:{end}");
            openList.Add(map[start]);
            while (openList.Count > 0)
            {

                // get the square with the lowest F score  
                var lowest = openList.Min(cell => cell.Position.F);
                current = openList.First(cell => cell.Position.F == lowest);

                // Console.Error.WriteLine($"scan for {current.Position}");

                // add the current square to the closed list  
                closedList.Add(current);
                // remove it from the open list  
                openList.Remove(current);

                // if we added the destination to the closed list, we've found a path  
                if (closedList.FirstOrDefault(cell => cell.Position == end) != null)
                    break;

                var adjacentSquares = GetWalkableAdjacentSquares(current.Position, map, openList, useVisited);
                g = current.Position.G + 1;

                foreach (var adjacentSquareCell in adjacentSquares)
                {
                    var adjacentSquare = adjacentSquareCell.Position;
                    // if this adjacent square is already in the closed list, ignore it  
                    if (closedList.FirstOrDefault(cell => cell.Position == adjacentSquare) != null)
                        continue;

                    // if it's not in the open list...  
                    if (openList.FirstOrDefault(cell => cell.Position == adjacentSquare) == null)
                    {
                        // compute its score, set the parent  
                        adjacentSquare.G = g;
                        adjacentSquare.H = adjacentSquare.Distance(end);
                        adjacentSquare.F = adjacentSquare.G + adjacentSquare.H;
                        adjacentSquareCell.Parent = current;

                        // and add it to the open list  
                        openList.Insert(0, adjacentSquareCell);
                    }
                    else
                    {
                        // test if using the current G score makes the adjacent square's F score  
                        // lower, if yes update the parent because it means it's a better path  
                        if (g + adjacentSquare.H < adjacentSquare.F)
                        {
                            adjacentSquare.G = g;
                            adjacentSquare.F = adjacentSquare.G + adjacentSquare.H;
                            adjacentSquareCell.Parent = current;
                        }
                    }
                }

            }
            var endCell = current;
            var cellLink = new List<MapCell>();
            while (current != null)
            {
                // Il ne faut pas ajouter ce qu'on a déjà fait
                if (!current.Visited && current.Position != start)
                    cellLink.Add(current);

                current = current.Parent;
            }


            cellLink.Reverse();
            return cellLink;
        }

        public static List<MapCell> GetWalkableAdjacentSquares(Position p, Map map, List<MapCell> openList, bool useVisited = true)
        {
            var list = new List<MapCell>();
            var left = p.X != 0 ? map[p.X - 1, p.Y] : null;
            var right = p.X != map.Width - 1 ? map[p.X + 1, p.Y] : null;
            var bot = p.Y != map.Height - 1 ? map[p.X, p.Y + 1] : null;
            var top = p.Y != 0 ? map[p.X, p.Y - 1] : null;

            //Console.Error.WriteLine("--");
            //Console.Error.WriteLine($"{top?.Position}");
            //Console.Error.WriteLine($"{left?.Position} {p} {right?.Position}");
            //Console.Error.WriteLine($"{bot?.Position}");
            //Console.Error.WriteLine($" [S: {bot?.CanGoHere}, N: {top?.CanGoHere}, E: {right?.CanGoHere}, W: {left?.CanGoHere}]");
            //Console.Error.WriteLine("--");

            var availables = new List<MapCell> { left, right, top, bot }
                .Where(c => c != null)
                .Where(c => (useVisited && c.CanGoHere) || !useVisited)
                .ToList();

            // Console.Error.WriteLine($"Availables: {availables.Count}");

            foreach (var i in availables)
            {
                var n = openList?.Find(c => c.Position == i.Position) ?? null;
                if (n == null) list.Add(i);
                else list.Add(n);
            }

            return list;
        }
    }

}
