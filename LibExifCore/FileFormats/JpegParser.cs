using System;
using System.IO;

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

            BigEndianBinaryReader bigReader = new BigEndianBinaryReader(fileStream);

            while (offset < length)
            {
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
                    // Move 8 bytes forward to the start of EXIF data
                    br.BaseStream.Seek(8, SeekOrigin.Current);

                    Tags = ReadExifData(bigReader);
                    return true;
                }
                else
                {
                    UInt16 nextOffset = bigReader.ReadUInt16();

                    offset += nextOffset;

                    // -Uint16 because we already read the size and that's the first 2 bytes of that section
                    br.BaseStream.Seek(nextOffset - sizeof(UInt16), SeekOrigin.Current);
                }
            }

            return false;
        }
    }
}
