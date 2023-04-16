namespace Tests;

using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Engine;

public class SchedulerControllerTest : GameTestCollection {
	private SchedulerController schedulerController = new();

	[SetUp]
	public void InitSchedulerController() {
		this.schedulerController = new();
		this.scene.Entities.Add(new Entity { this.schedulerController });
	}

	[Test]
	public void IsIScheduler() {
		Assert.That(this.schedulerController, Is.InstanceOf<IScheduler>());
	}

	[Test]
	public void IsStartupScript() {
		Assert.That(this.schedulerController, Is.InstanceOf<StartupScript>());
	}

	[Test]
	public void EnqueueOneFunc() {
		var func = Mock.Of<Func<Task>>();

		this.schedulerController.Enqueue((func, () => { }));

		this.game.WaitFrames(1);

		Mock.Get(func).Verify(func => func(), Times.Once);
	}

	[Test]
	public void EnqueueTwoFunc() {
		var result = "";
		var funcA = async () => {
			result += "A";
			_ = await this.game.Script.NextFrame();
			result += "A";
			_ = await this.game.Script.NextFrame();
		};
		var funcB = async () => {
			result += "B";
			_ = await this.game.Script.NextFrame();
			result += "B";
			_ = await this.game.Script.NextFrame();
		};

		this.schedulerController.Enqueue((funcA, () => { }));
		this.schedulerController.Enqueue((funcB, () => { }));

		this.game.WaitFrames(1);
		Assert.That(result, Is.EqualTo("A"));

		this.game.WaitFrames(1);
		Assert.That(result, Is.EqualTo("AA"));

		this.game.WaitFrames(1);
		Assert.That(result, Is.EqualTo("AAB"));

		this.game.WaitFrames(1);
		Assert.That(result, Is.EqualTo("AABB"));
	}

	[Test]
	public void EnqueueAfterPreviousFinished() {
		var func = Mock.Of<Func<Task>>();
		_ = Mock.Get(func).Setup(func => func()).Returns(Task.CompletedTask);

		this.schedulerController.Enqueue((func, () => { }));

		this.game.WaitFrames(1);

		this.schedulerController.Enqueue((func, () => { }));

		this.game.WaitFrames(1);

		Mock.Get(func).Verify(func => func(), Times.Exactly(2));
	}

	[Test]
	public void RunsFunc() {
		var func = Mock.Of<Func<Task>>();

		this.schedulerController.Run((func, () => { }));

		this.game.WaitFrames(1);

		Mock.Get(func).Verify(func => func(), Times.Once);
	}

	[Test]
	public void RunAndEnqueueFunc() {
		var result = "";
		var funcA = async () => {
			result += "A";
			_ = await this.game.Script.NextFrame();
			result += "A";
			_ = await this.game.Script.NextFrame();
		};
		var funcB = async () => {
			result += "B";
			_ = await this.game.Script.NextFrame();
			result += "B";
			_ = await this.game.Script.NextFrame();
		};

		this.schedulerController.Run((funcA, () => { }));
		this.schedulerController.Enqueue((funcB, () => { }));

		this.game.WaitFrames(1);
		Assert.That(result, Is.EqualTo("A"));

		this.game.WaitFrames(1);
		Assert.That(result, Is.EqualTo("AA"));

		this.game.WaitFrames(1);
		Assert.That(result, Is.EqualTo("AAB"));

		this.game.WaitFrames(1);
		Assert.That(result, Is.EqualTo("AABB"));
	}

	[Test]
	public void RunAfterEnqueueShouldClear() {
		var result = "";
		var funcA = async () => {
			result += "A";
			_ = await this.game.Script.NextFrame();
		};
		var funcB = async () => {
			result += "B";
			_ = await this.game.Script.NextFrame();
		};

		this.schedulerController.Enqueue((funcA, () => { }));
		this.schedulerController.Run((funcB, () => { }));

		this.game.WaitFrames(1);
		Assert.That(result, Is.EqualTo("B"));
	}

