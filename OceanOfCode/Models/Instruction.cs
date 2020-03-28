namespace OceanOfCode.Models
{
    using OceanOfCode.Enums;
    using OceanOfCode.Models.Orders;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class Instruction
    {
        /// <summary>
        /// La commande textuel envoyé
        /// </summary>
        public string FullCommand { get; set; }

        /// <summary>
        /// Les commandes é effectuer ou effectué
        /// </summary>
        public List<BaseOrder> Commands { get; set; } = new List<BaseOrder>();

        /// <summary>
        /// La position estimé
        /// </summary>
        public EstimatedPosition EstimatedPosition { get; set; } = new EstimatedPosition();

        public Instruction()
        {

        }

        public Instruction(Instruction i)
        {
            FullCommand = i.FullCommand;
            EstimatedPosition = i.EstimatedPosition;
            var tempCmd = new List<BaseOrder>().ToArray();
            Array.Copy(i.Commands.ToArray(), tempCmd, i.Commands.Count);
            Commands = tempCmd.ToList();
        }

        public override string ToString()
        {
            return "NotImplemented";
            //return $"D: {Device.ToText()} {DevicePosition} M: {Direction.ToMove()} L: {DeviceLoading.ToText()}";
        }

        public string ToCommand()
        {
            var command = string.Empty;
            var totalCommand = Commands.Count;
            for (var i = 0; i < totalCommand; i++)
            {
                var isLast = i == totalCommand - 1;
                command += $"{Commands[i].ToCommand()}{(!isLast ? "|" : "")}";
            }

#if WriteDebug
            // Console.Error.WriteLine($"Command: {command}");
#endif
            return command;
        }

        #region Helpers
        public Move MoveCommand => (Move)Commands.FirstOrDefault(c => c.OrderType == OrderType.Move) ?? null;
        public Surface SurfaceCommand => (Surface)Commands.FirstOrDefault(c => c.OrderType == OrderType.Surface) ?? null;
        public List<Device> DeviceCommands => Commands.Where(c => c.OrderType == OrderType.Device)
            .Select(c => (Device)c)
            .ToList();

        public Torpedo TorpedoCommand => (Torpedo)DeviceCommands.FirstOrDefault(d => d.DeviceType == DeviceType.Torpedo) ?? null;
        public Sonar SonarCommand => (Sonar)DeviceCommands.FirstOrDefault(d => d.DeviceType == DeviceType.Sonar) ?? null;
        public Silence SilenceCommand => (Silence)DeviceCommands.FirstOrDefault(d => d.DeviceType == DeviceType.Silence) ?? null;
        public Mine MineCommand => (Mine)DeviceCommands.FirstOrDefault(d => d.DeviceType == DeviceType.Mine) ?? null;

        #endregion
    }

}
