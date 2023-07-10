namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using ProjectZyheeda;
using Xunit;
using Xunit.Sdk;

public class TestKeyHoldExecutionStream : IDisposable {
	private readonly KeyHoldExecutionStream stream;

	public TestKeyHoldExecutionStream() {
		this.stream = new() {
			key = InputKeys.MouseLeft,
			scheduler = Mock.Of<ISchedulerEditor>(),
		};
	}

	private static FExecute Fail((IEnumerable<SystemError> system, IEnumerable<PlayerError> player) errors) {
		var messages = errors.system
			.Select(e => (string)e)
			.Concat(errors.player.Select(e => (string)e));
		throw new XunitException(string.Join(", ", messages));
	}

	[Fact]
	public void NotExecutionOnKeyDown() {
		var task = this.stream.NewExecute();

		_ = this.stream.ProcessEvent(InputKeys.MouseLeft, true);

		Assert.False(task.IsCompletedSuccessfully);
	}

	[Fact]
	public void ProcessEventOkay() {
		var task = this.stream.NewExecute();

		var result = this.stream.ProcessEvent(InputKeys.MouseLeft, true);

		Assert.True(result.Switch(_ => false, () => true));
	}

	[Fact]
	public async Task GetExecution() {
		var task = this.stream.NewExecute();

		_ = this.stream.ProcessEvent(InputKeys.MouseLeft, true);

		await Task.Delay(this.stream.minimumHold + TimeSpan.FromMilliseconds(25));

		_ = this.stream.ProcessEvent(InputKeys.MouseLeft, false);

		Assert.True(task.IsCompletedSuccessfully);
	}

	[Fact]
	public async Task GetMultipleExecutions() {
		var taskA = this.stream.NewExecute();

		_ = this.stream.ProcessEvent(InputKeys.MouseLeft, true);

		await Task.Delay(this.stream.minimumHold + TimeSpan.FromMilliseconds(25));

		_ = this.stream.ProcessEvent(InputKeys.MouseLeft, false);

		var taskB = this.stream.NewExecute();

		_ = this.stream.ProcessEvent(InputKeys.MouseLeft, true);

		await Task.Delay(this.stream.minimumHold + TimeSpan.FromMilliseconds(25));

		Assert.Multiple(
			() => Assert.True(taskA.IsCompletedSuccessfully),
			() => Assert.True(taskB.IsCompletedSuccessfully),
			() => Assert.NotSame(taskA, taskB)
		);
	}

	[Fact]
	public async Task GetExecutionWithoutRelease() {
		var task = this.stream.NewExecute();

		_ = this.stream.ProcessEvent(InputKeys.MouseLeft, true);

		await Task.Delay(this.stream.minimumHold + TimeSpan.FromMilliseconds(25));

		Assert.True(task.IsCompletedSuccessfully);
	}

	[Fact]
	public async Task NoExecutionBeforeMinimumHoldTime() {
		this.stream.minimumHold = TimeSpan.FromMilliseconds(200);

		var task = this.stream.NewExecute();

		_ = this.stream.ProcessEvent(InputKeys.MouseLeft, true);

		await Task.Delay(125);

		_ = this.stream.ProcessEvent(InputKeys.MouseLeft, false);

		await Task.Delay(125);

		Assert.False(task.IsCompletedSuccessfully);
	}

	[Fact]
	public async Task NewExecutionWhenPreviousWasCanceled() {
		this.stream.minimumHold = TimeSpan.FromMilliseconds(200);

		var task = this.stream.NewExecute();

		_ = this.stream.ProcessEvent(InputKeys.MouseLeft, true);

		await Task.Delay(125);

		_ = this.stream.ProcessEvent(InputKeys.MouseLeft, false);

		_ = this.stream.ProcessEvent(InputKeys.MouseLeft, true);

		await Task.Delay(125);

		Assert.False(task.IsCompletedSuccessfully);

		await Task.Delay(125);

		Assert.True(task.IsCompletedSuccessfully);
	}

	[Fact]
	public void NoExecutionWithWrongKey() {
		this.stream.key = InputKeys.MouseRight;
		var task = this.stream.NewExecute();

		var ok = this.stream.ProcessEvent(InputKeys.MouseLeft, true).Switch(_ => false, () => true);

		Assert.Multiple(
			() => Assert.True(ok),
			() => {
				Task.Delay(this.stream.minimumHold + TimeSpan.FromMilliseconds(25)).Wait();
				Assert.False(task.IsCompletedSuccessfully);
			}
		);
	}

