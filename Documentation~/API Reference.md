# QuickBin API Reference

## What QuickBin Is For
QuickBin does nothing more or less than write data as bytes, and read bytes as data. It is not a compression library, nor is it a data format library. While .NET already provides this functionality, QuickBin has an opinionated design to make the interface between the user and the serialization process virtually invisible.

This is done by providing a fluent API that, as much as possible, corresponds each piece of data to a single, extremely repeatable action, no matter the complexity. At the same time, because it is so focused on this one task, there are very few ways to use it incorrectly, and many ways to build on top of the basic functionality.

## Using QuickBin at the Top Level
QuickBin uses a top-down approach to serialization. At the bottom are `Write` and `Read` operations for primitive types, and at the top are methods that initialize the process and use the results. As such, QuickBin has no built-in mechanisms for handling circular references. This is left as an exercise for the user.

Each type gets one or more defitions for its respective `Read` and `Write` methods, each of which calls `Read` and `Write` methods for its fields, respectively. At the very highest level of this recursive structure, you create and use a `Serializer` or `Deserializer` object to create, or read from, a byte array. Here's an example of how you might use QuickBin to save and load a type to and from a file:
```cs
public void Save(MyType value, string path) =>
	File.WriteAllBytes(path, new Serializer().Write(value));

public MyType Load(string path) =>
	new Deserializer(File.ReadAllBytes(path))
		.Read(out MyType value)
		.Output(value);
```
The `Serializer` class will automatically cast to a byte array in appropriate contexts. This is why the `Serializer` object is passed directly into `File.WriteAllBytes`.

This example leaves out the infrastructure that has to be built around the `MyType` class to make it serializable, which will be covered in the next section, but for the purposes of the class that owns the `Save` and `Load` methods, nothing else needs to be done.

