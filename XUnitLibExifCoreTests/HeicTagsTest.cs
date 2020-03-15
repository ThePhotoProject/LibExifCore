using LibExifCore;
using Xunit;

namespace XUnitLibExifCoreTests
{
    public class HeicTagsTest
    {
        [Fact]
        public void CheckHeicTags()
        {
            string basePath = "../../../HeicTest/";
            string testImagePath = basePath + "test1.heic";

            EXIFParser parser = new EXIFParser(testImagePath);
            bool parsedOK = parser.ParseTags();

            Assert.True(parsedOK, "Image tags could not be parsed for " + testImagePath);

            if(parsedOK)
            {
                // TODO: check more fields here
                Assert.True(parser.Tags["Make"].Equals("Apple"), "Make tag is not correct");
            }
        }
    }
}
