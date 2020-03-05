using System;
using System.IO;

namespace LibExifCore.FileFormats
{
    public class HeicParser : FileParser
    {
        public override bool ParseTags(string filePath)
        {
            FileStream fileStream = File.OpenRead(filePath);

            // FIXME: The HEIC images from iPhone seem to be big endian. This might not be true
            // of all HEIC images though!
            BigEndianBinaryReader br = new BigEndianBinaryReader(fileStream);

            uint fTypeSize = br.ReadUInt32();          // Size of fType box

            // Is there a read with offset? Do we need to be seeking all the time?
            fileStream.Seek(fTypeSize, SeekOrigin.Begin);

            uint metadataSize = br.ReadUInt32();       // size of metadata box

            byte[] exifArray = new byte[] { (byte)'E', (byte)'x', (byte)'i', (byte)'f' };
            byte[] ilocArray = new byte[] { (byte)'i', (byte)'l', (byte)'o', (byte)'c' };

            // Reverse because we are searching in big endian
            Array.Reverse(exifArray);
            Array.Reverse(ilocArray);

            int exifTag = BitConverter.ToInt32(exifArray, 0);
            int ilocTag = BitConverter.ToInt32(ilocArray, 0);

            // Scan through metadata until we find (a) Exif, (b) iloc
            int exifOffset = -1;
            int ilocOffset = -1;
            for (int i = (int)fTypeSize; i < metadataSize + fTypeSize; i++)  // copying the code exactly, but I think 
            {                                                               // this should be var i = ftTypeSize + 4?
                fileStream.Seek(i, SeekOrigin.Begin);

                int nextValue = br.ReadInt32();

                if (nextValue == exifTag)
                {
                    exifOffset = i;
                }
                else if (nextValue == ilocTag)
                {
                    ilocOffset = i;
                }
            }

            if (exifOffset == -1 || ilocOffset == -1)
            {
                // Invalid file, EXIF or iLoc offset not found
                return false;
            }

            fileStream.Seek(exifOffset - 4, SeekOrigin.Begin);
            ushort exifItemIndex = br.ReadUInt16();

            //Scan through ilocs to find exif item location
            for (int i = ilocOffset + 12; i < metadataSize + fTypeSize; i += 16)
            {
                fileStream.Seek(i, SeekOrigin.Begin);
                ushort itemIndex = br.ReadUInt16();

                if (itemIndex == exifItemIndex)
                {
                    fileStream.Seek(i + 8, SeekOrigin.Begin);

                    uint exifLocation = br.ReadUInt32();
                    uint exifSize = br.ReadUInt32();

                    // Check prefix at exif exifOffset
                    fileStream.Seek(exifLocation, SeekOrigin.Begin);
                    uint prefixSize = 4 + br.ReadUInt32();
                    uint exifOffset2 = exifLocation + prefixSize;

                    Tags = ReadExifData(br, exifOffset2);
                    return true;
                }
            }

            return false;
        }
    }
}
