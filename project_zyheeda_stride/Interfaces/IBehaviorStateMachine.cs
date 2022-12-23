namespace ProjectZyheeda;

using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;

public interface IBehaviorStateMachine {
	void ExecuteNext(IAsyncEnumerable<U<Vector3, Entity>> targets);
	void ResetAndIdle();
}
