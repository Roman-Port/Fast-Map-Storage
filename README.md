# Fast-Map-Storage
FastMap, a quick and easy way to save 3d arrays

# Creating an Array
Creating an array with FastMap is easy. Make sure you've imported FastMapStore, then add this line to your code.
```c#
FastMap map = new FastMap(16, 256, 16);
```
This'll create a new 3d array with the size of 16 by 256 by 16.

# Setting Tiles
It's easy to set tiles inside of the 3d array. You can make a simple tile with nothing but an ID by adding this line.
```c#
map.tiles[0, 0, 0] = new FastMapBlock(1);
```
This line will set the tile at 0,0,0 to a block with ID 1. The ID can be between 0 and 65536. You can also add some data in the form of a string or a byte array with the following lines.
```c#
map.tiles[1, 0, 0] = new FastMapBlock(1, "This is some test data");
map.tiles[2, 0, 0] = new FastMapBlock(1, new byte[]{0,0,0});
```
You can even use up to 15 flags on each tile.
```c#
FastMapBlock b = new FastMapBlock(1);
b.flags[0] = true;
b.flags[1] = true;
b.flags[2] = true;
map.tiles[3, 0, 0] = b;
```

# Saving an Array
Saving arrays with FastMap is quick and easy. Just add these lines to your code.
```c#
byte[] buf = map.SaveMap();
System.IO.File.WriteAllBytes(@"E:\test.bin", buf);
```
This will export the map to the byte array, then save it to the disk.

# Loading an Array
Loading an array from disk or from RAM is easy. Just add this line to your code after creating the byte array ``buf``.
```c#
FastMap loadedMap = FastMap.LoadMap(buf);
```
You can load and modify data inside of this map.

# Getting Tiles
Getting tiles is just like setting tiles. Just access the array.
```c#
FastMapBlock b = map.tiles[1, 0, 0];
Console.WriteLine(b.id);
```

# Documentation
FastMapStore file is split up into three sections

<table>
  <tr>
    <td>Name</td>
    <td>Length</td>
  </tr>
  <tr>
    <td>Header</td>
    <td>Varies</td>
  </tr>
  <tr>
    <td>Content table</td>
    <td>W * L * H * 8</td>
  </tr>
  <tr>
    <td>Content data</td>
    <td>Varies</td>
  </tr>
</table>


## Header

This header introduces some basic information about the table.

<table>
  <tr>
    <td>Name</td>
    <td>Type</td>
    <td>Length</td>
    <td>Description</td>
  </tr>
  <tr>
    <td>Content table location</td>
    <td>16bit unsigned int</td>
    <td>2 bytes</td>
    <td>The position of the content table</td>
  </tr>
  <tr>
    <td>X Width</td>
    <td>16bit unsigned int</td>
    <td>2 bytes</td>
    <td>Width of the table X</td>
  </tr>
  <tr>
    <td>Y Width</td>
    <td>16bit unsigned int</td>
    <td>2 bytes</td>
    <td>Width of the table Y</td>
  </tr>
  <tr>
    <td>Z Width</td>
    <td>16bit unsigned int</td>
    <td>2 bytes</td>
    <td>Width of the table Z</td>
  </tr>
  <tr>
    <td>Revision ID</td>
    <td>16bit unsigned int</td>
    <td>2 bytes</td>
    <td>The revision ID of the software that created this data</td>
  </tr>
</table>


## Content Table

The content table is an array of all items in the 3d array, flattened to 1d. This table is laid out X, Y, Z. Each index in this table is 8 bytes and goes as follows.

<table>
  <tr>
    <td>Name</td>
    <td>Type</td>
    <td>Length</td>
    <td>Description</td>
  </tr>
  <tr>
    <td>Block ID</td>
    <td>16bit unsigned int</td>
    <td>2 bytes</td>
    <td>The block ID used. This can be anything you’d like</td>
  </tr>
  <tr>
    <td>Flags</td>
    <td>Bit flags</td>
    <td>2 bytes</td>
    <td>2 bytes of flags. Use the bits in these flags.</td>
  </tr>
  <tr>
    <td>Pointer to content OR additional flags</td>
    <td>32bit unsigned int OR bit flags</td>
    <td>4 bytes</td>
    <td>IF flag #0 is set to 1 above, treat this as an address pointer, from after the table, to where to look for content. If flag #0 is set to 0, treat this as additional flags.</td>
  </tr>
</table>


## Content Data

The content data is optionally pointed to by the content table. This could contain additional info about a block. Data here is unknown by FastMapStore and can be set to whatever you’d like.

<table>
  <tr>
    <td>Name</td>
    <td>Type</td>
    <td>Length</td>
    <td>Description</td>
  </tr>
  <tr>
    <td>Length</td>
    <td>16bit unsigned int</td>
    <td>2 bytes</td>
    <td>Length of content.</td>
  </tr>
  <tr>
    <td>Content</td>
    <td>Varies</td>
    <td>Same as length above</td>
    <td>Unknown</td>
  </tr>
</table>


