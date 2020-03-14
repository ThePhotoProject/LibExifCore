using System;
using System.IO;
using System.Collections.Generic;

namespace LibExifCore.FileFormats
{
    public abstract class FileParser
    {
        public Dictionary<string, object> Tags { get; protected set; }

        public FileParser()
        {
            Tags = new Dictionary<string, object>();
        }

        /// <summary>
        /// Parse the tags in the specified file
        /// </summary>
        /// <param name="filePath">Path to the file to analyze</param>
        /// <returns>True if tags were detected and processed, otherwise false</returns>
        public abstract bool ParseTags(string filePath);

        protected Dictionary<string, object> ReadExifData(BinaryReader reader)
        {
            // Test for TIFF validity and endian
            bool bigEndian;
            ushort tiffCheck = reader.ReadUInt16();
            if (tiffCheck == 0x4949)
            {
                bigEndian = false;
            }
            else if (tiffCheck == 0x4D4D)
            {
                bigEndian = true;
            }
            else
            {
                // Not valid TIFF data! (no 0x4949 or 0x4D4D)
                return null;
            }

            // Use a different reader depending on which endian data we have
            BinaryReader br;
            if(bigEndian)
            {
                br = new BigEndianBinaryReader(reader.BaseStream);
            }
            else
            {
                // BinaryReader is little-endian
                br = new BinaryReader(reader.BaseStream);
            }

            UInt16 tiffMarker = br.ReadUInt16();
            if (tiffMarker != 0x002A)
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

            uint exifOffset = (uint)(br.BaseStream.Position) - 8;
            Dictionary<string, object> tags = ReadTags(br, exifOffset, exifOffset + firstIFDOffset, EXIFStrings.TiffTags);

            try
            {
                if (tags.ContainsKey("ExifIFDPointer"))
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

                                // A corrupted file might have an invalid index for a particular tag. If that's the case, skip
                                // that tag but try to keep reading more tags.
                                if(!EXIFStrings.Values.ContainsKey(tag) || !EXIFStrings.Values[tag].ContainsKey(exifTagVal))
                                {
                                    continue;
                                }

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
            }
            catch(InvalidCastException)
            {
                // Sometimes a file has corrupted or invalid markers, so ExifIFDPointer is random garbage instead of 
                // a valid pointer. If that happens, we'll try to keep going for other tag types.
            }

            try
            {
                if (tags.ContainsKey("GPSInfoIFDPointer"))
                {
                    int gpsOffset = GetValueAsInt(tags["GPSInfoIFDPointer"]);
                    Dictionary<string, object> gpsDataTags = ReadTags(br, exifOffset, (uint)(exifOffset + gpsOffset), EXIFStrings.GPSTags);

                    foreach (string tag in gpsDataTags.Keys)
                    {
                        object keyValue = gpsDataTags[tag];

                        switch (tag)
                        {
                            case "GPSVersionID":
                                int tagVal = GetValueAsInt(gpsDataTags[tag]);
                                byte[] tagBytes = new byte[4];
                                tagBytes[0] = (byte)(tagVal >> 24);
                                tagBytes[1] = (byte)(tagVal >> 16);
                                tagBytes[2] = (byte)(tagVal >> 8);
                                tagBytes[3] = (byte)(tagVal);
                                keyValue = string.Format("{0}.{1}.{2}.{3}", tagBytes[0], tagBytes[1], tagBytes[2], tagBytes[3]);
                                break;
                        }
                        tags[tag] = keyValue;
                    }
                }
            }
            catch (InvalidCastException)
            {
                // A corrupted file might have a garbage GPSInfoIFDPointer. If this happens, skip trying to process
                // the GPS info but keep the rest of the image tags.
            }

            return tags;
        }

        protected virtual Dictionary<string, object> ReadTags(BinaryReader br, UInt32 tiffStart, UInt32 dirStart, Dictionary<int, string> strings)
        {
            br.BaseStream.Seek(dirStart, SeekOrigin.Begin);

            ushort numEntries = br.ReadUInt16();
            Dictionary<string, object> tags = new Dictionary<string, object>();

            for (int i = 0; i < numEntries; i++)
            {
                uint entryOffset = (uint)(dirStart + i * 12 + 2);    // WTF magic numbers?!

                br.BaseStream.Seek(entryOffset, SeekOrigin.Begin);

                int tagIndex = br.ReadUInt16();

                if (strings.ContainsKey(tagIndex))
                {
                    string tag = strings[tagIndex];

                    object readValue = ReadTagValue(br, entryOffset, tiffStart);
                    if (readValue != null)
                    {
                        tags[tag] = readValue;
                    }
                }
                else
                {
                    Console.WriteLine("Unknown tag: " + tagIndex);
                }
            }

            return tags;
        }

        protected object ReadTagValue(BinaryReader br, uint entryOffset, uint tiffStart)
        {
            br.BaseStream.Seek(entryOffset + 2, SeekOrigin.Begin);

            ushort tagType = br.ReadUInt16();
            uint numValues = br.ReadUInt32();
            uint valueOffset = br.ReadUInt32() + tiffStart;

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
                    for (int i = 0; i < numValues; i++)
                    {
                        uint numerator = parts[(i * 2)];
                        uint denominator = parts[(i * 2) + 1];

                        // A file that wasn't encoded properly could cause a divide by zero here. If that happens, skip
                        // this tag but keep reading more tags.
                        if(denominator == 0)
                        {
                            return null;
                        }

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

            if (numValues == 1 && result is Array)
            {
                // If there's only one item, return that rather than a one-item array
                Array resultArray = (Array)result;
                result = resultArray.GetValue(0);
            }

            return result;
        }

        private int GetValueAsInt(object obj)
        {
            if (obj is byte)
            {
                byte b = (byte)obj;
                return b;
            }

            if (obj is byte[])
            {
                // If the system architecture is little-endian, reverse the byte array.
                //if (BitConverter.IsLittleEndian)
                //    Array.Reverse(bytes);

                return BitConverter.ToInt32((byte[])obj, 0);
            }

            return Convert.ToInt32(obj);
        }
    }
}
