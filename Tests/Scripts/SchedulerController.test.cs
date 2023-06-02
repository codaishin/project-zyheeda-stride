namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Engine;
using Stride.Engine.Processors;

public class SchedulerControllerTest : GameTestCollection {
	private SchedulerController schedulerController = new();

	[SetUp]
	public void InitSchedulerController() {
		this.game.Services.RemoveService<ISystemMessage>();
		this.game.Services.AddService<ISystemMessage>(Mock.Of<ISystemMessage>());
		this.game.Services.RemoveService<IPlayerMessage>();
		this.game.Services.AddService<IPlayerMessage>(Mock.Of<IPlayerMessage>());

		this.schedulerController = new();
		this.Scene.Entities.Add(new Entity { this.schedulerController });
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
		var func = Mock.Of<Func<IEnumerable<Result<IWait>>>>();

		var ok = this.schedulerController.Enqueue((func, () => Result.Ok())).Switch(
			_ => false,
			() => true
		);

		Assert.That(ok, Is.True);

		this.game.WaitFrames(1);

		Mock.Get(func).Verify(func => func(), Times.Once);
	}

	[Test]
	public void EnqueueTwoFunc() {
		var result = "";
		IEnumerable<Result<IWait>> funcA() {
			result += "A";
			yield return new WaitFrame();
			result += "A";
			yield return new WaitFrame();
		};
		IEnumerable<Result<IWait>> funcB() {
			result += "B";
			yield return new WaitFrame();
			result += "B";
			yield return new WaitFrame();
		};

		_ = this.schedulerController.Enqueue((funcA, () => Result.Ok()));
		_ = this.schedulerController.Enqueue((funcB, () => Result.Ok()));

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
		var func = Mock.Of<Func<IEnumerable<Result<IWait>>>>();
		_ = Mock.Get(func).Setup(func => func()).Returns(Array.Empty<Result<IWait>>());

		_ = this.schedulerController.Enqueue((func, () => Result.Ok()));

		this.game.WaitFrames(1);

		_ = this.schedulerController.Enqueue((func, () => Result.Ok()));

		this.game.WaitFrames(1);

		Mock.Get(func).Verify(func => func(), Times.Exactly(2));
	}

	[Test]
	public void RunsFunc() {
		var func = Mock.Of<Func<IEnumerable<Result<IWait>>>>();
		_ = Mock.Get(func).Setup(func => func()).Returns(Array.Empty<Result<IWait>>());

		var ok = this.schedulerController.Run((func, () => Result.Ok())).Switch(
			_ => false,
			() => true
		);

		Assert.That(ok, Is.True);

		this.game.WaitFrames(1);

		Mock.Get(func).Verify(func => func(), Times.Once);
	}

	[Test]
	public void RunAndEnqueueFunc() {
		var result = "";
		IEnumerable<Result<IWait>> funcA() {
			result += "A";
			yield return new WaitFrame();
			result += "A";
			yield return new WaitFrame();
		};
		IEnumerable<Result<IWait>> funcB() {
			result += "B";
			yield return new WaitFrame();
			result += "B";
			yield return new WaitFrame();
		};

		_ = this.schedulerController.Run((funcA, () => Result.Ok()));
		_ = this.schedulerController.Enqueue((funcB, () => Result.Ok()));

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
		IEnumerable<Result<IWait>> func() {
			result += "A";
			yield return new WaitMilliSeconds(100);
			result += "A";
			yield return new WaitMilliSeconds(200);
		};


		_ = this.schedulerController.Run((func, () => Result.Ok()));

		Thread.Sleep(100);
		Assert.That(result, Is.EqualTo("A"));

		Thread.Sleep(200);
		Assert.That(result, Is.EqualTo("AA"));
	}

