using UnityEngine;
using QuickBin;

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

		// QuickBin requires you to serialize the length of strings and arrays yourself.
		// This is done so you can specify the type used to store the length.
		buffer.Write(name.Length)
			.Write(name)
			.Write(id)
			.Write(velocity);
	
	public static Deserializer Deserialize(Deserializer buffer, out ExampleClass produced) =>
		// Thanks to the "out" keyword, you can inline the declaration of variables,
		// and immediately use them in the next method call.
		
		// It also allows you to specify which type you want to extract with each call
		// without the use of generics or verbose method names.
		buffer.Read(out int nameLength)
			.Read(out string name, nameLength)
			.Read(out short id)
			.Read(out Vector2 velocity)
			// The Return method is a convenience method that allows for expression bodies like this.
			// All it does is spit back out what you put in.
			.Return(new(name, id, velocity), out produced);

	// Note that this is not necessarily a smart way to do a Clone,
	// but it's a good example of how to use QuickBin at the top level.
	public ExampleClass Clone() {
		var serializer = new Serializer().Write(this);

		// Serializer can be implicitly cast to a byte array so you don't have to think about it.
		// It's also what the constructor for Deserializer takes, so this becomes a dead simple operation.
		new Deserializer(serializer).Read(out ExampleClass produced);

		return produced;
	}
}

/*
This isn't the most conventional way to extend functionaltiy of a class, but consider these four points:
1. Extension methods need to be in static classes. (https://stackoverflow.com/a/3934737)
2. You need to make a unique name for each of those static classes, because:
3. Extension methods cannot be in nested classes.
4. As your types come and go, so too should the extensions for them.

Doing it as part of a partial in the same file is as close to boilerplate, junk, and cleanup free as you can get.
Keep in mind that it must be in the QuickBin namespace.
*/
namespace QuickBin {
	public sealed partial class Serializer {
		public Serializer Write(ExampleClass value) => value.Serialize(this);
	}

	public sealed partial class Deserializer {
		public Deserializer Read(out ExampleClass produced) => ExampleClass.Deserialize(this, out produced);
	}
}