namespace Tests;

using System.Threading.Tasks;
using ProjectZyheeda;
using Xunit;

public class TestInputStream {
	[Fact]
	public async Task NewAction() {
		var stream = new InputStream {
			activationKey = InputKeys.ShiftLeft,
			activation = InputActivation.OnPress
		};

		var task = stream.NewAction();

		Assert.False(task.IsCompletedSuccessfully);

		var result = stream.ProcessEvent(InputKeys.ShiftLeft, isDown: true);

		Assert.Multiple(() => {
			Assert.True(result.Switch(_ => false, () => true));
			Assert.True(task.IsCompletedSuccessfully);
		});

		var action = await task;

		Assert.Equal(InputAction.Run, action.UnpackOr(InputAction.Chain));
	}

	[Fact]
	public void NewActionCorrectKey() {
		var stream = new InputStream {
			activationKey = InputKeys.MouseRight,
			activation = InputActivation.OnPress
		};

		var task = stream.NewAction();

		var result = stream.ProcessEvent(InputKeys.ShiftLeft, isDown: true);

		Assert.Multiple(() => {
			Assert.True(result.Switch(_ => false, () => true));
			Assert.False(task.IsCompletedSuccessfully);
		});

		result = stream.ProcessEvent(InputKeys.MouseRight, isDown: true);

		Assert.Multiple(() => {
			Assert.True(result.Switch(_ => false, () => true));
			Assert.True(task.IsCompletedSuccessfully);
		});
	}

	[Fact]
	public void NewActionCorrectAction() {
		var stream = new InputStream {
			activationKey = InputKeys.MouseRight,
			activation = InputActivation.OnRelease
		};

		var task = stream.NewAction();

		var result = stream.ProcessEvent(InputKeys.MouseRight, isDown: true);

		Assert.Multiple(() => {
			Assert.True(result.Switch(_ => false, () => true));
			Assert.False(task.IsCompletedSuccessfully);
		});

		stream.activation = InputActivation.OnPress;

		result = stream.ProcessEvent(InputKeys.MouseRight, isDown: false);

		Assert.Multiple(() => {
			Assert.True(result.Switch(_ => false, () => true));
			Assert.False(task.IsCompletedSuccessfully);
		});
	}

	[Fact]
	public void NewActionChain() {
		var stream = new InputStream {
			activationKey = InputKeys.MouseRight,
			chainKey = InputKeys.ShiftLeft,
			activation = InputActivation.OnPress
		};

		var task = stream.NewAction();

		var result = stream.ProcessEvent(InputKeys.ShiftLeft, isDown: true);

		Assert.True(result.Switch(_ => false, () => true));

		result = stream.ProcessEvent(InputKeys.MouseRight, isDown: true);

		Assert.Multiple(async () => {
			Assert.True(result.Switch(_ => false, () => true));
			Assert.True(task.IsCompletedSuccessfully);
			var action = await task;
			Assert.Equal(InputAction.Chain, action.UnpackOr(InputAction.Run));
		});
	}

	[Fact]
	public void NewActionRunWhenChainKeyUp() {
		var stream = new InputStream {
			activationKey = InputKeys.MouseRight,
			chainKey = InputKeys.ShiftLeft,
			activation = InputActivation.OnPress
		};

		var task = stream.NewAction();

		var result = stream.ProcessEvent(InputKeys.ShiftLeft, isDown: false);

		Assert.True(result.Switch(_ => false, () => true));

		result = stream.ProcessEvent(InputKeys.MouseRight, isDown: true);

		Assert.Multiple(async () => {
			Assert.True(result.Switch(_ => false, () => true));
			Assert.True(task.IsCompletedSuccessfully);
			var action = await task;
			Assert.Equal(InputAction.Run, action.UnpackOr(InputAction.Chain));
		});

	}

	[Fact]
	public void NewTaskAfterProcessEvent() {
		var stream = new InputStream {
			activationKey = InputKeys.ShiftLeft,
			activation = InputActivation.OnPress
		};

		var taskA = stream.NewAction();

		var result = stream.ProcessEvent(InputKeys.ShiftLeft, isDown: true);

		Assert.True(result.Switch(_ => false, () => true));
		var taskB = stream.NewAction();

		Assert.NotSame(taskB, taskA);
	}
}
