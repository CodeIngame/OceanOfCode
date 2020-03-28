namespace OceanOfCode.Enums
{
    /// <summary>
    /// Défini les types de joueurs
    /// </summary>
    public enum PlayerType
    {
        None = 0,
        Me = 1,
        Enemy = 2,
        Virtual
    }

    /// <summary>
    /// Défini le type de céllule
    /// </summary>
    public enum CellType
    {
        Unknow = 0,
        Island = 1,
        Empty = 2
    }

    /// <summary>
    /// Défini le type d'arme
    /// </summary>
    public enum DeviceType
    {
        None = 0,
        Torpedo = 1,
        Sonar = 2,
        Silence = 3,
        Mine = 4
    }

    /// <summary>
    /// Les direction disponible
    /// </summary>
    public enum Direction
    {
        None = 0,
        West = 1,
        Est = 2,
        North = 3,
        South = 4,
    }

    /// <summary>
    /// Le type d'ordre
    /// </summary>
    public enum OrderType
    {
        None = 0,
        Move = 1,
        Device = 2,
        Surface = 3
    }

    /// <summary>
    /// Le type d'offset
    /// </summary>
    public enum OffsetType
    {
        XOffset,
        YOffset
    }
}
