namespace OceanOfCode.Models.Orders
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using OceanOfCode.Enums;
    using OceanOfCode.Helpers;

    public class Torpedo
          : Device
    {
        public Torpedo() : base()
        {
            DeviceType = DeviceType.Torpedo;
            Range = 4;
        }

        /// <summary>
        /// La position du tire
        /// </summary>
        public Position Position { get; set; } = new Position();
        public bool IsValid => Position.Known;

        public override bool CanUse()
        {
            return Couldown == 0;
        }

        public override string ToCommand()
        {
            return $"{DeviceType.ToText()} {Position.Coordonate}";
        }
    }
}
