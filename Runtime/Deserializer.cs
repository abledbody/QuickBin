using System;
using System.Collections.Generic;
using TextEncoding = System.Text.Encoding;
using QuickBin.ChainExtensions;

namespace QuickBin {
	
	public sealed partial class Deserializer {
		readonly byte[] buffer;
		int boolPlace = 0;
		byte flagByte = 0;
		
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
		public Deserializer(byte[] buffer, int readIndex = 0, int? forbiddenIndex = null) {
			this.buffer = buffer;
			ReadIndex = readIndex;
			ForbiddenIndex = forbiddenIndex ?? buffer.Length;
		}

		public static implicit operator byte[](Deserializer deserializer) => deserializer.buffer;
		public static implicit operator List<byte>(Deserializer deserializer) => new(deserializer.buffer);

		/// <summary>Clones the remaining bytes in the buffer.</summary>
		/// <returns>A new byte array containing the remaining bytes in the buffer.</returns>
		public byte[] CloneOutArray() {
			var result = new byte[Remaining];
			Array.Copy(buffer, ReadIndex, result, 0, Remaining);
			return result;
		}
		
		public Deserializer Validate<T>(Func<T> constructor, out T variable, Func<T> onOverflow = null) =>
			this.Assign(Overflowed ? (onOverflow == null ? default : onOverflow()) : constructor(), out variable);
		
		
		private static byte[] Extract(byte[] buffer, int index, int length) => buffer[index..(index + length)];

		// These ReadGeneric methods are the core of the Deserializer.
		// They make it possible to make every primitive Read method a single line.
		private Deserializer ReadGeneric<T>(int width, Func<byte[], int, T> f, out T produced) {
			var nextIndex = ReadIndex + width;
			// It's okay if ReadIndex + width == buffer.Length, because index represents the next byte to read, not the last byte read.
			if (nextIndex > buffer.Length || nextIndex > ForbiddenIndex) {
				Overflowed = true;
				produced = default;
				return this;
			}

			produced = f(buffer, ReadIndex);
			ReadIndex = nextIndex;
			boolPlace = 0;
			return this;
		}

		private Deserializer ReadGeneric<T>(int? byteLength, Func<byte[], int, int, T> f, out T produced) =>
			this.Assign(byteLength ?? Remaining, out var width)
			.ReadGeneric(width, (b, i) => f(b, i, width), out produced);

		public Deserializer Read(out bool produced)   => ReadGeneric(sizeof(bool), BitConverter.ToBoolean, out produced);
		public Deserializer Read(out byte produced)   => ReadGeneric(sizeof(byte), (b,i) => b[i], out produced);
		public Deserializer Read(out sbyte produced)  => ReadGeneric(sizeof(sbyte), (b,i) => (sbyte)b[i], out produced);
		public Deserializer Read(out char produced)   => ReadGeneric(sizeof(char), BitConverter.ToChar, out produced);
		public Deserializer Read(out short produced)  => ReadGeneric(sizeof(short), BitConverter.ToInt16, out produced);
		public Deserializer Read(out ushort produced) => ReadGeneric(sizeof(ushort), BitConverter.ToUInt16, out produced);
		public Deserializer Read(out int produced)    => ReadGeneric(sizeof(int), BitConverter.ToInt32, out produced);
		public Deserializer Read(out uint produced)   => ReadGeneric(sizeof(uint), BitConverter.ToUInt32, out produced);
		public Deserializer Read(out long produced)   => ReadGeneric(sizeof(long), BitConverter.ToInt64, out produced);
		public Deserializer Read(out ulong produced)  => ReadGeneric(sizeof(ulong), BitConverter.ToUInt64, out produced);
		public Deserializer Read(out float produced)  => ReadGeneric(sizeof(float), BitConverter.ToSingle, out produced);
		public Deserializer Read(out double produced) => ReadGeneric(sizeof(double), BitConverter.ToDouble, out produced);
		
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
		
		
		/// <summary>Reads a string from the Deserializer.</summary>
		/// <param name="produced">The string that was read.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <param name="length">The length of the string in bytes. Defaults to the remaining bytes in the buffer.</param>
		public Deserializer Read(out string produced, TextEncoding encoding, int? length = null) =>
			ReadGeneric(length, encoding.GetString, out produced);
		
		/// <summary>Reads a UTF-8 string from the Deserializer.</summary>
		/// <param name="produced">The string that was read.</param>
		/// <param name="length">The length of the string in bytes.</param>
		public Deserializer Read(out string produced, int? length = null) =>
			Read(out produced, TextEncoding.UTF8, length);
		
		/// <summary>Reads a byte array from the Deserializer.</summary>
		/// <param name="produced">The byte array that was read.</param>
		/// <param name="length">The length of the byte array in bytes. Defaults to the remaining bytes in the buffer.</param>
		public Deserializer Read(out byte[] produced, int? length = null) =>
			ReadGeneric(length, Extract, out produced);
		
		
		/// <summary>Reads a string from the Deserializer.</summary>
		/// <param name="produced">The string that was read.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <param name="readLen">The method to read out the length of the string. (e.g. <c>Len_i32</c>)</param>
		public Deserializer Read(out string produced, TextEncoding encoding, LengthReader readLen) {
			if (readLen(this, out var len)) {
				produced = default;
				return this;
			}
			return Read(out produced, encoding, len);
		}
		
		/// <summary>Reads a UTF-8 string from the Deserializer.</summary>
		/// <param name="produced">The string that was read.</param>
		/// <param name="readLen">The method to read out the length of the string. (e.g. <c>Len_i32</c>)</param>
		public Deserializer Read(out string produced, LengthReader readLen) =>
			Read(out produced, TextEncoding.UTF8, readLen);
		
		/// <summary>Reads a byte array from the Deserializer.</summary>
		/// <param name="produced">The byte array that was read.</param>
		/// <param name="readLen">The method to read out the length of the byte array. (e.g. <c>Len_i32</c>)</param>
		public Deserializer Read(out byte[] produced, LengthReader readLen) {
			if (readLen(this, out var len)) {
				produced = default;
				return this;
			}
			return Read(out produced, len);
		}
		

		/// <summary>
		/// Reads booleans from the same byte if possible.
		/// </summary>
		/// <param name="produced">The boolean that was read.</param>
		/// <param name="forceNewByte">Whether force reading from the next byte, even if there's still space for flags in the current byte.</param>
		/// <returns>This Deserializer.</returns>
		public Deserializer ReadFlag(out bool produced, bool forceNewByte = false) {
			if (forceNewByte)
				boolPlace = 0;
			
			if (boolPlace == 0)
				Read(out flagByte);
			
			produced = (flagByte & (1 << boolPlace)) != 0;
			
			boolPlace++;
			boolPlace %= 8;
			return this;
		}

		public Deserializer Read(out DateTime produced) =>
			Read(out long ticks)
			.Validate(() => new(ticks), out produced);

		public Deserializer Read(out TimeSpan produced) =>
			Read(out long ticks)
			.Validate(() => new(ticks), out produced);

		public Deserializer Read(out Version produced) =>
			Read(out int major)
			.Read(out int minor)
			.Read(out int build)
			.Read(out int revision)
			.Validate(() => new(major, minor, build, revision), out produced);
	}
}