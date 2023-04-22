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
			await wait.Wait(this.game.Script);
			token.SetResult();
		});

		await token.Task;

		Assert.That(this.game.UpdateTime.FrameCount, Is.EqualTo(frame + 1));
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
			await wait.Wait(this.game.Script);
			stopwatch.Stop();
			token.SetResult(stopwatch.ElapsedMilliseconds);
		});

		var time = await token.Task;

		Assert.That(time, Is.AtLeast(100));
	}
}
