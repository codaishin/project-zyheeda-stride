namespace ProjectZyheeda;

using Stride.Engine;
using Stride.Physics;

public static class CollisionExtensions {
	public static PhysicsComponent Other(this Collision collision, PhysicsComponent thisCollider) {
		return collision.ColliderA == thisCollider
			? collision.ColliderB
			: collision.ColliderA;
	}
}
