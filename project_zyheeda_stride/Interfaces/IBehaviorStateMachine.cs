namespace ProjectZyheeda;

using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;

public interface IBehaviorStateMachine {
	Task<bool> Execute(IAsyncEnumerable<U<Vector3, Entity>> targets);
	void ResetAndIdle();
}
