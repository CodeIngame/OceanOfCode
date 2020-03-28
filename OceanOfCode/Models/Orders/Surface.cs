namespace OceanOfCode.Models.Orders
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using OceanOfCode.Enums;

    public class Surface
         : BaseOrder
    {
        // Il faut ajouter un move aprés le surface

        public int Sector { get; set; } = -1;
        public bool IsValid => Sector != -1;
        public Surface()
        {
            OrderType = OrderType.Surface;
        }

        public override string ToCommand()
        {
            return "SURFACE";
        }
    }
}
