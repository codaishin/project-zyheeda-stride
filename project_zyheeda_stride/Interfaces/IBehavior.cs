namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;

public interface IBehavior {
	(Func<Coroutine>, Cancel) GetCoroutine(Func<Vector3> getTarget);
}
