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
        public List<List<MapCell>> Maze2D { get; set; } = new List<List<MapCell>>();
        public List<MapCell> Maze1D { get; set; } = new List<MapCell>();


        public MapCell this[int x, int y] => Maze2D[y][x];
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
        /// <summary>
        /// Détermine si la position est connu
        /// </summary>
        public bool Known => this.X != -1 && this.Y != -1;
        public string Coordonate => $"{X} {Y}";
        public override string ToString()
        {
            return $"{{{X}:{Y}}}";
        }
    }

    public class EstimatedPosition
        : Position
    {

        public int XPrecision { get; set; } = 0;
        public int YPrecision { get; set; } = 0;

        public EstimatedPosition()
        {
        }

        public EstimatedPosition(Position p)
        {
            X = p.X;
            Y = p.Y;
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
        public EstimatedPosition LastEstimatedPosition => LastInstruction.EstimatedPosition;

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
        public List<int> HealthPointHistoric { get; set; } = new List<int>();

        #region Helpers
        #region Devices
        public Torpedo Torpedo => (Torpedo)Device(DeviceType.Torpedo);
        public Sonar Sonar => (Sonar)Device(DeviceType.Sonar);
        public Mine Mine => (Mine)Device(DeviceType.Mine);
        public Silence Silence => (Silence)Device(DeviceType.Silence);
        protected Device Device(DeviceType DeviceType) => Devices.First(d => d.DeviceType == DeviceType);
        #endregion

        public Instruction LastInstruction => LastInstructions.Count > 1 ? LastInstructions[LastInstructions.Count - 1] : new Instruction();
        public bool Touched => HealthPointHistoric.Count > 2 && HealthPointHistoric[HealthPointHistoric.Count - 2] > HealthPointHistoric[HealthPointHistoric.Count - 1];
        public bool TouchedPerfect => HealthPointHistoric.Count > 2 && HealthPointHistoric[HealthPointHistoric.Count - 2] - 2 == HealthPointHistoric[HealthPointHistoric.Count - 1];

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
        public Direction Direction { get; set; } = Direction.None;
        public DeviceType DeviceUsed { get; set; } = DeviceType.None;
        public Position DeviceUsedPosition { get; set; }
        public DeviceType DeviceLoad { get; set; } = DeviceType.None;

        public EstimatedPosition EstimatedPosition { get; set; } = new EstimatedPosition();

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

            var height = _player.Position.Y + yOffset;
            var width = _player.Position.X + xOffset;

            if (height < 0 || height > Map.Height - 1)
                return null;

            if (width < 0 || width > Map.Width - 1)
                return null;

            var cell = Map[_player.Position.X + xOffset, _player.Position.Y + yOffset];
            return cell;
        }
        public void ResetVisitedCell()
        {
            Map.Maze2D.ForEach(row =>
            {
                row.ForEach(c => c.Visited = false);
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

            Console.Error.WriteLine($"Will use section: {sectionToUse} on -> {dico[sectionToUse][maxEmptyCell / 2]}");

            return dico[sectionToUse][maxEmptyCell / 2];
        }

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

                    Map.Maze1D.Add(cell);
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
            string opponentOrders = Helpers.ReadLine(debug: false);

            // On enregistre le précédent déplacement
            if (opponentOrders != "NA")
                Enemy.LastInstructions.Add(opponentOrders.ToInstructions());
            #endregion




        }
        #endregion

        public void WritePosition()
        {
            Console.Error.WriteLine($"My position is :{Me.Position}");
            if (Enemy.Position.Known)
                Console.Error.WriteLine($"Enemy position is :{Enemy.Position}");
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
            AnalyseHit(instruction);
            AnalyseToperdo(instruction);
        }

        #region Analyse Logics
        private void AnalyseHit(Instruction instruction)
        {
            var enemyTornadoUsed = Enemy.LastInstruction.DeviceUsed == DeviceType.Torpedo;
            var meTornadoUsed = Me.LastInstruction.DeviceUsed == DeviceType.Torpedo;

            var enemyTouchedMe = Me.Touched;
            var enemyLostHp = Enemy.Touched;

            if (enemyTornadoUsed && enemyTouchedMe && !enemyLostHp)
                Console.Error.WriteLine($"Enemy touched me with perfect: {Me.TouchedPerfect}");
            //else if (enemyTornadoUsed && enemyTouchedMe && enemyLostHp)
            //{
            //    var perfectEnemy = Enemy.TouchedPerfect;
            //    // il est au même emplacement que moi
            //    Console.Error.WriteLine($"Enemy touched me and he is close because he also loose hp");
            //}
            else if(meTornadoUsed && enemyLostHp)
            {
                var shootPosition = Me.LastInstruction.DeviceUsedPosition;
                var isPerfect = Enemy.TouchedPerfect;
                if(isPerfect)
                {
                    Enemy.Position = shootPosition;
                }
                else
                {
                    //Enemy.LastInstructions[Enemy.LastInstructions.Count-1].EstimatedPosition = new EstimatedPosition(shootPosition) { XPrecision = 1, YPrecision = 1 };
                }

            }
        }

        private void AnalyseToperdo(Instruction instruction)
        {
            var lastInstruction = Enemy.LastInstruction;
            if (lastInstruction.DeviceUsed == DeviceType.Torpedo)
            {
                lastInstruction.EstimatedPosition = new EstimatedPosition(lastInstruction.DeviceUsedPosition);
                lastInstruction.EstimatedPosition.XPrecision = Torpedo.Range;
                lastInstruction.EstimatedPosition.YPrecision = Torpedo.Range;

                Console.Error.WriteLine($"Enemy close to {lastInstruction.EstimatedPosition} - precision : {Torpedo.Range}");
            }
            else if(Enemy.LastInstructions.Count > 1)
            {
                //On regarde la position estimé du dernier coup
                var previousInstruction = Enemy.LastInstructions[Enemy.LastInstructions.Count - 2];
                if(previousInstruction.EstimatedPosition.Known)
                {
                    //On applique le déplacement du coup
                    var lastDirection = lastInstruction.Direction;
                    var newEstimatedPosition = new EstimatedPosition(previousInstruction.EstimatedPosition);
                    newEstimatedPosition.X += lastDirection == Direction.Est ? 1 : lastDirection == Direction.West ? -1 : 0;
                    newEstimatedPosition.Y += lastDirection == Direction.South ? 1 : lastDirection == Direction.North ? -1 : 0;
                    // TODO il faudrait vérifier que l'emplacement est accessible (pas un bout d'île)
                    lastInstruction.EstimatedPosition = newEstimatedPosition;
                    Console.Error.WriteLine($"Enemy estimated postion updated to: {lastInstruction.EstimatedPosition}");
                }
            }
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

                var direction = instruction.Direction;
                var cellToAttack = MapCell(PlayerType.Me, direction, Torpedo.Range);

                if (cellToAttack != null)
                {
                    instruction.DeviceUsed = DeviceType.Torpedo;
                    instruction.DeviceUsedPosition = cellToAttack.Position;

                    Console.Error.WriteLine($"Me: {Me.Position} shoot -> ${cellToAttack.Position}");
                }


                //                var lastMoves = Me.LastInstructions.Skip(Me.LastInstructions.Count - 4).Take(3).ToList();
                //                var lastEnemyMoves = Enemy.LastInstructions.Skip(Me.LastInstructions.Count - 4).Take(3).ToList();
                //                if (lastMoves.SequenceEqual(lastEnemyMoves))
                //                {
                //#if WriteDebug
                //                    Console.Error.WriteLine($"same scheme -> shoot him");
                //#endif
                //                    var action = Me.Torpedo.Use();

                //                    // faudra faire attention à l'ile
                //                    var isOutOfSouth = Me.Position.Y + 2 > Map.Height - 1;
                //                    var isOutOfNorth = Me.Position.Y - 2 < 0;
                //                    var isOutOfEst = Me.Position.X + 2 > Map.Width - 1;
                //                    var isOutOfWest = Me.Position.X - 2 < 0;
                //                    var offsetY = isOutOfSouth ? -2 : +2;
                //                    var offsetX = isOutOfWest ? -2 : +2;

                //                    //var direction = lastEnemyMoves[lastEnemyMoves.Count - 1].Direction;
                //                    //var offsetX = direction == Direction.West ? 2 : direction == Direction.Est ? -2 : 1;
                //                    //var offsetY = direction == Direction.South ? 2 : direction == Direction.North ? -2 : 1;


                //                    instruction.DeviceUsed = DeviceType.Torpedo;
                //                    instruction.DeviceUsedPosition = new Position { Y = Me.Position.Y + offsetY, X = Me.Position.X + offsetX };
                //                }
            }

        }

        #region Move

        /// <summary>
        /// Déplacer
        /// </summary>
        private void Move(Instruction instruction)
        {

            if (Enemy.Position.Known)
            {

            }
            else if (Enemy.LastEstimatedPosition.Known)
            {
                MoveToEstimatedPosition(instruction);
            }
            else
            {
                MoveRandom(instruction);
            }




        }

        #region Move Logics
        private void MoveRandom(Instruction instruction)
        {
            // A priori on ne connait pas la position de l'ennemi
            // On va se déplacer en essayant de ne pas s'enfermer

            var direction = Direction.None;
            // On regarde les positions proches
            //-------------
            //   N
            // W ME E
            //   S

            var west = MapCell(PlayerType.Me, Direction.West);
            var est = MapCell(PlayerType.Me, Direction.Est);
            var north = MapCell(PlayerType.Me, Direction.North);
            var south = MapCell(PlayerType.Me, Direction.South);

            var dico = new Dictionary<Direction, MapCell>
            {
                { Direction.West, west },
                { Direction.Est, est },
                { Direction.North, north },
                { Direction.South, south },
            };


            var canWest = CanDoAction(west);
            var canEst = CanDoAction(est);
            var canNorth = CanDoAction(north);
            var canSouth = CanDoAction(south);

            var section = Me.Position.Section;

            //Console.Error.WriteLine($"{north?.Position}");
            //Console.Error.WriteLine($"{west?.Position} {Me.Position} {est?.Position}");
            //Console.Error.WriteLine($"{south?.Position}");

            //Console.Error.WriteLine($"Section: {section} -> [S: {canSouth}, N: {canNorth}, E: {canEst}, W: {canWest}]");


            switch (section)
            {
                case 1:
                case 2:
                case 3:
                    // on est trop en haut
                    if (canSouth)
                        direction = Direction.South;

                    else if (section == 1 && canEst)
                        direction = Direction.Est;

                    else if (section == 3 && canWest)
                        direction = Direction.West;

                    break;
                case 4:
                    if (canEst)
                        direction = Direction.Est;
                    else if (canSouth)
                        direction = Direction.South;
                    break;
                case 6:
                    if (canWest)
                        direction = Direction.West;
                    else if (canSouth)
                        direction = Direction.South;
                    break;

                case 7:
                case 8:
                case 9:
                    // on est trop en bas
                    if (canNorth)
                        direction = Direction.North;
                    else if (section == 7 && canEst)
                        direction = Direction.Est;
                    else if (section == 9 && canWest)
                        direction = Direction.West;
                    break;
            }

            if (direction == Direction.None)
            {
                Console.Error.WriteLine("No direction found");

                if (canNorth)
                    direction = Direction.North;
                else if (canEst)
                    direction = Direction.Est;
                else if (canSouth)
                    direction = Direction.South;
                else if (canWest)
                    direction = Direction.West;
                else
                {
                    direction = Direction.Surface;
                    ResetVisitedCell();
                    var currentPosition = Me.Position;
                    Map[currentPosition.X, currentPosition.Y].Visited = true;
                }
            }


            // Je suppose que si je peux y aller je vais y aller
            Console.Error.WriteLine($"P: {Me.Position} - S: {Me.Position.Section} -> {direction.ToMove()}");
            if (dico.ContainsKey(direction))
                dico[direction].Visited = true;

            instruction.Direction = direction;
        }

        private void MoveToEstimatedPosition(Instruction instruction)
        {
            // c'est moche ...
            var distance = Me.Position.Distance(Enemy.LastEstimatedPosition);

            var west = MapCell(PlayerType.Me, Direction.West);
            var est = MapCell(PlayerType.Me, Direction.Est);
            var north = MapCell(PlayerType.Me, Direction.North);
            var south = MapCell(PlayerType.Me, Direction.South);

            var canWest = CanDoAction(west);
            var canEst = CanDoAction(est);
            var canNorth = CanDoAction(north);
            var canSouth = CanDoAction(south);

            var dico = new Dictionary<Direction, MapCell>
            {
                { Direction.West, west },
                { Direction.Est, est },
                { Direction.North, north },
                { Direction.South, south },
            };

            var direction = Direction.None;

            if (Me.Position.X > Enemy.Position.X && canWest)
            {
                direction = Direction.West;
            }
            else if (Me.Position.X < Enemy.Position.X && canEst)
            {
                direction = Direction.Est;

            }
            else
            {
                Console.Error.WriteLine("We are on same x");
                Enemy.LastEstimatedPosition.XPrecision = 0;
            }

            if (direction == Direction.None)
            {
                if (Me.Position.Y > Enemy.Position.Y && canNorth)
                {
                    direction = Direction.North;
                }
                else if (Me.Position.Y < Enemy.Position.Y && canSouth)
                {
                    direction = Direction.South;

                }
                else
                {
                    Console.Error.WriteLine("We are on same Y");
                    Enemy.LastEstimatedPosition.YPrecision = 0;
                }
            }

            if(direction == Direction.None)
            {
                Console.Error.WriteLine("Nothing found go random..");
                MoveRandom(instruction);
            } 
            else
            {
                Console.Error.WriteLine($"/2\\ - P: {Me.Position} - S: {Me.Position.Section} -> {direction.ToMove()}");
                if (dico.ContainsKey(direction))
                    dico[direction].Visited = true;

                instruction.Direction = direction;
            }

           

        }
        #endregion

        #endregion

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

        public static Direction DirectionToTake(this Position p1, Position p2, Map map)
        {
            var direction = Direction.None;



            return direction;
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
        public static Instruction ToInstructions(this string move)
        {
            // TORPEDO 0 5|MOVE N TORPEDO
            // MOVE N TORPEDO
            // MOVE E TORPEDO
            // TORPEDO 0 8|MOVE E TORPEDO
            // MOVE E| TORPEDO 8 6
            var instruction = new Instruction();
            var input = move;

            var multiOrders = move.Contains("|");
            if (multiOrders)
            {
                var inputs = move.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                var startByMove = inputs[0].Contains("MOVE");


                // Device
                var attack = inputs[startByMove ? 1 : 0].Split(' ');
                //Console.Error.WriteLine(attack[0]);
                var parsed = Enum.TryParse<DeviceType>(attack[0].ToPascalCase(), out var deviceType);
                instruction.DeviceUsed = deviceType;
                instruction.DeviceUsedPosition = new Position { X = int.Parse(attack[1]), Y = int.Parse(attack[2]) };

                input = inputs[startByMove ? 0 : 1];
            }

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
