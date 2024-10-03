using UnityEngine;

namespace QuickBin {
	public sealed partial class Serializer {
		public Serializer Write(Vector2 value) =>
			Write(value.x)
			.Write(value.y);
		
		public Serializer Write(Vector3 value) => 
			Write(value.x)
			.Write(value.y)
			.Write(value.z);
		
		public Serializer Write(Vector4 value) =>
			Write(value.x)
			.Write(value.y)
			.Write(value.z)
			.Write(value.w);
		
		public Serializer Write(Vector2Int value) =>
			Write(value.x)
			.Write(value.y);
		
		public Serializer Write(Vector3Int value) =>
			Write(value.x)
			.Write(value.y)
			.Write(value.z);

		public Serializer Write(Quaternion value) =>
			Write(value.x)
			.Write(value.y)
			.Write(value.z)
			.Write(value.w);
		
		public Serializer Write(Color value) =>
			Write(value.r)
			.Write(value.g)
			.Write(value.b)
			.Write(value.a);
		
		public Serializer Write(Color32 value) =>
			Write(value.r)
			.Write(value.g)
			.Write(value.b)
			.Write(value.a);
		
		public Serializer Write(Rect value) =>
			Write(value.x)
			.Write(value.y)
			.Write(value.width)
			.Write(value.height);

		public Serializer Write(RectInt value) =>
			Write(value.x)
			.Write(value.y)
			.Write(value.width)
			.Write(value.height);
		
		public Serializer Write(Bounds value) =>
			Write(value.center)
			.Write(value.size);
		
		public Serializer Write(BoundsInt value) =>
			Write(value.center)
			.Write(value.size);
		
		public Serializer Write(Matrix4x4 value) =>
			Write(value.GetColumn(0))
			.Write(value.GetColumn(1))
			.Write(value.GetColumn(2))
			.Write(value.GetColumn(3));
	}

	

	public sealed partial class Deserializer {
		public Deserializer Read(out Vector2 produced) =>
			Read(out float x)
			.Read(out float y)
			.Validate(() => new(x, y), out produced);

		public Deserializer Read(out Vector3 produced) =>
			Read(out float x)
			.Read(out float y)
			.Read(out float z)
			.Validate(() => new(x,y,z), out produced);

		public Deserializer Read(out Vector4 produced) =>
			Read(out float x)
			.Read(out float y)
			.Read(out float z)
			.Read(out float w)
			.Validate(() => new(x, y, z, w), out produced);

		public Deserializer Read(out Vector2Int produced) =>
			Read(out int x)
			.Read(out int y)
			.Validate(() => new(x, y), out produced);

		public Deserializer Read(out Vector3Int produced) =>
			Read(out int x)
			.Read(out int y)
			.Read(out int z)
			.Validate(() => new(x, y), out produced);

		public Deserializer Read(out Quaternion produced) =>
			Read(out float x)
			.Read(out float y)
			.Read(out float z)
			.Read(out float w)
			.Validate(() => new(x, y, z, w), out produced);

		public Deserializer Read(out Color produced) =>
			Read(out float r)
			.Read(out float g)
			.Read(out float b)
			.Read(out float a)
			.Validate(() => new(r, g, b, a), out produced);

		public Deserializer Read(out Color32 produced) =>
			Read(out byte r)
			.Read(out byte g)
			.Read(out byte b)
			.Read(out byte a)
			.Validate(() => new(r, g, b, a), out produced);

		public Deserializer Read(out Matrix4x4 produced) =>
			Read(out Vector4 c1)
			.Read(out Vector4 c2)
			.Read(out Vector4 c3)
			.Read(out Vector4 c4)
			.Validate(() => new(c1, c2, c3, c4), out produced);

		public Deserializer Read(out Rect produced) =>
			Read(out float x)
			.Read(out float y)
			.Read(out float width)
			.Read(out float height)
			.Validate(() => new(x, y, width, height), out produced);

		public Deserializer Read(out RectInt produced) =>
			Read(out int x)
			.Read(out int y)
			.Read(out int width)
			.Read(out int height)
			.Validate(() => new(x, y, width, height), out produced);

		public Deserializer Read(out Bounds produced) =>
			Read(out Vector3 center)
			.Read(out Vector3 size)
			.Validate(() => new(center, size), out produced);

		public Deserializer Read(out BoundsInt produced) =>
			Read(out Vector3Int center)
			.Read(out Vector3Int size)
			.Validate(() => new(center, size), out produced);
	}
}