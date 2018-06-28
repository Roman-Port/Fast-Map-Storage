using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastMapStore;

namespace FastMapStore_Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime time = DateTime.UtcNow;
            FastMap map = new FastMap(16, 256, 16);
            map.tiles[0, 0, 0] = new FastMapBlock(1);
            Console.WriteLine("Finished creating map. Took " + (DateTime.UtcNow - time).TotalMilliseconds.ToString() + " ms.");
            time = DateTime.UtcNow;
            byte[] buf = map.SaveMap();
            Console.WriteLine("Finished saving map. Took " + (DateTime.UtcNow - time).TotalMilliseconds.ToString() + " ms.");
            System.IO.File.WriteAllBytes(@"E:\test.bin", buf);
            //Now, load map
            time = DateTime.UtcNow;
            FastMap loadedMap = FastMap.LoadMap(buf);
            Console.WriteLine("Finished loading map. Took " + (DateTime.UtcNow - time).TotalMilliseconds.ToString() + " ms.");
            //Compare
            Console.WriteLine("Comparing...");
            loadedMap.EnumerateAllBlocks((ref FastMapBlock block, int x, int y, int z) =>
            {
                FastMapBlock realBlock = map.tiles[x, y, z];
                if(realBlock.id!=block.id)
                {
                    Console.WriteLine("Wrong block! Got " + block.id + " when I was supposed to see " + realBlock.id + " at pos " + x.ToString() + ", " + y.ToString() + ", " + z.ToString());
                }
                if(block.data!=null)
                {
                    if (Encoding.ASCII.GetString(block.data) != Encoding.ASCII.GetString(realBlock.data))
                    {
                        Console.WriteLine("Wrong block data! Got " + Encoding.ASCII.GetString(block.data) + " when I was supposed to see " + Encoding.ASCII.GetString(realBlock.data) + " at pos " + x.ToString() + ", " + y.ToString() + ", " + z.ToString());
                    }

                    if (realBlock.data.Length != block.data.Length)
                    {
                        Console.WriteLine("Wrong block data length! Got " + block.data.Length.ToString() + " when I was supposed to see " + realBlock.data.Length.ToString() + " at pos " + x.ToString() + ", " + y.ToString() + ", " + z.ToString());
                    }
                }
            });
            Console.WriteLine("Done comparing");
            Console.ReadLine();
        }
    }
}
