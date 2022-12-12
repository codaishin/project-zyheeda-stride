namespace ProjectZyheeda;

using Stride.Core.Mathematics;
using Stride.Engine;

public interface IBehaviorStateMachine {
	void ExecuteNext(U<Vector3, Entity>[] targets);
	void ResetAndIdle();
}
