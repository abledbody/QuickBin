using System;
using System.Collections.Generic;
using System.Text;
using QuickBin.ChainExtensions;

namespace QuickBin {
	public sealed class Deserializer {
		private readonly byte[] buffer;
		private int boolPlace = 0;
		private byte flagByte = 0;
		
		/// <summary>The next index in the buffer that will be read from.</summary>
		public int ReadIndex {get; private set;}
		/// <summary>The index of the first byte that is not readable by this Deserializer.</summary>
		public int ForbiddenIndex {get;}
		
		/// <summary>Whether or not the Deserializer has read all of the bytes that it is allowed to.</summary>
		public bool IsExhausted => ReadIndex >= ForbiddenIndex;
		
		/// <summary>The length of the buffer, including bytes that are not readable by this Deserializer.</summary>
		public int InternalLength => buffer.Length;
		/// <summary>The number of remaining bytes that can be read by this Deserializer.</summary>
		public int Remaining => ForbiddenIndex - ReadIndex;
		/// <summary>Whether the Deserializer has attempted to read more bytes than are in the buffer.</summary>
		public bool Overflowed {get; private set;}
		/// <summary>Indexes the buffer offset by the ReadIndex.</summary>
		/// <param name="index">The index of the byte to get.</param>
		/// <returns>The byte the specified number of indices after the ReadIndex.</returns>
		public byte this[int index] => buffer[ReadIndex + index];
		
		/// <summary>Creates a Deserializer from a byte array.</summary>
		/// <param name="buffer">The byte array to deserialize from.</param>
		/// <param name="readIndex">The index to start reading from.</param>
		/// <param name="forbiddenIndex">The index of the first byte that is not readable by this Deserializer.</param>
		/// <remarks>By default, Deserialize initializes to little-endian byte order.</remarks>
		public Deserializer(byte[] buffer, int readIndex = 0, int? forbiddenIndex = null) {
			this.buffer = buffer;
			ReadIndex = readIndex;
			ForbiddenIndex = forbiddenIndex ?? buffer.Length;
		}

		public static implicit operator byte[](Deserializer deserializer) => deserializer.buffer;
		public static implicit operator List<byte>(Deserializer deserializer) => new(deserializer.buffer);
		
		public Deserializer Validate<T>(Func<T> constructor, out T variable, Func<T> onOverflow = null) =>
			this.Assign(Overflowed ? (onOverflow == null ? default : onOverflow()) : constructor(), out variable);
		
		internal static byte[] Extract(ReadOnlySpan<byte> span) => span.ToArray();

		// The ReadGeneric method is the core of the Deserializer.
		// It handles advancing the ReadIndex, checking for overflow, and reading values from the buffer.
		internal Deserializer ReadGeneric<T>(int width, ByteReader<T> f, out T produced) {
			var nextIndex = ReadIndex + width;
			// It's okay if nextIndex == buffer.Length, because ReadIndex represents the next byte to read, not the last byte read.
			if (nextIndex > buffer.Length || nextIndex > ForbiddenIndex) {
				Overflowed = true;
				produced = default;
				return this;
			}

			produced = f(buffer.AsSpan(ReadIndex, width));
			ReadIndex = nextIndex;
			boolPlace = 0;
			return this;
		}
		
		/// <summary>A method that reads the length of a byte array from the Deserializer.</summary>
		/// <param name="buffer">The Deserializer to read the length from.</param>
		/// <param name="len">The length of the byte array.</param>
		/// <returns>Whether the Deserializer overflowed.</returns>
		public delegate bool LengthReader(Deserializer buffer, out int len);
		public static bool Len_i64(Deserializer buffer, out int len) => buffer.Read(out long   _len).Assign((int)_len, out len).Output(buffer.Overflowed);
		public static bool Len_u64(Deserializer buffer, out int len) => buffer.Read(out ulong  _len).Assign((int)_len, out len).Output(buffer.Overflowed);
		public static bool Len_i32(Deserializer buffer, out int len) => buffer.Read(out int    _len).Assign(     _len, out len).Output(buffer.Overflowed);
		public static bool Len_u32(Deserializer buffer, out int len) => buffer.Read(out uint   _len).Assign((int)_len, out len).Output(buffer.Overflowed);
		public static bool Len_i16(Deserializer buffer, out int len) => buffer.Read(out short  _len).Assign(     _len, out len).Output(buffer.Overflowed);
		public static bool Len_u16(Deserializer buffer, out int len) => buffer.Read(out ushort _len).Assign(     _len, out len).Output(buffer.Overflowed);
		public static bool Len_i8 (Deserializer buffer, out int len) => buffer.Read(out sbyte  _len).Assign(     _len, out len).Output(buffer.Overflowed);
		public static bool Len_u8 (Deserializer buffer, out int len) => buffer.Read(out byte   _len).Assign(     _len, out len).Output(buffer.Overflowed);
		
