namespace ProjectZyheeda;

using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;

public interface IGetTargets {
	Task<U<Vector3, Entity>[]> GetTargets();
}
