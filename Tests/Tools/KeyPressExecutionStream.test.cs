namespace Tests;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using ProjectZyheeda;
using Xunit;

public class TestKeyPressExecutionStream {
	[Fact]
	public void NewExecutionRun() {
		var stream = new KeyPressExecutionStream {
			activationKey = InputKeys.ShiftLeft,
			activation = InputActivation.OnPress,
			scheduler = Mock.Of<ISchedulerEditor>(),
		};

		var coroutine = Mock.Of<IEnumerable<Result<IWait>>>();
		var cancel = Mock.Of<Cancel>();
		var schedulerResult = Result.SystemError("Error");

		_ = Mock
			.Get(stream.scheduler!)
			.Setup(s => s.Run(coroutine, cancel))
			.Returns(schedulerResult);

		var task = stream.NewExecute();

		Assert.False(task.IsCompletedSuccessfully);

		var result = stream.ProcessEvent(InputKeys.ShiftLeft, isDown: true);


		Assert.Multiple(
			() => Assert.True(result.Switch(_ => false, () => true)),
			() => Assert.True(task.IsCompletedSuccessfully),
			async () => {
				var execute = (await task).UnpackOr(Mock.Of<FExecute>());
				Assert.Equal(schedulerResult, execute(coroutine, cancel));
			}
		);
	}

	[Fact]
	public void NewExecutionRunnerCorrectKey() {
		var stream = new KeyPressExecutionStream {
			activationKey = InputKeys.MouseRight,
			activation = InputActivation.OnPress,
			scheduler = Mock.Of<ISchedulerEditor>(),
		};

		var task = stream.NewExecute();

		var result = stream.ProcessEvent(InputKeys.ShiftLeft, isDown: true);

		Assert.Multiple(
			() => Assert.True(result.Switch(_ => false, () => true)),
			() => Assert.False(task.IsCompletedSuccessfully)
		);

		result = stream.ProcessEvent(InputKeys.MouseRight, isDown: true);

		Assert.Multiple(
			() => Assert.True(result.Switch(_ => false, () => true)),
			() => Assert.True(task.IsCompletedSuccessfully)
		);
	}

	[Fact]
	public void NewExecutionRunnerCorrectAction() {
		var stream = new KeyPressExecutionStream {
			activationKey = InputKeys.MouseRight,
			activation = InputActivation.OnRelease,
			scheduler = Mock.Of<ISchedulerEditor>(),
		};

		var task = stream.NewExecute();

		var result = stream.ProcessEvent(InputKeys.MouseRight, isDown: true);

		Assert.Multiple(
			() => Assert.True(result.Switch(_ => false, () => true)),
			() => Assert.False(task.IsCompletedSuccessfully)
		);

		stream.activation = InputActivation.OnPress;

		result = stream.ProcessEvent(InputKeys.MouseRight, isDown: false);

		Assert.Multiple(
			() => Assert.True(result.Switch(_ => false, () => true)),
			() => Assert.False(task.IsCompletedSuccessfully)
		);
	}

	[Fact]
	public void NewExecutionRunnerEnqueue() {
		var stream = new KeyPressExecutionStream {
			activationKey = InputKeys.MouseRight,
			enqueueKey = InputKeys.ShiftLeft,
			activation = InputActivation.OnPress,
			scheduler = Mock.Of<ISchedulerEditor>(),
		};

		var coroutine = Mock.Of<IEnumerable<Result<IWait>>>();
		var cancel = Mock.Of<Cancel>();
		var schedulerResult = Result.SystemError("Error");

		_ = Mock
			.Get(stream.scheduler!)
			.Setup(s => s.Enqueue(coroutine, cancel))
			.Returns(schedulerResult);

		var task = stream.NewExecute();

		var result = stream.ProcessEvent(InputKeys.ShiftLeft, isDown: true);

		Assert.True(result.Switch(_ => false, () => true));

		result = stream.ProcessEvent(InputKeys.MouseRight, isDown: true);

		Assert.Multiple(
			() => Assert.True(result.Switch(_ => false, () => true)),
			() => Assert.True(task.IsCompletedSuccessfully),
			async () => {
				var execute = (await task).UnpackOr(Mock.Of<FExecute>());
				Assert.Equal(schedulerResult, execute(coroutine, cancel));
			}
		);
	}

