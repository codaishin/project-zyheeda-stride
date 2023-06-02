namespace Tests;

using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using ProjectZyheeda;

public class TestWaitFrame : GameTestCollection {
	[Test]
	public async Task WaitNextFrame() {
		var wait = new WaitFrame();
		var token = new TaskCompletionSource();
		var frame = 0;

		this.Tasks.AddTask(async () => {
			frame = this.game.UpdateTime.FrameCount;
			_ = await wait.Wait(this.game.Script);
			token.SetResult();
		});

		await token.Task;

		Assert.That(this.game.UpdateTime.FrameCount, Is.EqualTo(frame + 1));
	}

	[Test]
	public async Task ResultOk() {
		var wait = new WaitFrame();
		var token = new TaskCompletionSource();
		Result result = Result.SystemError("result not set");

		this.Tasks.AddTask(async () => {
			result = await wait.Wait(this.game.Script);
			token.SetResult();
		});

		await token.Task;

		var ok = result.Switch(
			_ => false,
			() => true
		);

		Assert.That(ok, Is.True);
	}
}

public class TestWaitMilliseconds : GameTestCollection {

	[Test]
	public async Task Wait100ms() {
		var wait = new WaitMilliSeconds(100);
		var token = new TaskCompletionSource<long>();

		this.Tasks.AddTask(async () => {
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			_ = await wait.Wait(this.game.Script);
			stopwatch.Stop();
			token.SetResult(stopwatch.ElapsedMilliseconds);
		});

		var time = await token.Task;

		Assert.That(time, Is.AtLeast(100));
	}

	[Test]
	public async Task ResultOk() {
		var wait = new WaitMilliSeconds(0);
		var result = await wait.Wait(this.game.Script);
		var ok = result.Switch(
			_ => false,
			() => true
		);

		Assert.That(ok, Is.True);
	}
}

public class TestNoWait : GameTestCollection {
	[Test]
	public async Task CompletesImmediately() {
		var wait = new NoWait();
		var token = new TaskCompletionSource();
		var frame = 0;

		this.Tasks.AddTask(async () => {
			frame = this.game.UpdateTime.FrameCount;
			_ = await wait.Wait(this.game.Script);
			token.SetResult();
		});

		await token.Task;

		Assert.That(this.game.UpdateTime.FrameCount, Is.EqualTo(frame));
	}

	[Test]
	public async Task ResultOk() {
		var wait = new NoWait();
		var result = await wait.Wait(this.game.Script);
		var ok = result.Switch(
			_ => false,
			() => true
		);

		Assert.That(ok, Is.True);
	}
}
