using System;
using Bin = System.Buffers.Binary.BinaryPrimitives;

namespace QuickBin {
	internal delegate void ByteWriter<T>(Span<byte> destination, T value);
	internal delegate T ByteReader<T>(ReadOnlySpan<byte> buffer);
	
	internal struct Endianness {
		internal static readonly Endianness little = new() {
			write_u16 = Bin.WriteUInt16LittleEndian,
			write_i16 = Bin.WriteInt16LittleEndian,
			write_u32 = Bin.WriteUInt32LittleEndian,
			write_i32 = Bin.WriteInt32LittleEndian,
			write_u64 = Bin.WriteUInt64LittleEndian,
			write_i64 = Bin.WriteInt64LittleEndian,
			
			read_u16 = Bin.ReadUInt16LittleEndian,
			read_i16 = Bin.ReadInt16LittleEndian,
			read_u32 = Bin.ReadUInt32LittleEndian,
			read_i32 = Bin.ReadInt32LittleEndian,
			read_u64 = Bin.ReadUInt64LittleEndian,
			read_i64 = Bin.ReadInt64LittleEndian,
		};
		
		internal static readonly Endianness big = new() {
			write_u16 = Bin.WriteUInt16BigEndian,
			write_i16 = Bin.WriteInt16BigEndian,
			write_u32 = Bin.WriteUInt32BigEndian,
			write_i32 = Bin.WriteInt32BigEndian,
			write_u64 = Bin.WriteUInt64BigEndian,
			write_i64 = Bin.WriteInt64BigEndian,
			
			read_u16 = Bin.ReadUInt16BigEndian,
			read_i16 = Bin.ReadInt16BigEndian,
			read_u32 = Bin.ReadUInt32BigEndian,
			read_i32 = Bin.ReadInt32BigEndian,
			read_u64 = Bin.ReadUInt64BigEndian,
			read_i64 = Bin.ReadInt64BigEndian
		};
		
		internal ByteWriter<ushort> write_u16;
		internal ByteWriter<short>  write_i16;
		internal ByteWriter<uint>   write_u32;
		internal ByteWriter<int>    write_i32;
		internal ByteWriter<ulong>  write_u64;
		internal ByteWriter<long>   write_i64;
		
		internal ByteReader<ushort> read_u16;
		internal ByteReader<short>  read_i16;
		internal ByteReader<uint>   read_u32;
		internal ByteReader<int>    read_i32;
		internal ByteReader<ulong>  read_u64;
		internal ByteReader<long>   read_i64;
	}
}