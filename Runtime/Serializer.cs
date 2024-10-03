using System;
using System.Collections.Generic;
using TextEncoding = System.Text.Encoding;

namespace QuickBin {
	public sealed partial class Serializer {
		readonly List<byte> bytes;
		int boolPlace = 0;
		
		/// <summary>
		/// The number of bytes in the Serializer.
		/// </summary>
		public int Length => bytes.Count;

		/// <summary>
		/// Generates a Serializer, initializing an empty list with a capacity of 0.
		/// </summary>
		public Serializer() => bytes = new List<byte>();
		/// <summary>
		/// Generates a Serializer, initializing an empty list with the specified capacity.
		/// </summary>
		/// <param name="capacity">The capacity of the list. See documentation for System.Collections.Generic.List<T>(int capacity) for details.</param>
		public Serializer(int capacity) => bytes = new List<byte>(capacity);

		public static implicit operator byte[](Serializer serializer) => serializer.bytes.ToArray();
		public static implicit operator List<byte>(Serializer serializer) => serializer.bytes;

		/// <summary>
		/// Clears the internal List so that the Serializer can be reused.
		/// </summary>
		/// <returns>This Serializer.</returns>
		public Serializer Clear() {
			bytes.Clear();
			return this;
		}

		private Serializer WriteGeneric<T>(T value, Func<T, byte> f) {
			bytes.Add(f(value));
			boolPlace = 0;
			return this;
		}

		private Serializer WriteGeneric<T>(T value, Func<T, byte[]> f) {
			bytes.AddRange(f(value));
			boolPlace = 0;
			return this;
		}

		public Serializer Write(bool value)   => WriteGeneric(value, x => x ? (byte)1 : (byte)0);
		public Serializer Write(byte value)   => WriteGeneric(value, x => x);
		public Serializer Write(sbyte value)  => WriteGeneric(value, x => (byte)x);
		public Serializer Write(char value)   => WriteGeneric(value, BitConverter.GetBytes);
		public Serializer Write(short value)  => WriteGeneric(value, BitConverter.GetBytes);
		public Serializer Write(ushort value) => WriteGeneric(value, BitConverter.GetBytes);
		public Serializer Write(int value)    => WriteGeneric(value, BitConverter.GetBytes);
		public Serializer Write(uint value)   => WriteGeneric(value, BitConverter.GetBytes);
		public Serializer Write(long value)   => WriteGeneric(value, BitConverter.GetBytes);
		public Serializer Write(ulong value)  => WriteGeneric(value, BitConverter.GetBytes);
		public Serializer Write(float value)  => WriteGeneric(value, BitConverter.GetBytes);
		public Serializer Write(double value) => WriteGeneric(value, BitConverter.GetBytes);
		
		/// <summary>A method that writes the length of a byte array to the Serializer.</summary>
		/// <param name="buffer">The Serializer to write the length to.</param>
		/// <param name="value">The byte array to write the length of.</param>
		/// <returns>This Serializer.</returns>
		public delegate Serializer LengthWriter(Serializer buffer, byte[] value);
		public static Serializer Len_i64(Serializer buffer, byte[] value) => buffer.Write(value.LongLength);
		public static Serializer Len_u64(Serializer buffer, byte[] value) => buffer.Write((ulong)value.LongLength);
		public static Serializer Len_i32(Serializer buffer, byte[] value) => buffer.Write(value.Length);
		public static Serializer Len_u32(Serializer buffer, byte[] value) => buffer.Write((uint)value.LongLength);
		public static Serializer Len_i16(Serializer buffer, byte[] value) => buffer.Write((short)value.Length);
		public static Serializer Len_u16(Serializer buffer, byte[] value) => buffer.Write((ushort)value.Length);
		public static Serializer Len_i8(Serializer buffer, byte[] value) =>  buffer.Write((sbyte)value.Length);
		public static Serializer Len_u8(Serializer buffer, byte[] value) =>  buffer.Write((byte)value.Length);
		
		/// <summary>Writes a byte array to the Serializer.</summary>
		/// <param name="value">The byte array to write.</param>
		public Serializer Write(byte[] value) => WriteGeneric(value, x => x);
		
		/// <summary>Writes a byte array to the Serializer.</summary>
		/// <param name="value">The byte array to write.</param>
		/// <param name="writeLen">The method to use to write the length of the byte array. (e.g. <c>Len_i32</c>)</param>
		public Serializer Write(byte[] value, LengthWriter writeLen) => writeLen(this, value).Write(value);
		
		
		/// <summary>Writes a string to the Serializer.</summary>
		/// <param name="value">The string to write.</param>
		/// <param name="encoding">The encoding to use when converting the string to bytes.</param>
		public Serializer Write(string value, TextEncoding encoding) => Write(encoding.GetBytes(value));
		
		/// <summary>Writes a string to the Serializer.</summary>
		/// <param name="value">The string to write.</param>
		/// <param name="encoding">The encoding to use when converting the string to bytes.</param>
		/// <param name="writeLen">The method to use to write the length of the string. (e.g. <c>Len_i32</c>)</param>
		public Serializer Write(string value, TextEncoding encoding, LengthWriter writeLen) => Write(encoding.GetBytes(value), writeLen);
		
		/// <summary>Writes a string to the Serializer using UTF-8 encoding.</summary>
		/// <param name="value">The string to write.</param>
		public Serializer Write(string value) => Write(value, TextEncoding.UTF8);
		
		/// <summary>Writes a string to the Serializer using UTF-8 encoding.</summary>
		/// <param name="value">The string to write.</param>
		/// <param name="writeLen">The method to use to write the length of the string. (e.g. <c>Len_i32</c>)</param>
		public Serializer Write(string value, LengthWriter writeLen) => Write(value, TextEncoding.UTF8, writeLen);
		

		public Serializer Write(DateTime value) => Write(value.Ticks);
		public Serializer Write(TimeSpan value) => Write(value.Ticks);

		/// <summary>
		/// Writes booleans into the same byte if possible.
		/// </summary>
		/// <param name="value">The boolean to write.</param>
		/// <param name="forceNewByte">Whether to force writing a new byte, even if there's still space for flags in the current byte.</param>
		/// <returns>This Serializer.</returns>
		public Serializer WriteFlag(bool value, bool forceNewByte = false) {
			if (forceNewByte)
				boolPlace = 0;
			
			if (boolPlace == 0)
				Write(value);
			else
				bytes[^1] |= (byte)(value ? 1 << boolPlace : 0);
			
			boolPlace++;
			boolPlace %= 8;
			return this;
		}

		public Serializer Write(Version value) =>
			Write(value.Major)
			.Write(value.Minor)
			.Write(value.Build)
			.Write(value.Revision);
	}
}