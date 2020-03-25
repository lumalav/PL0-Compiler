namespace PL0Resources
{
    public class Instruction
    {
        /// <summary>
        /// Pos in instruction
        /// </summary>
        public int Pos { get; set; }
        /// <summary>
        /// Op Code
        /// </summary>
        public Op Code { get; set; }
        /// <summary>
        /// Register
        /// </summary>
        public int R { get; set; }
        /// <summary>
        /// L
        /// </summary>
        public int L { get; set; }
        /// <summary>
        /// M
        /// </summary>
        public int M { get; set; }

        public override string ToString()
        {
            return $"{(int)Code} {R} {L} {M}";
        }
    }
}
