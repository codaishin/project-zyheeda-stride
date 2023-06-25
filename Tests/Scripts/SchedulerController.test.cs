namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using ProjectZyheeda;
using Stride.Engine;
using Stride.Engine.Processors;
using Xunit;

public class SchedulerControllerTest : GameTestCollection {
	private readonly SchedulerController schedulerController;

	public SchedulerControllerTest(GameFixture fixture) : base(fixture) {
		this.game.Services.RemoveService<ISystemMessage>();
		this.game.Services.AddService<ISystemMessage>(Mock.Of<ISystemMessage>());
		this.game.Services.RemoveService<IPlayerMessage>();
		this.game.Services.AddService<IPlayerMessage>(Mock.Of<IPlayerMessage>());

		this.schedulerController = new();
		this.scene.Entities.Add(new Entity { this.schedulerController });
	}

	[Fact]
	public void IsIScheduler() {
		_ = Assert.IsAssignableFrom<IScheduler>(this.schedulerController);
	}

	[Fact]
	public void IsStartupScript() {
		_ = Assert.IsAssignableFrom<StartupScript>(this.schedulerController);
	}

	[Fact]
	public void EnqueueOneFunc() {
		var func = Mock.Of<Func<IEnumerable<Result<IWait>>>>();

		var ok = this.schedulerController.Enqueue(func, () => Result.Ok()).Switch(
			_ => false,
			() => true
		);

		Assert.True(ok);

		this.game.WaitFrames(1);

		Mock.Get(func).Verify(func => func(), Times.Once);
	}

	[Fact]
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

		_ = this.schedulerController.Enqueue(funcA, () => Result.Ok());
		_ = this.schedulerController.Enqueue(funcB, () => Result.Ok());

		this.game.WaitFrames(1);
		Assert.Equal("A", result);

		this.game.WaitFrames(1);
		Assert.Equal("AA", result);

		this.game.WaitFrames(1);
		Assert.Equal("AAB", result);

