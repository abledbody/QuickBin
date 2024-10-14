using System;
using System.Collections.Generic;
using System.Text;

namespace QuickBin {
	public sealed class Serializer {
		private readonly List<byte> bytes;
		private int boolPlace = 0;
		
		/// <summary>The number of bytes in the Serializer.</summary>
		public int Length => bytes.Count;
		

		/// <summary>Generates a Serializer, initializing an empty list with a capacity of 0.</summary>
		public Serializer() => bytes = new List<byte>();
		/// <summary>Generates a Serializer, initializing an empty list with the specified capacity.</summary>
		/// <param name="capacity">The capacity of the list. See documentation for System.Collections.Generic.List<T>(int capacity) for details.</param>
		/// <remarks>By default, Serializer initializes to little-endian byte order.</remarks>
		public Serializer(int capacity) {
			bytes = new List<byte>(capacity);
		}

		public static implicit operator byte[](Serializer serializer) => serializer.bytes.ToArray();
		public static implicit operator List<byte>(Serializer serializer) => serializer.bytes;

		/// <summary>Clears the internal List so that the Serializer can be reused.</summary>
		/// <returns>This Serializer.</returns>
		public Serializer Clear() {
			bytes.Clear();
			return this;
		}
		
		internal Serializer WriteGeneric<T>(int size, T value, ByteWriter<T> f) {
			Span<byte> bytes = stackalloc byte[size];
			f(bytes, value);
			
			foreach (byte b in bytes)
				this.bytes.Add(b);
			
			boolPlace = 0;
			return this;
		}
		
		internal Serializer WriteGeneric<T>(T value, Func<T, byte> f) {
			bytes.Add(f(value));
			boolPlace = 0;
			return this;
		}

		internal Serializer WriteGeneric<T>(T value, Func<T, byte[]> f) {
			bytes.AddRange(f(value));
			boolPlace = 0;
			return this;
		}
		
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
		

		/// <summary>Writes booleans into the same byte if possible.</summary>
		/// <param name="value">The boolean to write.</param>
		/// <param name="forceNewByte">Whether to force writing a new byte, even if there's still space for flags in the current byte.</param>
		/// <returns>This Serializer.</returns>
		public Serializer WriteFlag(bool value, bool forceNewByte = false) {
			if (forceNewByte)
				boolPlace = 0;
			
			if (boolPlace == 0)
				this.Write(value);
			else
				bytes[^1] |= (byte)(value ? 1 << boolPlace : 0);
			
			boolPlace++;
			boolPlace %= 8;
			return this;
		}
		
		public delegate Serializer WriteOperation<T>(T value);
		/// <summary>Writes several values of the same type to the Serializer.</summary>
		/// <param name="values">The values to write.</param>
		/// <param name="write">The method to use to write each value. Most <c>buffer.Write</c> methods should satisfy this signature.</param>
		/// <typeparam name="T">The type of the values to write.</typeparam>
		/// <returns>This Serializer.</returns>
		public Serializer WriteMany<T>(T[] values, WriteOperation<T> write) {
			for (int i = 0; i < values.Length; i++)
				write(values[i]);
			return this;
		}
	}
	
	/// <summary>
	/// The reason that absolutely <i>none</i> of the standard Write/Read methods are built into their respective classes is
	/// because they would take precedence over other extension methods. For MonoBehaviours, this is a huge problem, because
	/// they are all implicitly castable to bool. C# would rather cast a MonoBehaviour to a bool than use an extension
	/// method. Any time you'd try to call Write(MonoBehaviour), you'd get Write(bool) instead. This problem extends to all
	/// implicitly castable types. The only effective way to avoid this is to give all Write methods the same priority,
	/// which means making them all extension methods. Read methods are also extension methods for consistency.
	/// </summary>
	public static partial class QuickBinExtensions {
		public static Serializer Write(this Serializer buffer, bool value)   => buffer.WriteGeneric(value, x => x ? (byte)1 : (byte)0);
		public static Serializer Write(this Serializer buffer, byte value)   => buffer.WriteGeneric(value, x => x);
		public static Serializer Write(this Serializer buffer, sbyte value)  => buffer.WriteGeneric(value, x => (byte)x);
		public static Serializer Write(this Serializer buffer, char value)   => buffer.WriteGeneric(value, BitConverter.GetBytes);
		
