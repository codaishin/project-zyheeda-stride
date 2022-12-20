namespace ProjectZyheeda;

using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;

public interface IBehaviorStateMachine {
	void ExecuteNext(IEnumerable<Task<U<Vector3, Entity>>> targets);
	void ResetAndIdle();
}
