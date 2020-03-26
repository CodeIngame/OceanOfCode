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

            // game loop
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


        public MapCell this[int x, int y] => Maze2D[y][x];
        public MapCell this[Position p] => Maze2D[p.Y][p.X];

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
        /// Le caract�re qui correspond � la cellule
        /// </summary>
        public char Cell { get; set; }

        /// <summary>
        /// Pour la recherche de chemin il faut un parent
        /// </summary>
        public MapCell Parent;

        public bool CanGoHere => !Visited && CellType == CellType.Empty;
        /// <summary>
        /// On ne doit pas se d�placer sur une cellule qu'on a d�j� visit�
        /// Sauf dans le cas o� on refait surface
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
        /// La distance entre ce point et le d�but
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
        {

        }

        public Position(Position p)
        {
            X = p.X;
            Y = p.Y;
        }

        public void AddXOffset(int x)
        {
            X += x;
        }

        public void AddYOffset(int y)
        {
            Y += y;
        }


        /// <summary>
        /// Permet d'obtenir la section de la position
        /// </summary>
        public int Section => this.ToSection();
        /// <summary>
        /// D�termine si la position est connu
        /// </summary>
        public bool Known => this.X != -1 && this.Y != -1;
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

    public class Player
    {
        /// <summary>
        /// Identifiant du joueur
        /// Le joueur avec l'identifiant 0 commande
        /// </summary>
        public int PlayerId { get; set; }
        /// <summary>
        /// D�fini s'il s'agit de moi ou pas
        /// </summary>
        public PlayerType PlayerType { get; set; }

        /// <summary>
        /// La position du joueur
        /// </summary>
        public Position Position { get; set; } = new Position();


        /// <summary>
        /// Le nombre de point de vie
        /// on d�marre avec 6 points de vie
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
        /// D�fini si le joueur est le premier � jouer
        /// </summary>
        public bool First => PlayerId == 0;
        #endregion
    }


    public abstract class BaseOrder
    {
        public string Order { get; set; }
        public OrderType OrderType { get; protected set; }

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
        /// Le temps � attendre avant r�utilisation (disponibilit�)
        /// </summary>
        public int Couldown { get; set; }

        public abstract bool CanUse();
        public abstract string Use();

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
        public Position Position { get; set; }

        public override bool CanUse()
        {
            return Couldown == 0;
        }

        /// <summary>
        /// Il faut donner la position X, Y
        /// </summary>
        /// <returns></returns>
        public override string Use()
        {
            return "TORPEDO {0} {1}|";
        }
    }

    public class Sonar
    : Device
    {
        public Sonar() : base()
        {
            DeviceType = DeviceType.Sonar;
        }

        public int Sector { get; set; }

        public override bool CanUse()
        {
            return Couldown == 0;
        }

        /// <summary>
        /// Il faut donner le secteur
        /// </summary>
        /// <returns></returns>
        public override string Use()
        {
            return "SONAR {0}|";
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

        public override string Use()
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

        /// <summary>
        /// Il faut donner la direction puis la distance
        /// </summary>
        /// <returns></returns>
        public override string Use()
        {
            return "SILENCE {0} {1}|";
        }
    }
    #endregion
    public class Move
        : BaseOrder
    {
        public Direction Direction { get; set; }
        public bool WithLoading { get; set; }
        public DeviceType DeviceLoading { get; set; }

        public Move()
        {
            OrderType = OrderType.Move;
        }
    }

    public class Surface
        : BaseOrder
    {
        public int Sector { get; set; }
        public Surface()
        {
            OrderType = OrderType.Surface;
        }
    }

    public class Instruction
    {
        public string FullCommand { get; set; }

        public List<BaseOrder> Commands { get; set; } = new List<BaseOrder>();

        /// <summary>
        /// La direction demand�e
        /// </summary>
        public Direction Direction { get; set; } = Direction.None;
        /// <summary>
        /// L'arme utilis�
        /// </summary>
        public DeviceType Device { get; set; } = DeviceType.None;
        /// <summary>
        /// La position du tire de l'arme
        /// </summary>
        public Position DevicePosition { get; set; }
        /// <summary>
        /// L'arme charg�
        /// </summary>
        public DeviceType DeviceLoading { get; set; } = DeviceType.None;
        /// <summary>
        /// Surface utilis�
        /// </summary>
        public bool WithSurface { get; set; } = false;

        public EstimatedPosition EstimatedPosition { get; set; } = new EstimatedPosition();

        public Instruction()
        {

        }

        public Instruction(Instruction i)
        {
            FullCommand = i.FullCommand;
            Direction = i.Direction;
            Device = i.Device;
            DevicePosition = i.DevicePosition;
            DeviceLoading = i.DeviceLoading;
            EstimatedPosition = i.EstimatedPosition;
        }

        public override string ToString()
        {
            return $"D: {Device.ToText()} {DevicePosition} M: {Direction.ToMove()} L: {DeviceLoading.ToText()}";
        }

        public string ToCommand()
        {
            // Todo � refaire !
            var attack = Device != DeviceType.None ? $"{Device.ToText().ToUpper()} {DevicePosition.Coordonate}|" : "";
            var move = Direction.ToMove() == null ? "SURFACE" : Direction.ToMove();
            var load = DeviceLoading != DeviceType.None ? $" {DeviceLoading.ToText().ToUpper()}" : "";

            var baseMove = $"{move}{load}";

            var command = $"{attack}{baseMove}";
#if WriteDebug
            Console.Error.WriteLine($"Command: {command}");
#endif
            return command;
        }
    }

 
    #endregion

    public class GameManager
    {
        public List<Player> Players { get; set; } = new List<Player> { new Player { PlayerType = PlayerType.Me }, new Player { PlayerType = PlayerType.Enemy } };
        public Map Map { get; set; } = new Map();

        public List<Player> EnemyVirtualPlayers { get; set; } = new List<Player>();

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

            var xOffset = direction == Direction.West ? -1 : direction == Direction.Est ? 1 : 0;
            var yOffset = direction == Direction.North ? -1 : direction == Direction.South ? 1 : 0;

            xOffset *= distance;
            yOffset *= distance;

            // On veut aller � l'ouest mais on est d�j� au maximun ...
            if (_player.Position.X == 0 && direction == Direction.West)
                return null;

            // On veut aller � l'est mais on est d�j� au maximun ...
            if (_player.Position.X == Map.Width - 1 && direction == Direction.Est)
                return null;

            // On veut aller au sud mais on est d�j� au maximun ...
            if (_player.Position.Y == Map.Height - 1 && direction == Direction.South)
                return null;

            // On veut aller au nord mais on est d�j� au maximun ...
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
            // Il est important de positionner le navire dans la plus grande �tendu d'eau !
            // On va r�cup�rer les cellules par section
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
                    if(isEmpty)
                    {
                        var position = new Position { X = xNumber, Y = yNumber };
                        // Console.Error.WriteLine($"Adding virtual player at: {position}");
                        var player = new Player { PlayerId = playerId, Position = position };
                        EnemyVirtualPlayers.Add(player);
                        playerId++;
                    }
                    xNumber++;
                });
                yNumber++;
            });
            Console.Error.WriteLine($" -> Done with: {EnemyVirtualPlayers.Count}");
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


            //On doit maintenant choisir sa position de d�part
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
                // Todo si l'enemie perds des points de vie c'est que (il s'est touch� lui m�me ou que je lui ai tir� dessus)

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

            // On enregistre le pr�c�dent d�placement
            if (opponentOrders != "NA")
            {
                var copy = new Instruction { EstimatedPosition = new EstimatedPosition(Enemy.LastEstimatedPosition) };
                Enemy.LastInstructions.Add(opponentOrders.ToInstructions(copy));
            }
            #endregion
        }
        #endregion

        public void WritePosition()
        {
            // Console.Error.WriteLine($"My position is :{Me.Position}");
            //if (Enemy.Position.Known)
            //    Console.Error.WriteLine($"Enemy position is :{Enemy.Position}");
            //if (!Enemy.Position.Known && Enemy.LastEstimatedPosition.Known)
            //    Console.Error.WriteLine($"Enemy position is :{Enemy.LastEstimatedPosition}");
        }

        public void Play()
        {
            Console.Error.WriteLine($"Starting a new turn: {Counter+1}");
            var instruction = new Instruction();
            Analyze(instruction);
            Move(instruction);
            UseDevice(instruction);
            LoadDevice(instruction);

            Me.LastInstructions.Add(instruction);

            Counter++;
            Console.WriteLine(instruction.ToCommand());

        }

        #region Analyze
        /// <summary>
        /// Permet d'analyser ce qui s'est pass� sur le tour pr�c�dent
        /// </summary>
        /// <param name="instruction"></param>
        private void Analyze(Instruction instruction)
        {
            AnalyseCommand(Enemy.LastInstruction);

            if (Counter > 1)
            {
                AnalyseVirtualPlayers();
                AnalyseToperdo(instruction);
                AnalyseHitV2(instruction);
                AnalyseMove(instruction);
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
                // un d�placement
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
                    instruction.Direction = direction;

                    var loading = order.Substring(orderMove.Length - 1);

                    instruction.Commands.Add(new Move { Order = order, Direction = direction });

                }
                // une surface
                else if (order.Contains("SURFACE"))
                {
                    var orderSurface = order.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var sector = int.Parse(orderSurface[1]);
                    instruction.WithSurface = true;
                    if (instruction.EstimatedPosition.Section != sector)
                    {
                        var middle = sector.ToMidleSectionPosition();
                        instruction.EstimatedPosition = new EstimatedPosition(middle) { XPrecision = 3, YPrecision = 3 };
                        Console.Error.Write($" -> Surface set {instruction.EstimatedPosition}");
                       
                    }
                    instruction.Commands.Add(new Surface { Order = order, Sector = sector });
                }
                // d�placement silence
                else if(order.Contains("SILENCE"))
                {
                    instruction.Device = DeviceType.Silence;
                    instruction.Commands.Add(new Silence { Order = order });
                }
                // Une torpille
                else if(order.Contains("TORPEDO"))
                {
                    var ordersDevice = order.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var parsed = Enum.TryParse<DeviceType>(ordersDevice[0].ToPascalCase(), out var deviceType);
                    if (parsed)
                    {
                        instruction.Device = deviceType;
                        instruction.DevicePosition = new Position { X = int.Parse(ordersDevice[1]), Y = int.Parse(ordersDevice[2]) };
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
                        instruction.Commands.Add(new Sonar { Order = order, Sector = int.Parse(ordersDevice[1])});
                    }
                }
                else
                {
                    Console.Error.Write($" -> order not implemented: {order}");
                }
            }

            Console.Error.WriteLine($"Commands load: {instruction.Commands.Count}");
          
            Console.Error.WriteLine($"-> {instruction.EstimatedPosition}");
            Console.Error.WriteLine($"Instruction decrypted: {instruction}");
        }

        private void AnalyseHitV2(Instruction instruction)
        {
            // Pas d'analyse si la position est connue
            if (Enemy.Position.Known)
                return;

            Console.Error.Write($"AnalyseHit Ep: {(Enemy.LastInstruction.EstimatedPosition)}");
            var _analyseHitCase = "0";
            var associatedMessage = string.Empty;

            var enemyLostHp = Enemy.Touched;
            var enemyUsedToperdo = Enemy.LastInstruction.Device == DeviceType.Torpedo;
            var iUsedTorpedo = Me.LastInstruction.Device == DeviceType.Torpedo;
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
                    Enemy.Position = new Position(Enemy.LastInstruction.DevicePosition);

                }
                else
                {
                    _analyseHitCase = "2.2";
                    associatedMessage = "Auto shoot partial";
                    if (Enemy.LastEstimatedPosition.XPrecision > 1 && Enemy.LastEstimatedPosition.YPrecision > 1)
                        Enemy.LastEstimatedPosition = new EstimatedPosition(Enemy.LastInstruction.DevicePosition) { XPrecision = 1, YPrecision = 1 };
                    else
                        associatedMessage = "Auto shoot but precision is same (do not reset position)";
                }

            }
            else if (enemyLostHp && !enemyUsedToperdo && iUsedTorpedo)
            {
                //J'ai bien tir�
                _analyseHitCase = "3";
                if (Enemy.TouchedPerfect)
                {
                    _analyseHitCase = "3.1";
                    associatedMessage = "shoot was perfect";
                    Enemy.Position = new Position(Me.LastInstruction.DevicePosition);
                }
                else
                {
                    _analyseHitCase = "3.2";
                    associatedMessage = "shoot partial";
                    Enemy.LastEstimatedPosition = new EstimatedPosition(Me.LastInstruction.DevicePosition) { XPrecision = 1, YPrecision = 1 };
                }
            }
            else if (enemyLostHp && enemyUsedToperdo && iUsedTorpedo)
            {
                _analyseHitCase = "4";
                var distance = Me.LastInstruction.DevicePosition.Distance(Enemy.LastInstruction.DevicePosition);
                if (distance > Torpedo.Range)
                {
                    _analyseHitCase = "4.1";
                    if (Enemy.TouchedPerfect)
                    {
                        _analyseHitCase = "4.1.2";
                        associatedMessage = "shoot was perfect";
                        Enemy.Position = new Position(Me.LastInstruction.DevicePosition);
                    }
                    else
                    {
                        _analyseHitCase = "4.1.2";
                        associatedMessage = "shoot partial";
                        if (Enemy.LastEstimatedPosition.XPrecision > 1 && Enemy.LastEstimatedPosition.YPrecision > 1)
                            Enemy.LastEstimatedPosition = new EstimatedPosition(Me.LastInstruction.DevicePosition) { XPrecision = 1, YPrecision = 1 };
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
                // j'ai rat� mon tire..
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
                if (lastInstruction.Device == DeviceType.Torpedo)
                {
                    lastInstruction.EstimatedPosition = new EstimatedPosition(lastInstruction.DevicePosition);
                    lastInstruction.EstimatedPosition.XPrecision = Torpedo.Range;
                    lastInstruction.EstimatedPosition.YPrecision = Torpedo.Range;

                    associatedMessage = $" -> Enemy close to {lastInstruction.EstimatedPosition} - precision : {Torpedo.Range}";
                }
            }

            Console.Error.WriteLine(associatedMessage);


        }

        private void AnalyseMove(Instruction instruction)
        {
            Console.Error.Write($"AnalyseMove Ep: {(Enemy.Position.Known ? Enemy.Position : Enemy.LastInstruction.EstimatedPosition)}");
            var _analyseMoveCase = "0";

            var lastInstruction = Enemy.LastInstruction;
            if (!Enemy.Position.Known && Enemy.LastInstructions.Count > 0)
            {
                //On regarde la position estim� du dernier coup (la position pr�c�dante est recopi� N-2 = N-1)
                var previousInstruction = Enemy.LastInstruction;
                if (previousInstruction.EstimatedPosition.Known)
                {
                    //On applique le d�placement du coup
                    var lastDirection = lastInstruction.Direction;
                    var newEstimatedPosition = new EstimatedPosition(previousInstruction.EstimatedPosition);

                    var xOffset = lastDirection == Direction.Est ? 1 : lastDirection == Direction.West ? -1 : 0;
                    var yOffset = lastDirection == Direction.South ? 1 : lastDirection == Direction.North ? -1 : 0;

                    if (!newEstimatedPosition.IsValidPosition(Map, yOffset, xOffset))
                    {
                        _analyseMoveCase = "1.1";
                        Console.Error.Write(" No reachable position, define close position");
                    }
                    else
                    {
                        _analyseMoveCase = "1.2";
                        newEstimatedPosition.X += xOffset;
                        newEstimatedPosition.Y += yOffset;
                    }

                    // TODO il faudrait v�rifier que l'emplacement est accessible (pas un bout d'�le)
                    lastInstruction.EstimatedPosition = newEstimatedPosition;
                    // Console.Error.WriteLine($"Enemy estimated postion updated to: {lastInstruction.EstimatedPosition}");
                }
                else
                {
                    // On connait pas encore sa positon mais il a fait surface
                    if (previousInstruction.WithSurface)
                    {

                    }
                }
            }

            if (Enemy.Position.Known)
            {
                // Console.Error.WriteLine("Track move !");
                _analyseMoveCase = "2.1";

                var lastDirection = lastInstruction.Direction;
                var xOffset = lastDirection == Direction.Est ? 1 : lastDirection == Direction.West ? -1 : 0;
                var yOffset = lastDirection == Direction.South ? 1 : lastDirection == Direction.North ? -1 : 0;

                var newEnemyPosition = new Position(Enemy.Position);

                // Console.Error.WriteLine($"Tracked position {newEnemyPosition} + x: {xOffset}, + y: {yOffset}");


                if (newEnemyPosition.IsValidPosition(Map, yOffset, xOffset))
                {
                    _analyseMoveCase = "2.2";

                    //Console.Error.Write($"Enemy Position : {Enemy.Position} ->");
                    Enemy.Position.X += xOffset;
                    Enemy.Position.Y += yOffset;
                    //Console.Error.WriteLine($" {Enemy.Position}");

                }
            }
            Console.Error.WriteLine($" -> _analyseMoveCase: {_analyseMoveCase} -  EPosition : {(Enemy.Position.Known ? Enemy.Position : lastInstruction.EstimatedPosition)}");
        }

        private void AnalyseVirtualPlayers()
        {
            var direction = Enemy.LastInstruction.Direction;
            if (direction == Direction.None)
                return;

            Console.Error.Write("AnalyseVirtualPlayer");

            var xOffset = direction == Direction.Est ? 1 : direction == Direction.West ? -1 : 0;
            var yOffset = direction == Direction.South ? 1 : direction == Direction.North ? -1 : 0;

            var playerToRemove = new List<int>();
            Parallel.ForEach(EnemyVirtualPlayers, currentVp =>
            {
                if(currentVp.Position.IsValidPosition(Map, yOffset, xOffset)) {
                    currentVp.Position.X += xOffset;
                    currentVp.Position.Y += yOffset;
                } else
                {
                    playerToRemove.Add(currentVp.PlayerId);
                }
            });

            playerToRemove.ForEach(p => EnemyVirtualPlayers.RemoveAll(pp => pp.PlayerId == p));

            Console.Error.WriteLine($" ->  {EnemyVirtualPlayers.Count} still in the game");
            if(EnemyVirtualPlayers.Count == 1)
            {
                Enemy.Position = EnemyVirtualPlayers.First().Position;
                VirtualPlayersUsed = true;
            }

        }
        #endregion

        #endregion

        #region Use Device
        /// <summary>
        /// Attaquer
        /// </summary>
        private void UseDevice(Instruction instruction)
        {

            // il faut tirer et regarder au prochain coups si on a touch� quelque chose
            // on combine le tout avec les derniers d�placement pour localiser la position de quelqu'un
            if (Me.Torpedo.CanUse())
                UseTorpedo(instruction);

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
                instruction.Device = DeviceType.Torpedo;
                instruction.DevicePosition = cellToAttack.Position;
                associatedMessage += $" -- Me: {Me.Position} shoot -> {cellToAttack.Position}";
                // Console.Error.WriteLine($"Me: {Me.Position} shoot -> {cellToAttack.Position}");
            }

            Console.Error.Write($"-> _useTorpedo: {_useTorpedo} - msg: {associatedMessage}");


        }
        #endregion


        #region Move
        /// <summary>
        /// D�placer
        /// </summary>
        private void Move(Instruction instruction)
        {
            Console.Error.Write($"Move");
            var _caseMove = "0";
            var dico = new Dictionary<Direction, MapCell>
            {
                { Direction.West, West() },
                { Direction.Est, Est() },
                { Direction.North, North() },
                { Direction.South, South() },
            };
            var distance = Me.Position.Distance(Enemy.Position.Known ? Enemy.Position : Enemy.LastEstimatedPosition);

            if (Enemy.Position.Known)
            {
                if (distance >= 3)
                {
                    _caseMove = "1.1";
                    MoveToPosition(instruction, dico, Enemy.Position);
                }
                else
                {
                    // On va tenter un d�placement au centre de la section courante
                    _caseMove = "1.2";
                    var section = Me.Position.Section;
                    MoveToPosition(instruction, dico, section.ToMidleSectionPosition());

                }

            }
            else if (Enemy.LastEstimatedPosition.Known)
            {
                if (distance >= 3)
                {
                    _caseMove = "2.1";
                    MoveToPosition(instruction, dico, Enemy.LastEstimatedPosition);
                }
                else
                {
                    _caseMove = "2.3";
                    // On va tenter un d�placement au centre de la section courante
                    var section = Me.Position.Section;
                    MoveToPosition(instruction, dico, section.ToMidleSectionPosition());

                }
            }
            else
            {
                _caseMove = "3";
                // var empty = EmptyCell();
                // MoveToPosition(instruction, dico, empty.Position);
                MoveToPosition(instruction, dico, new Position { X = 7, Y = 7 });
            }

            var msg1 = $"PEnemy: {Enemy.Position} - distance: {distance}";
            var msg2 = $"(Estimated) PEnemy: {Enemy.LastEstimatedPosition} - distance: {distance}";
            var msg = _caseMove.Contains("1.") ? msg1 : _caseMove.Contains("2.") ? msg2 : "";
            Console.Error.WriteLine($" -> Move: {_caseMove} -> {msg}");

        }

        #region Move Logics
        private void MoveToPosition(Instruction instruction, Dictionary<Direction, MapCell> dico, Position targetPosition)
        {
            var _targetedPosition = targetPosition;
            var myPosition = Me.Position;

            // Si on est d�j� sur la cible ou si la cible est inatteignable
            if (myPosition == _targetedPosition || Map[targetPosition].CellType == CellType.Island)
            {
                Console.Error.WriteLine(!Map[targetPosition].CanGoHere ? $"{targetPosition} not accessible" : "I'm still at this position");
                MoveRandom(instruction, dico);
            }
            else
            {
                var direction = Direction.None;
                var paths = myPosition.FindPath(_targetedPosition, Map);
                if (paths.Count > 0)
                {
                    direction = myPosition.DirectionToTake(paths[0].Position);
                    Console.Error.WriteLine($" found {paths[0].Position} with: {direction.ToMove()}");

                    paths[0].Visited = true;
                    instruction.Direction = direction;
                }
                else
                {
                    Console.Error.WriteLine(" No move -> Surface");
                    instruction.Direction = Direction.None;
                    instruction.WithSurface = true;
                    ResetVisitedCells();
                    Map[myPosition].Visited = true;
                    // MoveRandom(instruction, dico);
                }
            }
        }

        private void MoveRandom(Instruction instruction, Dictionary<Direction, MapCell> dico)
        {
            // A priori on ne connait pas la position de l'ennemi
            // On va se d�placer en essayant de ne pas s'enfermer

            var direction = Direction.None;
            // On regarde les positions proches
            //-------------
            //   N
            // W ME E
            //   S



            var section = Me.Position.Section;

            //Console.Error.WriteLine($"{north?.Position}");
            //Console.Error.WriteLine($"{west?.Position} {Me.Position} {est?.Position}");
            //Console.Error.WriteLine($"{south?.Position}");

            //Console.Error.WriteLine($"Section: {section} -> [S: {CanSouth}, N: {CanNorth}, E: {CanEst}, W: {CanWest}]");


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
                    direction = Direction.None;
                    instruction.WithSurface = true;
                    ResetVisitedCells();
                    Map[Me.Position].Visited = true;
                }
            }


            // Je suppose que si je peux y aller je vais y aller
            Console.Error.WriteLine($"P: {Me.Position} - S: {Me.Position.Section} -> {direction.ToMove()}");
            if (dico.ContainsKey(direction))
                dico[direction].Visited = true;

            instruction.Direction = direction;
        }
        #endregion


        #endregion

        #region Loading
        /// <summary>
        /// Charger
        /// </summary>
        private void LoadDevice(Instruction instruction)
        {
            //Todo voir les priorit�s de chargement

            Console.Error.WriteLine($"LoadDevice -> silence: {Me.Silence.Couldown}, torpedo: {Me.Torpedo.Couldown}, silence: {Me.Sonar.Couldown}");

            if (Me.Silence.Couldown > 0)
            {
                instruction.DeviceLoading = DeviceType.Silence;
                return;
            }

            if (Me.Torpedo.Couldown > 0)
            {
                instruction.DeviceLoading = DeviceType.Torpedo;
                return;
            }

            if (Me.Sonar.Couldown > 0)
            {
                instruction.DeviceLoading = DeviceType.Sonar;
                return;
            }

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
        /// Log si n�cessaire l'information lue
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
        /// Permet de passer du caract�re au type de cellule
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
        /// Permet d'obtenir le niveau de section correspondant � une position
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
        /// Permet d'avoir la direction � prendre entre 2 points
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

        public static bool IsValidPosition(this Position p1, Map map, int yOffset = 0, int xOffset = 0)
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

            // Console.Error.WriteLine($"{p1} is valid position ? {isValid}  ({height}, {width})");

            return isValid;
        }

        public static Position PositionToTake(this Position p1, Direction direction)
        {
            switch (direction)
            {
                case Direction.West: p1.AddXOffset(-1); break;
                case Direction.Est: p1.AddXOffset(1); break;
                case Direction.North: p1.AddYOffset(-1); break;
                case Direction.South: p1.AddYOffset(1); break;
            }
            return p1;
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
            // Console.Error.WriteLine($"ToInstructions set command: {instruction.Command}");
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

    public static class PathFinder
    {
        public static List<MapCell> FindPath(this Position start, Position end, Map map, bool useVisited = true)
        {
            // reset de la pr�c�dante 
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
            Console.Error.Write($"Path finding with start:{start} - target:{end}");
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
                        // a voir
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
                            // a voir
                            adjacentSquareCell.Parent = current;
                        }
                    }
                }

            }
            var endCell = current;
            var cellLink = new List<MapCell>();
            while (current != null)
            {
                // Il ne faut pas ajouter ce qu'on a d�j� fait
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
            //Console.Error.WriteLine($" [S: {bot.CanGoHere}, N: {top.CanGoHere}, E: {right.CanGoHere}, W: {left.CanGoHere}]");
            //Console.Error.WriteLine("--");

            var availables = new List<MapCell> { left, right, top, bot }
                .Where(c => c != null)
                .Where(c => (useVisited && c.CanGoHere) || !useVisited)
                .ToList();

            // Console.Error.WriteLine($"Availables: {availables.Count}");

            foreach (var i in availables)
            {
                //Console.Error.WriteLine($"looking for: {i?.Position}");

                //if (i != null && ((useVisited && i.CanGoHere) || !useVisited))
                //{
                // Console.Error.WriteLine($"Adding: {i?.Position}");
                var n = openList.Find(c => c.Position == i.Position);
                if (n == null) list.Add(i);
                else list.Add(n);
                //}
            }

            return list;
        }
    }

    public static class SectionHelper
    {
        public static Position ToMidleSectionPosition(this int section)
        {
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
        // Permet de se d�placer de 1 � 4 cases sans en informer l'enemie
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
        // Surface = 99
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
    #endregion

    #endregion
}
