using NUnit.Framework;
using QuickBin;

using TextEncoding = System.Text.Encoding;

public class Tests {
    [Test]
    public static void FlagBools() {
        var testString = "Hello, world!";

        var buffer = new Serializer()
            .WriteFlag(false)
            .WriteFlag(true)
            .WriteFlag(true)
            .WriteFlag(false)
            .WriteFlag(true)
            .WriteFlag(false)
            .WriteFlag(false)
            .WriteFlag(false)
            .WriteFlag(true)
            .WriteFlag(true)
            .WriteFlag(false, true)
            .WriteFlag(true)
            .Write(10)
            .WriteFlag(true)
            .Write(testString, Serializer.Len_i32)
            .WriteFlag(false)
            .WriteFlag(true);

        Assert.AreEqual(((byte[])buffer)[0], (byte)0b0001_0110);
        Assert.AreEqual(((byte[])buffer)[1], (byte)0b0000_0011);
        Assert.AreEqual(((byte[])buffer)[2], (byte)0b0000_0010);

        new Deserializer(buffer)
            .ReadFlag(out bool a)
            .ReadFlag(out bool b)
            .ReadFlag(out bool c)
            .ReadFlag(out bool d)
            .ReadFlag(out bool e)
            .ReadFlag(out bool f)
            .ReadFlag(out bool g)
            .ReadFlag(out bool h)
            .ReadFlag(out bool i)
            .ReadFlag(out bool j)
            .ReadFlag(out bool k, true)
            .ReadFlag(out bool l)
            .Read(out int m)
            .ReadFlag(out bool n)
            .Read(out int length)
            .Read(out string o, length)
            .ReadFlag(out bool p)
            .ReadFlag(out bool q);

        Assert.IsFalse(a);
        Assert.IsTrue(b);
        Assert.IsTrue(c);
        Assert.IsFalse(d);
        Assert.IsTrue(e);
        Assert.IsFalse(f);
        Assert.IsFalse(g);
        Assert.IsFalse(h);
        Assert.IsTrue(i);
        Assert.IsTrue(j);
        Assert.IsFalse(k);
        Assert.IsTrue(l);
        Assert.AreEqual(10, m);
        Assert.IsTrue(n);
        Assert.AreEqual(testString, o);
        Assert.IsFalse(p);
        Assert.IsTrue(q);
    }
	
	[Test]
	public static void StringEncoding() {
		var testString = "Hello, World!";
		
		var buffer = new Serializer()
			.Write(testString, Serializer.Len_i64)
			.Write(testString, Serializer.Len_i32)
			.Write(testString, Serializer.Len_i16)
			.Write(testString, Serializer.Len_i8)
			.Write(testString, Serializer.Len_u64)
			.Write(testString, Serializer.Len_u32)
			.Write(testString, Serializer.Len_u16)
			.Write(testString, Serializer.Len_u8)
			.Write(testString, TextEncoding.ASCII, Serializer.Len_i32)
			.Write(testString, TextEncoding.BigEndianUnicode, Serializer.Len_i32)
			.Write(testString, TextEncoding.Unicode, Serializer.Len_i32)
			.Write(testString, TextEncoding.UTF32, Serializer.Len_i32)
			.Write(testString, TextEncoding.UTF7, Serializer.Len_i32)
			.Write(testString, TextEncoding.UTF8, Serializer.Len_i32);
		
		new Deserializer(buffer)
			.Read(out string str_long, Deserializer.Len_i64)
			.Read(out string str_int, Deserializer.Len_i32)
			.Read(out string str_short, Deserializer.Len_i16)
			.Read(out string str_sbyte, Deserializer.Len_i8)
			.Read(out string str_ulong, Deserializer.Len_u64)
			.Read(out string str_uint, Deserializer.Len_u32)
			.Read(out string str_ushort, Deserializer.Len_u16)
			.Read(out string str_byte, Deserializer.Len_u8)
			.Read(out string ascii, TextEncoding.ASCII, Deserializer.Len_i32)
			.Read(out string bigEndianUnicode, TextEncoding.BigEndianUnicode, Deserializer.Len_i32)
			.Read(out string unicode, TextEncoding.Unicode, Deserializer.Len_i32)
			.Read(out string utf32, TextEncoding.UTF32, Deserializer.Len_i32)
			.Read(out string utf7, TextEncoding.UTF7, Deserializer.Len_i32)
			.Read(out string utf8, TextEncoding.UTF8, Deserializer.Len_i32);
		
		Assert.AreEqual(testString, str_long);
		Assert.AreEqual(testString, str_int);
		Assert.AreEqual(testString, str_short);
		Assert.AreEqual(testString, str_sbyte);
		Assert.AreEqual(testString, str_ulong);
		Assert.AreEqual(testString, str_uint);
		Assert.AreEqual(testString, str_ushort);
		Assert.AreEqual(testString, str_byte);
		
		Assert.AreEqual(testString, ascii);
		Assert.AreEqual(testString, bigEndianUnicode);
		Assert.AreEqual(testString, unicode);
		Assert.AreEqual(testString, utf32);
		Assert.AreEqual(testString, utf7);
		Assert.AreEqual(testString, utf8);
	}
	
	[Test]
	public static void Overflow() {
		var overflowed = false;
		
		var buffer = new Serializer()
			.Write(10)
			.Write("Foo", Serializer.Len_i32);
		
		var reader = new Deserializer(buffer)
			.Read(out int good_a)
			.Read(out string good_b, Deserializer.Len_i32)
			.Validate(
				() => (good_a, good_b),
				out var goodProduced,
				() => overflowed = true
			);
		
		Assert.AreEqual(10, good_a);
		Assert.AreEqual("Foo", good_b);
		Assert.AreEqual(goodProduced, (10, "Foo"));
		Assert.IsFalse(overflowed);
		Assert.IsFalse(reader.Overflowed);
		
		reader = new Deserializer(buffer)
			.Read(out int bad_a)
			.Read(out string bad_b, Deserializer.Len_i32)
			.Read(out int bad_c)
			.Read(out string bad_d, Deserializer.Len_i32)
			.Validate(
				() => (bad_a, bad_b, bad_c, bad_d),
				out var badProduced,
				() => overflowed = true
			);
		
		Assert.AreEqual(10, bad_a);
		Assert.AreEqual("Foo", bad_b);
		Assert.AreEqual(default(int), bad_c);
		Assert.AreEqual(default(string), bad_d);
		Assert.AreEqual(badProduced, default((int, string, int, string)));
		Assert.IsTrue(overflowed);
		Assert.IsTrue(reader.Overflowed);
	}
}
