namespace OceanOfCode.Models.Orders
{
    using OceanOfCode.Enums;
    using OceanOfCode.Helpers;
    using System;

    public class Move
        : BaseOrder
    {
        public Direction Direction { get; set; } = Direction.None;
        public DeviceType DeviceLoading { get; set; } = DeviceType.None;

        public int X { get; set; }
        public int Y { get; set; }

        public Move()
        {
            OrderType = OrderType.Move;
        }

        public override string ToCommand()
        {
            var loading = DeviceLoading != DeviceType.None ? $" {DeviceLoading.ToText().ToUpper()}" : "";
            return $"MOVE {Direction.ToChar()}{loading}";
        }
    }

}
