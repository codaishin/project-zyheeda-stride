namespace ProjectZyheeda;

using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;

public interface IGetTargets {
	IEnumerable<Task<U<Vector3, Entity>>> GetTargets();
}
