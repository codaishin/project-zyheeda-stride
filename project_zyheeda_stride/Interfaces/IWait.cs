namespace ProjectZyheeda;

using System.Threading.Tasks;
using Stride.Engine.Processors;

public interface IWait {
	TaskCompletionSource<Result> Wait(ScriptSystem script);
}
