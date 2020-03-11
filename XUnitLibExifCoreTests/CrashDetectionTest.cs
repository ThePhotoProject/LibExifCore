using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using LibExifCore;
using Xunit;

namespace XUnitLibExifCoreTests
{
    public class CrashDetectionTest
    {
        [Fact]
        public void ParseAllImagesWithoutCrash()
        {
            // This function will attempt to parse all test images and verify that no exceptions are thrown
            // to the client code

            CheckAllImagesAtPath("../../../exif-samples-master");
        }

        private void CheckAllImagesAtPath(string path)
        {
            string[] extensions = new string[] { ".jpg", ".jpeg", ".heic" };

            List<string> imageFiles = Directory.GetFiles(path)
                                    .Where(file => extensions.Any(file.ToLower().EndsWith))
                                    .ToList();

            foreach (string img in imageFiles)
            {
                EXIFParser parser = new EXIFParser(img);
                parser.ParseTags();
            }

            // Recursively search folders for more images
            string[] directories = Directory.GetDirectories(path);
            foreach (string dir in directories)
            {
                CheckAllImagesAtPath(dir);
            }
        }
    }
}