		this.game.WaitFrames(1);
		Assert.Equal("AABB", result);
	}

	[Fact]
	public void EnqueueAfterPreviousFinished() {
		var func = Mock.Of<Func<IEnumerable<Result<IWait>>>>();
		_ = Mock.Get(func).Setup(func => func()).Returns(Array.Empty<Result<IWait>>());

		_ = this.schedulerController.Enqueue(func, () => Result.Ok());

		this.game.WaitFrames(1);

		_ = this.schedulerController.Enqueue(func, () => Result.Ok());

		this.game.WaitFrames(1);

		Mock.Get(func).Verify(func => func(), Times.Exactly(2));
	}

	[Fact]
	public void RunsFunc() {
		var func = Mock.Of<Func<IEnumerable<Result<IWait>>>>();
		_ = Mock.Get(func).Setup(func => func()).Returns(Array.Empty<Result<IWait>>());

		var ok = this.schedulerController.Run(func, () => Result.Ok()).Switch(
			_ => false,
			() => true
		);

		Assert.True(ok);

		this.game.WaitFrames(1);

		Mock.Get(func).Verify(func => func(), Times.Once);
	}

	[Fact]
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

		_ = this.schedulerController.Run(funcA, () => Result.Ok());
		_ = this.schedulerController.Enqueue(funcB, () => Result.Ok());

		this.game.WaitFrames(1);
		Assert.Equal("A", result);

		this.game.WaitFrames(1);
		Assert.Equal("AA", result);

		this.game.WaitFrames(1);
		Assert.Equal("AAB", result);

		this.game.WaitFrames(1);
		Assert.Equal("AABB", result);
	}

	[Fact]
	public void RunAndWaitSeconds() {
		var result = "";
		IEnumerable<Result<IWait>> func() {
			result += "A";
			yield return new WaitMilliSeconds(100);
			result += "A";
			yield return new WaitMilliSeconds(200);
		};


		_ = this.schedulerController.Run(func, () => Result.Ok());

		Thread.Sleep(100);
		Assert.Equal("A", result);

		Thread.Sleep(200);
		Assert.Equal("AA", result);
	}

	[Fact]
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

		_ = this.schedulerController.Enqueue(funcA, () => Result.Ok());
		_ = this.schedulerController.Run(funcB, () => Result.Ok());

		this.game.WaitFrames(1);
		Assert.Equal("B", result);
	}

	[Fact]
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

		_ = this.schedulerController.Enqueue(funcA, () => Result.Ok());

		this.game.WaitFrames(1);

		_ = this.schedulerController.Run(funcB, () => Result.Ok());

		this.game.WaitFrames(2);

		Assert.Equal("B", result);
	}

	[Fact]
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

		_ = this.schedulerController.Enqueue(funcA, () => Result.Ok());
		_ = this.schedulerController.Clear();
		_ = this.schedulerController.Enqueue(funcB, () => Result.Ok());

		this.game.WaitFrames(1);
		Assert.Equal("B", result);
	}

	[Fact]
	public void CLearOk() {
		var ok = this.schedulerController.Clear().Switch(
			_ => false,
			() => true
		);

		Assert.True(ok);
	}

	[Fact]
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

		_ = this.schedulerController.Enqueue(funcA, () => Result.Ok());

		this.game.WaitFrames(1);

		_ = this.schedulerController.Clear();

		this.game.WaitFrames(1);

		_ = this.schedulerController.Enqueue(funcB, () => Result.Ok());

		this.game.WaitFrames(2);

		Assert.Equal("B", result);
	}

	[Fact]
	public void LogRunErrors() {
		static IEnumerable<Result<IWait>> faultyRun() {
			yield return Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "BBB" }));
		}

		static Result cancel() {
			return Result.Ok();
		}

		_ = this.schedulerController.Enqueue(faultyRun, cancel);
		this.game.WaitFrames(10);

		Mock
			.Get(this.game.Services.GetService<ISystemMessage>())
			.Verify(s => s.Log("AAA"), Times.Once);
		Mock
			.Get(this.game.Services.GetService<IPlayerMessage>())
			.Verify(s => s.Log("BBB"), Times.Once);
	}

	[Fact]
	public void CallCancelOnClear() {
		static IEnumerable<Result<IWait>> idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}
		var cancel = Mock.Of<Cancel>();

		_ = this.schedulerController.Enqueue(idle2Frames, cancel);
		this.game.WaitFrames(1);

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);

		_ = this.schedulerController.Clear();

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Once);
	}

	[Fact]
	public void CallCancelOnRun() {
		static IEnumerable<Result<IWait>> idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}
		var cancel = Mock.Of<Cancel>();

		_ = this.schedulerController.Enqueue(idle2Frames, cancel);
		this.game.WaitFrames(1);

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);

		_ = this.schedulerController.Run(idle2Frames, () => Result.Ok());

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Once);
	}

	[Fact]
	public void CallCurrentCancel() {
		static IEnumerable<Result<IWait>> idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}
		var cancelA = Mock.Of<Cancel>();
		var cancelB = Mock.Of<Cancel>();

		_ = this.schedulerController.Enqueue(idle2Frames, cancelA);
		_ = this.schedulerController.Enqueue(idle2Frames, cancelB);

		this.game.WaitFrames(1);

		_ = this.schedulerController.Clear();

		Assert.Multiple(
			() => Mock.Get(cancelA).Verify(cancel => cancel(), Times.Once),
			() => Mock.Get(cancelB).Verify(cancel => cancel(), Times.Never)
		);
	}

	[Fact]
	public void CallCancelOnClearJustOnce() {
		static IEnumerable<Result<IWait>> idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}
		var cancel = Mock.Of<Cancel>();

		_ = this.schedulerController.Enqueue(idle2Frames, cancel);
		this.game.WaitFrames(1);

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);

		_ = this.schedulerController.Clear();
		_ = this.schedulerController.Clear();

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Once);
	}

	[Fact]
	public void ReturnClearErrors() {
		static IEnumerable<Result<IWait>> idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}

		static Result cancel() {
			return Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "BBB" }));
		}

		_ = this.schedulerController.Enqueue(idle2Frames, cancel);

		this.game.WaitFrames(1);

		var errors = this.schedulerController.Clear().Switch(
			errors => $"{(string)errors.system.FirstOrDefault()}, {(string)errors.player.FirstOrDefault()}",
			() => "no errors"
		);

		Assert.Equal("AAA, BBB", errors);
	}

	[Fact]
	public void ReturnClearErrorsOnRun() {
		static IEnumerable<Result<IWait>> idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}

		static Result cancel() {
			return Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "BBB" }));
		}

		_ = this.schedulerController.Enqueue(idle2Frames, cancel);

		this.game.WaitFrames(1);

		var errors = this.schedulerController.Run(idle2Frames, cancel).Switch(
			errors => $"{(string)errors.system.FirstOrDefault()}, {(string)errors.player.FirstOrDefault()}",
			() => "no errors"
		);

		Assert.Equal("AAA, BBB", errors);
	}

	[Fact]
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

		_ = this.schedulerController.Enqueue(countUp2Times, cancel);

		this.game.WaitFrames(1);

		_ = this.schedulerController.Run(countUp2Times, cancel);

		this.game.WaitFrames(2);

		Assert.True(count > 0);
	}

	[Fact]
	public void DoNotCallCancelAfterExecutionFinished() {
		static IEnumerable<Result<IWait>> doNotWait() {
			yield break;
		}
		var cancel = Mock.Of<Cancel>();

		_ = this.schedulerController.Enqueue(doNotWait, cancel);

		this.game.WaitFrames(1);

		_ = this.schedulerController.Clear();

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);
	}

	[Fact]
	public void LogWaitErrors() {
		var wait = Mock.Of<IWait>();
		var errors = (new SystemError[] { "AAA" }, new PlayerError[] { "LLL" });
		var token = new TaskCompletionSource<Result>();
		token.SetResult(Result.Errors(errors));

		_ = Mock
			.Get(wait)
			.Setup(w => w.Wait(It.IsAny<ScriptSystem>()))
			.Returns(token);

		IEnumerable<Result<IWait>> doNotWait() {
			yield return Result.Ok(wait);
		}

		var cancel = Mock.Of<Cancel>();

		_ = this.schedulerController.Run(doNotWait, cancel);

		this.game.WaitFrames(2);

		Mock
			.Get(this.game.Services.GetService<ISystemMessage>())
			.Verify(s => s.Log("AAA"), Times.Once);
		Mock
			.Get(this.game.Services.GetService<IPlayerMessage>())
			.Verify(s => s.Log("LLL"), Times.Once);
	}

	[Fact]
	public async Task CancelStopsAwaitingCurrentStep() {
		var result = "";

		IEnumerable<Result<IWait>> RunBar() {
			while (true) {
				result += "|";
				yield return new WaitMilliSeconds(100);
				result += "|";
			}
		}

		Result Cancel() {
			return Result.Ok();
		}

		_ = this.schedulerController.Run(RunBar, Cancel);

		await Task.Delay(100);

		_ = this.schedulerController.Clear();

		await Task.Delay(100);

		Assert.Equal("|", result);
	}

	[Fact]
	public void ClearOnlyClearsQueueWhenActiveHasNoCancel() {
		var result = "";

		IEnumerable<Result<IWait>> RunA() {
			result += "a";
			yield return new WaitFrame();
			result += "a";
		}

		IEnumerable<Result<IWait>> RunB() {
			result += "b";
			yield return new WaitFrame();
			result += "b";
		}

		IEnumerable<Result<IWait>> RunC() {
			result += "c";
			yield return new WaitFrame();
			result += "c";
		}

		Result Cancel() {
			return Result.Ok();
		}

		_ = this.schedulerController.Run(RunA);

		this.game.WaitFrames(1);

		_ = this.schedulerController.Run(RunB, Cancel);
		_ = this.schedulerController.Run(RunC, Cancel);

		this.game.WaitFrames(5);

		Assert.Equal("aacc", result);
	}
}
