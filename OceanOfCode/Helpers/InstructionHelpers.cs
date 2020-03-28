namespace OceanOfCode.Helpers
{
    using OceanOfCode.Models;

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

}
