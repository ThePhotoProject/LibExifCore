using System;
using System.IO;

namespace LibExifCore
{
    /// <summary>
    /// Extension functions for BinaryReader for reading multiple items at once
    /// </summary>
    public static class BinaryReaderExtensions
    {
        public static Int16[] ReadInt16(this BinaryReader reader, int numItems)
        {
            Int16[] array = new Int16[numItems];
            for(int i = 0; i < numItems; i++)
            {
                array[i] = reader.ReadInt16();
            }
            return array;
        }

        public static Int32[] ReadInt32(this BinaryReader reader, int numItems)
        {
            Int32[] array = new Int32[numItems];
            for (int i = 0; i < numItems; i++)
            {
                array[i] = reader.ReadInt32();
            }
            return array;
        }

        public static Int64[] ReadInt64(this BinaryReader reader, int numItems)
        {
            Int64[] array = new Int64[numItems];
            for (int i = 0; i < numItems; i++)
            {
                array[i] = reader.ReadInt64();
            }
            return array;
        }

        public static UInt16[] ReadUInt16(this BinaryReader reader, int numItems)
        {
            UInt16[] array = new UInt16[numItems];
            for (int i = 0; i < numItems; i++)
            {
                array[i] = reader.ReadUInt16();
            }
            return array;
        }

        public static UInt32[] ReadUInt32(this BinaryReader reader, int numItems)
        {
            UInt32[] array = new UInt32[numItems];
            for (int i = 0; i < numItems; i++)
            {
                array[i] = reader.ReadUInt32();
            }
            return array;
        }

        public static UInt64[] ReadUInt64(this BinaryReader reader, int numItems)
        {
            UInt64[] array = new UInt64[numItems];
            for (int i = 0; i < numItems; i++)
            {
                array[i] = reader.ReadUInt64();
            }
            return array;
        }
    }
}
