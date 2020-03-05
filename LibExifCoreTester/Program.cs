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
                Console.WriteLine("Usage: LibExifCore.exe [path]");
                Console.WriteLine("");
                Console.WriteLine("path: file path to the image to analyze");
                return;
            }

            // The image path should be passed as the first parameter
            string imgPath = args[0];

            if(!File.Exists(imgPath))
            {
                Console.WriteLine("File not found: " + imgPath);
                return;
            }

            Console.WriteLine("Scanning file: " + imgPath);

            EXIFParser parser = new EXIFParser(imgPath);

            Console.WriteLine("Detected Tags:");
            foreach(string key in parser.Tags.Keys)
            {
                string s = string.Format("{0}: {1}", key, parser.Tags[key]);

                Console.WriteLine(s);
            }

            Console.WriteLine("Test complete.");
        }
    }
}
