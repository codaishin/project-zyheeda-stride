namespace Tests;

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
	public async void EnqueueOneCoroutine() {
		var count = 0;

		IEnumerable<Result<IWait>> Coroutine() {
			yield return new WaitFrame();
			++count;
			yield return new WaitFrame();
			++count;
		}

		var ok = this.schedulerController.Enqueue(Coroutine(), () => Result.Ok()).Switch(
			_ => false,
			() => true
		);
		await this.game.Frames(1);

		Assert.Multiple(
			() => Assert.True(ok),
			async () => {
				await this.game.Frames(1);
				Assert.Equal(1, count);
			},
			async () => {
				await this.game.Frames(1);
				Assert.Equal(2, count);
			}
		);
	}

	[Fact]
	public async void EnqueueTwoCoroutines() {
		var result = "";
		IEnumerable<Result<IWait>> CoroutineA() {
			result += "A";
			yield return new WaitFrame();
			result += "A";
			yield return new WaitFrame();
		};
		IEnumerable<Result<IWait>> CoroutineB() {
			result += "B";
			yield return new WaitFrame();
			result += "B";
			yield return new WaitFrame();
		};

		_ = this.schedulerController.Enqueue(CoroutineA(), () => Result.Ok());
		_ = this.schedulerController.Enqueue(CoroutineB(), () => Result.Ok());

		await this.game.Frames(1);
		Assert.Equal("A", result);

		await this.game.Frames(1);
		Assert.Equal("AA", result);

		await this.game.Frames(1);
		Assert.Equal("AAB", result);

		await this.game.Frames(1);
		Assert.Equal("AABB", result);
	}

	[Fact]
	public async void RunsCoroutine() {
		var count = 0;

		IEnumerable<Result<IWait>> Coroutine() {
			yield return new WaitFrame();
			++count;
			yield return new WaitFrame();
			++count;
		}

		var ok = this.schedulerController.Run(Coroutine(), () => Result.Ok()).Switch(
			_ => false,
			() => true
		);
		await this.game.Frames(1);

		Assert.Multiple(
			() => Assert.True(ok),
			async () => {
				await this.game.Frames(1);
				Assert.Equal(1, count);
			},
			async () => {
				await this.game.Frames(1);
				Assert.Equal(2, count);
			}
		);
	}

	[Fact]
	public async void RunAndEnqueueCoroutine() {
		var result = "";
		IEnumerable<Result<IWait>> CoroutineA() {
			result += "A";
			yield return new WaitFrame();
			result += "A";
			yield return new WaitFrame();
		};
		IEnumerable<Result<IWait>> CoroutineB() {
			result += "B";
			yield return new WaitFrame();
			result += "B";
			yield return new WaitFrame();
		};

		_ = this.schedulerController.Run(CoroutineA(), () => Result.Ok());
		_ = this.schedulerController.Enqueue(CoroutineB(), () => Result.Ok());

		await this.game.Frames(1);
		Assert.Equal("A", result);

		await this.game.Frames(1);
		Assert.Equal("AA", result);

		await this.game.Frames(1);
		Assert.Equal("AAB", result);

		await this.game.Frames(1);
		Assert.Equal("AABB", result);
	}

	[Fact]
	public void RunAndWaitSeconds() {
		var result = "";
		IEnumerable<Result<IWait>> Coroutine() {
			result += "A";
			yield return new WaitMilliSeconds(100);
			result += "A";
			yield return new WaitMilliSeconds(200);
		};


		_ = this.schedulerController.Run(Coroutine(), () => Result.Ok());

		Thread.Sleep(100);
		Assert.Equal("A", result);

		Thread.Sleep(200);
		Assert.Equal("AA", result);
	}

	[Fact]
	public async void RunAfterEnqueueShouldClear() {
		var result = "";
		IEnumerable<Result<IWait>> CoroutineA() {
			result += "A";
			yield return new WaitFrame();
		};
		IEnumerable<Result<IWait>> CoroutineB() {
			result += "B";
			yield return new WaitFrame();
		};

		_ = this.schedulerController.Enqueue(CoroutineA(), () => Result.Ok());
		_ = this.schedulerController.Run(CoroutineB(), () => Result.Ok());

		await this.game.Frames(1);
		Assert.Equal("B", result);
	}

	[Fact]
	public async void RunAfterEnqueueShouldCancel() {
		var result = "";
		IEnumerable<Result<IWait>> CoroutineA() {
			yield return new WaitFrame();
			result += "A";
		};
		IEnumerable<Result<IWait>> CoroutineB() {
			yield return new WaitFrame();
			result += "B";
		};

		_ = this.schedulerController.Enqueue(CoroutineA(), () => Result.Ok());

		await this.game.Frames(1);

		_ = this.schedulerController.Run(CoroutineB(), () => Result.Ok());

		await this.game.Frames(2);

		Assert.Equal("B", result);
	}

	[Fact]
	public async void EnqueueClearEnqueue() {
		var result = "";
		IEnumerable<Result<IWait>> CoroutineA() {
			result += "A";
			yield return new WaitFrame();
		};
		IEnumerable<Result<IWait>> CoroutineB() {
			result += "B";
			yield return new WaitFrame();
		};

		_ = this.schedulerController.Enqueue(CoroutineA(), () => Result.Ok());
		_ = this.schedulerController.Clear();
		_ = this.schedulerController.Enqueue(CoroutineB(), () => Result.Ok());

		await this.game.Frames(1);
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
	public async void EnqueueClearEnqueueCancelFirstEnqueue() {
		var result = "";
		IEnumerable<Result<IWait>> CoroutineA() {
			yield return new WaitFrame();
			result += "A";
		};
		IEnumerable<Result<IWait>> CoroutineB() {
			yield return new WaitFrame();
			result += "B";
		};

		_ = this.schedulerController.Enqueue(CoroutineA(), () => Result.Ok());

		await this.game.Frames(1);

		_ = this.schedulerController.Clear();

		await this.game.Frames(1);

		_ = this.schedulerController.Enqueue(CoroutineB(), () => Result.Ok());

		await this.game.Frames(2);

		Assert.Equal("B", result);
	}

	[Fact]
	public async void LogRunErrors() {
		static IEnumerable<Result<IWait>> FaultyRun() {
			yield return Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "BBB" }));
		}

		static Result cancel() {
			return Result.Ok();
		}

		_ = this.schedulerController.Enqueue(FaultyRun(), cancel);
		await this.game.Frames(10);

		Mock
			.Get(this.game.Services.GetService<ISystemMessage>())
			.Verify(s => s.Log("AAA"), Times.Once);
		Mock
			.Get(this.game.Services.GetService<IPlayerMessage>())
			.Verify(s => s.Log("BBB"), Times.Once);
	}

	[Fact]
	public async void CallCancelOnClear() {
		static IEnumerable<Result<IWait>> Idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}
		var cancel = Mock.Of<Cancel>();

		_ = this.schedulerController.Enqueue(Idle2Frames(), cancel);
		await this.game.Frames(1);

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);

		_ = this.schedulerController.Clear();

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Once);
	}

	[Fact]
	public async void CallCancelOnRun() {
		static IEnumerable<Result<IWait>> Idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}
		var cancel = Mock.Of<Cancel>();

		_ = this.schedulerController.Enqueue(Idle2Frames(), cancel);
		await this.game.Frames(1);

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);

		_ = this.schedulerController.Run(Idle2Frames(), () => Result.Ok());

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Once);
	}

	[Fact]
	public async void CallCurrentCancel() {
		static IEnumerable<Result<IWait>> Idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}
		var cancelA = Mock.Of<Cancel>();
		var cancelB = Mock.Of<Cancel>();

		_ = this.schedulerController.Enqueue(Idle2Frames(), cancelA);
		_ = this.schedulerController.Enqueue(Idle2Frames(), cancelB);

		await this.game.Frames(1);

		_ = this.schedulerController.Clear();

		Assert.Multiple(
			() => Mock.Get(cancelA).Verify(cancel => cancel(), Times.Once),
			() => Mock.Get(cancelB).Verify(cancel => cancel(), Times.Never)
		);
	}

	[Fact]
	public async void CallCancelOnClearJustOnce() {
		static IEnumerable<Result<IWait>> Idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}
		var cancel = Mock.Of<Cancel>();

		_ = this.schedulerController.Enqueue(Idle2Frames(), cancel);
		await this.game.Frames(1);

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);

		_ = this.schedulerController.Clear();
		_ = this.schedulerController.Clear();

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Once);
	}

	[Fact]
	public async void ReturnClearErrors() {
		static IEnumerable<Result<IWait>> Idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}

		static Result cancel() {
			return Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "BBB" }));
		}

		_ = this.schedulerController.Enqueue(Idle2Frames(), cancel);

		await this.game.Frames(1);

		var errors = this.schedulerController.Clear().Switch(
			errors => $"{(string)errors.system.FirstOrDefault()}, {(string)errors.player.FirstOrDefault()}",
			() => "no errors"
		);

		Assert.Equal("AAA, BBB", errors);
	}

	[Fact]
	public async void ReturnClearErrorsOnRun() {
		static IEnumerable<Result<IWait>> Idle2Frames() {
			yield return new WaitFrame();
			yield return new WaitFrame();
		}

		static Result cancel() {
			return Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "BBB" }));
		}

		_ = this.schedulerController.Enqueue(Idle2Frames(), cancel);

		await this.game.Frames(1);

		var errors = this.schedulerController.Run(Idle2Frames(), cancel).Switch(
			errors => $"{(string)errors.system.FirstOrDefault()}, {(string)errors.player.FirstOrDefault()}",
			() => "no errors"
		);

		Assert.Equal("AAA, BBB", errors);
	}

	[Fact]
	public async void EnqueueOnRunEvenIfClearHadErrors() {
		var count = 0;

		IEnumerable<Result<IWait>> CountUp2Times() {
			yield return new WaitFrame();
			++count;
			yield return new WaitFrame();
			++count;
		}

		static Result cancel() {
			return Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "BBB" }));
		}

		_ = this.schedulerController.Enqueue(CountUp2Times(), cancel);

		await this.game.Frames(1);

		_ = this.schedulerController.Run(CountUp2Times(), cancel);

		await this.game.Frames(2);

		Assert.True(count > 0);
	}

	[Fact]
	public async void DoNotCallCancelAfterExecutionFinished() {
		static IEnumerable<Result<IWait>> DoNotWait() {
			yield break;
		}
		var cancel = Mock.Of<Cancel>();

		_ = this.schedulerController.Enqueue(DoNotWait(), cancel);

		await this.game.Frames(1);

		_ = this.schedulerController.Clear();

		Mock.Get(cancel).Verify(cancel => cancel(), Times.Never);
	}

	[Fact]
	public async void LogWaitErrors() {
		var wait = Mock.Of<IWait>();
		var errors = (new SystemError[] { "AAA" }, new PlayerError[] { "LLL" });
		var token = new TaskCompletionSource<Result>();
		token.SetResult(Result.Errors(errors));

		_ = Mock
			.Get(wait)
			.Setup(w => w.Wait(It.IsAny<ScriptSystem>()))
			.Returns(token);

		IEnumerable<Result<IWait>> DoNotWait() {
			yield return Result.Ok(wait);
		}

		var cancel = Mock.Of<Cancel>();

		_ = this.schedulerController.Run(DoNotWait(), cancel);

		await this.game.Frames(2);

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

		IEnumerable<Result<IWait>> Coroutine() {
			while (true) {
				result += "|";
				yield return new WaitMilliSeconds(100);
				result += "|";
			}
		}

		Result Cancel() {
			return Result.Ok();
		}

		_ = this.schedulerController.Run(Coroutine(), Cancel);

		await Task.Delay(100);

		_ = this.schedulerController.Clear();

		await Task.Delay(100);

		Assert.Equal("|", result);
	}

	[Fact]
	public async void ClearOnlyClearsQueueWhenActiveHasNoCancel() {
		var result = "";

		IEnumerable<Result<IWait>> CoroutineA() {
			result += "a";
			yield return new WaitFrame();
			result += "a";
		}

		IEnumerable<Result<IWait>> CoroutineB() {
			result += "b";
			yield return new WaitFrame();
			result += "b";
		}

		IEnumerable<Result<IWait>> CoroutineC() {
			result += "c";
			yield return new WaitFrame();
			result += "c";
		}

		Result Cancel() {
			return Result.Ok();
		}

		_ = this.schedulerController.Run(CoroutineA());

		await this.game.Frames(1);

		_ = this.schedulerController.Run(CoroutineB(), Cancel);
		_ = this.schedulerController.Run(CoroutineC(), Cancel);

		await this.game.Frames(5);

		Assert.Equal("aacc", result);
	}
}