	[Fact]
	public void NewExecutionRunnerRunWhenEnqueueKeyUp() {
		var stream = new KeyPressExecutionStream {
			activationKey = InputKeys.MouseRight,
			enqueueKey = InputKeys.ShiftLeft,
			activation = InputActivation.OnPress,
			scheduler = Mock.Of<ISchedulerEditor>(),
		};

		var coroutine = Mock.Of<IEnumerable<Result<IWait>>>();
		var cancel = Mock.Of<Cancel>();
		var schedulerResult = Result.SystemError("Error");

		_ = Mock
			.Get(stream.scheduler!)
			.Setup(s => s.Run(coroutine, cancel))
			.Returns(schedulerResult);

		var task = stream.NewExecute();

		_ = stream.ProcessEvent(InputKeys.ShiftLeft, isDown: false);
		_ = stream.ProcessEvent(InputKeys.MouseRight, isDown: true);

		Assert.Multiple(
			() => Assert.True(task.IsCompletedSuccessfully),
			async () => {
				var execute = (await task).UnpackOr(Mock.Of<FExecute>());
				Assert.Equal(schedulerResult, execute(coroutine, cancel));
			}
		);
	}

	[Fact]
	public void NewExecutionRunNoCancel() {
		var stream = new KeyPressExecutionStream {
			activationKey = InputKeys.ShiftLeft,
			activation = InputActivation.OnPress,
			scheduler = Mock.Of<ISchedulerEditor>(),
			canBeCanceled = false,
		};

		var coroutine = Mock.Of<IEnumerable<Result<IWait>>>();
		var cancel = Mock.Of<Cancel>();
		Result schedulerResult = Result.SystemError("Error");

		_ = Mock
			.Get(stream.scheduler!)
			.Setup(s => s.Run(coroutine, null))
			.Returns(schedulerResult);

		var task = stream.NewExecute();

		_ = stream.ProcessEvent(InputKeys.ShiftLeft, isDown: true);

		Assert.Multiple(
			() => Assert.True(task.IsCompletedSuccessfully),
			async () => {
				var execute = (await task).UnpackOr(Mock.Of<FExecute>());
				Assert.Equal(schedulerResult, execute(coroutine, cancel));
			}
		);
	}

	[Fact]
	public void NewExecutionEnqueueNoCancel() {
		var stream = new KeyPressExecutionStream {
			activationKey = InputKeys.ShiftLeft,
			activation = InputActivation.OnPress,
			enqueueKey = InputKeys.CapsLock,
			scheduler = Mock.Of<ISchedulerEditor>(),
			canBeCanceled = false,
		};

		var coroutine = Mock.Of<IEnumerable<Result<IWait>>>();
		var cancel = Mock.Of<Cancel>();
		Result schedulerResult = Result.SystemError("Error");

		_ = Mock
			.Get(stream.scheduler!)
			.Setup(s => s.Enqueue(coroutine, null))
			.Returns(schedulerResult);

		var task = stream.NewExecute();

		_ = stream.ProcessEvent(InputKeys.CapsLock, isDown: true);
		_ = stream.ProcessEvent(InputKeys.ShiftLeft, isDown: true);

		Assert.Multiple(
			() => Assert.True(task.IsCompletedSuccessfully),
			async () => {
				var execute = (await task).UnpackOr(Mock.Of<FExecute>());
				Assert.Equal(schedulerResult, execute(coroutine, cancel));
			}
		);
	}

	[Fact]
	public void NewTaskAfterProcessEvent() {
		var stream = new KeyPressExecutionStream {
			activationKey = InputKeys.ShiftLeft,
			activation = InputActivation.OnPress,
			scheduler = Mock.Of<ISchedulerEditor>(),
		};

		var taskA = stream.NewExecute();

		var result = stream.ProcessEvent(InputKeys.ShiftLeft, isDown: true);

		Assert.True(result.Switch(_ => false, () => true));
		var taskB = stream.NewExecute();

		Assert.NotSame(taskB, taskA);
	}

	[Fact]
	public async Task MissingScheduler() {
		var stream = new KeyPressExecutionStream {
			activationKey = InputKeys.ShiftLeft,
			activation = InputActivation.OnPress,
			scheduler = null,
		};

		var task = stream.NewExecute();

		_ = stream.ProcessEvent(InputKeys.ShiftLeft, isDown: true);

		var result = await task;
		var error = result.Switch(
			errors => (string)errors.system.FirstOrDefault(),
			_ => "no error"
		);

		Assert.Equal(stream.MissingField(nameof(stream.scheduler)), error);
	}
}