Note the use of the `Output` method in the `Load` method. This is just to get around the fact that the `Read` method returns the `Deserializer` object, and not the value that was read. You can read more about this method in the [Chain Extensions](#Chain-Extensions) section. Suffice it to say that you can just return the value directly if you would rather use block bodied methods.

## Writing Read/Write Methods
When you want to create a serialization strategy, the standard way is to create extension methods for Quickbin's `Serializer` and `Deserializer` types. Here is a template for creating these methods:
```cs
public static partial class QuickBinExtensions {
	public static Serializer Write(this Serializer buffer, MyType value) => buffer
		// Data writing code here
	
	public static Deserializer Read(this Deserializer buffer, out MyType value) => buffer
		// Data reading code here
}
```
It's recommended that you put this code in the same file as the type you are writing it for so that the relevant code is all in one place. This is why the template uses a `partial` class, because you may want to make several extensions in the same namespace, but different files. Even if you don't put it in the same file, for instance if you're writing an extension for a type that you don't have the source code for, it will work just as well.

Note that there is absolutely nothing stopping you from naming these methods differently, or even defining more than one of either method. This is especially useful for [creating multiple deserialization strategies for different versions of a data format.](#Versioning)

### Write
Of the two methods, `Write` is the easier to implement. All you need to do is call `Write` for each piece of data you want to serialize, like so:
```cs
public static Serializer Write(this Serializer buffer, MyType value) => buffer
	.Write(value.field1)
	.Write(value.field2)
	.Write(value.field3);
```
Alternatively, if the values you want to write are protected or private, you can implement the `Write` method as an instance method, and then call it from the extension method:
```cs
public Serializer Serialize(Serializer buffer) => buffer
	.Write(field1)
	.Write(field2)
	.Write(field3);

// ...

public static Serializer Write(this Serializer buffer, MyType value) => value.Serialize(buffer);
```
The same applies to deserialization, which will be covered in the next section.

### Read
Deserialization is where things can start to go wrong, since there's no guarantee that the data you're reading is valid. In principle, it works almost identically to serialization, with the exception that you need to somehow get the type you're creating out, and you need to validate the data you're reading. Here's an example of a deserialization method:
```cs
public static Deserializer Read(this Deserializer buffer, out MyType produced) => buffer
	.Read(out byte field1)
	.Read(out int field2)
	.Read(out bool field3)
	.Validate(() => MyType(), out produced);
```
The `Validate` method takes on a few duties at once. The first is that it works as a place to construct the object you want to deserialze. The second is that it gives you an opportunity to put it into the `out` parameter of your method. The third is that it can do something other than construct the object if the buffer overruns. When this happens, by default, it will just return the default value of the type you're trying to read, but you can also specify a custom action to take, and value to output, like so:
```cs
public static Deserializer Read(this Deserializer buffer, out MyType produced) => buffer
	.Read(out byte field1)
	.Read(out int field2)
	.Read(out bool field3)
	.Validate(() => MyType(), out produced, () => MyType.Invalid);
```
In this case, if the buffer overruns, the `produced` variable will be set to `MyType.Invalid`, a hypothetical static field of `MyType`. If you want to do more than just make a default value or throw an exception when the buffer overruns, then at any point in the call stack you can read the `buffer.Overrun` property to see if the buffer has overrun, and take appropriate action.

In effect, this is a somewhat roundabout Try pattern. The only reason it's not an actual Try pattern is because this would sacrifice method chaining, and because handling a sequence of several operations that, in all likelihood, handle failure in the exact same way with a Try pattern would require a lot of boilerplate, both of which would be detrimental to the clarity and conciseness of QuickBin's API.

Similar to serialization, you can access private or protected fields by implementing the `Read` method as an instance method, or, as demonstrated here, by using a constructor that takes a `Deserializer` as an argument:
```cs
public MyType(Deserializer buffer) => buffer
	.Read(out field1)
	.Read(out field2)
	.Read(out field3);

// ...

public static Deserializer Read(this Deserializer buffer, out MyType produced) => buffer
	.Validate(() => new MyType(buffer), out produced);
```

## Byte Arrays and Strings
### Length Prefixing
Byte arrays and strings have a little bit of extra infrastructure make working with them easier. It's perfectly possible to write and read them in the same way as any other type, but in practice these types are rarely a fixed size, and you will want their byte length to be serialized before the data itself. The `Write` and `Read` methods for these types both have an optional parameter for specifying how that length is written.
```cs
public static Serializer Write(this Serializer buffer, byte[] value) => buffer
	.Write(value, Serializer.Len_u16);

public static Deserializer Read(this Deserializer buffer, out byte[] value) => buffer
	.Read(out value, Deserializer.Len_u16);
```
You can find a complete list of these methods in the [Length Prefixers](#Length-Prefixers) section.

### String Encoding
Since there are several ways to encode a string into bytes, the `Write` and `Read` methods for strings have an optional parameter for specifying the encoding. If you don't specify an encoding, it will default to UTF-8.
```cs
using TextEncoding = System.Text.Encoding;

// ...

public static Serializer Write(this Serializer buffer, string value) => buffer
	.Write(value, TextEncoding.UTF16, Serializer.Len_u16);

public static Deserializer Read(this Deserializer buffer, out string value) => buffer
	.Read(out value, TextEncoding.UTF16, Deserializer.Len_u16);
```

## Flags
QuickBin does not natively support any sort of compression, with the exception of flags. Flags are a way to compress up to eight boolean values into a single byte. The `ReadFlag` and `WriteFlag` methods work the same as any other `Write` or `Read` operation.
```cs
public static Serializer Write(this Serializer buffer, MyType value) => buffer
	.WriteFlag(value.flag1, true)
	.WriteFlag(value.flag2)
	.WriteFlag(value.flag3);

public static Deserializer Read(this Deserializer buffer, out MyType value) => buffer
	.ReadFlag(out bool flag1, true)
	.ReadFlag(out bool flag2)
	.ReadFlag(out bool flag3)
	.Validate(() => new MyType(flag1, flag2, flag3), out value);
```
Even though three booleans are written and read, they are saved into the same byte. This works up until eight booleans are processed, at which point the next flag will be processed in the next byte. The optional second parameter of `WriteFlag` and `ReadFlag` will cause it to skip to the next byte if the first bit of the current byte has already been processed. Effectively, this is a way to start processing flags in a completely unprocessed byte, which can be used either to separate flag bytes by function, or to ensure that the byte was not used as part of a previous serialization operation.

## Endianness
QuickBin uses little-endian byte ordering by default. To write a value in big-endian byte ordering, you can use the `WriteBig` and `ReadBig` methods. These methods are available for all number types larger than a byte.
```cs
public static Serializer Write(this Serializer buffer, MyType value) => buffer
	.WriteBig(value.anInt)
	.WriteBig(value.aFloat)
	.Write(value.aUShort);

public static Deserializer Read(this Deserializer buffer, out MyType value) => buffer
	.ReadBig(out int anInt)
	.ReadBig(out float aFloat)
	.Read(out ushort aUShort)
	.Validate(() => new MyType(anInt, aFloat, aUShort), out value);
```

## Deserializer Members
The `Deserializer` class has substantially more internal state than `Serializer`, a good chunk of which is exposed to the user.

### Properties
`int ReadIndex` The index in the provided array that the buffer will read from next.

`int ForbiddenIndex` The index in the provided array that the buffer will not attempt to read past. This is essentially a compromise towards being unable to use `Span` without turning Deserializer into a ref struct, which would make it impossible to extend in anything earlier than C# 13.

`bool IsExhausted` Indicates that there are no more bytes left to read.

`int InternalLength` The length of the buffer that the Deserializer is reading from. This is does not account for `ReadIndex` or `ForbiddenIndex`.

`int Remaining` The number of readable bytes left in the buffer.

`bool Overflowed` Indicates that the buffer has tried to read more data than was accessible to it, and that at least one of the `Read` operations has produced a default value.

`byte [index]` Reads a byte a certain number of bytes ahead of the `ReadIndex`.

### Methods
`.Validate(constructor, out variable, onOverflow = null)` Validates that the buffer has not overrun, and if it has, sets the output variable to the result of onOverflow, or the default value of the type if onOverflow is null. If the buffer has not overrun, it sets the output variable to the result of the constructor.

`.ReadMany(count, out produced, read)` Performs the same `read` function `count` times.

## Chain Extensions
Since QuickBin is designed to be used in a very fluent style, several extra methods are provided in the `ChainExtensions` namespace to make writing block bodied methods less of a necessity. While these methods are made mostly for use in QuickBin, they will be available for use on all types when imported.

`.Assign(value, out variable)` Outputs the first argument to the second argument. Exactly equivalent to `variable = value`. This is a viable alternative to `Validate` if you're certain the buffer can't overrun.

`.Then(action)` Executes the action.

`.Output(value)` Returns `value`.

`.When(condition, action)` Executes the action if the condition is true.

`.ForEach(values, action)` Executes `action` for each value in `values`.

## Patterns and Practices

### Versioning
When a project is still in active development, data formats are likely to change. The best way to handle this is to store a version number in the first bytes of the file, and then switch between different deserialization strategies based on that version number. QuickBin allows you to do this by defining multiple `Read` methods for the same type, and then using the `Read` method that corresponds to the version number you read from the file.

Here's a hypothetical Paintjob class that has had its format change over time, starting with just a palette index, then adding a camoflage asset reference string, and finally adding a decal object:
```cs
public static Serializer Write(this Serializer buffer, Paintjob value) => buffer
	.Write(new Version(1, 0))
	.Write(value.palette)
	.Write(value.camoflage, Serializer.Len_u16)
	.Write(value.decal);

public static Deserializer Read0_1(this Deserializer buffer, out Paintjob produced) => buffer
	.Read(out ushort palette)
	.Validate(new Paintjob(palette, "", new()), out produced);

public static Deserializer Read0_2(this Deserializer buffer, out Paintjob produced) => buffer
	.Read(out ushort palette)
	.Read(out string camoflage, Deserializer.Len_u16)
	.Validate(new Paintjob(palette, camoflage, new()), out produced);

public static Deserializer Read1_0(this Deserializer buffer, out Paintjob produced) => buffer
	.Read(out ushort palette)
	.Read(out string camoflage, Deserializer.Len_u16)
	.Read(out Decal decal)
	.Validate(new Paintjob(palette, camoflage, decal), out produced);
```
All it takes to interpret an old version of the Paintjob class is to switch on the version number read from the file, and call the corresponding `Read` method.
```cs
public static Deserializer Read(this Deserializer buffer, out Paintjob produced) => buffer
	.Read(out Version version)
	.Output(
		version switch {
			{ Major: 0, Minor: 1 } => buffer.Read0_1(out produced),
			{ Major: 0, Minor: 2 } => buffer.Read0_2(out produced),
			{ Major: 1, Minor: 0 } => buffer.Read1_0(out produced),
			_ => throw new Exception($"Unsupported version {version}")
		}
	);
}
```

### Collections
QuickBin provides the `WriteMany` and `ReadMany` methods to make serializing collections easier. In order to know how to read and write the elements in a collection, each of these methods requires a delegate that takes a `Serializer` or `Deserializer` and the element to be written or read. This is the exact same pattern as all of the default `Write` and `Read` methods, so in most cases you can just pass `buffer.Write` or `buffer.Read` respectively, and C# will automatically determine which overload to use based on the type in the collection.

It's somewhat more difficult to handle automatic length prefixing with generic types, so it's expected that you write and read the length of the collection yourself. Here's an example of how to use `WriteMany` and `ReadMany` to serialize an array of integers:
```cs
public static Serializer Write(this Serializer buffer, MyType value) => buffer
	.Write((ushort)value.ints.Length)
	.WriteMany(value.ints, buffer.Write);

public static Deserializer Read(this Deserializer buffer, out MyType produced) => buffer
	.Read(out ushort count)
	.ReadMany(out int[] ints, buffer.Read, count)
	.Validate(() => new MyType(ints), out produced);
```

### Thread Safety
QuickBin is not thread safe, and doesn't need to be. In practice, the lifetime of an instance of `Serializer` or `Deserializer` should be confined entirely to the method that creates it, and so references to them on other threads should never be a concern. Additionally, Serializers can be written to Serializers, and Deserializers can be read out of Deserializers, allowing you to cleanly separate serialization tasks from one another.

That being said, QuickBin was not designed with concurrency in mind, and has no infrastructure to support it. For the time being, it is entirely up to you to figure out what working with QuickBin in a concurrent context looks like.

## Natively Supported Types
QuickBin has built-in support for all primitive types, as well as some System and UnityEngine types. Here is a list of all types that QuickBin can serialize and deserialize out of the box:
|  System  | UnityEngine |
|----------|-------------|
|`bool`    |`Vector2`    |
|`byte`    |`Vector3`    |
|`char`    |`Vector4`    |
|`decimal` |`Quaternion` |
|`double`  |`Color`      |
|`float`   |`Color32`    |
|`int`     |`Rect`       |
|`long`    |`Matrix4x4`  |
|`sbyte`   |`Bounds`     |
|`short`   |             |
|`uint`    |             |
|`ulong`   |             |
|`ushort`  |             |
|`byte[]`  |             |
|`string`  |             |
|`DateTime`|             |
|`TimeSpan`|             |
|`Version` |             |

## Length Prefixers
Length prefixers are static methods on the Serializer and Deserializer class that write or read the byte length of a string or byte array. They are used as optional parameters in the `Write` and `Read` methods for these types. Here is a list of all length prefixers:

|  Name   |  Type  |
|-------- |--------|
|`Len_u8` |`byte`  |
|`Len_i8` |`sbyte` |
|`Len_u16`|`ushort`|
|`Len_i16`|`short` |
|`Len_u32`|`uint`  |
|`Len_i32`|`int`   |
|`Len_u64`|`ulong` |
|`Len_i64`|`long`  |