namespace OceanOfCode.Models.Orders
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using OceanOfCode.Enums;
    using OceanOfCode.Helpers;

    public class Silence
        : Device
    {
        public Silence() : base()
        {
            DeviceType = DeviceType.Silence;
            Range = 4;
        }

        public int Distance { get; set; } = -1;
        public Direction Direction { get; set; } = Direction.None;

        public override bool CanUse()
        {
            return Couldown == 0;
        }


        public override string ToCommand()
        {
            return $"{DeviceType.ToText()} {Direction.ToChar()} {Distance}";
        }
    }
}
