namespace ProjectZyheeda;

using System.Threading.Tasks;
using Stride.Engine.Processors;

public readonly struct WaitFrame : IWait {
	public TaskCompletionSource<Result> Wait(ScriptSystem script) {
		var token = new TaskCompletionSource<Result>();
		var run = async () => {
			_ = await script.NextFrame();
			token.SetResult(Result.Ok());
		};
		_ = run();
		return token;
	}
}

public readonly struct WaitMilliSeconds : IWait {
	public readonly int milliSeconds;

	public WaitMilliSeconds(int milliseconds) {
		this.milliSeconds = milliseconds;
	}

	public TaskCompletionSource<Result> Wait(ScriptSystem script) {
		var token = new TaskCompletionSource<Result>();
		var ms = this.milliSeconds;
		var run = async () => {
			await Task.Delay(ms);
			token.SetResult(Result.Ok());
		};
		_ = run();
		return token;
	}
}

public readonly struct NoWait : IWait {
	public TaskCompletionSource<Result> Wait(ScriptSystem script) {
		var token = new TaskCompletionSource<Result>();
		token.SetResult(Result.Ok());
		return token;
	}
}
