namespace Tests;

using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ProjectZyheeda;
using Stride.Engine.Processors;
using Xunit;

public class TestWaitFrame : GameTestCollection {
	public TestWaitFrame(GameFixture fixture) : base(fixture) { }

	[Fact]
	public async Task WaitNextFrame() {
		var wait = new WaitFrame();
		var token = new TaskCompletionSource();
		var frame = 0;

		this.tasks.AddTask(async () => {
			frame = this.game.UpdateTime.FrameCount;
			_ = await wait.Wait(this.game.Script);
			token.SetResult();
		});

		await token.Task;

		Assert.Equal(frame + 1, this.game.UpdateTime.FrameCount);
	}

	[Fact]
	public async Task ResultOk() {
		var wait = new WaitFrame();
		var token = new TaskCompletionSource();
		Result result = Result.SystemError("result not set");

		this.tasks.AddTask(async () => {
			result = await wait.Wait(this.game.Script);
			token.SetResult();
		});

		await token.Task;

		var ok = result.Switch(
			_ => false,
			() => true
		);

		Assert.True(ok);
	}
}

public class TestWaitMilliseconds : GameTestCollection {
	public TestWaitMilliseconds(GameFixture fixture) : base(fixture) { }

	[Fact]
	public async Task Wait100ms() {
		var wait = new WaitMilliSeconds(100);
		var token = new TaskCompletionSource<long>();

		this.tasks.AddTask(async () => {
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			_ = await wait.Wait(this.game.Script);
			stopwatch.Stop();
			token.SetResult(stopwatch.ElapsedMilliseconds);
		});

		var time = await token.Task;

		Assert.True(time > 100);
	}

	[Fact]
	public async Task ResultOk() {
		var wait = new WaitMilliSeconds(0);
		var result = await wait.Wait(this.game.Script);
		var ok = result.Switch(
			_ => false,
			() => true
		);

		Assert.True(ok);
	}
}

public class TestNoWait : GameTestCollection {
	public TestNoWait(GameFixture fixture) : base(fixture) { }

	[Fact]
	public async Task CompletesImmediately() {
		var wait = new NoWait();
		var token = new TaskCompletionSource();
		var frame = 0;

		this.tasks.AddTask(async () => {
			frame = this.game.UpdateTime.FrameCount;
			_ = await wait.Wait(this.game.Script);
			token.SetResult();
		});

		await token.Task;

		Assert.Equal(frame, this.game.UpdateTime.FrameCount);
	}

	[Fact]
	public async Task ResultOk() {
		var wait = new NoWait();
		var result = await wait.Wait(this.game.Script);
		var ok = result.Switch(
			_ => false,
			() => true
		);

		Assert.True(ok);
	}
}

public class TestWaitMultiple : GameTestCollection {
	public TestWaitMultiple(GameFixture fixture) : base(fixture) { }

	private class MockWait : IWait {
		public TaskCompletionSource<Result> token = new();

		public Task<Result> Wait(ScriptSystem script) {
			return this.token.Task;
		}
	}

	[Fact]
	public void Completes() {
		var waits = new[] { new MockWait(), new MockWait() };
		var wait = new WaitMultiple(waits);

		var task = wait.Wait(this.game.Script);

		Assert.False(task.IsCompletedSuccessfully);

		waits[0].token.SetResult(Result.Ok());

		Assert.False(task.IsCompletedSuccessfully);

		waits[1].token.SetResult(Result.Ok());

		Assert.True(task.IsCompletedSuccessfully);
	}

	[Fact]
	public async Task ResultErrorA() {
		var waits = new[] { new MockWait(), new MockWait() };
		var wait = new WaitMultiple(waits);

		var task = wait.Wait(this.game.Script);

		waits[0].token.SetResult(Result.SystemError("AAA"));
		waits[1].token.SetResult(Result.Ok());

		var error = (await task).Switch<string>(
			errors => errors.system.FirstOrDefault(),
			() => "no errors"
		);

		Assert.Equal("AAA", error);
	}

	[Fact]
	public async Task ResultErrorB() {
		var waits = new[] { new MockWait(), new MockWait() };
		var wait = new WaitMultiple(waits);

		var task = wait.Wait(this.game.Script);

		waits[0].token.SetResult(Result.Ok());
		waits[1].token.SetResult(Result.SystemError("BBB"));

		var error = (await task).Switch<string>(
			errors => errors.system.FirstOrDefault(),
			() => "no errors"
		);

		Assert.Equal("BBB", error);
	}

	[Fact]
	public async Task ResultErrorAB() {
		var waits = new[] { new MockWait(), new MockWait() };
		var wait = new WaitMultiple(waits);

		var task = wait.Wait(this.game.Script);

		waits[0].token.SetResult(Result.SystemError("AAA"));
		waits[1].token.SetResult(Result.SystemError("BBB"));

		var error = (await task).Switch(
			errors => string.Join(", ", errors.system.Select(e => (string)e)),
			() => "no errors"
		);

		Assert.Equal("AAA, BBB", error);
	}

	[Fact]
	public async Task ResultOKNoWaits() {
		var wait = new WaitMultiple();
		var task = wait.Wait(this.game.Script);

		var error = (await task).Switch(
			errors => "had errors",
			() => "no errors"
		);

		Assert.Equal("no errors", error);
	}
}
