namespace ProjectZyheeda;

using System.Threading.Tasks;
using Stride.Engine.Processors;

public readonly struct WaitFrame : IWait {
	public async Task Wait(ScriptSystem script) {
		_ = await script.NextFrame();
	}
}

public readonly struct WaitMilliSeconds : IWait {
	private readonly int milliSeconds;

	public WaitMilliSeconds(int milliseconds) {
		this.milliSeconds = milliseconds;
	}

	public Task Wait(ScriptSystem script) {
		return Task.Delay(this.milliSeconds);
	}
}

public readonly struct NoWait : IWait {
	public Task Wait(ScriptSystem script) {
		return Task.CompletedTask;
	}
}
