namespace ProjectZyheeda;

using Stride.Core.Mathematics;
using Stride.Engine;

public static class BehaviorTargetExtensions {
	public static Vector3 Position(this U<Vector3, Entity> value) {
		return value.Switch(
			vector => vector,
			entity => entity.Transform.Position
		);
	}
}
