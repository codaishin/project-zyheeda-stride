namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;
using Stride.Engine;

public interface IBehaviorStateMachine {
	(Func<Coroutine>, Cancel) GetExecution(U<Vector3, Entity> target);
}
