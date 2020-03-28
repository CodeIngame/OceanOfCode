namespace OceanOfCode
{
    using OceanOfCode.Services;
    using System;

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
}