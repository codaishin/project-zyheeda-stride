namespace ProjectZyheeda;

using Stride.Core.Mathematics;
using Stride.Engine;

public interface IBehaviorStateMachine {
	void ExecuteNext(IMaybe<IUnion<Vector3, Entity>> target);
	void ResetAndIdle();
}