		/// <summary>Reads booleans from the same byte if possible.</summary>
		/// <param name="produced">The boolean that was read.</param>
		/// <param name="forceNewByte">Whether force reading from the next byte, even if there's still space for flags in the current byte.</param>
		/// <returns>This Deserializer.</returns>
		public Deserializer ReadFlag(out bool produced, bool forceNewByte = false) {
			if (forceNewByte)
				boolPlace = 0;
			
			if (boolPlace == 0)
				this.Read(out flagByte);
			
			produced = (flagByte & (1 << boolPlace)) != 0;
			
			boolPlace++;
			boolPlace %= 8;
			return this;
		}
		
		public delegate Deserializer ReadOperation<T>(out T produced);
		/// <summary>Reads several values of the same type from the Deserializer.</summary>
		/// <param name="produced">The values that were read.</param>
		/// <param name="read">The method to use to read each value. Most <c>buffer.Read</c> methods should satisfy this signature.</param>
		/// <param name="count">The number of values to read.</param>
		/// <typeparam name="T">The type of the values to read.</typeparam>
		/// <returns>This Deserializer.</returns>
		public Deserializer ReadMany<T>(out T[] produced, ReadOperation<T> read, int count) {
			produced = new T[count];
			for (var i = 0; i < count; i++)
				read(out produced[i]);
			return this;
		}
	}
	
	public static partial class QuickBinExtensions {
		public static Deserializer Read(this Deserializer buffer, out bool produced) => buffer.ReadGeneric(sizeof(bool),   BitConverter.ToBoolean,      out produced);
		public static Deserializer Read(this Deserializer buffer, out byte produced) => buffer.ReadGeneric(sizeof(byte),   (span) => span[0],           out produced);
		public static Deserializer Read(this Deserializer buffer, out sbyte produced) => buffer.ReadGeneric(sizeof(sbyte),  (span) => (sbyte)span[0],    out produced);
		public static Deserializer Read(this Deserializer buffer, out char produced) => buffer.ReadGeneric(sizeof(char),   BitConverter.ToChar,         out produced);
		
		private static Deserializer Read(this Deserializer buffer, out short produced,  Endianness endianness) => buffer.ReadGeneric(sizeof(short),  endianness.read_i16, out produced);
		private static Deserializer Read(this Deserializer buffer, out ushort produced, Endianness endianness) => buffer.ReadGeneric(sizeof(ushort), endianness.read_u16, out produced);
		private static Deserializer Read(this Deserializer buffer, out int produced,    Endianness endianness) => buffer.ReadGeneric(sizeof(int),    endianness.read_i32, out produced);
		private static Deserializer Read(this Deserializer buffer, out uint produced,   Endianness endianness) => buffer.ReadGeneric(sizeof(uint),   endianness.read_u32, out produced);
		private static Deserializer Read(this Deserializer buffer, out long produced,   Endianness endianness) => buffer.ReadGeneric(sizeof(long),   endianness.read_i64, out produced);
		private static Deserializer Read(this Deserializer buffer, out ulong produced,  Endianness endianness) => buffer.ReadGeneric(sizeof(ulong),  endianness.read_u64, out produced);
		private static Deserializer Read(this Deserializer buffer, out float produced,  Endianness endianness) =>
			buffer.ReadGeneric(sizeof(float),  span => BitConverter.Int32BitsToSingle(endianness.read_i32(span)), out produced);
		private static Deserializer Read(this Deserializer buffer, out double produced, Endianness endianness) =>
			buffer.ReadGeneric(sizeof(double), span => BitConverter.Int64BitsToDouble(endianness.read_i64(span)), out produced);
		
		public static Deserializer Read(this Deserializer buffer, out short produced)  => buffer.Read(out produced, Endianness.little);
		public static Deserializer Read(this Deserializer buffer, out ushort produced) => buffer.Read(out produced, Endianness.little);
		public static Deserializer Read(this Deserializer buffer, out int produced)    => buffer.Read(out produced, Endianness.little);
		public static Deserializer Read(this Deserializer buffer, out uint produced)   => buffer.Read(out produced, Endianness.little);
		public static Deserializer Read(this Deserializer buffer, out long produced)   => buffer.Read(out produced, Endianness.little);
		public static Deserializer Read(this Deserializer buffer, out ulong produced)  => buffer.Read(out produced, Endianness.little);
		public static Deserializer Read(this Deserializer buffer, out float produced)  => buffer.Read(out produced, Endianness.little);
		public static Deserializer Read(this Deserializer buffer, out double produced) => buffer.Read(out produced, Endianness.little);
		
