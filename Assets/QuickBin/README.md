# QuickBin
QuickBin is intended to make binary serialization and deserialization nearly thoughtless.

Serialization and deserialization are made to be mirror images of each other, so it's easy to see what you're doing, and make sure you serialize and deserialize your data the exact same way. The heavy focus on method chaining makes it easy to read, and quick to implement.

## The Serializer class
The serializer is a wrapper for a `List<byte>`. Each time you call `Serializer.Write` it picks the appropriate overload method for the provided type, and adds the bytes to the list. Once you're ready to use the produced bytes, like to write them to a file, you can simply put the reference to the serializer into an argument or field of type `byte[]`, and it will implicitly convert itself.
```cs
var helloWorld = "Hello, QuickBin!";

Serializer buffer = new()
  .Write(10)
  .Write(18.5f)
  .Write(helloWorld.Length)
  .Write(helloWorld);

byte[] bytes = buffer;
```

## The Deserializer class
The deserializer holds three pieces of information: A reference to a `byte[] buffer`, an `int ReadIndex`, and an `int ForbiddenIndex`. The deserializer will never mutate the byte array, and can effectively only read between `ReadIndex` and `ForbiddenIndex`. Every time you call `Deserializer.Read`, `ReadIndex` will increment by the number of bytes read for the specified type. If you attempt to read data beyond the end of the buffer, or beyond `ForbiddenIndex`, QuickBin will throw an exception.

Each overload for `Deserializer.Read` provides an `out` argument. Using initialization syntax or providing an existing typed field is how the deserializer selects the correct overload method for converting bytes into a type.
```cs
new Deserializer(bytes)
  .Read(out int firstNumber)
  .Read(out float secondNumber)
  .Read(out int helloWorldLength)
  .Read(out helloWorld, helloWorldLength);

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
    .Return(new(foo, bar), out produced);
}
```
