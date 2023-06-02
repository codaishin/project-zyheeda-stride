namespace Tests;

using System.Threading.Tasks;
using NUnit.Framework;
using ProjectZyheeda;

public class TestInputStream {
	[Test]
	public async Task NewAction() {
		var stream = new InputStream {
			activationKey = InputKeys.ShiftLeft,
			activation = InputActivation.OnPress
		};

		var task = stream.NewAction();

		Assert.That(task.IsCompletedSuccessfully, Is.False);

		var result = stream.ProcessEvent(InputKeys.ShiftLeft, isDown: true);

		Assert.Multiple(() => {
			Assert.That(result.Switch(_ => false, () => true), Is.True);
			Assert.That(task.IsCompletedSuccessfully, Is.True);
		});

		var action = await task;

		Assert.That(action.UnpackOr(InputAction.Chain), Is.EqualTo(InputAction.Run));
	}

	[Test]
	public void NewActionCorrectKey() {
		var stream = new InputStream {
			activationKey = InputKeys.MouseRight,
			activation = InputActivation.OnPress
		};

		var task = stream.NewAction();

		var result = stream.ProcessEvent(InputKeys.ShiftLeft, isDown: true);

		Assert.Multiple(() => {
			Assert.That(result.Switch(_ => false, () => true), Is.True);
			Assert.That(task.IsCompletedSuccessfully, Is.False);
		});

		result = stream.ProcessEvent(InputKeys.MouseRight, isDown: true);

		Assert.Multiple(() => {
			Assert.That(result.Switch(_ => false, () => true), Is.True);
			Assert.That(task.IsCompletedSuccessfully, Is.True);
		});
	}

	[Test]
	public void NewActionCorrectAction() {
		var stream = new InputStream {
			activationKey = InputKeys.MouseRight,
			activation = InputActivation.OnRelease
		};

		var task = stream.NewAction();

		var result = stream.ProcessEvent(InputKeys.MouseRight, isDown: true);

		Assert.Multiple(() => {
			Assert.That(result.Switch(_ => false, () => true), Is.True);
			Assert.That(task.IsCompletedSuccessfully, Is.False);
		});

		stream.activation = InputActivation.OnPress;

		result = stream.ProcessEvent(InputKeys.MouseRight, isDown: false);

		Assert.Multiple(() => {
			Assert.That(result.Switch(_ => false, () => true), Is.True);
			Assert.That(task.IsCompletedSuccessfully, Is.False);
		});
	}

	[Test]
	public void NewActionChain() {
		var stream = new InputStream {
			activationKey = InputKeys.MouseRight,
			chainKey = InputKeys.ShiftLeft,
			activation = InputActivation.OnPress
		};

		var task = stream.NewAction();

		var result = stream.ProcessEvent(InputKeys.ShiftLeft, isDown: true);

		Assert.That(result.Switch(_ => false, () => true), Is.True);

		result = stream.ProcessEvent(InputKeys.MouseRight, isDown: true);

		Assert.Multiple(async () => {
			Assert.That(result.Switch(_ => false, () => true), Is.True);
			Assert.That(task.IsCompletedSuccessfully, Is.True);
			var action = await task;
			Assert.That(action.UnpackOr(InputAction.Run), Is.EqualTo(InputAction.Chain));
		});
	}

	[Test]
	public void NewActionRunWhenChainKeyUp() {
		var stream = new InputStream {
			activationKey = InputKeys.MouseRight,
			chainKey = InputKeys.ShiftLeft,
			activation = InputActivation.OnPress
		};

		var task = stream.NewAction();

		var result = stream.ProcessEvent(InputKeys.ShiftLeft, isDown: false);

		Assert.That(result.Switch(_ => false, () => true), Is.True);

		result = stream.ProcessEvent(InputKeys.MouseRight, isDown: true);

		Assert.Multiple(async () => {
			Assert.That(result.Switch(_ => false, () => true), Is.True);
			Assert.That(task.IsCompletedSuccessfully, Is.True);
			var action = await task;
			Assert.That(action.UnpackOr(InputAction.Chain), Is.EqualTo(InputAction.Run));
		});

	}

	[Test]
	public void NewTaskAfterProcessEvent() {
		var stream = new InputStream {
			activationKey = InputKeys.ShiftLeft,
			activation = InputActivation.OnPress
		};

		var taskA = stream.NewAction();

		var result = stream.ProcessEvent(InputKeys.ShiftLeft, isDown: true);

		Assert.That(result.Switch(_ => false, () => true), Is.True);
		var taskB = stream.NewAction();

		Assert.That(taskA, Is.Not.SameAs(taskB));
	}
}
