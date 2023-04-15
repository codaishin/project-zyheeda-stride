namespace ProjectZyheeda;

using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;

public interface IBehavior {
	(Func<Task>, Cancel) GetExecution(U<Vector3, Entity> target);
}
