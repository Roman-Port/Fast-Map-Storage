using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastMapStore
{
    public delegate void FastMapEnumerate(ref FastMapBlock block, int x, int y, int z);

    public class FastMap
    {
        public FastMapBlock[,,] tiles;
        public readonly UInt16 sizeX;
        public readonly UInt16 sizeY;
        public readonly UInt16 sizeZ;
        public readonly UInt16 revsionId = REVSION_ID;

        public FastMap(UInt16 _x, UInt16 _y, UInt16 _z)
        {
            //Init
            sizeX = _x;
            sizeY = _y;
            sizeZ = _z;
            tiles = new FastMapBlock[_x, _y, _z];
            //Fill
            EnumerateAllBlocks((ref FastMapBlock block, int x, int y, int z) =>
            {
                block = new FastMapBlock(0);
            });
        }

        public FastMap(UInt16 _x, UInt16 _y, UInt16 _z, UInt16 _rev)
        {
            //Init
            sizeX = _x;
            sizeY = _y;
            sizeZ = _z;
            tiles = new FastMapBlock[_x, _y, _z];
            revsionId = _rev;
        }

        public void EnumerateAllBlocks(FastMapEnumerate callback)
        {
            Parallel.For(0, sizeZ, delegate (int z) {
                Parallel.For(0, sizeY, delegate (int y)
                {
                    Parallel.For(0, sizeX, delegate (int x)
                    {
                        callback(ref tiles[x, y, z], x, y, z);
                    });
                });
            });
        }

        private static byte[] UInt16ToByte(UInt16 num)
        {
            byte[] data = BitConverter.GetBytes(num);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(data);
            return data;
        }

        private static byte[] UInt32ToByte(UInt32 num)
        {
            byte[] data = BitConverter.GetBytes(num);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(data);
            return data;
        }

        private static void UInt16ToStream(UInt16 num, ref MemoryStream ms)
        {
            byte[] data = BitConverter.GetBytes(num);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(data);
            ms.Write(data, 0, 2);
        }

        private static UInt16 UInt16FromStream(ref MemoryStream ms)
        {
            byte[] buf = new byte[2];
            ms.Read(buf, 0, 2);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(buf);
            UInt16 data = BitConverter.ToUInt16(buf, 0);
            return data;
        }

        private static UInt16 UInt16FromBytes(byte[] buf)
        {
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(buf);
            UInt16 data = BitConverter.ToUInt16(buf, 0);
            return data;
        }

        private static UInt32 UInt32FromBytes(byte[] buf)
        {
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(buf);
            UInt32 data = BitConverter.ToUInt32(buf, 0);
            return data;
        }

        public const UInt16 REVSION_ID = 1;

        public byte[] SaveMap()
        {
            //First, build the header
            MemoryStream ms = new MemoryStream();
            //Write header data
            UInt16ToStream(10, ref ms);
            UInt16ToStream(sizeX, ref ms);
            UInt16ToStream(sizeY, ref ms);
            UInt16ToStream(sizeZ, ref ms);
            UInt16ToStream(REVSION_ID,ref ms);
            //Now, write the content table.
            //Get the position of the content table
            UInt32 contentTableOffset = 0;
            long contentTableStart = ms.Position;
            byte[] table = new byte[((int)sizeX * (int)sizeY * (int)sizeZ) * 8];
            long contentTableAfter = contentTableStart + table.Length;
            //Enumerate through the table
            EnumerateAllBlocks((ref FastMapBlock block, int x, int y, int z) =>
            {
                //Write the entry in the table at position.
                int pos = ((sizeX * sizeY) * z) + (sizeX * y) + x;
                pos *= 8;
                byte[] buf = UInt16ToByte(block.id);
                table[pos] = buf[0];
                table[pos+1] = buf[1];
                //Now setup flags.
                List<bool> flags = new List<bool>();
                flags.Add(block.data!=null); // If it is true, the pointer is valid
                flags.AddRange(block.flags);
                BitArray bc = new BitArray(flags.ToArray());
                bc.CopyTo(buf, 0);
                //Write the buffer with the flags
                table[pos+2] = buf[0];
                table[pos + 3] = buf[1];
                table[pos + 4] = 0;
                table[pos + 5] = 0;
                if(block.data!=null)
                {
                    //Write pointer to data and add data.
                    lock(ms)
                    {
                        buf = UInt32ToByte(contentTableOffset);
                        table[pos + 4] = buf[0];
                        table[pos + 5] = buf[1];
                        table[pos + 6] = buf[2];
                        table[pos + 7] = buf[3];
                        //Now, write this content
                        ms.Position = contentTableOffset + contentTableAfter;
                        contentTableOffset += (UInt32)block.data.Length;
                        contentTableOffset += 4;
                        //Write the length
                        buf = UInt16ToByte((UInt16)block.data.Length);
                        ms.Write(buf, 0, 2);
                        //Now write content
                        ms.Write(block.data, 0, block.data.Length);
                    }
                    //Todo: write
                }
            });
            //Set the reader position back
            ms.Position = contentTableStart;
            //Write
            ms.Write(table, 0, table.Length);
            //Now, make it into a byte array
            ms.Position = 0;
            byte[] outBuf = new byte[ms.Length];
            ms.Read(outBuf, 0, (int)ms.Length);
            ms.Close();
            return outBuf;
        }

        public static FastMap LoadMap(byte[] data)
        {
            //Open stream
            MemoryStream ms = new MemoryStream(data);
            //Open meta and header
            UInt16 contentTableLocation = UInt16FromStream(ref ms);
            UInt16 mapX = UInt16FromStream(ref ms);
            UInt16 mapY = UInt16FromStream(ref ms);
            UInt16 mapZ = UInt16FromStream(ref ms);
            UInt16 rev = UInt16FromStream(ref ms);
            //Create map
            FastMap m = new FastMap(mapX, mapY, mapZ, rev);
            //Now jump to the table location.
            ms.Position = contentTableLocation;
            //Now, read in the table.
            byte[] table = new byte[((int)mapX * (int)mapY * (int)mapZ) * 8];
            ms.Read(table, 0, table.Length);
            //Now, fill in all the blocks
            m.EnumerateAllBlocks((ref FastMapBlock block, int x, int y, int z) =>
            {
                //Write the entry in the table at position.
                int pos = ((m.sizeX * m.sizeY) * z) + (m.sizeX * y) + x;
                pos *= 8;
                //Create block
                FastMapBlock b = new FastMapBlock();
                //Jump to this position
                UInt16 id = UInt16FromBytes(new byte[] { table[pos], table[pos + 1] });
                byte[] flagsByte = new byte[] { table[pos + 2], table[pos + 3] };
                BitArray bc = new BitArray(flagsByte);
                b.id = id;
                b.flags = new bool[] { bc[1], bc[2],bc[3],bc[4],bc[5],bc[6], bc[7], bc[8], bc[9], bc[10],bc[11],bc[12],bc[13],bc[14], bc[15] }; //Skip the 0th bool becasue that isn't shown to the user
                //If the additional data flag, bc[0], is true, get the data it is pointing to.
                if(bc[0])
                {
                    lock(ms)
                    {
                        //Jump to the location offered by the pointer.
                        long pointer = UInt32FromBytes(new byte[] { table[pos+4], table[pos + 5], table[pos + 6], table[pos + 7] });
                        pointer += contentTableLocation;
                        pointer += table.Length;
                        //Jump
                        ms.Position = pointer;
                        //Read in length
                        byte[] buf = new byte[2];
                        ms.Read(buf, 0, 2);
                        if (!BitConverter.IsLittleEndian)
                            Array.Reverse(buf);
                        UInt32 len = BitConverter.ToUInt16(buf, 0);
                        //Now, read in content
                        b.data = new byte[len];
                        ms.Read(b.data, 0, (int)len);
                    }
                } else
                {
                    //No data. Set it to null
                    b.data = null;
                }
                //Set block
                block = b;
            });
            //Return
            ms.Close();
            return m;
        }
    }


    public class FastMapBlock
    {
        public UInt16 id;
        ///<doc>
        ///Provides 15 flags that you can set.
        public bool[] flags = new bool[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false,false };
        ///<doc>
        ///Not required. Keep this null to save space.
        public byte[] data = null;

        public FastMapBlock()
        {

        }
        public FastMapBlock(UInt16 _id)
        {
            id = _id;
        }

        public FastMapBlock(UInt16 _id, byte[] _data)
        {
            id = _id;
            data = _data;
        }

        public FastMapBlock(UInt16 _id, string _msg)
        {
            data = Encoding.UTF8.GetBytes(_msg);
            id = _id;
        }
    }
}
