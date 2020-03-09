using System;
using System.IO;
using LibExifCore;

namespace LibExifCoreExample
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length == 0 || string.IsNullOrEmpty(args[0]))
            {
                Console.WriteLine("LibExifCore Example Tool");
                Console.WriteLine("");
                Console.WriteLine("Usage: LibExifCoreExample.exe [path]");
                Console.WriteLine("");
                Console.WriteLine("path: file path to the image to analyze");
                return;
            }

            // The image path should be passed as the first parameter
            string imgPath = args[0];

            PrintImageTags(imgPath);

            Console.WriteLine("Test complete.");
        }

        private static void PrintImageTags(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                Console.WriteLine("File not found: " + imagePath);
                return;
            }

            Console.WriteLine("Scanning file: " + imagePath);

            EXIFParser parser = new EXIFParser(imagePath);
            if (parser.ParseTags())
            {
                Console.WriteLine("Detected Tags:");
                foreach (string key in parser.Tags.Keys)
                {
                    string s = string.Format("{0}: {1}", key, parser.Tags[key]);

                    Console.WriteLine(s);
                }
            }
            else
            {
                Console.WriteLine("No valid tags detected.");
            }
        }
    }
}
