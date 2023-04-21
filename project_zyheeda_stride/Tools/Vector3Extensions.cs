namespace ProjectZyheeda;

using Stride.Core.Mathematics;

public static class Vector3Extensions {
	public static Vector3 MoveTowards(this Vector3 current, Vector3 target, float delta) {
		var diff = target - current;
		return delta < diff.Length()
			? current + (Vector3.Normalize(diff) * delta)
			: target;
	}
}