	[Test]
	public void RunAfterEnqueueShouldClear() {
		var result = "";
		IEnumerable<Result<IWait>> funcA() {
			result += "A";
			yield return new WaitFrame();
		};
		IEnumerable<Result<IWait>> funcB() {
			result += "B";
			yield return new WaitFrame();
		};

		_ = this.schedulerController.Enqueue((funcA, () => Result.Ok()));
		_ = this.schedulerController.Run((funcB, () => Result.Ok()));

		this.game.WaitFrames(1);
		Assert.That(result, Is.EqualTo("B"));
	}

	[Test]
	public void RunAfterEnqueueShouldCancel() {
		var result = "";
		IEnumerable<Result<IWait>> funcA() {
			yield return new WaitFrame();
			result += "A";
		};
		IEnumerable<Result<IWait>> funcB() {
			yield return new WaitFrame();
			result += "B";
		};

		_ = this.schedulerController.Enqueue((funcA, () => Result.Ok()));

		this.game.WaitFrames(1);

		_ = this.schedulerController.Run((funcB, () => Result.Ok()));

		this.game.WaitFrames(2);

		Assert.That(result, Is.EqualTo("B"));
	}

	[Test]
	public void EnqueueClearEnqueue() {
		var result = "";
		IEnumerable<Result<IWait>> funcA() {
			result += "A";
			yield return new WaitFrame();
		};
		IEnumerable<Result<IWait>> funcB() {
			result += "B";
			yield return new WaitFrame();
		};

		_ = this.schedulerController.Enqueue((funcA, () => Result.Ok()));
		_ = this.schedulerController.Clear();
		_ = this.schedulerController.Enqueue((funcB, () => Result.Ok()));

		this.game.WaitFrames(1);
		Assert.That(result, Is.EqualTo("B"));
	}

	[Test]
	public void CLearOk() {
		var ok = this.schedulerController.Clear().Switch(
			_ => false,
			() => true
		);

		Assert.That(ok, Is.True);
	}

	[Test]
	public void EnqueueClearEnqueueCancelFirstEnqueue() {
		var result = "";
		IEnumerable<Result<IWait>> funcA() {
			yield return new WaitFrame();
			result += "A";
		};
		IEnumerable<Result<IWait>> funcB() {
			yield return new WaitFrame();
			result += "B";
		};

		_ = this.schedulerController.Enqueue((funcA, () => Result.Ok()));

		this.game.WaitFrames(1);

		_ = this.schedulerController.Clear();

		this.game.WaitFrames(1);

		_ = this.schedulerController.Enqueue((funcB, () => Result.Ok()));

		this.game.WaitFrames(2);

		Assert.That(result, Is.EqualTo("B"));
	}

