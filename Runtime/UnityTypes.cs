using UnityEngine;

namespace QuickBin {
	public static partial class QuickBinExtensions {
		public static Serializer Write(this Serializer buffer, Vector2 value) => buffer
			.Write(value.x)
			.Write(value.y);
		
		public static Serializer Write(this Serializer buffer, Vector3 value) => buffer
			.Write(value.x)
			.Write(value.y)
			.Write(value.z);
		
		public static Serializer Write(this Serializer buffer, Vector4 value) => buffer
			.Write(value.x)
			.Write(value.y)
			.Write(value.z)
			.Write(value.w);
		
		public static Serializer Write(this Serializer buffer, Vector2Int value) => buffer
			.Write(value.x)
			.Write(value.y);
		
		public static Serializer Write(this Serializer buffer, Vector3Int value) => buffer
			.Write(value.x)
			.Write(value.y)
			.Write(value.z);

		public static Serializer Write(this Serializer buffer, Quaternion value) => buffer
			.Write(value.x)
			.Write(value.y)
			.Write(value.z)
			.Write(value.w);
		
		public static Serializer Write(this Serializer buffer, Color value) => buffer
			.Write(value.r)
			.Write(value.g)
			.Write(value.b)
			.Write(value.a);
		
		public static Serializer Write(this Serializer buffer, Color32 value) => buffer
			.Write(value.r)
			.Write(value.g)
			.Write(value.b)
			.Write(value.a);
		
		public static Serializer Write(this Serializer buffer, Rect value) => buffer
			.Write(value.x)
			.Write(value.y)
			.Write(value.width)
			.Write(value.height);

		public static Serializer Write(this Serializer buffer, RectInt value) => buffer
			.Write(value.x)
			.Write(value.y)
			.Write(value.width)
			.Write(value.height);
		
		public static Serializer Write(this Serializer buffer, Bounds value) => buffer
			.Write(value.center)
			.Write(value.size);
		
		public static Serializer Write(this Serializer buffer, BoundsInt value) => buffer
			.Write(value.center)
			.Write(value.size);
		
		public static Serializer Write(this Serializer buffer, Matrix4x4 value) => buffer
			.Write(value.GetColumn(0))
			.Write(value.GetColumn(1))
			.Write(value.GetColumn(2))
			.Write(value.GetColumn(3));
		
		public static Deserializer Read(this Deserializer buffer, out Vector2 produced) => buffer
			.Read(out float x)
			.Read(out float y)
			.Validate(() => new(x, y), out produced);

		public static Deserializer Read(this Deserializer buffer, out Vector3 produced) => buffer
			.Read(out float x)
			.Read(out float y)
			.Read(out float z)
			.Validate(() => new(x,y,z), out produced);

		public static Deserializer Read(this Deserializer buffer, out Vector4 produced) => buffer
			.Read(out float x)
			.Read(out float y)
			.Read(out float z)
			.Read(out float w)
			.Validate(() => new(x, y, z, w), out produced);

		public static Deserializer Read(this Deserializer buffer, out Vector2Int produced) => buffer
			.Read(out int x)
			.Read(out int y)
			.Validate(() => new(x, y), out produced);

		public static Deserializer Read(this Deserializer buffer, out Vector3Int produced) => buffer
			.Read(out int x)
			.Read(out int y)
			.Read(out int z)
			.Validate(() => new(x, y), out produced);

		public static Deserializer Read(this Deserializer buffer, out Quaternion produced) => buffer
			.Read(out float x)
			.Read(out float y)
			.Read(out float z)
			.Read(out float w)
			.Validate(() => new(x, y, z, w), out produced);

		public static Deserializer Read(this Deserializer buffer, out Color produced) => buffer
			.Read(out float r)
			.Read(out float g)
			.Read(out float b)
			.Read(out float a)
			.Validate(() => new(r, g, b, a), out produced);

		public static Deserializer Read(this Deserializer buffer, out Color32 produced) => buffer
			.Read(out byte r)
			.Read(out byte g)
			.Read(out byte b)
			.Read(out byte a)
			.Validate(() => new(r, g, b, a), out produced);

		public static Deserializer Read(this Deserializer buffer, out Matrix4x4 produced) => buffer
			.Read(out Vector4 c1)
			.Read(out Vector4 c2)
			.Read(out Vector4 c3)
			.Read(out Vector4 c4)
			.Validate(() => new(c1, c2, c3, c4), out produced);

		public static Deserializer Read(this Deserializer buffer, out Rect produced) => buffer
			.Read(out float x)
			.Read(out float y)
			.Read(out float width)
			.Read(out float height)
			.Validate(() => new(x, y, width, height), out produced);

		public static Deserializer Read(this Deserializer buffer, out RectInt produced) => buffer
			.Read(out int x)
			.Read(out int y)
			.Read(out int width)
			.Read(out int height)
			.Validate(() => new(x, y, width, height), out produced);

		public static Deserializer Read(this Deserializer buffer, out Bounds produced) => buffer
			.Read(out Vector3 center)
			.Read(out Vector3 size)
			.Validate(() => new(center, size), out produced);

		public static Deserializer Read(this Deserializer buffer, out BoundsInt produced) => buffer
			.Read(out Vector3Int center)
			.Read(out Vector3Int size)
			.Validate(() => new(center, size), out produced);
	}
}