namespace OceanOfCode.Helpers
{
    using OceanOfCode.Enums;

    public static class DirectionHelpers
    {
        public static int GetOffset(this Direction nextDirection, OffsetType offsetType)
        {
            var xOffset = nextDirection == Direction.West ? -1 : nextDirection == Direction.Est ? 1 : 0;
            var yOffset = nextDirection == Direction.North ? -1 : nextDirection == Direction.South ? 1 : 0;

            return offsetType == OffsetType.XOffset ? xOffset : yOffset;
        }

        public static string ToMove(this Direction direction)
        {
            switch (direction)
            {
                case Direction.South: return "MOVE S";
                case Direction.North: return "MOVE N";
                case Direction.West: return "MOVE W";
                case Direction.Est: return "MOVE E";
            }
            return null;
        }

        public static string ToChar(this Direction direction)
        {
            switch (direction)
            {
                case Direction.South: return "S";
                case Direction.North: return "N";
                case Direction.West: return "W";
                case Direction.Est: return "E";
            }
            return null;
        }

        public static Direction ToInverse(this Direction direction)
        {
            switch (direction)
            {
                case Direction.South: return Direction.North;
                case Direction.North: return Direction.South;
                case Direction.West: return Direction.Est;
                case Direction.Est: return Direction.West;

            }
            return Direction.North;
        }
    }

}
