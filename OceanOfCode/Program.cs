#define WriteDebug

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace OceanOfCode
{
    class Program
    {
        static void Main(string[] args)
        {
            var gameManager = new GameManager();
            gameManager.InitializeMap();

            while (true)
            {
                gameManager.SetPlayersInformations();
                gameManager.Play();
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
        public List<List<MapCell>> Maze2D { get; set; } = new List<List<MapCell>>();

        #region Helpers
        public MapCell this[int x, int y] => Maze2D[y][x];
        public MapCell this[Position p] => Maze2D[p.Y][p.X];

        #endregion

        /// <summary>
        /// Permet d'afficher la carte
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var mapTxt = new StringBuilder();
            Maze2D.ForEach(row =>
            {
                row.ForEach(c =>
                {
                    mapTxt.Append($"{c.Cell}");
                });
                mapTxt.AppendLine("");
            });
            return mapTxt.ToString();
        }


    }
    public class MapCell
    {
        /// <summary>
        /// La position de la cellule
        /// </summary>
        public Position Position { get; set; }
        /// <summary>
        /// Le caractére qui correspond é la cellule
        /// </summary>
        public char Cell { get; set; }

        /// <summary>
        /// Pour la recherche de chemin il faut un parent
        /// </summary>
        public MapCell Parent;

        public bool CanGoHere => !Visited && CellType == CellType.Empty;
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
    public class PositionFinder
    {
        /// <summary>
        /// La somme de g + h
        /// </summary>
        public int F { get; set; }
        /// <summary>
        /// La distance entre ce point et le début
        /// </summary>
        public int G { get; set; }
        /// <summary>
        /// La distance entre ce point et la cible
        /// </summary>
        public int H { get; set; }

    }
    public class Position
        : PositionFinder
    {
        public int X { get; set; } = -1;
        public int Y { get; set; } = -1;

        public Position()
        { }

        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Position(Position p) : this(p.X, p.Y)
        {
        }

        /// <summary>
        /// Permet d'obtenir la section de la position
        /// </summary>
        public int Section => this.ToSection();
        /// <summary>
        /// Détermine si la position est connu
        /// </summary>
        public bool Known => X != -1 && Y != -1;
        public string Coordonate => $"{X} {Y}";
        public override string ToString()
        {
            return $"{{{X}:{Y}}}";
        }

        #region Operators
        public override bool Equals(object obj)
        {
            Position p2 = obj as Position;
            if (p2 == null)
                return false;

            return X == p2.X && Y == p2.Y;
        }

        public static bool operator ==(Position a, Position b)
        {
            return a.X.Equals(b?.X) && a.Y.Equals(b?.Y);
        }

        public static bool operator !=(Position a, Position b)
        {
            return !a.X.Equals(b?.X) || !a.Y.Equals(b?.Y);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion

    }

    public class EstimatedPosition
        : Position
    {

        public int XPrecision { get; set; } = -1;
        public int YPrecision { get; set; } = -1;

        public EstimatedPosition()
        {
        }

        public EstimatedPosition(Position p)
        {
            X = p.X;
            Y = p.Y;
        }

        public EstimatedPosition(EstimatedPosition p)
        {
            X = p.X;
            Y = p.Y;
            XPrecision = p.XPrecision;
            YPrecision = p.YPrecision;
        }

        public override string ToString()
        {
            if (XPrecision != -1 && YPrecision != -1)
                return $"{{{X}:{Y}}} precision: {XPrecision}:{YPrecision}";

            return base.ToString();
        }
    }

    public class VirtualPlayer
    {
        public bool StillInGame { get; set; } = true;
    }

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

        /// <summary>
        /// Provient des instructions
        /// </summary>
        public EstimatedPosition LastEstimatedPosition
        {
            get { return LastInstruction.EstimatedPosition; }
            set { LastInstruction.EstimatedPosition = value; }
        }

        public List<Instruction> LastInstructions { get; set; } = new List<Instruction>();
        public Instruction LastInstruction
        {
            get { return LastInstructions.Count > 0 ? LastInstructions[LastInstructions.Count - 1] : new Instruction(); }
            set { LastInstructions[LastInstructions.Count - 1] = value; }
        }

        public List<int> HealthPointHistoric { get; set; } = new List<int>();

        #region Helpers
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

    #region Orders
    public abstract class BaseOrder
    {
        public string Order { get; set; }
        public OrderType OrderType { get; protected set; }

        public abstract string ToCommand();

    }

    #region Devices
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

    public class Silence
    : Device
    {
        public Silence() : base()
        {
            DeviceType = DeviceType.Silence;
            Range = 4;
        }

        public int Distance { get; set; } = -1;
        public Direction Direction { get; set; } = Direction.None;

        public override bool CanUse()
        {
            return Couldown == 0;
        }


        public override string ToCommand()
        {
            return $"{DeviceType.ToText()} {Direction.ToChar()} {Distance}";
        }
    }
    #endregion
    public class Move
        : BaseOrder
    {
        public Direction Direction { get; set; } = Direction.None;
        public DeviceType DeviceLoading { get; set; } = DeviceType.None;

        public int X { get; set; }
        public int Y { get; set; }

        public Move()
        {
            OrderType = OrderType.Move;
        }

        public override string ToCommand()
        {
            var loading = DeviceLoading != DeviceType.None ? $" {DeviceLoading.ToText().ToUpper()}" : "";
            return $"MOVE {Direction.ToChar()}{loading}";
        }
    }

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
    #endregion

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


    #endregion

    public class GameManager
    {
        public List<Player> Players { get; set; } = new List<Player> { new Player { PlayerType = PlayerType.Me }, new Player { PlayerType = PlayerType.Enemy } };
        public Map Map { get; set; } = new Map();

        public List<Player> EnemyVirtualPlayers { get; set; } = new List<Player>();
        public List<Player> MeVirtualPlayers { get; set; } = new List<Player>();

        public void ResetVirtualPlayer(PlayerType playerType)
        {
            var virtualPlayer = playerType == PlayerType.Me ? MeVirtualPlayers : EnemyVirtualPlayers;
            virtualPlayer.ForEach(vp => vp.StillInGame = true);
        }

        /// <summary>
        /// Le nombre de tour
        /// </summary>
        public int Counter { get; set; } = 0;
        public bool VirtualPlayersUsed { get; set; } = false;

        #region Player Methods
        public Player Me => Players.First(p => p.PlayerType == PlayerType.Me);
        public Player Enemy => Players.First(p => p.PlayerType == PlayerType.Enemy);
        #endregion

        #region Map Methods
        public MapCell MapCell(PlayerType playerType, Direction direction, int distance = 1)
        {
            Player _player;
            if (playerType == PlayerType.Me)
                _player = Me;
            else
                _player = Enemy;

            var xOffset = direction.GetOffset(OffsetType.XOffset);
            var yOffset = direction.GetOffset(OffsetType.YOffset);

            xOffset *= distance;
            yOffset *= distance;

            // On veut aller é l'ouest mais on est déjà au maximun ...
            if (_player.Position.X == 0 && direction == Direction.West)
                return null;

            // On veut aller é l'est mais on est déjà au maximun ...
            if (_player.Position.X == Map.Width - 1 && direction == Direction.Est)
                return null;

            // On veut aller au sud mais on est déjà au maximun ...
            if (_player.Position.Y == Map.Height - 1 && direction == Direction.South)
                return null;

            // On veut aller au nord mais on est déjà au maximun ...
            if (_player.Position.Y == 0 && direction == Direction.North)
                return null;


            if (!_player.Position.IsValidPosition(Map, yOffset, xOffset))
                return null;


            var cell = Map[_player.Position.X + xOffset, _player.Position.Y + yOffset];
            return cell;
        }
        public void ResetVisitedCells()
        {
            Map.Maze2D.ForEach(row =>
            {
                row.ForEach(c =>
                {
                    c.Visited = false;
                    c.Parent = null;
                    c.Position.F = 0;
                    c.Position.G = 0;
                    c.Position.H = 0;
                }
                );
            });
        }
        public MapCell EmptyCell()
        {
            // Il est important de positionner le navire dans la plus grande étendu d'eau !
            // On va récupérer les cellules par section
            var dico = new Dictionary<int, List<MapCell>>();
            Map.Maze2D.ForEach(row =>
            {
                row.ForEach(c =>
                {
                    var sectionNumber = c.Position.Section;
                    var isEmpty = c.CellType == CellType.Empty && !c.Visited;

                    if (!dico.ContainsKey(sectionNumber))
                        dico.Add(sectionNumber, new List<MapCell>());

                    if (isEmpty)
                        dico[sectionNumber].Add(c);
                });
            });

            int maxEmptyCell = 0;
            int sectionToUse = 0;
            foreach (var group in dico)
            {
                if (group.Value.Count >= maxEmptyCell)
                {
                    maxEmptyCell = group.Value.Count;
                    sectionToUse = group.Key;
                }
            }

            Console.Error.WriteLine($"Will use section: {sectionToUse} on -> {dico[sectionToUse][maxEmptyCell / 2].Position}");

            return dico[sectionToUse][maxEmptyCell / 2];
        }
        public void BuildVirtualPlayers()
        {
            Console.Error.Write("BuildVirtualMaps");
            int playerId = 10;
            var yNumber = 0;
            Map.Maze2D.ForEach(row =>
            {
                var xNumber = 0;
                row.ForEach(c =>
                {
                    var isEmpty = c.CellType == CellType.Empty;
                    if (isEmpty)
                    {
                        //var position = new Position { X = xNumber, Y = yNumber };
                        // Console.Error.WriteLine($"Adding virtual player at: {position}");
                        var player = new Player { PlayerId = playerId, Position = new Position { X = xNumber, Y = yNumber } };
                        var player2 = new Player { PlayerId = playerId, Position = new Position { X = xNumber, Y = yNumber } };

                        EnemyVirtualPlayers.Add(player);
                        MeVirtualPlayers.Add(player2);
                        playerId++;
                    }
                    xNumber++;
                });
                yNumber++;
            });
            Console.Error.WriteLine($" -> Done with: {EnemyVirtualPlayers.Count}");
        }
        public void AddSilenceVirtualPlayer(PlayerType p)
        {
            // On va ajouter 16 joueurs dans le games
            // Todo é voir
            // Uniquement dans le cas ou la position est connue
        }

        #region Helpers
        public MapCell West(PlayerType pt = PlayerType.Me) => MapCell(pt, Direction.West);
        public MapCell North(PlayerType pt = PlayerType.Me) => MapCell(pt, Direction.North);
        public MapCell South(PlayerType pt = PlayerType.Me) => MapCell(pt, Direction.South);
        public MapCell Est(PlayerType pt = PlayerType.Me) => MapCell(pt, Direction.Est);

        public bool CanWest => CanDoAction(West());
        public bool CanEst => CanDoAction(Est());
        public bool CanNorth => CanDoAction(North());
        public bool CanSouth => CanDoAction(South());

        #endregion
        public bool CanDoAction(MapCell mapCell)
        {
            if (mapCell == null)
                return false;

            if (mapCell.Visited)
                return false;

            if (mapCell.CellType != CellType.Empty)
                return false;


            return true;
        }
        #endregion

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
            for (int y = 0; y < Map.Height; y++)
            {
                var x = 0;
                Map.Maze2D.Add(Helpers.ReadLine(debug: false).Select(c =>
                {
                    var cell = new MapCell
                    {
                        Cell = c,
                        Position = new Position { X = x, Y = y }
                    };
                    x++;

                    return cell;
                }).ToList());
            }

            BuildVirtualPlayers();

            //Console.Error.WriteLine("--Map saved format--");
            //Console.Error.WriteLine(Map.ToString());


            //On doit maintenant choisir sa position de départ
            var startCell = EmptyCell();
            startCell.Visited = true;

#if WriteDebug
            Console.Error.WriteLine($"Map initialized - start position {startCell.Position}");
#endif
            Console.WriteLine($"{startCell.Position.X} {startCell.Position.Y}");

        }
        public void SetPlayersInformations()
        {
            #region Update
            var inputs = Helpers.ReadLine(debug: false).Split(' ');
            Me.Position.X = int.Parse(inputs[0]);
            Me.Position.Y = int.Parse(inputs[1]);

            if (Counter > 0)
            {
                // Non disponible au 1er tours
                // Todo si l'enemie perds des points de vie c'est que (il s'est touché lui méme ou que je lui ai tiré dessus)

                var myHp = int.Parse(inputs[2]);
                var enemyHp = int.Parse(inputs[3]);
                Me.HealthPoint = myHp;
                Me.HealthPointHistoric.Add(myHp);
                Enemy.HealthPoint = enemyHp;
                Enemy.HealthPointHistoric.Add(enemyHp);

            }


            if (inputs.Length >= 7)
            {
                Me.Torpedo.Couldown = int.Parse(inputs[4]);
                Me.Sonar.Couldown = int.Parse(inputs[5]);
                Me.Silence.Couldown = int.Parse(inputs[6]);
                Me.Mine.Couldown = int.Parse(inputs[7]);
            }

            string sonarResult = Helpers.ReadLine(debug: false);
            string opponentOrders = Helpers.ReadLine(debug: false);

            // Mon ordre de sonar est validé
            if (sonarResult != "NA")
            {
                var sonarCommand = Me.LastInstruction.SonarCommand;
                if (sonarResult.Contains("Y"))
                {
                    var currentEnemySector = Enemy.LastInstruction.EstimatedPosition.Section;
                    var isOk = sonarCommand.Sector == currentEnemySector;
                    if (!isOk)
                    {
                        Console.Error.WriteLine("SetPlayersInformations -> updated position from sonar");
                        Enemy.LastEstimatedPosition = new EstimatedPosition(sonarCommand.Sector.ToMidleSectionPosition()) { XPrecision = 4, Y = 4 };
                    }

                }
            }

            // On enregistre le précédent déplacement
            if (opponentOrders != "NA")
            {
                var copy = new Instruction { EstimatedPosition = new EstimatedPosition(Enemy.LastEstimatedPosition) };
                Enemy.LastInstructions.Add(opponentOrders.ToInstructions(copy));
            }
            #endregion
        }
        #endregion

        public void Play()
        {
            Console.Error.WriteLine($"Starting a new turn: {Counter + 1}");
            var instruction = new Instruction();

            AnalyzeEnemy(instruction);
            Move(instruction);
            UseDevice(instruction);
            LoadDevice(instruction);

            Me.LastInstructions.Add(instruction);

            AnalyseMe();

            Counter++;
            Console.WriteLine(instruction.ToCommand());

        }

        #region Analyze
        private void AnalyseMe()
        {
            // todo il faut analyser le silence pour reset le virtual player
            if (Counter >= 0)
            {
                //foreach (var cmd in Me.LastInstruction.Commands)
                //{
                //    switch (cmd.OrderType)
                //    {
                //        case OrderType.None:
                //            break;
                //        case OrderType.Move:
                //            AnalyseMove(instruction);
                //            AnalyseVirtualPlayers(PlayerType.Enemy);
                //            break;
                //        case OrderType.Device:
                //            var device = (Device)cmd;



                //            break;
                //        case OrderType.Surface:
                //            break;
                //    }
                //}
                if(Me.LastInstruction.SurfaceCommand != null)
                {

                }
                AnalyseVirtualPlayers(PlayerType.Me);

                if (Me.LastInstruction.SilenceCommand != null)
                {

                }
            }

        }

        /// <summary>
        /// Permet d'analyser ce qui s'est passé sur le tour précédent
        /// </summary>
        /// <param name="instruction"></param>
        private void AnalyzeEnemy(Instruction instruction)
        {
            AnalyseCommand(Enemy.LastInstruction);

            if (Counter > 1)
            {
                Console.Error.WriteLine("--Analyze--");

                // Il faut analyser dans l'ordre des commandes de l'enemie
                foreach (var cmd in Enemy.LastInstruction.Commands)
                {
                    switch (cmd.OrderType)
                    {
                        case OrderType.None:
                            break;
                        case OrderType.Move:
                            AnalyseMove(instruction);
                            AnalyseVirtualPlayers(PlayerType.Enemy);
                            break;
                        case OrderType.Device:
                            var device = (Device)cmd;
                            AnalyseDevice(device, instruction);
                            break;
                        case OrderType.Surface:
                            break;
                    }
                }


                Console.Error.WriteLine("--Analyze Done--");
            }
        }

        #region Analyse Logics

        private void AnalyseCommand(Instruction instruction)
        {
            if (string.IsNullOrEmpty(instruction.FullCommand))
            {
                Console.Error.WriteLine($"No command to analyse");
                return;
            }

            Console.Error.Write($"AnalyseCommand Ep {instruction.EstimatedPosition}");
            // MOVE direction POWER
            // MOVE N TORPEDO
            // MOVE E| TORPEDO 8 6
            // MOVE E| TORPEDO 8 6
            // TORPEDO 0 8|MOVE E TORPEDO
            // SURFACE 5 | MOVE W
            // SURFACE 7
            // MOVE W TORPEDO|TORPEDO 2 11|SONAR 4|SILENCE W 4
            // TODO

            var cmd = instruction.FullCommand;
            foreach (var order in cmd.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                // un déplacement
                if (order.Contains("MOVE"))
                {
                    var orderMove = order.Substring(0, "MOVE N".Length);
                    var direction = Direction.None;
                    switch (orderMove)
                    {
                        case "MOVE S": direction = Direction.South; break;
                        case "MOVE N": direction = Direction.North; break;
                        case "MOVE W": direction = Direction.West; break;
                        case "MOVE E": direction = Direction.Est; break;
                    }


                    var loading = order.Substring(orderMove.Length - 1);

                    instruction.Commands.Add(new Move { Order = order, Direction = direction });

                }
                // une surface
                else if (order.Contains("SURFACE"))
                {
                    var orderSurface = order.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var sector = int.Parse(orderSurface[1]);
                    if (instruction.EstimatedPosition.Section != sector)
                    {
                        var middle = sector.ToMidleSectionPosition();
                        instruction.EstimatedPosition = new EstimatedPosition(middle) { XPrecision = 3, YPrecision = 3 };
                        // Console.Error.Write($" -> Surface set {instruction.EstimatedPosition}");

                    }
                    instruction.Commands.Add(new Surface { Order = order, Sector = sector });
                }
                // déplacement silence
                else if (order.Contains("SILENCE"))
                {
                    instruction.Commands.Add(new Silence { Order = order });
                }
                // Une torpille
                else if (order.Contains("TORPEDO"))
                {
                    var ordersDevice = order.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var parsed = Enum.TryParse<DeviceType>(ordersDevice[0].ToPascalCase(), out var deviceType);
                    if (parsed)
                    {
                        instruction.Commands.Add(new Torpedo { Order = order, Position = new Position { X = int.Parse(ordersDevice[1]), Y = int.Parse(ordersDevice[2]) } });
                    }
                }
                // Le sonar
                else if (order.Contains("SONAR"))
                {
                    var ordersDevice = order.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var parsed = Enum.TryParse<DeviceType>(ordersDevice[0].ToPascalCase(), out var deviceType);
                    if (parsed)
                    {
                        instruction.Commands.Add(new Sonar { Order = order, Sector = int.Parse(ordersDevice[1]) });
                    }
                }
                else
                {
                    Console.Error.Write($" -> order not implemented: {order}");
                }
            }

            Console.Error.Write($" -> Instruction decrypted: {instruction.ToCommand()}");

            Console.Error.Write($" -> Commands load: {instruction.Commands.Count}");

            Console.Error.WriteLine($" => {instruction.EstimatedPosition}");
        }


        #region Analyse Move
        private void AnalyseMove(Instruction instruction)
        {
            Console.Error.Write($"AnalyseMove Ep: {(Enemy.Position.Known ? Enemy.Position : Enemy.LastInstruction.EstimatedPosition)}");
            var _analyseMoveCase = "0";

            var lastInstruction = Enemy.LastInstruction;
            var lastDirection = lastInstruction.MoveCommand?.Direction ?? Direction.None;
            if (!Enemy.Position.Known && Enemy.LastInstructions.Count > 0)
            {
                //On regarde la position estimé du dernier coup (la position précédante est recopié N-2 = N-1)
                var previousInstruction = Enemy.LastInstruction;
                if (previousInstruction.EstimatedPosition.Known)
                {
                    //On applique le déplacement du coup
                  
                    var newEstimatedPosition = new EstimatedPosition(previousInstruction.EstimatedPosition);

                    var xOffset = lastDirection.GetOffset(OffsetType.XOffset);
                    var yOffset = lastDirection.GetOffset(OffsetType.YOffset);

                    if (!newEstimatedPosition.IsValidPosition(Map, yOffset, xOffset))
                    {
                        _analyseMoveCase = "1.1";

                        // On reset pour le moment ...
                        newEstimatedPosition.X = -1;
                        newEstimatedPosition.Y = -1;
                        newEstimatedPosition.XPrecision += 1;
                        newEstimatedPosition.YPrecision += 1;

                        //Console.Error.Write(" No reachable position, define close position");
                    }
                    else
                    {
                        _analyseMoveCase = "1.2";
                        newEstimatedPosition.X += xOffset;
                        newEstimatedPosition.Y += yOffset;
                    }

                    lastInstruction.EstimatedPosition = newEstimatedPosition;
                    // Console.Error.WriteLine($"Enemy estimated postion updated to: {lastInstruction.EstimatedPosition}");
                }
                else
                {
                    _analyseMoveCase = "2.1";

                    // On connait pas encore sa positon mais il a fait surface
                    if (previousInstruction.SurfaceCommand?.IsValid == true)
                    {
                        _analyseMoveCase = "2.1.1";
                    }
                }
            }

            if (Enemy.Position.Known)
            {
                // Console.Error.WriteLine("Track move !");
                _analyseMoveCase = "3.1";

                var xOffset = lastDirection.GetOffset(OffsetType.XOffset);
                var yOffset = lastDirection.GetOffset(OffsetType.YOffset);

                var newEnemyPosition = new Position(Enemy.Position);

                // Console.Error.WriteLine($"Tracked position {newEnemyPosition} + x: {xOffset}, + y: {yOffset}");


                if (newEnemyPosition.IsValidPosition(Map, yOffset, xOffset))
                {
                    _analyseMoveCase = "3.2";

                    //Console.Error.Write($"Enemy Position : {Enemy.Position} ->");
                    Enemy.Position.X += xOffset;
                    Enemy.Position.Y += yOffset;
                    //Console.Error.WriteLine($" {Enemy.Position}");

                }
            }
            Console.Error.WriteLine($" -> _analyseMoveCase: {_analyseMoveCase} -  EPosition : {(Enemy.Position.Known ? Enemy.Position : lastInstruction.EstimatedPosition)}");
        }


        private void AnalyseVirtualPlayers(PlayerType pt)
        {

            Console.Error.Write("AnalyseVirtualPlayer");

            var direction = pt == PlayerType.Me 
                ? Me.LastInstruction.MoveCommand?.Direction ?? Direction.None 
                : Enemy.LastInstruction.MoveCommand?.Direction ?? Direction.None;

            if (direction == Direction.None)
                return;

            var virtuals = pt == PlayerType.Me ? MeVirtualPlayers : EnemyVirtualPlayers;

            var vpToTake = virtuals.Where(ev => ev.StillInGame).ToList();
            for (var i = 0; i < vpToTake.Count; i++)
            {
                if (vpToTake[i].Position.IsValidPosition(Map, direction))
                {
                    var xOffset = direction.GetOffset(OffsetType.XOffset);
                    var yOffset = direction.GetOffset(OffsetType.YOffset);

                    vpToTake[i].Position.X += xOffset;
                    vpToTake[i].Position.Y += yOffset;
                }
                else
                {
                    vpToTake[i].StillInGame = false;
                }
            }

            var stillInGame = vpToTake.Count(ev => ev.StillInGame);
            if (stillInGame == 1)
            {
                if (pt == PlayerType.Enemy)
                {
                    Enemy.Position = new Position(vpToTake.First(x => x.StillInGame).Position);
                    VirtualPlayersUsed = true;
                }

                if (pt == PlayerType.Me)
                {
                    Console.Error.WriteLine($" Enemy know i'm at {vpToTake.First(x => x.StillInGame).Position}");
                }
            }

            Console.Error.WriteLine($" ->  {stillInGame} {pt.ToString()}");
        }

        #endregion


        #region Analyse Device

        private void AnalyseDevice(Device device, Instruction instruction)
        {
            switch (device.DeviceType)
            {
                case DeviceType.Mine:
                    break;
                case DeviceType.Sonar:
                    break;
                case DeviceType.Silence:
                    // Il faut reset les virtual players
                    AddSilenceVirtualPlayer(PlayerType.Enemy);
                    ResetVirtualPlayer(PlayerType.Enemy);

                    break;
                case DeviceType.Torpedo:
                    AnalyseToperdo(instruction);
                    AnalyseTorpedoHitV2(instruction);
                    break;
            }
        }

        private void AnalyseTorpedoHitV2(Instruction instruction)
        {
            // Pas d'analyse si la position est connue
            if (Enemy.Position.Known)
                return;

            Console.Error.Write($"AnalyseHit Ep: {(Enemy.LastInstruction.EstimatedPosition)}");
            var _analyseHitCase = "0";
            var associatedMessage = string.Empty;

            var enemyLostHp = Enemy.Touched;
            var enemyUsedToperdo = Enemy.LastInstruction.TorpedoCommand?.IsValid == true;
            var iUsedTorpedo = Me.LastInstruction.TorpedoCommand?.IsValid == true;
            // var enemyTouchedPerfect = Enemy.TouchedPerfect;

            if (enemyLostHp && !enemyUsedToperdo && !iUsedTorpedo)
            {
                // Surface
                _analyseHitCase = "1";
                associatedMessage = "Enemy used surface";
            }
            else if (enemyLostHp && enemyUsedToperdo && !iUsedTorpedo)
            {
                // Auto shoot
                _analyseHitCase = "2";
                if (Enemy.TouchedPerfect)
                {
                    _analyseHitCase = "2.1";
                    associatedMessage = "Auto shoot perfect";
                    Enemy.Position = new Position(Enemy.LastInstruction.TorpedoCommand.Position);

                }
                else
                {
                    _analyseHitCase = "2.2";
                    associatedMessage = "Auto shoot partial";
                    if (Enemy.LastEstimatedPosition.XPrecision > 1 && Enemy.LastEstimatedPosition.YPrecision > 1)
                        Enemy.LastEstimatedPosition = new EstimatedPosition(Enemy.LastInstruction.TorpedoCommand.Position) { XPrecision = 1, YPrecision = 1 };
                    else
                        associatedMessage = "Auto shoot but precision is same (do not reset position)";
                }

            }
            else if (enemyLostHp && !enemyUsedToperdo && iUsedTorpedo)
            {
                //J'ai bien tiré
                _analyseHitCase = "3";
                if (Enemy.TouchedPerfect)
                {
                    _analyseHitCase = "3.1";
                    associatedMessage = "shoot was perfect";
                    Enemy.Position = new Position(Me.LastInstruction.TorpedoCommand.Position);
                }
                else
                {
                    _analyseHitCase = "3.2";
                    associatedMessage = "shoot partial";
                    Enemy.LastEstimatedPosition = new EstimatedPosition(Me.LastInstruction.TorpedoCommand.Position) { XPrecision = 1, YPrecision = 1 };
                }
            }
            else if (enemyLostHp && enemyUsedToperdo && iUsedTorpedo)
            {
                _analyseHitCase = "4";
                var distance = Me.LastInstruction.TorpedoCommand.Position.Distance(Enemy.LastInstruction.TorpedoCommand.Position);
                if (distance > Torpedo.Range)
                {
                    _analyseHitCase = "4.1";
                    if (Enemy.TouchedPerfect)
                    {
                        _analyseHitCase = "4.1.2";
                        associatedMessage = "shoot was perfect";
                        Enemy.Position = new Position(Me.LastInstruction.TorpedoCommand.Position);
                    }
                    else
                    {
                        _analyseHitCase = "4.1.2";
                        associatedMessage = "shoot partial";
                        if (Enemy.LastEstimatedPosition.XPrecision > 1 && Enemy.LastEstimatedPosition.YPrecision > 1)
                            Enemy.LastEstimatedPosition = new EstimatedPosition(Me.LastInstruction.TorpedoCommand.Position) { XPrecision = 1, YPrecision = 1 };
                        else
                            associatedMessage = "shoot partial but precision is same (do not reset position)";
                    }
                }
                else
                {
                    _analyseHitCase = "4.2";

                    var hpLost = Enemy.TotalHpLost;
                    if (hpLost == 1)
                    {
                        _analyseHitCase = "4.2.1";
                        associatedMessage = $"We shoot togather and he lost {hpLost} hp";
                        // Enemy.LastEstimatedPosition = new EstimatedPosition(Me.LastInstruction.DeviceUsedPosition) { XPrecision = 4, YPrecision = 4 };
                    }
                    else if (hpLost == 2)
                    {
                        _analyseHitCase = "4.2.2";
                    }
                    else if (hpLost == 3)
                    {
                        _analyseHitCase = "4.2.3";
                    }
                }

            }
            else if (!enemyLostHp && iUsedTorpedo)
            {
                _analyseHitCase = "5";
                associatedMessage = "Shoot failed, reset precision";
                // j'ai raté mon tire..
                Enemy.LastEstimatedPosition = new EstimatedPosition();
                //Enemy.LastEstimatedPosition.XPrecision = 4;
                //Enemy.LastEstimatedPosition.YPrecision = 4;
            }
            Console.Error.WriteLine($" -> _analyseHitCase: {_analyseHitCase} - msg: {associatedMessage} new (estimated) Ep: {(Enemy.Position.Known ? Enemy.Position : Enemy.LastEstimatedPosition)}");

        }

        private void AnalyseToperdo(Instruction instruction)
        {
            if (Enemy.Position.Known)
                return;

            Console.Error.Write("AnalyseToperdo");

            var associatedMessage = string.Empty;

            var shouldAnalyse = true;
            if (Enemy.LastInstructions.Count > 0)
            {
                var previousInstruction = Enemy.LastInstructions[Enemy.LastInstructions.Count - 2];
                shouldAnalyse = !previousInstruction.EstimatedPosition.Known
                    || (previousInstruction.EstimatedPosition.XPrecision == Torpedo.Range && previousInstruction.EstimatedPosition.YPrecision == Torpedo.Range)
                    || (previousInstruction.EstimatedPosition.XPrecision == -1 && previousInstruction.EstimatedPosition.YPrecision == -1);
                //if(!shouldAnalyse)
                //    Console.Error.WriteLine($"Do not analyse 'AnalyseToperdo'");
            }

            if (shouldAnalyse)
            {
                var lastInstruction = Enemy.LastInstruction;
                if (lastInstruction.TorpedoCommand?.IsValid == true)
                {
                    lastInstruction.EstimatedPosition = new EstimatedPosition(lastInstruction.TorpedoCommand.Position);
                    lastInstruction.EstimatedPosition.XPrecision = Torpedo.Range;
                    lastInstruction.EstimatedPosition.YPrecision = Torpedo.Range;

                    associatedMessage = $" -> Enemy close to {lastInstruction.EstimatedPosition} - precision : {Torpedo.Range}";
                }
            }

            Console.Error.WriteLine(associatedMessage);


        }

        #endregion

        #endregion

        #endregion

        #region Use Device
        /// <summary>
        /// Attaquer
        /// </summary>
        private void UseDevice(Instruction instruction)
        {
            // il faut tirer et regarder au prochain coups si on a touché quelque chose
            // on combine le tout avec les derniers déplacement pour localiser la position de quelqu'un
            if (Me.Torpedo.CanUse())
                UseTorpedo(instruction);
            if (Me.Silence.CanUse())
                UseSilence(instruction);


        }

        private void UseTorpedo(Instruction instruction)
        {
            var _useTorpedo = "0";
            var associatedMessage = string.Empty;
            Console.Error.Write($"UseTorpedo");

            MapCell cellToAttack = null;
            if (Enemy.Position.Known)
            {
                _useTorpedo = "1";
                var distance = Me.Position.Distance(Enemy.Position);
                if (distance <= Torpedo.Range)
                    cellToAttack = Map[Enemy.Position];

                associatedMessage = $"[A] Position know - distance:{distance}";
            }
            else if (Enemy.LastEstimatedPosition.Known)
            {
                _useTorpedo = "2";
                var distance = Me.Position.Distance(Enemy.LastEstimatedPosition);
                if (distance <= Torpedo.Range)
                {
                    _useTorpedo = "2.1";
                    cellToAttack = Map[Enemy.LastEstimatedPosition];

                }
                else if (distance > Torpedo.Range && distance < Torpedo.Range + 2)
                {
                    _useTorpedo = "2.2";
                    var idealTarget = Enemy.LastEstimatedPosition;
                    var actualPosition = Me.Position;
                    var lastPath = PathFinder.FindPath(actualPosition, idealTarget, Map, false);
                    if (lastPath.Count >= Torpedo.Range)
                        cellToAttack = lastPath[Torpedo.Range - 2];
                }

                associatedMessage = $"[A] Estimated Position know - distance:{distance}";

            }
            else
            {
                _useTorpedo = "3";

            }


            // On se se tire pas dessus !
            if (cellToAttack != null && (cellToAttack.Position == Me.Position || cellToAttack.Position.Distance(Me.Position) <= 2)) //&& Me.HealthPoint < Enemy.HealthPoint)
            {
                _useTorpedo = "4";
                cellToAttack = null;
                associatedMessage = $"[A] Canceled shoot";

            }


            if (cellToAttack != null)
            {
                instruction.Commands.Add(new Torpedo { Position = cellToAttack.Position });

                associatedMessage += $" -- Me: {Me.Position} shoot -> {cellToAttack.Position}";
                // Console.Error.WriteLine($"Me: {Me.Position} shoot -> {cellToAttack.Position}");
            }

            Console.Error.Write($"-> _useTorpedo: {_useTorpedo} - msg: {associatedMessage}");


        }

        private void UseSilence(Instruction instruction)
        {

            // Utilisation du silence en défensif
            var virtualMeInGame = MeVirtualPlayers.Count(x => x.StillInGame);
            if (virtualMeInGame < 10)
            {
                var d = new List<Direction> { Direction.Est, Direction.West, Direction.None, Direction.South };

                var choices = new Dictionary<Direction, List<int>> {
                    { Direction.Est, Enumerable.Range(1, 4).ToList() },
                    { Direction.North, Enumerable.Range(1, 4).ToList() },
                    { Direction.West, Enumerable.Range(1, 4).ToList() },
                    { Direction.South, Enumerable.Range(1, 4).ToList() }
                };

                var result = new Dictionary<Direction, int>
                {
                    {  Direction.Est, 0 },
                    {  Direction.West, 0 },
                    {  Direction.North, 0 },
                    {  Direction.South, 0 }
                };

                int xOffset = 0;
                int yOffset = 0;

                foreach (var _d in choices)
                {
                    xOffset = _d.Key.GetOffset(OffsetType.XOffset);
                    yOffset = _d.Key.GetOffset(OffsetType.YOffset);

                    foreach (var distance in _d.Value)
                    {
                        var isValid = Me.Position.IsValidPosition(Map, yOffset * distance, xOffset * distance, true);
                        if (isValid)
                        {
                            result[_d.Key] = distance;
                        }
                        else
                        {
                            break;
                        }
                    };
                }

                var max = result.ToList().OrderByDescending(x => x.Value).First();

                xOffset = max.Key.GetOffset(OffsetType.XOffset);
                yOffset = max.Key.GetOffset(OffsetType.YOffset);
                Console.Error.Write("UseSilence setting visited: [");
                for (int i = 1; i <= max.Value; i++)
                {
                    Console.Error.Write($" {Map[Me.Position.X + xOffset * i, Me.Position.Y + yOffset * i].Position},");
                    Map[Me.Position.X + xOffset * i, Me.Position.Y + yOffset * i].Visited = true;
                }
                Console.Error.WriteLine("] => done!");

                instruction.Commands.Add(new Silence { Direction = max.Key, Distance = max.Value });
                ResetVirtualPlayer(PlayerType.Me);
            }


        }
        #endregion


        #region Move
        /// <summary>
        /// Déplacer
        /// </summary>
        private void Move(Instruction instruction)
        {
            Console.Error.Write($"Move");
            var _caseMove = "0";
 
            var distance = Me.Position.Distance(Enemy.Position.Known ? Enemy.Position : Enemy.LastEstimatedPosition);

            if (Enemy.Position.Known)
            {
                if (distance >= Torpedo.Range)
                {
                    _caseMove = "1.1";
                    MoveToPosition(instruction, Enemy.Position);
                }
                else
                {
                    // On va tenter un déplacement au centre de la section courante
                    _caseMove = "1.2";
                    var section = Me.Position.Section;
                    MoveToPosition(instruction, section.ToMidleSectionPosition());

                }

            }
            else if (Enemy.LastEstimatedPosition.Known)
            {
                if (distance >= 3)
                {
                    _caseMove = "2.1";
                    MoveToPosition(instruction, Enemy.LastEstimatedPosition);
                }
                else
                {
                    _caseMove = "2.3";
                    // On va tenter un déplacement au centre de la section courante
                    var section = Me.Position.Section;
                    MoveToPosition(instruction, section.ToMidleSectionPosition());

                }
            }
            else
            {
                _caseMove = "3";
                // var empty = EmptyCell();
                // MoveToPosition(instruction, dico, empty.Position);
                MoveToPosition(instruction, new Position { X = 7, Y = 7 });
            }

            var msg1 = $"PEnemy: {Enemy.Position} - distance: {distance}";
            var msg2 = $"(Estimated) PEnemy: {Enemy.LastEstimatedPosition} - distance: {distance}";
            var msg = _caseMove.Contains("1.") ? msg1 : _caseMove.Contains("2.") ? msg2 : "";
            Console.Error.WriteLine($" -> _caseMove: {_caseMove} -> {msg}");

        }

        #region Move Logics
        private void MoveToPosition(Instruction instruction, Position targetPosition)
        {
            var _targetedPosition = targetPosition;
            var myPosition = Me.Position;

            // Si on est déjà sur la cible ou si la cible est inatteignable
            if (myPosition == _targetedPosition || Map[targetPosition].CellType == CellType.Island)
            {
                Console.Error.WriteLine(!Map[targetPosition].CanGoHere ? $" {targetPosition} not accessible" : "I'm still at this position");
                var d = MoveRandom(instruction);
                
                var x = d.GetOffset(OffsetType.XOffset);
                var y = d.GetOffset(OffsetType.YOffset);
                Map[Me.Position.X + x, Me.Position.Y + y].Visited = true;
            }
            else
            {
                var paths = myPosition.FindPath(_targetedPosition, Map);
                if (paths.Count > 0)
                {
                    var direction = myPosition.DirectionToTake(paths[0].Position);
                    Console.Error.WriteLine($" found {paths[0].Position} with: {direction.ToMove()}");

                    paths[0].Visited = true;
                    instruction.Commands.Add(new Move { Direction = direction, X = paths[0].Position.X, Y = paths[0].Position.Y });
                }
                else
                {
                    Console.Error.WriteLine(" No move -> Surface");
                    instruction.Commands.Add(new Surface());
                    ResetVisitedCells();
                    Map[myPosition].Visited = true;
                    // MoveRandom(instruction, dico);
                }
            }

            if(instruction.MoveCommand == null)
            {
                Console.Error.WriteLine("No move..");
            }
            else
            {
                //Me.Position = new Position(instruction.MoveCommand);
                Me.Position.X = instruction.MoveCommand.X;
                Me.Position.Y = instruction.MoveCommand.Y;
            }
        }

        private Direction MoveRandom(Instruction instruction)
        {
            var direction = Direction.None;
            var section = Me.Position.Section;

            switch (section)
            {
                case 1:
                case 2:
                case 3:
                    // on est trop en haut
                    if (CanSouth)
                        direction = Direction.South;

                    else if (section == 1 && CanEst)
                        direction = Direction.Est;

                    else if (section == 3 && CanWest)
                        direction = Direction.West;

                    break;
                case 4:
                    if (CanEst)
                        direction = Direction.Est;
                    else if (CanSouth)
                        direction = Direction.South;
                    break;
                case 6:
                    if (CanWest)
                        direction = Direction.West;
                    else if (CanSouth)
                        direction = Direction.South;
                    break;

                case 7:
                case 8:
                case 9:
                    // on est trop en bas
                    if (CanNorth)
                        direction = Direction.North;
                    else if (section == 7 && CanEst)
                        direction = Direction.Est;
                    else if (section == 9 && CanWest)
                        direction = Direction.West;
                    break;
            }

            if (direction == Direction.None)
            {
                if (CanNorth)
                    direction = Direction.North;
                else if (CanEst)
                    direction = Direction.Est;
                else if (CanSouth)
                    direction = Direction.South;
                else if (CanWest)
                    direction = Direction.West;
                else
                {
                    Console.Error.WriteLine("Will surface ...");
                    direction = Direction.None;
                    instruction.Commands.Add(new Surface());
                    ResetVisitedCells();
                    Map[Me.Position].Visited = true;
                }
            }


            // Je suppose que si je peux y aller je vais y aller
            Console.Error.WriteLine($"myPosition: {Me.Position} - sector: {Me.Position.Section} move to {direction.ToMove()}");
            var newPosition = Me.Position.NewPosition(direction);

            instruction.Commands.Add(new Move { Direction = direction, X = newPosition.X, Y = newPosition.Y });
            return direction;
        }
        #endregion


        #endregion

        #region Loading
        /// <summary>
        /// Charger
        /// </summary>
        private void LoadDevice(Instruction instruction)
        {
            //Todo voir les priorités de chargement
            // Actuellement si je fais surface => pas de move => pas de loading
            if (instruction.MoveCommand == null)
                return;

            Console.Error.WriteLine($"LoadDevice -> silence: {Me.Silence.Couldown}, torpedo: {Me.Torpedo.Couldown}, silence: {Me.Sonar.Couldown}");

          


            if (Me.Silence.Couldown > 0)
            {
                instruction.MoveCommand.DeviceLoading = DeviceType.Silence;
                return;
            }

            if (Me.Torpedo.Couldown > 0)
            {
                instruction.MoveCommand.DeviceLoading = DeviceType.Torpedo;
                return;
            }

            //if (Me.Sonar.Couldown > 0)
            //{
            //    instruction.MoveCommand.DeviceLoading = DeviceType.Sonar;
            //    return;
            //}

            // WTF
            // Console.WriteLine("MOVE W TORPEDO|TORPEDO 2 11|SONAR 4|SILENCE W 4");

        }
        #endregion

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

        public static T DeepClone<T>(this T obj)
        {
            // Use [Serializable]
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }
    }

    public static class CellTypeHelper
    {
        /// <summary>
        /// Permet de passer du caractére au type de cellule
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
        /// <summary>
        /// Permet d'obtenir le niveau de section correspondant é une position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
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
                section += 0;
            else if (position.Y <= 9)
                section += 3;
            else
                section += 6;

            // Console.Error.WriteLine($"{position} is on section {section}");

            return section;
        }

        public static int Distance(this Position p1, Position p2)
        {
            return Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y);
        }

        /// <summary>
        /// Permet d'avoir la direction é prendre entre 2 points
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static Direction DirectionToTake(this Position p1, Position p2)
        {
            var direction = Direction.None;
            if (p1.X > p2.X)
                direction = Direction.West;
            else if (p1.X < p2.X)
                direction = Direction.Est;

            if (p1.Y > p2.Y)
                direction = Direction.North;
            else if (p1.Y < p2.Y)
                direction = Direction.South;

            return direction;
        }

        public static bool IsValidPosition(this Position p1, Map map, Direction nextDirection)
        {
            var xOffset = nextDirection.GetOffset(OffsetType.XOffset);
            var yOffset = nextDirection.GetOffset(OffsetType.YOffset);
            return p1.IsValidPosition(map, yOffset, xOffset);
        }

        public static bool IsValidPosition(this Position p1, Map map, int yOffset = 0, int xOffset = 0, bool checkVisited = false)
        {
            var isValid = true;
            var height = p1.Y + yOffset;
            var width = p1.X + xOffset;

            if (height < 0 || height > map.Height - 1)
                isValid = false;

            if (isValid && (width < 0 || width > map.Width - 1))
                isValid = false;

            if (isValid && map[p1.X + xOffset, p1.Y + yOffset].CellType == CellType.Island)
                isValid = false;

            if (isValid && checkVisited && map[p1.X + xOffset, p1.Y + yOffset].Visited)
                isValid = false;

            // Console.Error.WriteLine($"{p1} is valid position ? {isValid}  ({height}, {width})");

            return isValid;
        }

        public static Position NewPosition(this Position p1, Direction nextDirection)
        {
            var xOffset = nextDirection.GetOffset(OffsetType.XOffset);
            var yOffset = nextDirection.GetOffset(OffsetType.YOffset);

            return new Position(p1.X + xOffset, p1.Y + yOffset);
        }
    }
    public static class DirectionHelpers
    {
        public static int GetOffset(this Direction nextDirection, OffsetType offsetType)
        {
            var xOffset = nextDirection == Direction.West ? -1 : nextDirection == Direction.Est ? 1 : 0;
            var yOffset = nextDirection == Direction.North ? -1 : nextDirection == Direction.South ? 1 : 0;

            return offsetType == OffsetType.XOffset ? xOffset : yOffset;
        }

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

        public static string ToChar(this Direction direction)
        {
            switch (direction)
            {
                case Direction.South: return "S";
                case Direction.North: return "N";
                case Direction.West: return "W";
                case Direction.Est: return "E";
            }
            return null;
        }

        public static Direction ToInverse(this Direction direction)
        {
            switch (direction)
            {
                case Direction.South: return Direction.North;
                case Direction.North: return Direction.South;
                case Direction.West: return Direction.Est;
                case Direction.Est: return Direction.West;

            }
            return Direction.North;
        }
    }

    public static class InstructionHelpers
    {
        /// <summary>
        /// Permet de comprendre l'ordre passer
        /// </summary>
        /// <param name="move"></param>
        /// <returns></returns>
        public static Instruction ToInstructions(this string move, Instruction instruction)
        {

            instruction.FullCommand = move;
            return instruction;

        }
    }

    public static class DeviceTypeHelpers
    {
        public static string ToText(this DeviceType deviceType)
        {
            switch (deviceType)
            {
                case DeviceType.Torpedo: return "TORPEDO";
                case DeviceType.Sonar: return "SONAR";
                case DeviceType.Silence: return "SILENCE";
                case DeviceType.Mine: return "MINE";
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

    public static class PathFinder
    {
        public static List<MapCell> FindPath(this Position start, Position end, Map map, bool useVisited = true)
        {
            // reset de la précédante 
            map.Maze2D.ForEach(row =>
            {
                row.ForEach(c =>
                {
                    c.Parent = null;
                    c.Position.F = 0;
                    c.Position.G = 0;
                    c.Position.H = 0;

                });
            });

            MapCell current = null;
            var openList = new List<MapCell>();
            var closedList = new List<MapCell>();
            int g = 0;
            Console.Error.Write($" Path finding with start:{start} - target:{end}");
            openList.Add(map[start]);
            while (openList.Count > 0)
            {

                // get the square with the lowest F score  
                var lowest = openList.Min(cell => cell.Position.F);
                current = openList.First(cell => cell.Position.F == lowest);

                // Console.Error.WriteLine($"scan for {current.Position}");

                // add the current square to the closed list  
                closedList.Add(current);
                // remove it from the open list  
                openList.Remove(current);

                // if we added the destination to the closed list, we've found a path  
                if (closedList.FirstOrDefault(cell => cell.Position == end) != null)
                    break;

                var adjacentSquares = GetWalkableAdjacentSquares(current.Position, map, openList, useVisited);
                g = current.Position.G + 1;

                foreach (var adjacentSquareCell in adjacentSquares)
                {
                    var adjacentSquare = adjacentSquareCell.Position;
                    // if this adjacent square is already in the closed list, ignore it  
                    if (closedList.FirstOrDefault(cell => cell.Position == adjacentSquare) != null)
                        continue;

                    // if it's not in the open list...  
                    if (openList.FirstOrDefault(cell => cell.Position == adjacentSquare) == null)
                    {
                        // compute its score, set the parent  
                        adjacentSquare.G = g;
                        adjacentSquare.H = adjacentSquare.Distance(end);
                        adjacentSquare.F = adjacentSquare.G + adjacentSquare.H;
                        adjacentSquareCell.Parent = current;

                        // and add it to the open list  
                        openList.Insert(0, adjacentSquareCell);
                    }
                    else
                    {
                        // test if using the current G score makes the adjacent square's F score  
                        // lower, if yes update the parent because it means it's a better path  
                        if (g + adjacentSquare.H < adjacentSquare.F)
                        {
                            adjacentSquare.G = g;
                            adjacentSquare.F = adjacentSquare.G + adjacentSquare.H;
                            adjacentSquareCell.Parent = current;
                        }
                    }
                }

            }
            var endCell = current;
            var cellLink = new List<MapCell>();
            while (current != null)
            {
                // Il ne faut pas ajouter ce qu'on a déjà fait
                if (!current.Visited && current.Position != start)
                    cellLink.Add(current);

                current = current.Parent;
            }


            cellLink.Reverse();
            return cellLink;
        }

        public static List<MapCell> GetWalkableAdjacentSquares(Position p, Map map, List<MapCell> openList, bool useVisited = true)
        {
            var list = new List<MapCell>();
            var left = p.X != 0 ? map[p.X - 1, p.Y] : null;
            var right = p.X != map.Width - 1 ? map[p.X + 1, p.Y] : null;
            var bot = p.Y != map.Height - 1 ? map[p.X, p.Y + 1] : null;
            var top = p.Y != 0 ? map[p.X, p.Y - 1] : null;

            //Console.Error.WriteLine("--");
            //Console.Error.WriteLine($"{top?.Position}");
            //Console.Error.WriteLine($"{left?.Position} {p} {right?.Position}");
            //Console.Error.WriteLine($"{bot?.Position}");
            //Console.Error.WriteLine($" [S: {bot?.CanGoHere}, N: {top?.CanGoHere}, E: {right?.CanGoHere}, W: {left?.CanGoHere}]");
            //Console.Error.WriteLine("--");

            var availables = new List<MapCell> { left, right, top, bot }
                .Where(c => c != null)
                .Where(c => (useVisited && c.CanGoHere) || !useVisited)
                .ToList();

            // Console.Error.WriteLine($"Availables: {availables.Count}");

            foreach (var i in availables)
            {
                var n = openList.Find(c => c.Position == i.Position);
                if (n == null) list.Add(i);
                else list.Add(n);
            }

            return list;
        }
    }

    public static class SectionHelper
    {
        public static Position ToMidleSectionPosition(this int section)
        {
            // TODO prendre le milieu accessible
            switch (section)
            {
                case 1: return new Position { X = 2, Y = 2 };
                case 2: return new Position { X = 7, Y = 2 };
                case 3: return new Position { X = 12, Y = 2 };
                case 4: return new Position { X = 2, Y = 7 };
                case 5: return new Position { X = 7, Y = 7 };
                case 6: return new Position { X = 12, Y = 7 };
                case 7: return new Position { X = 2, Y = 12 };
                case 8: return new Position { X = 7, Y = 12 };
                case 9: return new Position { X = 12, Y = 12 };
            }
            return null;
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
        // Permet de se déplacer de 1 é 4 cases sans en informer l'enemie
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
    }

    public enum PositionToTake
    {
        None = 0,
        Real = 1,
        Estimated = 2
    }

    public enum OrderType
    {
        None = 0,
        Move = 1,
        Device = 2,
        Surface = 3
    }

    public enum OffsetType
    {
        XOffset,
        YOffset
    }
    #endregion

    #endregion
}