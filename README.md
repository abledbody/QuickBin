# QuickBin
QuickBin is intended to make binary serialization and deserialization nearly thoughtless.

Serialization and deserialization are made to be mirror images of each other, so it's easy to see what you're doing, and make sure you serialize and deserialize your data the exact same way. The heavy focus on method chaining makes it easy to read, and quick to implement.

You can find a detailed explanation of QuickBin's API in the [API Reference document](API%20Reference.md).

## Unity Installation
To install QuickBin, go to the package manager, press the plus button in the top left, select `Add package by git URL...` and enter `https://github.com/abledbody/QuickBin.git`

## The Serializer class
The serializer is a wrapper for a `List<byte>`. Each time you call `Serializer.Write` it picks the appropriate overload method for the provided type, and adds the bytes to the list. Once you're ready to use the produced bytes, like to write them to a file, you can simply put the reference to the serializer into an argument or field of type `byte[]`, and it will implicitly convert itself.
```cs
var helloWorld = "Hello, QuickBin!";

var buffer = new Serializer()
  .Write(10)
  .Write(18.5f)
  .Write(helloWorld, Serializer.Len_u16);

byte[] bytes = buffer;
```

## The Deserializer class
The deserializer is primarily a wrapper around a `byte[] buffer`, with an `int ReadIndex`, an `int ForbiddenIndex`. The deserializer will never mutate the byte array, and can effectively only read between `ReadIndex` and `ForbiddenIndex`. Every time you call `Deserializer.Read`, `ReadIndex` will increment by the number of bytes read for the specified type. If you attempt to read data beyond the end of the buffer, or beyond `ForbiddenIndex`, it will stop producing meaningful data, and `Deserializer.Overflowed` will be set to true.

Each overload for `Deserializer.Read` provides an `out` argument. Using initialization syntax or providing an existing typed field is how the deserializer selects the correct overload method for converting bytes into a type.
```cs
new Deserializer(bytes)
  .Read(out int firstNumber)
  .Read(out float secondNumber)
  .Read(out helloWorld, Deserializer.Len_u16);

Debug.Log($"firstNumber: {firstNumber}, secondNumber: {secondNumber}, helloWorld: {helloWorld}");
```

## Custom extensions
Extending the serializer and deserializer is fairly easy. Just make a static class (I recommend it also be partial) and write a `Write` method for `Serializer`, and a `Read` method for `Deserializer`.
```cs
public static partial class QuickBinExtensions {
  public static Serializer Write(this Serializer buffer, ExampleClass value) => buffer
    .Write(value.foo)
    .Write(value.bar);
  
  public static Deserializer Read(this Deserializer buffer, out ExampleClass produced) => buffer
    .Read(out int foo)
    .Read(out double bar)
    .Validate(() => new(foo, bar), out produced);
}
```
