namespace OceanOfCode.Models.Orders
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using OceanOfCode.Enums;
    using OceanOfCode.Helpers;

    public class Sonar
      : Device
    {
        public Sonar() : base()
        {
            DeviceType = DeviceType.Sonar;
        }

        public int Sector { get; set; } = -1;

        public override bool CanUse()
        {
            return Couldown == 0;
        }


        public override string ToCommand()
        {
            return $"{DeviceType.ToText()} {Sector}";
        }
    }
}
