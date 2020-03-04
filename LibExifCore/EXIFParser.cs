using System;
using System.Collections.Generic;
using System.IO;

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

        // This is for HEIC
        public EXIFParser(string imagePath) : base()
        {
            FileStream fileStream = File.OpenRead(imagePath);
            BigEndianBinaryReader br = new BigEndianBinaryReader(fileStream);

            uint fTypeSize = br.ReadUInt32();          // Size of fType box

            // Is there a read with offset? Do we need to be seeking all the time?
            fileStream.Seek(fTypeSize, SeekOrigin.Begin);

            uint metadataSize = br.ReadUInt32();       // size of metadata box

            byte[] exifArray = new byte[] { (byte)'E', (byte)'x', (byte)'i', (byte)'f' };
            byte[] ilocArray = new byte[] { (byte)'i', (byte)'l', (byte)'o', (byte)'c' };

            Array.Reverse(exifArray);
            Array.Reverse(ilocArray);

            int exifTag = BitConverter.ToInt32(exifArray, 0);
            int ilocTag = BitConverter.ToInt32(ilocArray, 0);

            // Scan through metadata until we find (a) Exif, (b) iloc
            int exifOffset = -1;
            int ilocOffset = -1;
            for(int i = (int)fTypeSize; i < metadataSize + fTypeSize; i++)  // copying the code exactly, but I think 
            {                                                               // this should be var i = ftTypeSize + 4?
                // Is there a read with offset? Do we need to be seeking all the time?
                fileStream.Seek(i, SeekOrigin.Begin);

                int nextValue = br.ReadInt32();

                if(nextValue == exifTag)
                {
                    exifOffset = i;
                }
                else if (nextValue == ilocTag)
                {
                    ilocOffset = i;
                }
            }

            if(exifOffset == -1 || ilocOffset == -1)
            {
                // Invalid file, EXIF or iLoc offset not found
                return;
            }

            fileStream.Seek(exifOffset - 4, SeekOrigin.Begin);
            ushort exifItemIndex = br.ReadUInt16();

            //Scan through ilocs to find exif item location
            for (int i = ilocOffset + 12; i < metadataSize + fTypeSize; i += 16)
            {
                fileStream.Seek(i, SeekOrigin.Begin);
                ushort itemIndex = br.ReadUInt16();

                if(itemIndex == exifItemIndex)
                {
                    fileStream.Seek(i + 8, SeekOrigin.Begin);

                    uint exifLocation = br.ReadUInt32();
                    uint exifSize = br.ReadUInt32();

                    // Check prefix at exif exifOffset
                    fileStream.Seek(exifLocation, SeekOrigin.Begin);
                    uint prefixSize = 4 + br.ReadUInt32();
                    uint exifOffset2 = exifLocation + prefixSize;

                    Tags = ReadExifData(br, exifOffset2);
                }
            }
        }

        private Dictionary<string, object> ReadExifData(BinaryReader br, uint exifOffset)
        {
            // Should we make assumptions about endianness for HEIC? Really the only code
            // difference would be instantiating a BinaryReader vs BigEndianBinaryReader

            br.BaseStream.Seek(exifOffset, SeekOrigin.Begin);

            //bool bigEndian;

            // Test for TIFF validity and endian
            ushort tiffCheck = br.ReadUInt16();
            if(tiffCheck == 0x4949)
            {
                //bigEndian = false;
            }
            else if(tiffCheck == 0x4D4D)
            {
                //bigEndian = true;
            }
            else
            {
                // Not valid TIFF data! (no 0x4949 or 0x4D4D)
                return null;
            }

            br.BaseStream.Seek(exifOffset + 2, SeekOrigin.Begin);
            if(br.ReadUInt16() != 0x002A)
            {
                // Not valid TIFF data! (no 0x002A)
                return null;
            }

            UInt32 firstIFDOffset = br.ReadUInt32();
            if (firstIFDOffset < 8)
            {
                // Invalid TIFF data, the first offset is less than 8.
                return null;
            }

            Dictionary<string, object> tags = ReadTags(br, exifOffset, exifOffset + firstIFDOffset, EXIFStrings.TiffTags);

            if(tags.ContainsKey("ExifIFDPointer"))
            {
                UInt32 exifPointer = (UInt32)tags["ExifIFDPointer"];

                Dictionary<string, object> exifDataTags = ReadTags(br, exifOffset, (UInt32)(exifOffset + exifPointer), EXIFStrings.ExifTags);
                
                foreach (string tag in exifDataTags.Keys)
                {
                    object keyValue = exifDataTags[tag];

                    switch (tag)
                    {
                        case "LightSource":
                        case "Flash":
                        case "MeteringMode":
                        case "ExposureProgram":
                        case "SensingMethod":
                        case "SceneCaptureType":
                        case "SceneType":
                        case "CustomRendered":
                        case "WhiteBalance":
                        case "GainControl":
                        case "Contrast":
                        case "Saturation":
                        case "Sharpness":
                        case "SubjectDistanceRange":
                        case "FileSource":
                            int exifTagVal = GetValueAsInt(exifDataTags[tag]);
                            keyValue = EXIFStrings.Values[tag][exifTagVal];
                            break;

                        case "ExifVersion":
                        case "FlashpixVersion":
                            int exifTagVal2 = GetValueAsInt(exifDataTags[tag]);
                            byte[] tagBytes = new byte[4];
                            tagBytes[0] = (byte)(exifTagVal2 >> 24);
                            tagBytes[1] = (byte)(exifTagVal2 >> 16);
                            tagBytes[2] = (byte)(exifTagVal2 >> 8);
                            tagBytes[3] = (byte)(exifTagVal2);
                            keyValue = tagBytes[0].ToString() + tagBytes[1].ToString() + tagBytes[2].ToString() + tagBytes[3].ToString();
                            break;

                        case "ComponentsConfiguration":
                            int exifTagVal3 = GetValueAsInt(exifDataTags[tag]);
                            byte[] tagBytes2 = new byte[4];
                            tagBytes2[0] = (byte)(exifTagVal3 >> 24);
                            tagBytes2[1] = (byte)(exifTagVal3 >> 16);
                            tagBytes2[2] = (byte)(exifTagVal3 >> 8);
                            tagBytes2[3] = (byte)(exifTagVal3);

                            keyValue =
                                EXIFStrings.ComponentStrings[tagBytes2[0]] +
                                EXIFStrings.ComponentStrings[tagBytes2[1]] +
                                EXIFStrings.ComponentStrings[tagBytes2[2]] +
                                EXIFStrings.ComponentStrings[tagBytes2[3]];
                            break;
                    }
                    tags[tag] = keyValue;
                }
            }

            if (tags.ContainsKey("GPSInfoIFDPointer"))
            {
                int gpsOffset = GetValueAsInt(tags["GPSInfoIFDPointer"]);
                Dictionary<string, object> gpsDataTags = ReadTags(br, exifOffset, (uint)(exifOffset + gpsOffset), EXIFStrings.GPSTags);

                foreach (string tag in gpsDataTags.Keys)
                {
                    switch (tag)
                    {
                        case "GPSVersionID":
                            int tagVal = GetValueAsInt(gpsDataTags[tag]);
                            byte[] tagBytes = new byte[4];
                            tagBytes[0] = (byte)(tagVal >> 24);
                            tagBytes[1] = (byte)(tagVal >> 16);
                            tagBytes[2] = (byte)(tagVal >> 8);
                            tagBytes[3] = (byte)(tagVal);
                            gpsDataTags[tag] = string.Format("{0}.{1}.{2}.{3}", tagBytes[0], tagBytes[1], tagBytes[2], tagBytes[3]);
                            break;
                    }
                    tags[tag] = gpsDataTags[tag];
                }
            }

            return tags;
        }

        private Dictionary<string, object> ReadTags(BinaryReader br, UInt32 tiffStart, UInt32 dirStart, Dictionary<int,string> strings)
        {
            br.BaseStream.Seek(dirStart, SeekOrigin.Begin);

            ushort numEntries = br.ReadUInt16();
            Dictionary<string, object> tags = new Dictionary<string, object>();

            for(int i = 0; i < numEntries; i++)
            {
                uint entryOffset = (uint)(dirStart + i * 12 + 2);    // WTF magic numbers?!

                br.BaseStream.Seek(entryOffset, SeekOrigin.Begin);

                int tagIndex = br.ReadUInt16();

                if (strings.ContainsKey(tagIndex))
                {
                    string tag = strings[tagIndex];

                    tags[tag] = ReadTagValue(br, entryOffset, tiffStart, dirStart);
                }
                else
                {
                    Console.WriteLine("Unknown tag: " + tagIndex);
                }
            }

            return tags;
        }

        private object ReadTagValue(BinaryReader br, uint entryOffset, uint tiffStart, uint dirStart)
        {
            br.BaseStream.Seek(entryOffset + 2, SeekOrigin.Begin);

            ushort tagType = br.ReadUInt16();
            uint numValues = br.ReadUInt32();
            uint valueOffset = br.ReadUInt32() + tiffStart;

#if false
            uint offset;

            switch (tagType)
            {
                case 1: // byte, 8-bit unsigned int
                case 7: // undefined, 8-bit byte, value depending on field
                    if (numValues == 1)
                    {
                        br.BaseStream.Seek(entryOffset + 8, SeekOrigin.Begin);
                        return br.ReadByte();
                    }
                    else
                    {
                        offset = numValues > 4 ? valueOffset : (entryOffset + 8);
                        byte[] vals1 = new byte[numValues];

                        br.BaseStream.Seek(offset, SeekOrigin.Begin);

                        for (int n = 0; n < numValues; n++)
                        {
                            vals1[n] = br.ReadByte();
                        }
                        return vals1;
                    }

                case 2: // ascii, 8-bit byte
                    offset = numValues > 4 ? valueOffset : (entryOffset + 8);
                    br.BaseStream.Seek(offset, SeekOrigin.Begin);
                    byte[] vals = new byte[numValues - 1];
                    for(int i = 0; i < vals.Length; i++)
                    {
                        vals[i] = br.ReadByte();
                    }
                    return System.Text.ASCIIEncoding.ASCII.GetString(vals, 0, vals.Length);

                case 3: // short, 16 bit int
                    if (numValues == 1)
                    {
                        br.BaseStream.Seek(entryOffset + 8, SeekOrigin.Begin);
                        return br.ReadUInt16();
                    }
                    else
                    {
                        offset = numValues > 2 ? valueOffset : (entryOffset + 8);
                        ushort[] vals2 = new ushort[numValues];

                        br.BaseStream.Seek(offset, SeekOrigin.Begin);
                        for (int n = 0; n < numValues; n++)
                        {
                            vals2[n] = br.ReadUInt16();
                        }
                        return vals2;
                    }

                case 4: // long, 32 bit int
                    if (numValues == 1)
                    {
                        br.BaseStream.Seek(entryOffset + 8, SeekOrigin.Begin);
                        return br.ReadUInt32();
                    }
                    else
                    {
                        br.BaseStream.Seek(valueOffset, SeekOrigin.Begin);
                        uint[] vals3 = new uint[numValues];
                        for (int n = 0; n < numValues; n++)
                        {
                            vals3[n] = br.ReadUInt32();
                        }
                        return vals3;
                    }

                case 5:    // rational = two long values, first is numerator, second is denominator
                    if (numValues == 1)
                    {
                        br.BaseStream.Seek(valueOffset, SeekOrigin.Begin);
                        uint numerator = br.ReadUInt32();
                        uint denominator = br.ReadUInt32();
                        return (numerator / denominator);
                    }
                    else
                    {
                        br.BaseStream.Seek(valueOffset, SeekOrigin.Begin);

                        float[] vals4 = new float[numValues];
                        for (int n = 0; n < numValues; n++)
                        {
                            uint numerator = br.ReadUInt32();
                            uint denominator = br.ReadUInt32();
                            vals4[n] = (numerator / denominator);
                        }
                        return vals4;
                    }

                case 9: // slong, 32 bit signed int
                    if (numValues == 1)
                    {
                        br.BaseStream.Seek(entryOffset + 8, SeekOrigin.Begin);
                        return br.ReadInt32();
                    }
                    else
                    {
                        br.BaseStream.Seek(valueOffset, SeekOrigin.Begin);
                        int[] vals5 = new int[numValues];
                        for (int n = 0; n < numValues; n++)
                        {
                            vals5[n] = br.ReadInt32();
                        }
                        return vals5;
                    }

                case 10: // signed rational, two slongs, first is numerator, second is denominator
                    if (numValues == 1)
                    {
                        br.BaseStream.Seek(valueOffset, SeekOrigin.Begin);
                        int numerator = br.ReadInt32();
                        int denominator = br.ReadInt32();
                        return (numerator / denominator);
                    }
                    else
                    {
                        br.BaseStream.Seek(valueOffset, SeekOrigin.Begin);

                        float[] vals6 = new float[numValues];
                        for (int n = 0; n < numValues; n++)
                        {
                            int numerator = br.ReadInt32();
                            int denominator = br.ReadInt32();

                            vals6[n] = (numerator / denominator);
                        }
                        return vals6;
                    }
            }
            return null;

#else
            object result = null;
            uint offset = entryOffset + 8;

            switch (tagType)
            {
                case 1: // byte, 8-bit unsigned int
                case 7: // undefined, 8-bit byte, value depending on field
                    if (numValues != 1)
                    {
                        offset = numValues > 4 ? valueOffset : (entryOffset + 8);
                    }
                    
                    br.BaseStream.Seek(offset, SeekOrigin.Begin);
                    result = br.ReadBytes((int)numValues);
                    break;

                case 2: // ascii, 8-bit byte
                    offset = numValues > 4 ? valueOffset : (entryOffset + 8);
                    br.BaseStream.Seek(offset, SeekOrigin.Begin);
                    byte[] vals = br.ReadBytes((int)numValues - 1);
                    result = System.Text.ASCIIEncoding.ASCII.GetString(vals, 0, vals.Length);
                    break;

                case 3: // short, 16 bit int
                    if (numValues != 1)
                    {
                        offset = numValues > 2 ? valueOffset : (entryOffset + 8);
                    }
                    br.BaseStream.Seek(offset, SeekOrigin.Begin);
                    result = br.ReadUInt16((int)numValues);
                    break;

                case 4: // long, 32 bit int
                    if (numValues != 1)
                    {
                        offset = valueOffset;
                    }

                    br.BaseStream.Seek(offset, SeekOrigin.Begin);
                    result = br.ReadUInt32((int)numValues);
                    break;

                case 5:    // rational = two long values, first is numerator, second is denominator
                    br.BaseStream.Seek(valueOffset, SeekOrigin.Begin);
                    UInt32[] parts = br.ReadUInt32((int)numValues * 2);
                    float[] floats = new float[numValues];
                    for(int i = 0; i < numValues; i++)
                    {
                        uint numerator = parts[(i * 2)];
                        uint denominator = parts[(i * 2) + 1];
                        floats[i] = (numerator / denominator);
                    }

                    result = floats;
                    break;

                case 9: // slong, 32 bit signed int
                    if (numValues != 1)
                    {
                        offset = valueOffset;
                    }

                    br.BaseStream.Seek(offset, SeekOrigin.Begin);
                    result = br.ReadInt32((int)numValues);
                    break;

                case 10: // signed rational, two slongs, first is numerator, second is denominator
                    br.BaseStream.Seek(valueOffset, SeekOrigin.Begin);
                    Int32[] sparts = br.ReadInt32((int)numValues * 2);
                    float[] sfloats = new float[numValues];
                    for (int i = 0; i < numValues; i++)
                    {
                        int numerator = sparts[(i * 2)];
                        int denominator = sparts[(i * 2) + 1];
                        sfloats[i] = (numerator / denominator);
                    }

                    result = sfloats;
                    break;
            }

            if(numValues == 1 && result is Array)
            {
                // If there's only one item, return that rather than a one-item array
                Array resultArray = (Array)result;
                result = resultArray.GetValue(0);
            }

            return result;
#endif
        }

        private int GetValueAsInt(object obj)
        {
            if(obj is byte)
            {
                byte b = (byte)obj;
                return b;
            }

            if(obj is byte[])
            {
                // If the system architecture is little-endian (that is, little end first),
                // reverse the byte array.
                //if (BitConverter.IsLittleEndian)
                //    Array.Reverse(bytes);

                return BitConverter.ToInt32((byte[])obj, 0);
            }

            return Convert.ToInt32(obj);
        }
    }
}
