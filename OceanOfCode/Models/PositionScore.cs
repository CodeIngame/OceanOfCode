namespace OceanOfCode.Models
{
    public class PositionScore
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
}
