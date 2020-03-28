namespace OceanOfCode.Helpers
{
    using OceanOfCode.Enums;
    using OceanOfCode.Models;
    using System;

    public static class PositionHelpers
    {
        /// <summary>
        /// Permet d'obtenir le niveau de section correspondant é une position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static int ToSection(this Position position)
        {
            var section = 0;
            if (position.X <= 4)
                section = 1;
            else if (position.X <= 9)
                section = 2;
            else
                section = 3;

            if (position.Y <= 4)
                section += 0;
            else if (position.Y <= 9)
                section += 3;
            else
                section += 6;

            // Console.Error.WriteLine($"{position} is on section {section}");

            return section;
        }

        public static int Distance(this Position p1, Position p2)
        {
            return Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y);
        }

        /// <summary>
        /// Permet d'avoir la direction é prendre entre 2 points
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static Direction DirectionToTake(this Position p1, Position p2)
        {
            var direction = Direction.None;
            if (p1.X > p2.X)
                direction = Direction.West;
            else if (p1.X < p2.X)
                direction = Direction.Est;

            if (p1.Y > p2.Y)
                direction = Direction.North;
            else if (p1.Y < p2.Y)
                direction = Direction.South;

            return direction;
        }

        public static bool IsValidPosition(this Position p1, Map map, Direction nextDirection)
        {
            var xOffset = nextDirection.GetOffset(OffsetType.XOffset);
            var yOffset = nextDirection.GetOffset(OffsetType.YOffset);
            return p1.IsValidPosition(map, yOffset, xOffset);
        }

        public static bool IsValidPosition(this Position p1, Map map, int yOffset = 0, int xOffset = 0, bool checkVisited = false)
        {
            var isValid = true;
            var height = p1.Y + yOffset;
            var width = p1.X + xOffset;

            if (height < 0 || height > map.Height - 1)
                isValid = false;

            if (isValid && (width < 0 || width > map.Width - 1))
                isValid = false;

            if (isValid && map[p1.X + xOffset, p1.Y + yOffset].CellType == CellType.Island)
                isValid = false;

            if (isValid && checkVisited && map[p1.X + xOffset, p1.Y + yOffset].Visited)
                isValid = false;

            // Console.Error.WriteLine($"{p1} is valid position ? {isValid}  ({height}, {width})");

            return isValid;
        }

        public static Position NewPosition(this Position p1, Direction nextDirection)
        {
            var xOffset = nextDirection.GetOffset(OffsetType.XOffset);
            var yOffset = nextDirection.GetOffset(OffsetType.YOffset);

            return new Position(p1.X + xOffset, p1.Y + yOffset);
        }
    }
}
