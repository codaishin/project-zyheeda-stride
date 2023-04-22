namespace Tests;

using System;
using System.Collections.Generic;
using System.Threading;
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
		var func = Mock.Of<Func<IEnumerable<IWait>>>();

		this.schedulerController.Enqueue((func, () => { }));

		this.game.WaitFrames(1);

		Mock.Get(func).Verify(func => func(), Times.Once);
	}

	[Test]
	public void EnqueueTwoFunc() {
		var result = "";
		IEnumerable<IWait> funcA() {
			result += "A";
			yield return new WaitFrame();
			result += "A";
			yield return new WaitFrame();
		};
		IEnumerable<IWait> funcB() {
			result += "B";
			yield return new WaitFrame();
			result += "B";
			yield return new WaitFrame();
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
		var func = Mock.Of<Func<IEnumerable<IWait>>>();
		_ = Mock.Get(func).Setup(func => func()).Returns(Array.Empty<IWait>());

		this.schedulerController.Enqueue((func, () => { }));

		this.game.WaitFrames(1);

		this.schedulerController.Enqueue((func, () => { }));

		this.game.WaitFrames(1);

		Mock.Get(func).Verify(func => func(), Times.Exactly(2));
	}

	[Test]
	public void RunsFunc() {
		var func = Mock.Of<Func<IEnumerable<IWait>>>();
		_ = Mock.Get(func).Setup(func => func()).Returns(Array.Empty<IWait>());

		this.schedulerController.Run((func, () => { }));

		this.game.WaitFrames(1);

		Mock.Get(func).Verify(func => func(), Times.Once);
	}

	[Test]
	public void RunAndEnqueueFunc() {
		var result = "";
		IEnumerable<IWait> funcA() {
			result += "A";
			yield return new WaitFrame();
			result += "A";
			yield return new WaitFrame();
		};
		IEnumerable<IWait> funcB() {
			result += "B";
			yield return new WaitFrame();
			result += "B";
			yield return new WaitFrame();
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
	public void RunAndWaitSeconds() {
		var result = "";
		IEnumerable<IWait> func() {
			result += "A";
			yield return new WaitMilliSeconds(100);
			result += "A";
			yield return new WaitMilliSeconds(200);
		};


		this.schedulerController.Run((func, () => { }));

		Thread.Sleep(100);
		Assert.That(result, Is.EqualTo("A"));

		Thread.Sleep(200);
		Assert.That(result, Is.EqualTo("AA"));
	}

	[Test]
	public void RunAfterEnqueueShouldClear() {
		var result = "";
		IEnumerable<IWait> funcA() {
			result += "A";
			yield return new WaitFrame();
		};
		IEnumerable<IWait> funcB() {
			result += "B";
			yield return new WaitFrame();
		};

		this.schedulerController.Enqueue((funcA, () => { }));
		this.schedulerController.Run((funcB, () => { }));

		this.game.WaitFrames(1);
		Assert.That(result, Is.EqualTo("B"));
	}

	[Test]
	public void RunAfterEnqueueShouldCancel() {
		var result = "";
		IEnumerable<IWait> funcA() {
			yield return new WaitFrame();
			result += "A";
		};
		IEnumerable<IWait> funcB() {
			yield return new WaitFrame();
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
		IEnumerable<IWait> funcA() {
			result += "A";
			yield return new WaitFrame();
		};
		IEnumerable<IWait> funcB() {
			result += "B";
			yield return new WaitFrame();
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
		IEnumerable<IWait> funcA() {
			yield return new WaitFrame();
			result += "A";
		};
		IEnumerable<IWait> funcB() {
			yield return new WaitFrame();
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
		static IEnumerable<IWait> idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}
		var cancel = Mock.Of<Action>();

		this.schedulerController.Enqueue((idle2Frames, cancel));
		this.game.WaitFrames(1);

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);

		this.schedulerController.Clear();

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Once);
	}

	[Test]
	public void CallCancelOnRun() {
		static IEnumerable<IWait> idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}
		var cancel = Mock.Of<Action>();

		this.schedulerController.Enqueue((idle2Frames, cancel));
		this.game.WaitFrames(1);

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);

		this.schedulerController.Run((idle2Frames, () => { }));

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Once);
	}

	[Test]
	public void CallCurrentCancel() {
		static IEnumerable<IWait> idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}
		var cancelA = Mock.Of<Action>();
		var cancelB = Mock.Of<Action>();

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
		static IEnumerable<IWait> idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}
		var cancel = Mock.Of<Action>();

		this.schedulerController.Enqueue((idle2Frames, cancel));
		this.game.WaitFrames(1);

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);

		this.schedulerController.Clear();
		this.schedulerController.Clear();

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Once);
	}

	[Test]
	public void DoNotCallCancelAfterExecutionFinished() {
		static IEnumerable<IWait> doNotWait() {
			yield break;
		}
		var cancel = Mock.Of<Action>();

		this.schedulerController.Enqueue((doNotWait, cancel));

		this.game.WaitFrames(1);

		this.schedulerController.Clear();

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);
	}
}