		private static Serializer Write(this Serializer buffer, short value, Endianness endianness)  => buffer.WriteGeneric(sizeof(short),  value, endianness.write_i16);
		private static Serializer Write(this Serializer buffer, ushort value, Endianness endianness) => buffer.WriteGeneric(sizeof(ushort), value, endianness.write_u16);
		private static Serializer Write(this Serializer buffer, int value, Endianness endianness)    => buffer.WriteGeneric(sizeof(int),    value, endianness.write_i32);
		private static Serializer Write(this Serializer buffer, uint value, Endianness endianness)   => buffer.WriteGeneric(sizeof(uint),   value, endianness.write_u32);
		private static Serializer Write(this Serializer buffer, long value, Endianness endianness)   => buffer.WriteGeneric(sizeof(long),   value, endianness.write_i64);
		private static Serializer Write(this Serializer buffer, ulong value, Endianness endianness)  => buffer.WriteGeneric(sizeof(ulong),  value, endianness.write_u64);
		private static Serializer Write(this Serializer buffer, float value, Endianness endianness)  =>
			buffer.WriteGeneric(sizeof(float),  value, (dest, x) => endianness.write_i32(dest, BitConverter.SingleToInt32Bits(x)));
		private static Serializer Write(this Serializer buffer, double value, Endianness endianness) =>
			buffer.WriteGeneric(sizeof(double), value, (dest, x) => endianness.write_i64(dest, BitConverter.DoubleToInt64Bits(x)));
		
		public static Serializer Write(this Serializer buffer, short value)  => buffer.Write(value, Endianness.little);
		public static Serializer Write(this Serializer buffer, ushort value) => buffer.Write(value, Endianness.little);
		public static Serializer Write(this Serializer buffer, int value)    => buffer.Write(value, Endianness.little);
		public static Serializer Write(this Serializer buffer, uint value)   => buffer.Write(value, Endianness.little);
		public static Serializer Write(this Serializer buffer, long value)   => buffer.Write(value, Endianness.little);
		public static Serializer Write(this Serializer buffer, ulong value)  => buffer.Write(value, Endianness.little);
		public static Serializer Write(this Serializer buffer, float value)  => buffer.Write(value, Endianness.little);
		public static Serializer Write(this Serializer buffer, double value) => buffer.Write(value, Endianness.little);
		
		public static Serializer WriteBig(this Serializer buffer, short value)  => buffer.Write(value, Endianness.big);
		public static Serializer WriteBig(this Serializer buffer, ushort value) => buffer.Write(value, Endianness.big);
		public static Serializer WriteBig(this Serializer buffer, int value)    => buffer.Write(value, Endianness.big);
		public static Serializer WriteBig(this Serializer buffer, uint value)   => buffer.Write(value, Endianness.big);
		public static Serializer WriteBig(this Serializer buffer, long value)   => buffer.Write(value, Endianness.big);
		public static Serializer WriteBig(this Serializer buffer, ulong value)  => buffer.Write(value, Endianness.big);
		public static Serializer WriteBig(this Serializer buffer, float value)  => buffer.Write(value, Endianness.big);
		public static Serializer WriteBig(this Serializer buffer, double value) => buffer.Write(value, Endianness.big);
		
		
		/// <summary>Writes a byte array to the Serializer.</summary>
		/// <param name="value">The byte array to write.</param>
		public static Serializer Write(this Serializer buffer, byte[] value) => buffer
			.WriteGeneric(value, x => x);
		
		/// <summary>Writes a byte array to the Serializer.</summary>
		/// <param name="value">The byte array to write.</param>
		/// <param name="writeLen">The method to use to write the length of the byte array. (e.g. <c>Len_i32</c>)</param>
		public static Serializer Write(this Serializer buffer, byte[] value, Serializer.LengthWriter writeLen) =>
			writeLen(buffer, value).Write(value);
		
		
		/// <summary>Writes a string to the Serializer.</summary>
		/// <param name="value">The string to write.</param>
		/// <param name="encoding">The encoding to use when converting the string to bytes.</param>
		public static Serializer Write(this Serializer buffer, string value, Encoding encoding) => buffer
			.Write(encoding.GetBytes(value));
		
		/// <summary>Writes a string to the Serializer.</summary>
		/// <param name="value">The string to write.</param>
		/// <param name="encoding">The encoding to use when converting the string to bytes.</param>
		/// <param name="writeLen">The method to use to write the length of the string. (e.g. <c>Len_i32</c>)</param>
		public static Serializer Write(this Serializer buffer, string value, Encoding encoding, Serializer.LengthWriter writeLen) => buffer
			.Write(encoding.GetBytes(value), writeLen);
		
		/// <summary>Writes a string to the Serializer using UTF-8 encoding.</summary>
		/// <param name="value">The string to write.</param>
		public static Serializer Write(this Serializer buffer, string value) => buffer
			.Write(value, Encoding.UTF8);
		
		/// <summary>Writes a string to the Serializer using UTF-8 encoding.</summary>
		/// <param name="value">The string to write.</param>
		/// <param name="writeLen">The method to use to write the length of the string. (e.g. <c>Len_i32</c>)</param>
		public static Serializer Write(this Serializer buffer, string value, Serializer.LengthWriter writeLen) => buffer
			.Write(value, Encoding.UTF8, writeLen);
		
		
		public static Serializer Write(this Serializer buffer, DateTime value) => buffer.Write(value.Ticks);
		public static Serializer Write(this Serializer buffer, TimeSpan value) => buffer.Write(value.Ticks);
		
		public static Serializer Write(this Serializer buffer, Version value) => buffer
			.Write(value.Major)
			.Write(value.Minor)
			.Write(value.Build)
			.Write(value.Revision);
	}
}