namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;

public interface IBehavior {
	Result<(Func<Coroutine>, Cancel)> GetCoroutine(Func<Vector3> getTarget);
	Result<(Func<Coroutine>, Cancel)> GetCoroutine();
}

public interface IBehaviorEditor : IBehavior { }
