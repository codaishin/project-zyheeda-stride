namespace Tests;

using System.Diagnostics;
using System.Threading.Tasks;
using ProjectZyheeda;
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
