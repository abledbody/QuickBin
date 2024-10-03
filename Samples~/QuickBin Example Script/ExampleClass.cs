using UnityEngine;
using QuickBin.ChainExtensions;

namespace QuickBin.Example {
	public class ExampleClass {
		// All of these types are easily serialized in QuickBin.
		public string name;
		private short id;
		public Vector2 velocity;


		public ExampleClass(string name, short id, Vector2 velocity) {
			this.name = name;
			this.id = id;
			this.velocity = velocity;
		}

		// Notice that below this type, we have a partial class that extends Serializer and Deserializer.
		// We still define the methods here so that id, a private field, can be serialized.
		// It also confines the complexity of the extension to this type.

		// It's recommended you follow these patterns for Serialize and Deserialize
		// in your types to make using QuickBin's chaining easier.
		public Serializer Serialize(Serializer buffer) =>
			// You can chain off each Write or Read call, and you can also split up
			// the calls to insert some functionality in between.
			// Both serialization and deserialization are done in the same order.

			// When writing a string, you can specify a length writer which will write the length
			// of the string as a specific integer type just before the string itself.
			buffer.Write(name, Serializer.Len_u16)
				.Write(id)
				.Write(velocity);
		
		public static Deserializer Deserialize(Deserializer buffer, out ExampleClass produced) =>
			// Thanks to the "out" keyword, you can inline the declaration of variables,
			// and immediately use them in the next method call. It also allows you to specify
			// which type you want to extract with each call without the use of generics or verbose method names.
			
			// For demonstration purposes, we're skipping the use of Deserializer.Len_u16 here, and
			// reading the string length directly from the buffer. This is a perfectly valid way to do it,
			// although it's recommended that you make serializer/deserializer symmetric for readability.
			buffer.Read(out ushort nameLength)
				.Read(out string name, nameLength)
				.Read(out short id)
				.Read(out Vector2 velocity)
				// It's possible for the Deserializer to overflow the buffer. If this happens,
				// calling Validate will not execute the constructor, and will instead perform an
				// alternative action, or just output a default value if one is not provided.
				.Validate(
					() => new(name, id, velocity),
					out produced,
					() => new(null, -1, Vector2.zero)
				);

		// Note that this is not necessarily a smart way to do a Clone,
		// but it's a good example of how to use QuickBin at the top level.
		public ExampleClass Clone() {
			var serializer = new Serializer().Write(this);

			// Serializer can be implicitly cast to a byte array so you don't have to think about it.
			// A byte array is also what the constructor for Deserializer takes, so this becomes a dead simple operation.
			var deserializer = new Deserializer(serializer).Read(out ExampleClass produced);
			
			// At any point in the call stack you can check buffer.Overflowed to see
			// if there was enough data to produce a valid object.
			if (deserializer.Overflowed)
				throw new System.Exception("Oh no! Not enough data.");

			return produced;
		}
	}

	// Take note that this is a partial class. By doing this, you can put the extensions alongside
	// the type that they handle without having to come up with a unique name for each extension class.
	public static partial class QuickBinExtensions {
		public static Serializer Write(this Serializer buffer, ExampleClass value) =>
			value.Serialize(buffer);
		
		public static Deserializer Read(this Deserializer buffer, out ExampleClass produced) =>
			ExampleClass.Deserialize(buffer, out produced);
	}
}