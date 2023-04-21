namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;
using Stride.Engine;

public interface IBehavior {
	(Func<Coroutine>, Cancel) GetCoroutine(U<Vector3, Entity> target);
}
