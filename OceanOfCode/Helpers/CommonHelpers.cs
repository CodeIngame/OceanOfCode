namespace OceanOfCode.Helpers
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    public static class CommonHelpers
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
}
