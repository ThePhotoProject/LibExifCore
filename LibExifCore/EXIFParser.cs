using System;
using System.Collections.Generic;
using System.IO;
using LibExifCore.FileFormats;

namespace LibExifCore
{
    /// <summary>
    /// EXIFParser reads the metadata tags associated with a file.
    /// </summary>
    public class EXIFParser
    {
        public Dictionary<string, object> Tags { get; private set; }

        public EXIFParser()
        {
            Tags = new Dictionary<string, object>();
        }

        public EXIFParser(string imagePath) : base()
        {
            FileParser parser = null;

            string extension = Path.GetExtension(imagePath).ToLower();
            if(extension.Equals(".heic"))
            {
                parser = new HeicParser();
            }
            else if(extension.Equals(".jpg") || extension.Equals(".jpeg"))
            {
                parser = new JpegParser();
            }
            else
            {
                throw new NotImplementedException("Support for " + extension + " files is not yet implemented.");
            }

            if (parser.ParseTags(imagePath))
            {
                Tags = parser.Tags;
            }
        }
    }
}
