namespace ProjectZyheeda;

using System.Threading.Tasks;
using Stride.Engine.Processors;

public interface IWait {
	Task Wait(ScriptSystem script);
}
