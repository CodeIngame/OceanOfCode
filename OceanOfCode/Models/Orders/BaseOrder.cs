namespace OceanOfCode.Models.Orders
{
    using OceanOfCode.Enums;

    public abstract class BaseOrder
    {
        public string Order { get; set; }
        public OrderType OrderType { get; protected set; }

        public abstract string ToCommand();
    }
 
}