	[Test]
	public void LogRunErrors() {
		static IEnumerable<Result<IWait>> faultyRun() {
			yield return Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "BBB" }));
		}

		static Result cancel() {
			return Result.Ok();
		}

		_ = this.schedulerController.Enqueue((faultyRun, cancel));
		this.game.WaitFrames(10);

		Mock
			.Get(this.game.Services.GetService<ISystemMessage>())
			.Verify(s => s.Log("AAA"), Times.Once);
		Mock
			.Get(this.game.Services.GetService<IPlayerMessage>())
			.Verify(s => s.Log("BBB"), Times.Once);
	}

	[Test]
	public void CallCancelOnClear() {
		static IEnumerable<Result<IWait>> idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}
		var cancel = Mock.Of<Cancel>();

		_ = this.schedulerController.Enqueue((idle2Frames, cancel));
		this.game.WaitFrames(1);

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);

		_ = this.schedulerController.Clear();

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Once);
	}

	[Test]
	public void CallCancelOnRun() {
		static IEnumerable<Result<IWait>> idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}
		var cancel = Mock.Of<Cancel>();

		_ = this.schedulerController.Enqueue((idle2Frames, cancel));
		this.game.WaitFrames(1);

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);

		_ = this.schedulerController.Run((idle2Frames, () => Result.Ok()));

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Once);
	}

	[Test]
	public void CallCurrentCancel() {
		static IEnumerable<Result<IWait>> idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}
		var cancelA = Mock.Of<Cancel>();
		var cancelB = Mock.Of<Cancel>();

		_ = this.schedulerController.Enqueue((idle2Frames, cancelA));
		_ = this.schedulerController.Enqueue((idle2Frames, cancelB));

		this.game.WaitFrames(1);

		_ = this.schedulerController.Clear();

		Assert.Multiple(() => {
			Mock.Get(cancelA).Verify(cancel => cancel(), Times.Once);
			Mock.Get(cancelB).Verify(cancel => cancel(), Times.Never);
		});
	}

	[Test]
	public void CallCancelOnClearJustOnce() {
		static IEnumerable<Result<IWait>> idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}
		var cancel = Mock.Of<Cancel>();

		_ = this.schedulerController.Enqueue((idle2Frames, cancel));
		this.game.WaitFrames(1);

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);

		_ = this.schedulerController.Clear();
		_ = this.schedulerController.Clear();

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Once);
	}

	[Test]
	public void ReturnClearErrors() {
		static IEnumerable<Result<IWait>> idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}

		static Result cancel() {
			return Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "BBB" }));
		}

		_ = this.schedulerController.Enqueue((idle2Frames, cancel));

		this.game.WaitFrames(1);

		var errors = this.schedulerController.Clear().Switch(
			errors => $"{(string)errors.system.FirstOrDefault()}, {(string)errors.player.FirstOrDefault()}",
			() => "no errors"
		);

		Assert.That(errors, Is.EqualTo("AAA, BBB"));
	}

	[Test]
	public void ReturnClearErrorsOnRun() {
		static IEnumerable<Result<IWait>> idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}

		static Result cancel() {
			return Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "BBB" }));
		}

		_ = this.schedulerController.Enqueue((idle2Frames, cancel));

		this.game.WaitFrames(1);

		var errors = this.schedulerController.Run((idle2Frames, cancel)).Switch(
			errors => $"{(string)errors.system.FirstOrDefault()}, {(string)errors.player.FirstOrDefault()}",
			() => "no errors"
		);

		Assert.That(errors, Is.EqualTo("AAA, BBB"));
	}

	[Test]
	public void EnqueueOnRunEvenIfClearHadErrors() {
		var count = 0;

		IEnumerable<Result<IWait>> countUp2Times() {
			yield return new WaitFrame();
			++count;
			yield return new WaitFrame();
			++count;
		}

		static Result cancel() {
			return Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "BBB" }));
		}

		_ = this.schedulerController.Enqueue((countUp2Times, cancel));

		this.game.WaitFrames(1);

		_ = this.schedulerController.Run((countUp2Times, cancel));

		this.game.WaitFrames(2);

		Assert.That(count, Is.GreaterThan(0));
	}

	[Test]
	public void DoNotCallCancelAfterExecutionFinished() {
		static IEnumerable<Result<IWait>> doNotWait() {
			yield break;
		}
		var cancel = Mock.Of<Cancel>();

		_ = this.schedulerController.Enqueue((doNotWait, cancel));

		this.game.WaitFrames(1);

		_ = this.schedulerController.Clear();

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);
	}

	[Test]
	public void LogWaitErrors() {
		var wait = Mock.Of<IWait>();
		var errors = (new SystemError[] { "AAA" }, new PlayerError[] { "LLL" });

		_ = Mock
			.Get(wait)
			.Setup(w => w.Wait(It.IsAny<ScriptSystem>()))
			.Returns(Task.FromResult((Result)Result.Errors(errors)));

		IEnumerable<Result<IWait>> doNotWait() {
			yield return Result.Ok(wait);
		}

		var cancel = Mock.Of<Cancel>();

		_ = this.schedulerController.Run((doNotWait, cancel));

		this.game.WaitFrames(2);

		Mock
			.Get(this.game.Services.GetService<ISystemMessage>())
			.Verify(s => s.Log("AAA"), Times.Once);
		Mock
			.Get(this.game.Services.GetService<IPlayerMessage>())
			.Verify(s => s.Log("LLL"), Times.Once);
	}
}
