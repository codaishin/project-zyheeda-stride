namespace ProjectZyheeda;

using System;
using System.Linq;
using System.Threading.Tasks;
using Stride.Engine.Processors;

public readonly struct WaitFrame : IWait {
	public async Task<Result> Wait(ScriptSystem script) {
		_ = await script.NextFrame();
		return Result.Ok();
	}
}

public readonly struct WaitMilliSeconds : IWait {
	public readonly int milliSeconds;

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


public readonly struct WaitMultiple : IWait {
	private readonly IWait[] waits;
	private static readonly Result fallbackResult = Result.Ok();

	public WaitMultiple() {
		this.waits = Array.Empty<IWait>();
	}

	public WaitMultiple(params IWait[] waits) {
		this.waits = waits;
	}

	private static Result ConcatResults(Result a, Result b) {
		return a.FlatMap(() => b);
	}

	public async Task<Result> Wait(ScriptSystem script) {
		var tasks = this.waits.Select(w => w.Wait(script));
		var results = await Task.WhenAll(tasks);
		return results.Aggregate(WaitMultiple.fallbackResult, WaitMultiple.ConcatResults);
	}
}
