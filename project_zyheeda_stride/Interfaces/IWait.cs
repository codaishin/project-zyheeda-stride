namespace ProjectZyheeda;

using System.Threading.Tasks;
using Stride.Engine.Processors;

public interface IWait {
	Task<Result> Wait(ScriptSystem script);
}
