using System;
using LibExifCore;
using Xunit;

namespace XUnitLibExifCoreTests
{
    public class TagFunctionalityTest
    {
        [Fact]
        public void CheckImageOrientation()
        {
            string basePath = "../../../exif-samples-master/jpg/orientation/";

            // There are 8 possible orientation values and we have test images for each one.
            for(int i = 1; i <= 8; i++)
            {
                string landscapeName = string.Format("{0}landscape_{1}.jpg", basePath, i);
                string portraitName = string.Format("{0}portrait_{1}.jpg", basePath, i);

                // Check landscape mode photos
                EXIFParser landscapeParser = new EXIFParser(landscapeName);
                bool parsedOK = landscapeParser.ParseTags();

                Assert.True(parsedOK, "Image tags could not be parsed for " + landscapeName);

                if (parsedOK)
                {
                    Assert.True((UInt16)(landscapeParser.Tags["Orientation"]) == i, landscapeName + " did not have expected orientation tag");
                }                

                // Check portrait mode photos
                EXIFParser portraitParser = new EXIFParser(portraitName);
                parsedOK = portraitParser.ParseTags();

                Assert.True(parsedOK, "Image tags could not be parsed for " + portraitName);

                if (parsedOK)
                {
                    Assert.True((UInt16)(portraitParser.Tags["Orientation"]) == i, portraitName + " did not have expected orientation tag");
                }
            }
        }
    }
}
