namespace ProjectZyheeda;

using System.Threading.Tasks;
using Stride.Engine.Processors;

public readonly struct WaitFrame : IWait {
	public async Task<Result> Wait(ScriptSystem script) {
		_ = await script.NextFrame();
		return Result.Ok();
	}
}

public readonly struct WaitMilliSeconds : IWait {
	private readonly int milliSeconds;

	public WaitMilliSeconds(int milliseconds) {
		this.milliSeconds = milliseconds;
	}

	public async Task<Result> Wait(ScriptSystem script) {
		await Task.Delay(this.milliSeconds);
		return Result.Ok();
	}
}

public readonly struct NoWait : IWait {
	public Task<Result> Wait(ScriptSystem script) {
		return Task.FromResult(Result.Ok());
	}
}
