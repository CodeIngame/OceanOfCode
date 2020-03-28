namespace OceanOfCode.Models.Orders
{
    using OceanOfCode.Enums;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public abstract class Device
      : BaseOrder
    {
        /// <summary>
        /// Le type d'arme
        /// </summary>
        public DeviceType DeviceType { get; set; }
        public static int Range { get; set; } = 0;
        /// <summary>
        /// Le temps é attendre avant réutilisation (disponibilité)
        /// </summary>
        public int Couldown { get; set; }

        public abstract bool CanUse();

        public Device()
        {
            OrderType = OrderType.Device;
        }
    }
}