		public static Deserializer ReadBig(this Deserializer buffer, out short produced)  => buffer.Read(out produced, Endianness.big);
		public static Deserializer ReadBig(this Deserializer buffer, out ushort produced) => buffer.Read(out produced, Endianness.big);
		public static Deserializer ReadBig(this Deserializer buffer, out int produced)    => buffer.Read(out produced, Endianness.big);
		public static Deserializer ReadBig(this Deserializer buffer, out uint produced)   => buffer.Read(out produced, Endianness.big);
		public static Deserializer ReadBig(this Deserializer buffer, out long produced)   => buffer.Read(out produced, Endianness.big);
		public static Deserializer ReadBig(this Deserializer buffer, out ulong produced)  => buffer.Read(out produced, Endianness.big);
		public static Deserializer ReadBig(this Deserializer buffer, out float produced)  => buffer.Read(out produced, Endianness.big);
		public static Deserializer ReadBig(this Deserializer buffer, out double produced) => buffer.Read(out produced, Endianness.big);
		

		/// <summary>Reads a string from the Deserializer.</summary>
		/// <param name="produced">The string that was read.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <param name="length">The length of the string in bytes. Defaults to the remaining bytes in the buffer.</param>
		public static Deserializer Read(this Deserializer buffer, out string produced, Encoding encoding, int? length = null) => buffer
			.ReadGeneric(length ?? buffer.Remaining, encoding.GetString, out produced);
		
		/// <summary>Reads a UTF-8 string from the Deserializer.</summary>
		/// <param name="produced">The string that was read.</param>
		/// <param name="length">The length of the string in bytes.</param>
		public static Deserializer Read(this Deserializer buffer, out string produced, int? length = null) => buffer
			.Read(out produced, Encoding.UTF8, length);
		
		/// <summary>Reads a byte array from the Deserializer.</summary>
		/// <param name="produced">The byte array that was read.</param>
		/// <param name="length">The length of the byte array in bytes. Defaults to the remaining bytes in the buffer.</param>
		public static Deserializer Read(this Deserializer buffer, out byte[] produced, int? length = null) => buffer
			.ReadGeneric(length ?? buffer.Remaining, Deserializer.Extract, out produced);
		
		
		/// <summary>Reads a string from the Deserializer.</summary>
		/// <param name="produced">The string that was read.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <param name="readLen">The method to read out the length of the string. (e.g. <c>Len_i32</c>)</param>
		public static Deserializer Read(this Deserializer buffer, out string produced, Encoding encoding, Deserializer.LengthReader readLen) {
			if (readLen(buffer, out var len)) {
				produced = default;
				return buffer;
			}
			return buffer.Read(out produced, encoding, len);
		}
		
		/// <summary>Reads a UTF-8 string from the Deserializer.</summary>
		/// <param name="produced">The string that was read.</param>
		/// <param name="readLen">The method to read out the length of the string. (e.g. <c>Len_i32</c>)</param>
		public static Deserializer Read(this Deserializer buffer, out string produced, Deserializer.LengthReader readLen) => buffer
			.Read(out produced, Encoding.UTF8, readLen);
		
		/// <summary>Reads a byte array from the Deserializer.</summary>
		/// <param name="produced">The byte array that was read.</param>
		/// <param name="readLen">The method to read out the length of the byte array. (e.g. <c>Len_i32</c>)</param>
		public static Deserializer Read(this Deserializer buffer, out byte[] produced, Deserializer.LengthReader readLen) {
			if (readLen(buffer, out var len)) {
				produced = default;
				return buffer;
			}
			return buffer.Read(out produced, len);
		}
		
		public static Deserializer Read(this Deserializer buffer, out DateTime produced) => buffer
			.Read(out long ticks)
			.Validate(() => new(ticks), out produced);

		public static Deserializer Read(this Deserializer buffer, out TimeSpan produced) => buffer
			.Read(out long ticks)
			.Validate(() => new(ticks), out produced);

		public static Deserializer Read(this Deserializer buffer, out Version produced) => buffer
			.Read(out int major)
			.Read(out int minor)
			.Read(out int build)
			.Read(out int revision)
			.Validate(() => new(major, minor, build, revision), out produced);
	}
}