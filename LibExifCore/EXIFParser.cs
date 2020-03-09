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
        private FileParser _parser;
        private string _imagePath;

        public EXIFParser()
        {
            Tags = new Dictionary<string, object>();
        }

        public EXIFParser(string imagePath) : base()
        {
            _imagePath = imagePath;

            string extension = Path.GetExtension(imagePath).ToLower();
            if(extension.Equals(".heic"))
            {
                _parser = new HeicParser();
            }
            else if(extension.Equals(".jpg") || extension.Equals(".jpeg"))
            {
                _parser = new JpegParser();
            }
            else
            {
                throw new NotImplementedException("Support for " + extension + " files is not yet implemented.");
            }
        }

        public bool ParseTags()
        {
            // Clear any previous tags
            _parser.Tags.Clear();

            bool success = _parser.ParseTags(_imagePath);
            if(success)
            {
                Tags = _parser.Tags;
            }

            return success;
        }

    }
}