	[Test]
	public void RunAfterEnqueueShouldCancel() {
		var result = "";
		var funcA = async () => {
			_ = await this.game.Script.NextFrame();
			result += "A";
		};
		var funcB = async () => {
			_ = await this.game.Script.NextFrame();
			result += "B";
		};

		this.schedulerController.Enqueue((funcA, () => { }));

		this.game.WaitFrames(1);

		this.schedulerController.Run((funcB, () => { }));

		this.game.WaitFrames(2);

		Assert.That(result, Is.EqualTo("B"));
	}

	[Test]
	public void EnqueueClearEnqueue() {
		var result = "";
		var funcA = async () => {
			result += "A";
			_ = await this.game.Script.NextFrame();
		};
		var funcB = async () => {
			result += "B";
			_ = await this.game.Script.NextFrame();
		};

		this.schedulerController.Enqueue((funcA, () => { }));
		this.schedulerController.Clear();
		this.schedulerController.Enqueue((funcB, () => { }));

		this.game.WaitFrames(1);
		Assert.That(result, Is.EqualTo("B"));
	}

	[Test]
	public void EnqueueClearEnqueueCancelFirstEnqueue() {
		var result = "";
		var funcA = async () => {
			_ = await this.game.Script.NextFrame();
			result += "A";
		};
		var funcB = async () => {
			_ = await this.game.Script.NextFrame();
			result += "B";
		};

		this.schedulerController.Enqueue((funcA, () => { }));

		this.game.WaitFrames(1);

		this.schedulerController.Clear();

		this.game.WaitFrames(1);

		this.schedulerController.Enqueue((funcB, () => { }));

		this.game.WaitFrames(2);

		Assert.That(result, Is.EqualTo("B"));
	}

	[Test]
	public void CallCancelOnClear() {
		async Task idle2Frames() {
			_ = await this.game.Script.NextFrame();
			_ = await this.game.Script.NextFrame();
		}
		var cancel = Mock.Of<Cancel>();

		this.schedulerController.Enqueue((idle2Frames, cancel));
		this.game.WaitFrames(1);

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);

		this.schedulerController.Clear();

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Once);
	}

	[Test]
	public void CallCancelOnRun() {
		async Task idle2Frames() {
			_ = await this.game.Script.NextFrame();
			_ = await this.game.Script.NextFrame();
		}
		var cancel = Mock.Of<Cancel>();

		this.schedulerController.Enqueue((idle2Frames, cancel));
		this.game.WaitFrames(1);

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);

		this.schedulerController.Run((idle2Frames, () => { }));

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Once);
	}

	[Test]
	public void CallCurrentCancel() {
		async Task idle2Frames() {
			_ = await this.game.Script.NextFrame();
			_ = await this.game.Script.NextFrame();
		}
		var cancelA = Mock.Of<Cancel>();
		var cancelB = Mock.Of<Cancel>();

		this.schedulerController.Enqueue((idle2Frames, cancelA));
		this.schedulerController.Enqueue((idle2Frames, cancelB));

		this.game.WaitFrames(1);

		this.schedulerController.Clear();

		Assert.Multiple(() => {
			Mock.Get(cancelA).Verify(cancel => cancel(), Times.Once);
			Mock.Get(cancelB).Verify(cancel => cancel(), Times.Never);
		});
	}

	[Test]
	public void CallCancelOnClearJustOnce() {
		async Task idle2Frames() {
			_ = await this.game.Script.NextFrame();
			_ = await this.game.Script.NextFrame();
		}
		var cancel = Mock.Of<Cancel>();

		this.schedulerController.Enqueue((idle2Frames, cancel));
		this.game.WaitFrames(1);

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);

		this.schedulerController.Clear();
		this.schedulerController.Clear();

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Once);
	}

	[Test]
	public void DoNotCallCancelAfterExecutionFinished() {
		static Task doNotWait() {
			return Task.CompletedTask;
		}
		var cancel = Mock.Of<Cancel>();

		this.schedulerController.Enqueue((doNotWait, cancel));

		this.game.WaitFrames(1);

		this.schedulerController.Clear();

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);
	}
}
