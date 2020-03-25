using System;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var ti = new TestInstructions();
            ti.Handle();

            Console.ReadLine();
        }
    }
}
