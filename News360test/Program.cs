using System;
using System.IO;

namespace News360test
{
    class MainClass
    {

        public static void Main(string[] args)
        {
            string filename;
            EquationNormalizer normalizer = new EquationNormalizer();

            // check parameters
            if (args.Length > 0)
            {
                filename = args[0];
                // load from file mode
                MainClass.LoadFromFile(filename, normalizer);
            }
            else
            {
                // interractive mode
                MainClass.StartInteractive(normalizer);
            }
        }

        private static void LoadFromFile(string filename, EquationNormalizer normalizer)
        {
            string equation;
            string output = String.Empty;
            StreamReader file;
            try
            {
                file = new StreamReader(filename);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Cannot find file {0}", filename);
                return;
            }


            while ((equation = file.ReadLine()) != null)
            {
                if (equation.Length > 0)
                {
                    output += equation + "   ->   " + normalizer.Normalize(equation) + "\n";
                }
            }

            string outputFileName = filename + ".out";

            File.WriteAllText(outputFileName, output);
            Console.WriteLine("File {0} with output has been generated", outputFileName);
        }

        private static void StartInteractive(EquationNormalizer normalizer)
        {
            while (true)
            {
                Console.WriteLine("Please enter an equation below:");
                Console.Write("> ");
                string equation = Console.ReadLine();
                Console.WriteLine("\n" + normalizer.Normalize(equation) + "\n");
            }
        }
    }
}
