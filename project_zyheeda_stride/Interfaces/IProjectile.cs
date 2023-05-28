namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;
using Stride.Engine;

public interface IProjectile {
	event Action<PhysicsComponent>? OnHit;
	void Follow(Vector3 start, Func<Vector3> getTarget, float rangeMultiplier);
}
