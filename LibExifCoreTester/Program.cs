using System;
using LibExifCore;

namespace LibExifCoreTester
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Running LibExifCore Test...");

            // The image path should be passed as the first parameter
            string imgPath = args[0];

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
