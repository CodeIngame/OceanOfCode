namespace OceanOfCode.Services
{
    using OceanOfCode.Enums;
    using OceanOfCode.Helpers;
    using OceanOfCode.Models;
    using OceanOfCode.Models.Orders;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

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
            if (_player.CurrentPosition.X == 0 && direction == Direction.West)
                return null;

            // On veut aller é l'est mais on est déjà au maximun ...
            if (_player.CurrentPosition.X == Map.Width - 1 && direction == Direction.Est)
                return null;

            // On veut aller au sud mais on est déjà au maximun ...
            if (_player.CurrentPosition.Y == Map.Height - 1 && direction == Direction.South)
                return null;

            // On veut aller au nord mais on est déjà au maximun ...
            if (_player.CurrentPosition.Y == 0 && direction == Direction.North)
                return null;


            if (!_player.CurrentPosition.IsValidPosition(Map, yOffset, xOffset))
                return null;


            var cell = Map[_player.CurrentPosition.X + xOffset, _player.CurrentPosition.Y + yOffset];
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
                        var _enemy = new Player { PlayerId = playerId, CurrentPosition = new Position { X = xNumber, Y = yNumber }, PlayerType = PlayerType.Virtual };
                        var _me = new Player { PlayerId = playerId, CurrentPosition = new Position { X = xNumber, Y = yNumber }, PlayerType = PlayerType.Virtual };

                        EnemyVirtualPlayers.Add(_enemy);
                        MeVirtualPlayers.Add(_me);
                        playerId++;
                    }
                    xNumber++;
                });
                yNumber++;
            });
            Console.Error.WriteLine($" -> Done with: {EnemyVirtualPlayers.Count}");
        }
        public void AddSilenceVirtualPlayer(PlayerType pt)
        {
            // On va ajouter 16 joueurs dans le games pour chaque virtual players
            // Todo é voir
            // Uniquement dans le cas ou la position est connue
            var _virtualPlayers = pt == PlayerType.Me ? MeVirtualPlayers : EnemyVirtualPlayers;
            //var _newPlayers = 

            //Parallel.ForEach(_virtualPlayers, virtualPlayer =>
            //{
            //    Enumerable.Range(1, 4).Select(i =>
            //    {

            //    });
            //});

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
            var inputs = CommonHelpers.ReadLine().Split(' ');
            Map.Width = int.Parse(inputs[0]);
            Map.Height = int.Parse(inputs[1]);
            Me.PlayerId = int.Parse(inputs[2]);
        }

        public void InitializeMap()
        {
            for (int y = 0; y < Map.Height; y++)
            {
                var x = 0;
                Map.Maze2D.Add(CommonHelpers.ReadLine(debug: false).Select(c =>
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
            var inputs = CommonHelpers.ReadLine(debug: false).Split(' ');
            Me.CurrentPosition.X = int.Parse(inputs[0]);
            Me.CurrentPosition.Y = int.Parse(inputs[1]);

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

            string sonarResult = CommonHelpers.ReadLine(debug: false);
            string opponentOrders = CommonHelpers.ReadLine(debug: false);

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
                        Enemy.LastEstimatedPosition = new EstimatedPosition(sonarCommand.Sector.ToMidleSectionPosition(Map)) { XPrecision = 4, Y = 4 };
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
                if (Me.LastInstruction.SurfaceCommand != null)
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
                        var middle = sector.ToMidleSectionPosition(Map);
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
            Console.Error.Write($"AnalyseMove Ep: {(Enemy.CurrentPosition.Known ? Enemy.CurrentPosition : Enemy.LastInstruction.EstimatedPosition)}");
            var _analyseMoveCase = "0";

            var lastInstruction = Enemy.LastInstruction;
            var lastDirection = lastInstruction.MoveCommand?.Direction ?? Direction.None;
            if (!Enemy.CurrentPosition.Known && Enemy.LastInstructions.Count > 0)
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

            if (Enemy.CurrentPosition.Known)
            {
                // Console.Error.WriteLine("Track move !");
                _analyseMoveCase = "3.1";

                var xOffset = lastDirection.GetOffset(OffsetType.XOffset);
                var yOffset = lastDirection.GetOffset(OffsetType.YOffset);

                var newEnemyPosition = new Position(Enemy.CurrentPosition);

                // Console.Error.WriteLine($"Tracked position {newEnemyPosition} + x: {xOffset}, + y: {yOffset}");


                if (newEnemyPosition.IsValidPosition(Map, yOffset, xOffset))
                {
                    _analyseMoveCase = "3.2";

                    //Console.Error.Write($"Enemy Position : {Enemy.Position} ->");
                    Enemy.CurrentPosition.X += xOffset;
                    Enemy.CurrentPosition.Y += yOffset;
                    //Console.Error.WriteLine($" {Enemy.Position}");

                }
            }
            Console.Error.WriteLine($" -> _analyseMoveCase: {_analyseMoveCase} -  EPosition : {(Enemy.CurrentPosition.Known ? Enemy.CurrentPosition : lastInstruction.EstimatedPosition)}");
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

            Parallel.ForEach(virtuals.Where(ev => ev.StillInGame), (currentVirtual) =>
            {
                if (currentVirtual.CurrentPosition.IsValidPosition(Map, direction))
                {
                    var xOffset = direction.GetOffset(OffsetType.XOffset);
                    var yOffset = direction.GetOffset(OffsetType.YOffset);

                    currentVirtual.CurrentPosition.X += xOffset;
                    currentVirtual.CurrentPosition.Y += yOffset;
                }
                else
                {
                    currentVirtual.StillInGame = false;
                }

            });

            var stillInGame = virtuals.Count(ev => ev.StillInGame);
            if (stillInGame == 1)
            {
                var lastPlayer = virtuals.First(x => x.StillInGame);
                if (pt == PlayerType.Enemy)
                {
                    Enemy.CurrentPosition = new Position(lastPlayer.CurrentPosition);
                    VirtualPlayersUsed = true;
                }

                if (pt == PlayerType.Me)
                {
                    Console.Error.WriteLine($" Enemy know i'm at {lastPlayer.CurrentPosition}");
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
            if (Enemy.CurrentPosition.Known)
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
                    Enemy.CurrentPosition = new Position(Enemy.LastInstruction.TorpedoCommand.Position);

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
                    Enemy.CurrentPosition = new Position(Me.LastInstruction.TorpedoCommand.Position);
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
                        Enemy.CurrentPosition = new Position(Me.LastInstruction.TorpedoCommand.Position);
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
            Console.Error.WriteLine($" -> _analyseHitCase: {_analyseHitCase} - msg: {associatedMessage} new (estimated) Ep: {(Enemy.CurrentPosition.Known ? Enemy.CurrentPosition : Enemy.LastEstimatedPosition)}");

        }

        private void AnalyseToperdo(Instruction instruction)
        {
            if (Enemy.CurrentPosition.Known)
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
            if (Me.Sonar.CanUse())
                UseSonar(instruction);


        }

        private void UseTorpedo(Instruction instruction)
        {
            var _useTorpedo = "0";
            var associatedMessage = string.Empty;
            Console.Error.Write($"UseTorpedo");

            MapCell cellToAttack = null;
            if (Enemy.CurrentPosition.Known)
            {
                _useTorpedo = "1";
                var distance = Me.CurrentPosition.Distance(Enemy.CurrentPosition);
                if (distance <= Torpedo.Range)
                    cellToAttack = Map[Enemy.CurrentPosition];

                associatedMessage = $"[A] Position know - distance:{distance}";
            }
            else if (Enemy.LastEstimatedPosition.Known)
            {
                _useTorpedo = "2";
                var distance = Me.CurrentPosition.Distance(Enemy.LastEstimatedPosition);
                if (distance <= Torpedo.Range)
                {
                    _useTorpedo = "2.1";
                    cellToAttack = Map[Enemy.LastEstimatedPosition];

                }
                else if (distance > Torpedo.Range && distance < Torpedo.Range + 2)
                {
                    _useTorpedo = "2.2";
                    var idealTarget = Enemy.LastEstimatedPosition;
                    var actualPosition = Me.CurrentPosition;
                    var lastPath = PathFinderHelpers.FindPath(actualPosition, idealTarget, Map, false);
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
            // On tire pas sur les iles

            if (
                cellToAttack != null &&
                    (
                        cellToAttack.Position == Me.CurrentPosition
                        || cellToAttack.Position.Distance(Me.CurrentPosition) <= 2
                        || cellToAttack.CellType == CellType.Island
                    )
               ) //&& Me.HealthPoint < Enemy.HealthPoint)
            {
                _useTorpedo = "4";
                cellToAttack = null;
                associatedMessage = $"[A] Canceled shoot";

            }


            if (cellToAttack != null)
            {
                instruction.Commands.Add(new Torpedo { Position = cellToAttack.Position });

                associatedMessage += $" -- Me: {Me.CurrentPosition} shoot -> {cellToAttack.Position}";
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
                        var isValid = Me.CurrentPosition.IsValidPosition(Map, yOffset * distance, xOffset * distance, true);
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
                    Console.Error.Write($" {Map[Me.CurrentPosition.X + xOffset * i, Me.CurrentPosition.Y + yOffset * i].Position},");
                    Map[Me.CurrentPosition.X + xOffset * i, Me.CurrentPosition.Y + yOffset * i].Visited = true;
                }
                Console.Error.WriteLine("] => done!");

                instruction.Commands.Add(new Silence { Direction = max.Key, Distance = max.Value });
                ResetVirtualPlayer(PlayerType.Me);
            }


        }

        private void UseSonar(Instruction instruction)
        {
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

            var distance = Me.CurrentPosition.Distance(Enemy.CurrentPosition.Known ? Enemy.CurrentPosition : Enemy.LastEstimatedPosition);

            if (Enemy.CurrentPosition.Known)
            {
                if (distance >= Torpedo.Range)
                {
                    _caseMove = "1.1";
                    MoveToPosition(instruction, Enemy.CurrentPosition);
                }
                else
                {
                    // On va tenter un déplacement au centre de la section courante
                    _caseMove = "1.2";
                    var section = Me.CurrentPosition.Section;
                    MoveToPosition(instruction, section.ToMidleSectionPosition(Map));

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
                    var section = Me.CurrentPosition.Section;
                    MoveToPosition(instruction, section.ToMidleSectionPosition(Map));
                }
            }
            else
            {
                _caseMove = "3";
                // var empty = EmptyCell();
                // MoveToPosition(instruction, dico, empty.Position);
                MoveToPosition(instruction, new Position { X = 7, Y = 7 });
            }

            var msg1 = $"PEnemy: {Enemy.CurrentPosition} - distance: {distance}";
            var msg2 = $"(Estimated) PEnemy: {Enemy.LastEstimatedPosition} - distance: {distance}";
            var msg = _caseMove.Contains("1.") ? msg1 : _caseMove.Contains("2.") ? msg2 : "";
            Console.Error.WriteLine($" -> _caseMove: {_caseMove} -> {msg}");

        }

        #region Move Logics
        private void MoveToPosition(Instruction instruction, Position targetPosition)
        {
            var _targetedPosition = targetPosition;
            var myPosition = Me.CurrentPosition;

            // Si on est déjà sur la cible ou si la cible est inatteignable
            if (myPosition == _targetedPosition || Map[targetPosition].CellType == CellType.Island)
            {
                Console.Error.WriteLine(!Map[targetPosition].CanGoHere ? $" {targetPosition} not accessible" : "I'm still at this position");
                var d = MoveRandom(instruction);

                var x = d.GetOffset(OffsetType.XOffset);
                var y = d.GetOffset(OffsetType.YOffset);
                Map[Me.CurrentPosition.X + x, Me.CurrentPosition.Y + y].Visited = true;
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

            if (instruction.MoveCommand == null)
            {
                Console.Error.WriteLine("No move..");
            }
            else
            {
                //Me.Position = new Position(instruction.MoveCommand);
                Me.CurrentPosition.X = instruction.MoveCommand.X;
                Me.CurrentPosition.Y = instruction.MoveCommand.Y;
            }
        }

        private Direction MoveRandom(Instruction instruction)
        {
            var direction = Direction.None;
            var adjs = PathFinderHelpers.GetWalkableAdjacentSquares(Me.CurrentPosition, Map, null, true);
            if (adjs.Count > 0)
            {
                var p = adjs.First().Position;
                var d = Me.CurrentPosition.DirectionToTake(p);
                direction = d;
            }

            #region Poubelle
            //var section = Me.Position.Section;
            //switch (section)
            //{
            //    case 1:
            //    case 2:
            //    case 3:
            //        // on est trop en haut
            //        if (CanSouth)
            //            direction = Direction.South;

            //        else if (section == 1 && CanEst)
            //            direction = Direction.Est;

            //        else if (section == 3 && CanWest)
            //            direction = Direction.West;

            //        break;
            //    case 4:
            //        if (CanEst)
            //            direction = Direction.Est;
            //        else if (CanSouth)
            //            direction = Direction.South;
            //        break;
            //    case 6:
            //        if (CanWest)
            //            direction = Direction.West;
            //        else if (CanSouth)
            //            direction = Direction.South;
            //        break;

            //    case 7:
            //    case 8:
            //    case 9:
            //        // on est trop en bas
            //        if (CanNorth)
            //            direction = Direction.North;
            //        else if (section == 7 && CanEst)
            //            direction = Direction.Est;
            //        else if (section == 9 && CanWest)
            //            direction = Direction.West;
            //        break;
            //}
            #endregion
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
                    Map[Me.CurrentPosition].Visited = true;
                }
            }


            // Je suppose que si je peux y aller je vais y aller
            Console.Error.WriteLine($"myPosition: {Me.CurrentPosition} - sector: {Me.CurrentPosition.Section} move to {direction.ToMove()}");
            var newPosition = Me.CurrentPosition.NewPosition(direction);

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


            if (Me.Torpedo.Couldown > 0)
            {
                instruction.MoveCommand.DeviceLoading = DeviceType.Torpedo;
                return;
            }

            if (Me.Silence.Couldown > 0)
            {
                instruction.MoveCommand.DeviceLoading = DeviceType.Silence;
                return;
            }

            if (Me.Sonar.Couldown > 0)
            {
                instruction.MoveCommand.DeviceLoading = DeviceType.Sonar;
                return;
            }

            // WTF
            // Console.WriteLine("MOVE W TORPEDO|TORPEDO 2 11|SONAR 4|SILENCE W 4");

        }
        #endregion

    }

}
