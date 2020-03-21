#define WriteDebug

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace OceanOfCode
{
    class Program
    {
        static void Main(string[] args)
        {
            var gameManager = new GameManager();
            gameManager.InitializeMap();

            // Console.WriteLine("7 7");

            // game loop
            while (true)
            {
                gameManager.SetPlayersInformations();
                gameManager.WritePosition();
                gameManager.Play();

                // Write an action using Console.WriteLine()
                // To debug: Console.Error.WriteLine("Debug messages...");


                //Console.WriteLine("MOVE N TORPEDO");
            }
        }
    }

    #region Models
    public class Map
    {
        /// <summary>
        /// La largeur de la carte
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// La hauteur de la carte
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// La configuration de la carte
        /// </summary>
        public List<List<MapCell>> MapConfiguration { get; set; } = new List<List<MapCell>>();
    }

    public class MapCell
    {
        /// <summary>
        /// La position de la cellule
        /// </summary>
        public Position Position { get; set; }
        /// <summary>
        /// Le caractère qui correspond à la cellule
        /// </summary>
        public char Cell { get; set; }

        /// <summary>
        /// On ne doit pas se déplacer sur une cellule qu'on a déjà visité
        /// Sauf dans le cas où on refait surface
        /// </summary>
        public bool Visited { get; set; } = false;
        /// <summary>
        /// Le type de cellule
        /// </summary>
        public CellType CellType => Cell.ToCellType();
    }

    public class Position
    {
        public int X { get; set; } = -1;
        public int Y { get; set; } = -1;

        /// <summary>
        /// Permet d'obtenir la section de la position
        /// </summary>
        public int Section => this.ToSection();
        public override string ToString()
        {
            return $"{{{X}:{Y}}}";
        }

    }

    public class Player
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
        public Position Position { get; set; } = new Position();

        /// <summary>
        /// Le nombre de point de vie
        /// on démarre avec 6 points de vie
        /// </summary>
        public int HealthPoint { get; set; } = 6;

        /// <summary>
        /// Les 'armes' disponibles
        /// </summary>
        public List<Device> Devices { get; set; } = new List<Device> {
            new Torpedo { },
            new Sonar {  },
            new Silence { },
            new Mine {  }
        };

        public List<Instruction> LastInstructions = new List<Instruction>();

        #region Helpers
        public Torpedo Torpedo => (Torpedo)Device(DeviceType.Torpedo);
        public Sonar Sonar => (Sonar)Device(DeviceType.Sonar);
        public Mine Mine => (Mine)Device(DeviceType.Mine);
        public Silence Silence => (Silence)Device(DeviceType.Silence);
        protected Device Device(DeviceType DeviceType) => Devices.First(d => d.DeviceType == DeviceType);

        /// <summary>
        /// Défini si le joueur est le premier à jouer
        /// </summary>
        public bool First => PlayerId == 0;
        public bool ImOnTop => Position?.Y <= 7;
        public bool ImOnLeft => Position?.X <= 7;
        #endregion
    }

    public class Instruction
    {
        public string Command { get; set; }
        /// <summary>
        /// La direction demandée
        /// </summary>
        public Direction Direction { get; set; }
        public DeviceType DeviceUsed { get; set; }
        public Position DeviceUsedPosition { get; set; }
        public DeviceType DeviceLoad { get; set; }

        public override string ToString()
        {
            return $"D: {DeviceUsed.ToText()} {DeviceUsedPosition} M: {Direction.ToMove()} L: {DeviceLoad.ToText()}";
        }

        public string ToCommand()
        {
            var attack = DeviceUsed != DeviceType.None ? $"{DeviceUsed.ToText().ToUpper()} {DeviceUsedPosition.Y} {DeviceUsedPosition.X}|" : "";
            var move = Direction.ToMove();
            var load = $" {DeviceLoad.ToText().ToUpper()}";

            var command = $"{attack}{move}{load}";
#if WriteDebug
            Console.Error.WriteLine($"Command: {command}");
#endif
            return command;
        }
    }

    public abstract class Device
    {
        /// <summary>
        /// Le type d'arme
        /// </summary>
        public DeviceType DeviceType { get; set; }
        /// <summary>
        /// Le temps à attendre avant réutilisation (disponibilité)
        /// </summary>
        public int Couldown { get; set; }

        public abstract bool CanUse();
        public abstract string Use();
    }

    public class Torpedo
        : Device
    {
        public Torpedo()
        {
            DeviceType = DeviceType.Torpedo;
        }

        public override bool CanUse()
        {
            return Couldown == 0;
        }

        public override string Use()
        {
            return "TORPEDO {0} {1}|";
        }
    }

    public class Sonar
    : Device
    {
        public Sonar()
        {
            DeviceType = DeviceType.Sonar;
        }

        public override bool CanUse()
        {
            throw new NotImplementedException();
        }

        public override string Use()
        {
            throw new NotImplementedException();
        }
    }

    public class Mine
    : Device
    {
        public Mine()
        {
            DeviceType = DeviceType.Mine;
        }

        public override bool CanUse()
        {
            throw new NotImplementedException();
        }

        public override string Use()
        {
            throw new NotImplementedException();
        }
    }

    public class Silence
    : Device
    {
        public Silence()
        {
            DeviceType = DeviceType.Silence;
        }

        public override bool CanUse()
        {
            throw new NotImplementedException();
        }

        public override string Use()
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    public class GameManager
    {
        public List<Player> Players { get; set; } = new List<Player> { new Player { PlayerType = PlayerType.Me }, new Player { PlayerType = PlayerType.Enemy } };
        public Map Map { get; set; } = new Map();

        /// <summary>
        /// Le nombre de tour
        /// </summary>
        public int Counter { get; set; } = 0;

        #region Player Methods
        public Player Me => Players.First(p => p.PlayerType == PlayerType.Me);
        public Player Enemy => Players.First(p => p.PlayerType == PlayerType.Enemy);
        #endregion

        #region Map Methods
        public MapCell MapCell(PlayerType playerType, Direction direction)
        {
            Player _player;
            if (playerType == PlayerType.Me)
                _player = Me;
            else
                _player = Enemy;

            var xOffset = direction == Direction.West ? -1 : direction == Direction.Est ? 1 : 0;
            var yOffset = direction == Direction.North ? -1 : direction == Direction.South ? 1 : 0;



            // On veut aller à l'ouest mais on est déjà au maximun ...
            if (_player.Position.X == 0 && direction == Direction.West)
                return null;

            // On veut aller à l'est mais on est déjà au maximun ...
            if (_player.Position.X == Map.Width - 1 && direction == Direction.Est)
                return null;

            // On veut aller au sud mais on est déjà au maximun ...
            if (_player.Position.Y == Map.Height - 1 && direction == Direction.South)
                return null;

            // On veut aller au nord mais on est déjà au maximun ...
            if (_player.Position.Y == 0 && direction == Direction.North)
                return null;

            var cell = Map.MapConfiguration[_player.Position.Y + yOffset][_player.Position.X + xOffset];
            return cell;
        }
        public void ResetVisitedCell()
        {
            Map.MapConfiguration.ForEach(row =>
            {
                row.ForEach(c => c.Visited = false);
            });
        }
        public MapCell EmptyCell()
        {
            var cells = Map.MapConfiguration.Select(row =>
            {
                var cellule = row.First(c => c.CellType == CellType.Empty);
                return cellule;
            }).ToList();
            return cells[0];
        }

        #endregion

        public bool CanDoAction(MapCell mapCell, Direction direction)
        {
            if (mapCell == null)
                return false;

            if (mapCell.Visited)
                return false;

            if (mapCell.CellType != CellType.Empty)
                return false;

            // Je suppose que si je peux y aller je vais y aller
            // A modifier plus tard
            Console.Error.WriteLine($"Position : {Me.Position} -> {direction.ToMove()} on {mapCell.CellType.ToText()}");
            mapCell.Visited = true;
            return true;
        }



        #region Ctor & Initialization
        public GameManager()
        {
            var inputs = Helpers.ReadLine().Split(' ');
            Map.Width = int.Parse(inputs[0]);
            Map.Height = int.Parse(inputs[1]);
            Me.PlayerId = int.Parse(inputs[2]);
        }

        public void InitializeMap()
        {
            for (int x = 0; x < Map.Height; x++)
            {
                var y = 0;
                Map.MapConfiguration.Add(Helpers.ReadLine(debug: true).Select(c =>
                {
                    var cell = new MapCell
                    {
                        Cell = c,
                        Position = new Position { X = x, Y = y }
                    };
                    y++;
                    return cell;
                }).ToList());
            }

            //On doit maintenant choisir sa position de départ
            var startCell = EmptyCell();
            startCell.Visited = true;

#if WriteDebug
            Console.Error.WriteLine($"Map initialized - start position {startCell.Position}");
#endif
            Console.WriteLine($"{startCell.Position.Y} {startCell.Position.X}");

        }
        public void SetPlayersInformations()
        {
            var inputs = Helpers.ReadLine().Split(' ');
            Me.Position.X = int.Parse(inputs[0]);
            Me.Position.Y = int.Parse(inputs[1]);

            if (Counter > 0)
            {
                // Non disponible au 1er tours
                Me.HealthPoint = int.Parse(inputs[2]);
                // Todo si l'enemie perds des points de vie c'est que (il s'est touché lui même ou que je lui ai tiré dessus)
                Enemy.HealthPoint = int.Parse(inputs[3]);
            }


            if (inputs.Length >= 7)
            {
                Me.Torpedo.Couldown = int.Parse(inputs[4]);
                Me.Sonar.Couldown = int.Parse(inputs[5]);
                Me.Silence.Couldown = int.Parse(inputs[6]);
                Me.Mine.Couldown = int.Parse(inputs[7]);
            }

            string sonarResult = Helpers.ReadLine();
            string opponentOrders = Helpers.ReadLine();

            // On enregistre le précédent déplacement
            if (opponentOrders != "NA")
                Enemy.LastInstructions.Add(opponentOrders.ToInstructions());

        }
        #endregion

        public void WritePosition()
        {
            Console.Error.WriteLine($"My position is :{Me.Position}");
            Console.Error.WriteLine($"Enemy position is :{Enemy.Position}");
        }


        public void Play()
        {
            var instruction = new Instruction();

            UseDevice(instruction);
            Move(instruction);
            LoadDevice(instruction);

            Me.LastInstructions.Add(instruction);

            Counter++;
            Console.WriteLine(instruction.ToCommand());

        }

        /// <summary>
        /// Attaquer
        /// </summary>
        private void UseDevice(Instruction instruction)
        {

            // il faut tirer et regarder au prochain coups si on a touché quelque chose
            // on combine le tout avec les derniers déplacement pour localiser la position de quelqu'un

            if (Me.Torpedo.CanUse())
            {

#if WriteDebug
                Console.Error.WriteLine($"I can use torpedo");
#endif

                var lastMoves = Me.LastInstructions.Skip(Me.LastInstructions.Count - 4).Take(3).ToList();
                var lastEnemyMoves = Enemy.LastInstructions.Skip(Me.LastInstructions.Count - 4).Take(3).ToList();
                if (lastMoves.SequenceEqual(lastEnemyMoves))
                {
#if WriteDebug
                    Console.Error.WriteLine($"same scheme -> shoot him");
#endif
                    var action = Me.Torpedo.Use();

                    // faudra faire attention à l'ile
                    //var isOutOfSouth = Me.Position.Y + 2 > Map.Height-1;
                    //var isOutOfNorth = Me.Position.Y - 2 < 0 ;
                    //var isOutOfEst = Me.Position.X + 2 > Map.Width - 1;
                    //var isOutOfWest = Me.Position.X - 2 < 0;
                    //var offsetY = isOutOfSouth ? -2 : +2;
                    //var offsetX = isOutOfWest ? -2 : +2;
                    var direction = lastEnemyMoves[lastEnemyMoves.Count - 1].Direction;

                    var offsetX = direction == Direction.West ? 2 : direction == Direction.Est ? -2 : 1;
                    var offsetY = direction == Direction.South ? 2 : direction == Direction.North ? -2 : 1;


                    instruction.DeviceUsed = DeviceType.Torpedo;
                    instruction.DeviceUsedPosition = new Position { Y = Me.Position.Y + offsetY, X = Me.Position.X + offsetX };
                }
            }

        }

        /// <summary>
        /// Déplacer
        /// </summary>
        private void Move(Instruction instruction)
        {
            var direction = Direction.None;
            // On regarde les positions proches
            var west = MapCell(PlayerType.Me, Direction.West);
            var est = MapCell(PlayerType.Me, Direction.Est);
            var north = MapCell(PlayerType.Me, Direction.North);
            var south = MapCell(PlayerType.Me, Direction.South);

            // on regarde les quels sont accessibles et pour le moment on prend la première
            if (CanDoAction(west, Direction.West))
                direction = Direction.West;
            else if (CanDoAction(south, Direction.South))
                direction = Direction.South;
            else if (CanDoAction(est, Direction.Est))
                direction = Direction.Est;
            else if (CanDoAction(north, Direction.North))
                direction = Direction.North;
            else
            {
                direction = Direction.Surface;
                ResetVisitedCell();
                var currentPosition = Me.Position;
                Map.MapConfiguration[currentPosition.Y][currentPosition.X].Visited = true;
            }

            instruction.Direction = direction;
        }

        /// <summary>
        /// Charger
        /// </summary>
        private void LoadDevice(Instruction instruction)
        {
            if (Me.Torpedo.Couldown > 0)
                instruction.DeviceLoad = DeviceType.Torpedo;

            //if (Me.Sonar.Couldown < 0)
            //    actions += " SONAR";
        }

    }



    #region Common

    #region Helpers
    public static class Helpers
    {
        /// <summary>
        /// Permet de lire la ligne et de retourner son contenu
        /// Log si nécessaire l'information lue
        /// </summary>
        /// <param name="newLine"></param>
        /// <param name="debug"></param>
        /// <returns></returns>
        public static string ReadLine(bool newLine = true, bool debug = true)
        {
            var msg = Console.ReadLine();
            if (debug)
            {
                if (newLine)
                    Console.Error.WriteLine(msg);
                else
                    Console.Error.Write(msg);
            }

            return msg;
        }
    }

    public static class CellTypeHelper
    {
        /// <summary>
        /// Permet de passer du caractère au type de cellule
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static CellType ToCellType(this char c)
        {
            switch (c)
            {
                case 'x': return CellType.Island;
                case '.': return CellType.Empty;
                default: return CellType.Unknow;
            }
        }
        public static string ToText(this CellType cellType)
        {
            switch (cellType)
            {

                case CellType.Island: return "Island";
                case CellType.Empty: return "Empty";
            }
            return "Unknow";
        }
    }

    public static class PositionHelpers
    {
        public static int ToSection(this Position position)
        {
            var section = 0;
            if (position.X <= 4)
                section = 1;
            else if (position.X <= 9)
                section = 2;
            else
                section = 3;

            if (position.Y <= 4)
                section = 1;
            else if (position.Y <= 9)
                section += 3;
            else
                section += 6;

            return section;
        }
    }

    public static class DirectionHelpers
    {
        public static string ToMove(this Direction direction)
        {
            switch (direction)
            {
                case Direction.South: return "MOVE S";
                case Direction.North: return "MOVE N";
                case Direction.West: return "MOVE W";
                case Direction.Est: return "MOVE E";

                case Direction.Surface: return "SURFACE";

            }
            return null;
        }
    }

    public static class InstructionHelpers
    {
        public static Instruction ToInstructions(this string move)
        {
            // TORPEDO 0 5|MOVE N TORPEDO
            // MOVE N TORPEDO
            // MOVE E TORPEDO
            // TORPEDO 0 8|MOVE E TORPEDO
            var instruction = new Instruction();
            var input = move;

            var multiOrders = move.Contains("|");
            if (multiOrders)
            {
                var inputs = move.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                // Device
                var attack = inputs[0].Split(' ');
                //Console.Error.WriteLine(attack[0]);
                var parsed = Enum.TryParse<DeviceType>(attack[0].ToPascalCase(), out var deviceType);
                instruction.DeviceUsed = deviceType;
                instruction.DeviceUsedPosition = new Position { Y = int.Parse(attack[1]), X = int.Parse(attack[2]) };

                input = inputs[1];
            }

            var moveUsed = input.Substring(0, "MOVE E".Length);
            switch (input)
            {
                case "MOVE S": instruction.Direction = Direction.South; break;
                case "MOVE N": instruction.Direction = Direction.North; break;
                case "MOVE W": instruction.Direction = Direction.West; break;
                case "MOVE E": instruction.Direction = Direction.Est; break;
            }

            var loadedDevice = input.Substring(moveUsed.Length);
            var parsedLoad = Enum.TryParse<DeviceType>(loadedDevice.ToPascalCase(), out var deviceTypeLoaded);
            instruction.DeviceLoad = deviceTypeLoaded;

            Console.Error.WriteLine($"Instruction decrypted: {instruction}");

            return instruction;

        }
    }

    public static class DeviceTypeHelpers
    {
        public static string ToText(this DeviceType deviceType)
        {
            switch (deviceType)
            {
                case DeviceType.Torpedo: return "Torpedo";
                case DeviceType.Sonar: return "Sonar";
                case DeviceType.Silence: return "Silence";
                case DeviceType.Mine: return "Mine";
            }
            return "None";
        }
    }
    
    public static class StringHelpers
    {
        public static string ToPascalCase(this string text)
        {
            if (text == null || text.Length < 2)
                return text;

            return text.Substring(0, 1).ToUpper() + text.Substring(1).ToLower();
        }
    }
    #endregion

    #region Enums
    public enum PlayerType
    {
        None = 0,
        Me = 1,
        Enemy = 2
    }

    public enum CellType
    {
        Unknow = 0,
        Island = 1,
        Empty = 2
    }

    public enum DeviceType
    {
        None = 0,
        Torpedo = 1,
        Sonar = 2,
        Silence = 3,
        Mine = 4
    }

    public enum Direction
    {
        None = 0,
        West = 1,
        Est = 2,
        North = 3,
        South = 4,
        Surface = 99
    }
    #endregion

    #endregion
}
