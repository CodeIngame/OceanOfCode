namespace OceanOfCode.Models.Orders
{
    using System;
    using OceanOfCode.Enums;

    public class Mine
    : Device
    {
        public Mine() : base()
        {
            DeviceType = DeviceType.Mine;
        }

        public override bool CanUse()
        {
            throw new NotImplementedException();
        }


        public override string ToCommand()
        {
            throw new NotImplementedException();
        }
    }

}
