using System;

namespace VerifyBot.Helper
{
    public static class SecretsReader
    {
        private const string SecretsFile = "secrets.txt";

        public static string GetSecret(string name)
        {
            try
            {
                var file = System.IO.File.ReadAllLines(SecretsFile);

                foreach (var line in file)
                {
                    if (line.StartsWith(name))
                    {
                        return line.Remove(0, name.Length + 1).Trim();
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading secret {name} ({ex})");
                return string.Empty;
            }
        }
    }
}