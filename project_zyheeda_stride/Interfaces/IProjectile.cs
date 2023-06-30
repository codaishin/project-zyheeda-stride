namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;
using Stride.Engine;

public interface IProjectile {
	event Action<PhysicsComponent>? OnHit;
	event Action? OnRangeLimit;
	Result Follow(Vector3 start, Func<Result<Vector3>> getTarget, float rangeMultiplier);
}
