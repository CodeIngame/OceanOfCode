namespace OceanOfCode.Models
{
    using OceanOfCode.Enums;
    using OceanOfCode.Models.Orders;
    using System.Collections.Generic;
    using System.Linq;

    public class Player
        : VirtualPlayer
    {
        /// <summary>
        /// Identifiant du joueur
        /// Le joueur avec l'identifiant 0 commande
        /// </summary>
        public int PlayerId { get; set; }
        /// <summary>
        /// Défini s'il s'agit de moi ou pas
        /// </summary>
        public PlayerType PlayerType { get; set; }

        /// <summary>
        /// La position du joueur
        /// </summary>
        public Position CurrentPosition { get; set; } = new Position();

        /// <summary>
        /// Le nombre de point de vie
        /// on démarre avec 6 points de vie
        /// </summary>
        public int HealthPoint { get; set; } = 6;
        public List<int> HealthPointHistoric { get; set; } = new List<int>();
        public List<Instruction> LastInstructions { get; set; } = new List<Instruction>();

        /// <summary>
        /// Les 'armes' disponibles
        /// </summary>
        public List<Device> Devices { get; set; } = new List<Device> {
            new Torpedo { },
            new Sonar {  },
            new Silence { },
            new Mine {  }
        };

        #region Helpers
        /// <summary>
        /// Provient des instructions
        /// </summary>
        public EstimatedPosition LastEstimatedPosition
        {
            get { return LastInstruction.EstimatedPosition; }
            set { LastInstruction.EstimatedPosition = value; }
        }

        public Instruction LastInstruction
        {
            get { return LastInstructions.Count > 0 ? LastInstructions[LastInstructions.Count - 1] : new Instruction(); }
            set { LastInstructions[LastInstructions.Count - 1] = value; }
        }
        #region Devices
        public Torpedo Torpedo => (Torpedo)Device(DeviceType.Torpedo);
        public Sonar Sonar => (Sonar)Device(DeviceType.Sonar);
        public Mine Mine => (Mine)Device(DeviceType.Mine);
        public Silence Silence => (Silence)Device(DeviceType.Silence);
        protected Device Device(DeviceType DeviceType) => Devices.First(d => d.DeviceType == DeviceType);
        #endregion

        public bool Touched => HealthPointHistoric.Count > 1 && HealthPointHistoric[HealthPointHistoric.Count - 2] > HealthPointHistoric[HealthPointHistoric.Count - 1];
        public bool TouchedPerfect => HealthPointHistoric.Count > 1 && HealthPointHistoric[HealthPointHistoric.Count - 2] - 2 == HealthPointHistoric[HealthPointHistoric.Count - 1];
        public int TotalHpLost => HealthPointHistoric[HealthPointHistoric.Count - 2] - HealthPointHistoric[HealthPointHistoric.Count - 1];

        /// <summary>
        /// Défini si le joueur est le premier é jouer
        /// </summary>
        public bool First => PlayerId == 0;
        #endregion
    }
}
