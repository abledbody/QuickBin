using System;
using System.Collections.Generic;
using TextEncoding = System.Text.Encoding;

namespace QuickBin {
	public sealed partial class Deserializer {
		readonly byte[] buffer;
		
		/// <summary>
		/// The next index in the buffer that will be read from.
		/// </summary>
		public int ReadIndex {get; private set;}
		/// <summary>
		/// The index of the first byte that is not readable by this Deserializer.
		/// </summary>
		public int ForbiddenIndex {get;}
		
		/// <summary>
		/// Whether or not the Deserializer has read all of the bytes that it is allowed to.
		/// </summary>
		public bool IsExhausted => ReadIndex >= ForbiddenIndex;
		/// <summary>
		/// The length of the buffer, including bytes that are not readable by this Deserializer.
		/// </summary>
		public int InternalLength => buffer.Length;
		/// <summary>
		/// The number of remaining bytes that can be read by this Deserializer.
		/// </summary>
		public int Remaining => ForbiddenIndex - ReadIndex;
		/// <summary>
		/// Indexes the buffer offset by the ReadIndex.
		/// </summary>
		/// <param name="index">The index of the byte to get.</param>
		/// <returns>The byte the specified number of indices after the ReadIndex.</returns>
		public byte this[int index] => buffer[ReadIndex + index];

		/// <summary>
		/// Creates a Deserializer from a byte array.
		/// </summary>
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

		/// <summary>
		/// Clones the remaining bytes in the buffer.
		/// </summary>
		/// <returns>A new byte array containing the remaining bytes in the buffer.</returns>
		public byte[] CloneOutArray() {
			var result = new byte[Remaining];
			Array.Copy(buffer, ReadIndex, result, 0, Remaining);
			return result;
		}
		
		private static byte[] Extract(byte[] buffer, int index, int length) {
			var result = new byte[length];
			Array.Copy(buffer, index, result, 0, length);
			return result;
		}
		
		/// <summary>
		/// Executes an action for each value, providing an enumerated integer. This method is purely for the convenience of chaining.
		/// </summary>
		/// <param name="count">How many times to execute the action.</param>
		/// <returns>This Deserializer</returns>
		public Deserializer ForEach(int count, Action<int> action) {
			for (var i = ReadIndex; i < count; i++)
				action(i);
			return this;
		}

		/// <summary>
		/// Puts obj into produced. No really, that's all it does.
		/// </summary>
		/// <returns>This Deserializer</returns>
		public Deserializer Return<T>(T obj, out T produced) {
			produced = obj;
			return this;
		}


		// These ReadGeneric methods are the core of the Deserializer.
		// They make it possible to make every primitive Read method a single line.
		private Deserializer ReadGeneric<T>(int width, Func<byte[], int, T> f, out T produced) {
			var nextIndex = ReadIndex + width;
			// It's okay if ReadIndex + width == buffer.Length, because index represents the next byte to read, not the last byte read.
			if (nextIndex > buffer.Length) throw new IndexOutOfRangeException("Read exceeds length of buffer.");
			if (nextIndex > ForbiddenIndex) throw new ForbiddenIndexException(ForbiddenIndex, ReadIndex);

			produced = f(buffer, ReadIndex);
			ReadIndex = nextIndex;
			return this;
		}

		private Deserializer ReadGeneric<T>(int? byteLength, Func<byte[], int, int, T> f, out T produced) {
			if (!byteLength.HasValue)
				byteLength = Remaining;
			
			var nextIndex = ReadIndex + byteLength.Value;
			if (nextIndex > buffer.Length) throw new IndexOutOfRangeException("Read exceeds length of buffer.");
			if (nextIndex > ForbiddenIndex) throw new ForbiddenIndexException(ForbiddenIndex, ReadIndex);
			
			produced = f(buffer, ReadIndex, byteLength.Value);
			ReadIndex = nextIndex;
			return this;
		}

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

		public Deserializer Read(out string produced, int? length = null) => ReadGeneric(length, TextEncoding.UTF8.GetString, out produced);
		public Deserializer Read(out byte[] produced, int? length = null) => ReadGeneric(length, Extract, out produced);

		public Deserializer Read(out DateTime produced) =>
			Read(out long ticks)
			.Return(new(ticks), out produced);

		public Deserializer Read(out TimeSpan produced) =>
			Read(out long ticks)
			.Return(new(ticks), out produced);

		public Deserializer Read(out Version produced) =>
			Read(out int major)
			.Read(out int minor)
			.Read(out int build)
			.Read(out int revision)
			.Return(new(major, minor, build, revision), out produced);
	}

	public class ForbiddenIndexException : Exception {
		public ForbiddenIndexException(int forbidden, int index) : base($"Attempted to read at index {index}, which is beyond the forbidden index of {forbidden}.") {}
	}
}