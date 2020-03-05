using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LibExifCore.FileFormats
{
    public class JpegParser : FileParser
    {
        public override bool ParseTags(string filePath)
        {
            FileStream fileStream = File.OpenRead(filePath);
            BinaryReader br = new BinaryReader(fileStream);

            if(br.ReadByte() != 0xFF || br.ReadByte() != 0xD8)
            {
                // Not a valid JPEG. First two bytes must be 0xFF,0xD8
                return false;
            }

            int offset = 2;
            long length = fileStream.Length;

            while (offset < length)
            {
                br.BaseStream.Seek(offset, SeekOrigin.Begin);

                if(br.ReadByte() != 0xFF)
                {
                    // Not a valid marker at this offset, something is wrong!
                    return false;
                }

                byte marker = br.ReadByte();

                // It's possible to handle other markers here as well,
                // but right now this is only handling 0xFFE1 for EXIF data
                if (marker == 0xE1)
                {
                    Tags = ReadExifData(br, (uint)(offset + 4 + 6)); //, dataView.getUint16(offset + 2) - 2);
                    return true;
                }
                else
                {
                    offset += 2 + br.ReadUInt16();
                }
            }

            return false;
        }
    }
}
