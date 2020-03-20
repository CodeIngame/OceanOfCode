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


            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");
            Console.WriteLine("7 7");

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

        public List<Direction> LastMoves = new List<Direction>();

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
            return "TORPEDO {0} {1}";
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

        public Player Me => Players.First(p => p.PlayerType == PlayerType.Me);
        public Player Enemy => Players.First(p => p.PlayerType == PlayerType.Enemy);

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
            Console.Error.WriteLine($"current position : {Me.Position} - want to {direction.ToMove()}");
            mapCell.Visited = true;
            Me.LastMoves.Add(direction);
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
                Map.MapConfiguration.Add(Helpers.ReadLine(debug: false).Select(c =>
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

#if WriteDebug
            // Console.Error.WriteLine($"mapRow: {Map.MapConfiguration.Count}");
#endif
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
                Enemy.LastMoves.Add(opponentOrders.ToDirection());

        }
        #endregion

        public void WritePosition()
        {
            Console.Error.WriteLine($"My position is :{Me.Position}");
            Console.Error.WriteLine($"Enemy position is :{Enemy.Position}");
        }


        public void Play()
        {
            string actions = "";

            UseDevice(ref actions);
            Move(ref actions);
            LoadDevice(ref actions);

            Counter++;
            Console.WriteLine(actions);

        }

        /// <summary>
        /// Attaquer
        /// </summary>
        private void UseDevice(ref string actions)
        {
            if (Me.Torpedo.CanUse())
            {

#if WriteDebug
                Console.Error.WriteLine($"I can use torpedo");
#endif

                var lastMoves = Me.LastMoves.Skip(Me.LastMoves.Count - 4).Take(3).ToList();
                var lastEnemyMoves = Enemy.LastMoves.Skip(Me.LastMoves.Count - 4).Take(3).ToList();
                if (lastMoves.SequenceEqual(lastEnemyMoves))
                {
#if WriteDebug
                    Console.Error.WriteLine($"same scheme -> shoot him");
#endif
                    var action = Me.Torpedo.Use();

                    // faudra faire attention à l'ile
                    var isOutOfSouth = Me.Position.Y + 2 > Map.Height-1;
                    var isOutOfNorth = Me.Position.Y - 2 < 0 ;
                    var isOutOfEst = Me.Position.X + 2 > Map.Width - 1;
                    var isOutOfWest = Me.Position.X - 2 < 0;

                    var offsetY = isOutOfSouth ? -2 : +2 ;
                    var offsetX = isOutOfWest ? -2 : +2;



                    actions += string.Format($"{action}|", Me.Position.Y+offsetY, Me.Position.X+offsetX);
                }
            }

        }

        /// <summary>
        /// Déplacer
        /// </summary>
        private void Move(ref string actions)
        {
            // On regarde les positions proches
            var west = MapCell(PlayerType.Me, Direction.West);
            var est = MapCell(PlayerType.Me, Direction.Est);
            var north = MapCell(PlayerType.Me, Direction.North);
            var south = MapCell(PlayerType.Me, Direction.South);

            // on regarde les quels sont accessibles et pour le moment on prend la première
            if (CanDoAction(west, Direction.West))
                actions += Direction.West.ToMove();
            else if (CanDoAction(est, Direction.Est))
                actions += Direction.Est.ToMove();
            else if (CanDoAction(north, Direction.North))
                actions += Direction.North.ToMove();
            else if (CanDoAction(south, Direction.South))
                actions += Direction.South.ToMove();
            else
            {
                actions += "SURFACE";
            }
        }

        /// <summary>
        /// Charger
        /// </summary>
        private void LoadDevice(ref string actions)
        {
            if (Me.Torpedo.Couldown > 0)
                actions += " TORPEDO";

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

    public static class CharHelpers
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
            }
            return null;
        }

        public static Direction ToDirection(this string move)
        {
            var input = move;
            var multiOrders = input.Contains("|");
            if(multiOrders)
                input = input.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)[1];

            switch (input)
            {
                case "MOVE S": return Direction.South;
                case "MOVE N": return Direction.North;
                case "MOVE W": return Direction.West;
                case "MOVE E": return Direction.Est;
            }
            return Direction.None;
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
        South = 4
    }
    #endregion

    #endregion
}
