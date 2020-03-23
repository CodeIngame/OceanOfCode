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

            // game loop
            while (true)
            {
                gameManager.SetPlayersInformations();
                gameManager.WritePosition();
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
        /// Le caractère qui correspond à la cellule
        /// </summary>
        public char Cell { get; set; }

        /// <summary>
        /// Pour la recherche de chemin il faut un parent
        /// </summary>
        public MapCell Parent { get; set; }

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

    public class Position
    {
        public int X { get; set; } = -1;
        public int Y { get; set; } = -1;

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
        /// Détermine si la position est connu
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

        public override string ToString()
        {
            if(XPrecision != -1 && YPrecision != -1)
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
            get { return LastInstructions.Count > 0 ? LastInstructions[LastInstructions.Count-1] : new Instruction(); }
            set { LastInstructions[LastInstructions.Count-1] = value; }
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
        /// Défini si le joueur est le premier à jouer
        /// </summary>
        public bool First => PlayerId == 0;
        #endregion
    }

    public class Instruction
    {
        public string Command { get; set; }
        /// <summary>
        /// La direction demandée
        /// </summary>
        public Direction Direction { get; set; } = Direction.None;
        public DeviceType DeviceUsed { get; set; } = DeviceType.None;
        public Position DeviceUsedPosition { get; set; }
        public DeviceType DeviceLoad { get; set; } = DeviceType.None;

        public EstimatedPosition EstimatedPosition { get; set; } = new EstimatedPosition();

        public Instruction()
        {

        }

        public Instruction(Instruction i)
        {
            Command = i.Command;
            Direction = i.Direction;
            DeviceUsed = i.DeviceUsed;
            DeviceUsedPosition = i.DeviceUsedPosition;
            DeviceLoad = i.DeviceLoad;
            EstimatedPosition = i.EstimatedPosition;
        }

        public override string ToString()
        {
            return $"D: {DeviceUsed.ToText()} {DeviceUsedPosition} M: {Direction.ToMove()} L: {DeviceLoad.ToText()}";
        }

        public string ToCommand()
        {
            var attack = DeviceUsed != DeviceType.None ? $"{DeviceUsed.ToText().ToUpper()} {DeviceUsedPosition.Coordonate}|" : "";
            var move = Direction.ToMove();
            var load = DeviceLoad != DeviceType.None ? $" {DeviceLoad.ToText().ToUpper()}" : "";

            var baseMove = $"{move}{load}";

            var command = $"{attack}{baseMove}";
#if WriteDebug
            Console.Error.WriteLine($"Command: {command}");
#endif
            return command;
        }
    }

    #region Devices
    public abstract class Device
    {
        /// <summary>
        /// Le type d'arme
        /// </summary>
        public DeviceType DeviceType { get; set; }
        public static int Range { get; set; } = 0;
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
            Range = 4;
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

            // Todo
            // Il est important de positionner le navire dans la plus grande étendu d'eau !

            // On va récupérer les cellules par section
            var dico = new Dictionary<int, List<MapCell>>();
            Map.Maze2D.ForEach(row =>
            {
                row.ForEach(c =>
                {
                    var sectionNumber = c.Position.Section;
                    var isEmpty = c.CellType == CellType.Empty;

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
                // Todo si l'enemie perds des points de vie c'est que (il s'est touché lui même ou que je lui ai tiré dessus)

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
            string opponentOrders = Helpers.ReadLine(debug: true);

            // On enregistre le précédent déplacement
            if (opponentOrders != "NA")
            {
                var copy = new Instruction(Enemy.LastInstruction);
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
        /// Permet d'analyser ce qui s'est passé sur le tour précédent
        /// </summary>
        /// <param name="instruction"></param>
        private void Analyze(Instruction instruction)
        {
            if (Counter > 1)
            {
                AnalyseToperdo(instruction);
                AnalyseHit(instruction);
                AnalyseMove(instruction);
            }

        }

        #region Analyse Logics
        private void AnalyseHit(Instruction instruction)
        {
            if (Enemy.Position.Known)
                return;

            Console.Error.Write($"AnalyseHit Ep: {(Enemy.Position.Known ? Enemy.Position : Enemy.LastInstruction.EstimatedPosition)}");
            var _analyseHitCase = "0";

            var enemyUsedToperdo = Enemy.LastInstruction.DeviceUsed == DeviceType.Torpedo;
            var iUsedTorpedo = Me.LastInstruction.DeviceUsed == DeviceType.Torpedo;

            var enemyLostHp = Enemy.Touched;
            var iLostHp = Me.Touched;
            var enemyTouchedPerfect = Enemy.TouchedPerfect;
      

            if (iUsedTorpedo && !enemyUsedToperdo && enemyLostHp)
            {
                _analyseHitCase = "1";
                // J'ai tiré et il n'a pa tiré -> l'enemi à perdu des points de vie
                var shootPosition = Me.LastInstruction.DeviceUsedPosition;
                Console.Error.WriteLine($"I shoot and touch with perfect: ({enemyTouchedPerfect})");
                if (enemyTouchedPerfect)
                {
                    _analyseHitCase = "1.1";
                    Enemy.Position = shootPosition;
                    Enemy.LastEstimatedPosition = new EstimatedPosition(shootPosition) { XPrecision = 0, YPrecision = 0 };
                }
                else
                {
                    _analyseHitCase = "1.2";
                }
            }
            else if (iUsedTorpedo && enemyUsedToperdo && enemyLostHp)
            {
                _analyseHitCase = "2";

                // Nous avons tiré -> l'enemi à perdu des points de vie
                // Attention si on tire tous les 2 avec un tires sur l'enemi il peut perdre 2hp mais ça ne sera pas un perfect
                var enemyShoot = Enemy.LastInstruction.DeviceUsedPosition;
                var myShoot = Me.LastInstruction.DeviceUsedPosition;
                var distance = myShoot.Distance(enemyShoot);
                var isCloseShoot = distance <= 2;
                var hpLost = Enemy.TotalHpLost;
                Console.Error.Write($" We shoot and touch with perfect: ({enemyTouchedPerfect}) : fake perfect {isCloseShoot}");
                if (isCloseShoot)
                    Console.Error.Write($" myShoot: {myShoot} - enemyShoot: {enemyShoot} with hp lost: {hpLost}");

                if (hpLost == 1)
                {
                    _analyseHitCase = "2.1";
                    // un seul de nous 2 à bien tirer (moi ?)
                }
                else if (hpLost == 2)
                {
                    _analyseHitCase = "2.2";

                }
                else if (hpLost == 3)
                {
                    _analyseHitCase = "2.3";
                    // Un des 2 a très bien tirer
                }

            }
            else if (!iUsedTorpedo && enemyUsedToperdo && enemyLostHp)
            {
                _analyseHitCase = "3";

                //Il a tiré -> il a perdu des points de vie
                var shootPosition = Enemy.LastInstruction.DeviceUsedPosition;
                if (enemyTouchedPerfect)
                {
                    _analyseHitCase = "3.1";
                    //Il s'est tiré dessus parfaitement c'est con ^^
                    // Console.Error.WriteLine($"Auto kill: ({shootPosition})");
                    Enemy.Position = shootPosition;
                    Enemy.LastEstimatedPosition = new EstimatedPosition(shootPosition) { XPrecision = 0, YPrecision = 0 };
                }
                else
                {
                    _analyseHitCase = "3.2";
                    // Console.Error.WriteLine($"Auto kill, he is close to ({shootPosition})");
                    Enemy.LastEstimatedPosition = new EstimatedPosition(shootPosition) { XPrecision = 1, YPrecision = 1 };
                }
            }
            else if (enemyUsedToperdo && !enemyLostHp && !iLostHp)
            {
                _analyseHitCase = "4";

                var lastDirection = Enemy.LastInstruction.Direction;
                var lastEstimatedPosition = Enemy.LastEstimatedPosition;

                var yOffset = lastDirection == Direction.North ? +3 : lastDirection == Direction.South ? -3 : 0;
                var xOffset = lastDirection == Direction.Est ? +3 : lastDirection == Direction.West ? -3 : 0;

                var newEstimatedPosition = new EstimatedPosition { X = lastEstimatedPosition.X + xOffset, Y = lastEstimatedPosition.Y + yOffset };
                if (newEstimatedPosition.IsValidPosition(Map))
                {
                    _analyseHitCase = "4.1";
                    // Console.Error.WriteLine($"Random shoot - position updated to: {newEstimatedPosition}");
                    Enemy.LastEstimatedPosition = newEstimatedPosition;
                }



            }
            else if (iUsedTorpedo && !enemyUsedToperdo && !enemyLostHp && Enemy.LastEstimatedPosition.Known)
            {
                _analyseHitCase = "5";
                // J'ai tiré a coté on décale de 2 le tire dans la directio opposé de la mienne
                var currentEstimatedPosition = new EstimatedPosition(Enemy.LastEstimatedPosition);
                var myDirection = Me.LastInstruction.Direction;
                currentEstimatedPosition = new EstimatedPosition(currentEstimatedPosition.PositionToTake(myDirection));

                if (currentEstimatedPosition.IsValidPosition(Map))
                {
                    _analyseHitCase = "5.1";
                    Enemy.LastEstimatedPosition = currentEstimatedPosition;
                }
            }

            Console.Error.WriteLine($" -> _analyseHitCase: {_analyseHitCase} - EPosition : {(Enemy.Position.Known ? Enemy.Position : Enemy.LastEstimatedPosition)}");

        }

        private void AnalyseToperdo(Instruction instruction)
        {
            if (Enemy.Position.Known)
                return;

            Console.Error.WriteLine("AnalyseToperdo");

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
                if (lastInstruction.DeviceUsed == DeviceType.Torpedo)
                {
                    lastInstruction.EstimatedPosition = new EstimatedPosition(lastInstruction.DeviceUsedPosition);
                    lastInstruction.EstimatedPosition.XPrecision = Torpedo.Range;
                    lastInstruction.EstimatedPosition.YPrecision = Torpedo.Range;

                    Console.Error.WriteLine($"Enemy close to {lastInstruction.EstimatedPosition} - precision : {Torpedo.Range}");
                }
            }

        }

        private void AnalyseMove(Instruction instruction)
        {
            Console.Error.Write($"AnalyseMove Ep: {(Enemy.Position.Known ? Enemy.Position : Enemy.LastInstruction.EstimatedPosition)}");
            var _analyseMoveCase = "0";

            var lastInstruction = Enemy.LastInstruction;
            if (!Enemy.Position.Known && Enemy.LastInstructions.Count > 0)
            {
                //On regarde la position estimé du dernier coup (la position précédante est recopié N-2 = N-1)
                var previousInstruction = Enemy.LastInstruction;
                if (previousInstruction.EstimatedPosition.Known)
                {
                    //On applique le déplacement du coup
                    var lastDirection = lastInstruction.Direction;
                    var newEstimatedPosition = new EstimatedPosition(previousInstruction.EstimatedPosition);

                    var xOffset = lastDirection == Direction.Est ? 1 : lastDirection == Direction.West ? -1 : 0;
                    var yOffset = lastDirection == Direction.South ? 1 : lastDirection == Direction.North ? -1 : 0;

                    if (!newEstimatedPosition.IsValidPosition(Map, yOffset, xOffset))
                    {
                        _analyseMoveCase = "1.1";
                        Console.Error.WriteLine("No reachable position, define close position");
                    }
                    else
                    {
                        _analyseMoveCase = "1.2";
                        newEstimatedPosition.X += xOffset;
                        newEstimatedPosition.Y += yOffset;
                    }

                    // TODO il faudrait vérifier que l'emplacement est accessible (pas un bout d'île)
                    lastInstruction.EstimatedPosition = newEstimatedPosition;
                    // Console.Error.WriteLine($"Enemy estimated postion updated to: {lastInstruction.EstimatedPosition}");
                }
                else
                {
                    // On connait pas encore sa positon mais il a fait surface
                    if(previousInstruction.Direction == Direction.Surface)
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
        #endregion

        #endregion


        /// <summary>
        /// Attaquer
        /// </summary>
        private void UseDevice(Instruction instruction)
        {

            // il faut tirer et regarder au prochain coups si on a touché quelque chose
            // on combine le tout avec les derniers déplacement pour localiser la position de quelqu'un

            if (Me.Torpedo.CanUse())
            {
                MapCell cellToAttack = null;
                if (Enemy.Position.Known)
                {
                    var distance = Me.Position.Distance(Enemy.Position);
                    if (distance <= Torpedo.Range)
                        cellToAttack = Map[Enemy.Position];

                    Console.Error.WriteLine($"[A] Position know - distance:{distance}");
                }
                else if (Enemy.LastEstimatedPosition.Known)
                {
                    var distance = Me.Position.Distance(Enemy.LastEstimatedPosition);
                    if (distance <= Torpedo.Range)
                        cellToAttack = Map[Enemy.LastEstimatedPosition];
                    else if (distance <= Torpedo.Range + 1)
                    {
                        var idealTarget = Enemy.LastEstimatedPosition;
                        var actualPosition = Me.Position;
                        var lastPath = PathFinder.FindPath(actualPosition, idealTarget, Map, false);
                        cellToAttack = lastPath[lastPath.Count - 2];
                    }

                    Console.Error.WriteLine($"[A] Estimated Position know - distance:{distance}");

                }
                else
                {
                    //direction = instruction.Direction;
                    //cellToAttack = MapCell(PlayerType.Me, direction, Torpedo.Range);
                    //Console.Error.WriteLine("[A] Random shoot");
                }


                // On se se tire pas dessus !
                if (cellToAttack != null && (cellToAttack.Position == Me.Position || cellToAttack.Position.Distance(Me.Position) <= 2)) //&& Me.HealthPoint < Enemy.HealthPoint)
                {
                    cellToAttack = null;
                    Console.Error.WriteLine("[A] Canceled shoot");

                }


                if (cellToAttack != null)
                {
                    instruction.DeviceUsed = DeviceType.Torpedo;
                    instruction.DeviceUsedPosition = cellToAttack.Position;
                    Console.Error.WriteLine($"Me: {Me.Position} shoot -> {cellToAttack.Position}");
                }

            }

        }

        #region Move

        /// <summary>
        /// Déplacer
        /// </summary>
        private void Move(Instruction instruction)
        {
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
                    // On va tenter un déplacement au centre de la section courante
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
                    // On va tenter un déplacement au centre de la section courante
                    var section = Me.Position.Section;
                    MoveToPosition(instruction, dico, section.ToMidleSectionPosition());

                }
            }
            else
            {
                _caseMove = "3";
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

            // Si on est déjà sur la cible ou si la cible est inatteignable
            if (myPosition == _targetedPosition || !Map[targetPosition].CanGoHere)
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
                    instruction.Direction = Direction.Surface;
                    ResetVisitedCells();
                    Map[myPosition].Visited = true;
                    // MoveRandom(instruction, dico);
                }
            }
        }

        private void MoveRandom(Instruction instruction, Dictionary<Direction, MapCell> dico)
        {
            // A priori on ne connait pas la position de l'ennemi
            // On va se déplacer en essayant de ne pas s'enfermer

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
                    direction = Direction.Surface;
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

            if (Me.Torpedo.Couldown > 0)
                instruction.DeviceLoad = DeviceType.Torpedo;

            //if (Me.Sonar.Couldown < 0)
            //    actions += " SONAR";

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
        /// <summary>
        /// Permet d'obtenir le niveau de section correspondant à une position
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
        /// Permet d'avoir la direction à prendre entre 2 points
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

                case Direction.Surface: return "SURFACE";

            }
            return null;
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
            Console.Error.Write($"ToInstructions Ep {instruction.EstimatedPosition}");
            // MOVE N TORPEDO
            // TORPEDO 0 8|MOVE E TORPEDO
            // SURFACE 5 | MOVE W
            // MOVE E| TORPEDO 8 6
            // SURFACE 7
            var input = move;

            Action<string[], Direction> setInstructionSurface = (string[] cmd, Direction d) =>
            {
                instruction.Direction = d;
                instruction.Command = input;
                var secteur = int.Parse(cmd[1]);
                if (instruction.EstimatedPosition.Section != secteur)
                {
                    var middle = secteur.ToMidleSectionPosition();
                    instruction.EstimatedPosition = new EstimatedPosition(middle) { XPrecision = 3, YPrecision = 3 };

                }
            };

            var multiOrders = move.Contains("|");
            if (multiOrders)
            {
                var inputs = move.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                var startByMove = inputs[0].Contains("MOVE");
                var attack = inputs[startByMove ? 1 : 0].Split(' ');
                var parsed = Enum.TryParse<DeviceType>(attack[0].ToPascalCase(), out var deviceType);


                if(parsed)
                {
                    instruction.DeviceUsed = deviceType;
                    instruction.DeviceUsedPosition = new Position { X = int.Parse(attack[1]), Y = int.Parse(attack[2]) };
                } 
                else
                {
                    // c'est surment une surface
                    var surfaceParsed = Enum.TryParse<Direction>(attack[0].ToPascalCase(), out var directionSurface);
                    if(surfaceParsed)
                    {
                        setInstructionSurface(attack, directionSurface);
                    }
                }
                input = inputs[startByMove ? 0 : 1];


            }

            if (!input.Contains("SURFACE"))
            {

                var moveUsed = input.Substring(0, "MOVE E".Length);

                switch (moveUsed)
                {
                    case "MOVE S": instruction.Direction = Direction.South; break;
                    case "MOVE N": instruction.Direction = Direction.North; break;
                    case "MOVE W": instruction.Direction = Direction.West; break;
                    case "MOVE E": instruction.Direction = Direction.Est; break;
                }

                var loadedDevice = input.Substring(moveUsed.Length);
                var parsedLoad = Enum.TryParse<DeviceType>(loadedDevice.ToPascalCase(), out var deviceTypeLoaded);
                instruction.DeviceLoad = deviceTypeLoaded;
            }

            if (input.Contains("SURFACE"))
            {
                var s = input.Split(' ');
                setInstructionSurface(s, Direction.Surface);
            }


            Console.Error.WriteLine($"-> {instruction.EstimatedPosition}");
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
            var right = p.X != 13 ? map[p.X + 1, p.Y] : null;
            var bot = p.Y != 13 ? map[p.X, p.Y + 1] : null;
            var top = p.Y != 0 ? map[p.X, p.Y - 1] : null;

            //Console.Error.WriteLine("--");
            //Console.Error.WriteLine($"{top?.Position}");
            //Console.Error.WriteLine($"{left?.Position} {p} {right?.Position}");
            //Console.Error.WriteLine($"{bot?.Position}");
            //Console.Error.WriteLine($" [S: {bot.CanGoHere}, N: {top.CanGoHere}, E: {right.CanGoHere}, W: {left.CanGoHere}]");
            //Console.Error.WriteLine("--");

            var availables = new List<MapCell> { left, right, top, bot };
            foreach (var i in availables)
            {
                if (i != null && ((useVisited && i.CanGoHere) || !useVisited))
                {
                    var n = openList.Find(c => c.Position == i.Position);
                    if (n == null) list.Add(i);
                    else list.Add(n);
                }
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

    public enum PositionToTake
    {
        None = 0,
        Real = 1,
        Estimated = 2
    }
    #endregion

    #endregion
}
