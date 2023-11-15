using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using QuickBin;

public static class Tester {
    [MenuItem("QuickBin/Test")]
    public static void Test() {
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
            .Write(testString.Length)
            .Write(testString)
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

        Debug.Log("QuickBin tests passed.");
    }
}