	[Fact]
	public async Task ExecutionCallsSchedulerRun() {
		this.stream.minimumHold = TimeSpan.FromMilliseconds(0);

		Cancel cancel = () => Result.Ok();
		var task = this.stream.NewExecute();

		_ = this.stream.ProcessEvent(InputKeys.MouseLeft, true);

		var execution = (await task).Switch(TestKeyHoldExecutionStream.Fail, e => e);

		_ = execution(Enumerable.Empty<Result<IWait>>(), cancel);

		Mock
			.Get(this.stream.scheduler!)
			.Verify(s => s.Run(It.IsAny<IEnumerable<Result<IWait>>>(), cancel), Times.Once);
	}

	[Fact]
	public async Task CoroutineCanceledOnHoldRelease() {
		this.stream.minimumHold = TimeSpan.FromMilliseconds(0);
		var modifiedCoroutine = Enumerable.Empty<Result<IWait>>();

		_ = Mock
			.Get(this.stream.scheduler!)
			.Setup(s => s.Run(It.IsAny<IEnumerable<Result<IWait>>>(), It.IsAny<Cancel>()))
			.Returns((IEnumerable<Result<IWait>> coroutine, Cancel _) => {
				modifiedCoroutine = coroutine;
				return Result.Ok();
			});

		var task = this.stream.NewExecute();

		_ = this.stream.ProcessEvent(InputKeys.MouseLeft, true);

		var execution = (await task).Switch(TestKeyHoldExecutionStream.Fail, e => e);

		var coroutine = new Result<IWait>[] { new WaitFrame(), new WaitFrame(), new WaitFrame() };
		_ = execution(coroutine, () => Result.Ok());

		var enumerator = modifiedCoroutine.GetEnumerator();

		Assert.Multiple(
			() => Assert.True(enumerator.MoveNext()),
			() => {
				_ = this.stream.ProcessEvent(InputKeys.MouseLeft, false);
				Assert.True(enumerator.MoveNext());
			},
			() => {
				var wait = enumerator.Current.UnpackOr(new WaitMilliSeconds(1));
				_ = Assert.IsType<NoWait>(wait);
			},
			() => Assert.False(enumerator.MoveNext())
		);
	}

	[Fact]
	public async Task CallCancelOnHoldRelease() {
		this.stream.minimumHold = TimeSpan.FromMilliseconds(0);
		var modifiedCoroutine = Enumerable.Empty<Result<IWait>>();
		var cancel = Mock.Of<Cancel>();

		_ = Mock
			.Get(this.stream.scheduler!)
			.Setup(s => s.Run(It.IsAny<IEnumerable<Result<IWait>>>(), It.IsAny<Cancel>()))
			.Returns((IEnumerable<Result<IWait>> coroutine, Cancel _) => {
				modifiedCoroutine = coroutine;
				return Result.Ok();
			});

		_ = Mock
			.Get(cancel)
			.Setup(c => c())
			.Returns(Result.SystemError("AAA"));

		var task = this.stream.NewExecute();

		_ = this.stream.ProcessEvent(InputKeys.MouseLeft, true);

		var execution = (await task).Switch(TestKeyHoldExecutionStream.Fail, e => e);

		var coroutine = new Result<IWait>[] { new WaitFrame(), new WaitFrame(), new WaitFrame() };
		_ = execution(coroutine, cancel);

		var enumerator = modifiedCoroutine.GetEnumerator();

		Assert.Multiple(
			() => {
				_ = enumerator.MoveNext();
				Mock.Get(cancel).Verify(c => c(), Times.Never);
			},
			() => {
				_ = this.stream.ProcessEvent(InputKeys.MouseLeft, false);
				_ = enumerator.MoveNext();
				Mock.Get(cancel).Verify(c => c(), Times.Once);
			},
			() => {
				var error = enumerator.Current.Switch(error => (string)error.system.FirstOrDefault(), _ => "no errors");
				Assert.Equal("AAA", error);
			}
		);
	}
	[Fact]
	public async Task MissingScheduler() {
		this.stream.scheduler = null;

		var task = this.stream.NewExecute();

		_ = this.stream.ProcessEvent(InputKeys.MouseLeft, true);

		var execution = (await task).Switch(TestKeyHoldExecutionStream.Fail, e => e);

		var error = execution(Enumerable.Empty<Result<IWait>>(), () => Result.Ok()).Switch(
			errors => (string)errors.system.FirstOrDefault(),
			() => "no errors"
		);

		Assert.Equal(this.stream.MissingField(nameof(this.stream.scheduler)), error);
	}

	[Fact]
	public void CancelInSameFrameAsStart() {
		this.stream.minimumHold = TimeSpan.FromMilliseconds(100);

		var beginAwaiting = async (Task t) => await t;
		var task = this.stream.NewExecute();

		_ = this.stream.ProcessEvent(InputKeys.MouseLeft, true);

		_ = beginAwaiting(task);

		_ = this.stream.ProcessEvent(InputKeys.MouseLeft, false);
	}


	public void Dispose() {
		GC.SuppressFinalize(this);
	}
}
