namespace ProjectZyheeda;

using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;

public interface IBehaviorStateMachine {
	Task<bool> Execute(U<Vector3, Entity> target);
	void ResetAndIdle();
}
