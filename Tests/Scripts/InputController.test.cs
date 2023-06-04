namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

public class TestInputController : GameTestCollection {
	private readonly InputController controller;
	private readonly IInputStream inputStream;
	private readonly IBehavior behavior;
	private readonly IGetTarget getTarget;
	private readonly IScheduler scheduler;
	private readonly List<TaskCompletionSource<Result<InputAction>>> newActionFnTaskTokens;
	private readonly ISystemMessage systemMessage;
	private readonly IPlayerMessage playerMessage;
	private readonly IInputDispatcher dispatcher;

	public TestInputController(GameFixture fixture) : base(fixture) {
		var newActionCallCount = 0;
		this.controller = new InputController {
			input = this.inputStream = Mock.Of<IInputStream>(),
			behavior = Maybe.Some(this.behavior = Mock.Of<IBehavior>()),
			getTarget = Maybe.Some(this.getTarget = Mock.Of<IGetTarget>()),
			scheduler = Maybe.Some(this.scheduler = Mock.Of<IScheduler>())
		};

		this.newActionFnTaskTokens = new List<TaskCompletionSource<Result<InputAction>>> {
			new TaskCompletionSource<Result<InputAction>>(),
			new TaskCompletionSource<Result<InputAction>>(),
			new TaskCompletionSource<Result<InputAction>>(),
			new TaskCompletionSource<Result<InputAction>>(),
			new TaskCompletionSource<Result<InputAction>>(),
		};

		_ = Mock
			.Get(this.controller.input)
			.Setup(i => i.NewAction())
			.Returns(() => this.newActionFnTaskTokens[newActionCallCount++].Task);

		_ = Mock
			.Get(this.getTarget)
			.Setup(c => c.GetTarget())
			.Returns(Result.Ok(() => Vector3.One));

		this.game.Services.RemoveService<ISystemMessage>();
		this.game.Services.AddService(this.systemMessage = Mock.Of<ISystemMessage>());
		this.game.Services.RemoveService<IPlayerMessage>();
		this.game.Services.AddService(this.playerMessage = Mock.Of<IPlayerMessage>());
		this.game.Services.RemoveService<IInputDispatcher>();
		this.game.Services.AddService(this.dispatcher = Mock.Of<IInputDispatcher>());

		this.scene.Entities.Add(new Entity { this.controller });

		this.game.WaitFrames(2);
	}

	[Fact]
	public void AddInputStreamToDispatcher() {
		Mock
			.Get(this.game.Services.GetService<IInputDispatcher>())
			.Verify(d => d.Add(this.inputStream), Times.Once);
	}

	[Fact]
	public void RemoveInputStreamFromDispatcher() {
		_ = this.scene.Entities.Remove(this.controller.Entity);

		this.game.WaitFrames(1);

		this.scene.Entities.Add(this.controller.Entity);

		this.game.WaitFrames(1);

		_ = this.scene.Entities.Remove(this.controller.Entity);

		this.game.WaitFrames(1);

		Mock
			.Get(this.game.Services.GetService<IInputDispatcher>())
			.Verify(d => d.Remove(this.inputStream), Times.Exactly(2));
	}

	[Fact]
	public void RunBehaviorWithTarget() {
		static IEnumerable<Result<IWait>> run() {
			yield break;
		}

		(Func<IEnumerable<Result<IWait>>>, Cancel) execution = (run, () => Result.Ok());

		_ = Mock.Get(this.getTarget)
			.Setup(c => c.GetTarget())
			.Returns(Result.Ok(() => new Vector3(1, 2, 3)));
		_ = Mock.Get(this.behavior)
			.Setup(c => c.GetCoroutine(It.IsAny<Func<Vector3>>()))
			.Returns((Func<Vector3> getTarget) => {
				Assert.Equal(new Vector3(1, 2, 3), getTarget());
				return execution;
			});
		this.newActionFnTaskTokens[0].SetResult(InputAction.Run);

		this.game.WaitFrames(1);

		Mock
			.Get(this.behavior)
			.Verify(b => b.GetCoroutine(It.IsAny<Func<Vector3>>()), Times.Once);
	}

	[Fact]
	public void RunBehaviorWithTargetTwice() {
		static IEnumerable<Result<IWait>> run() {
			yield break;
		}

		(Func<IEnumerable<Result<IWait>>>, Cancel) execution = (run, () => Result.Ok());

		_ = Mock.Get(this.getTarget)
			.Setup(c => c.GetTarget())
			.Returns(Result.Ok(() => new Vector3(1, 2, 3)));
		_ = Mock.Get(this.behavior)
			.Setup(c => c.GetCoroutine(It.IsAny<Func<Vector3>>()))
			.Returns(execution);
		this.newActionFnTaskTokens[0].SetResult(InputAction.Run);
		this.newActionFnTaskTokens[1].SetResult(InputAction.Run);

		this.game.WaitFrames(2);

		Mock
			.Get(this.scheduler)
			.Verify(b => b.Run(execution), Times.Exactly(2));
	}

	[Fact]
	public void EnqueueBehavior() {
		static IEnumerable<Result<IWait>> run() {
			yield break;
		}

		(Func<IEnumerable<Result<IWait>>>, Cancel) execution = (run, () => Result.Ok());

		_ = Mock.Get(this.getTarget)
			.Setup(c => c.GetTarget())
			.Returns(Result.Ok(() => new Vector3(1, 2, 3)));
		_ = Mock.Get(this.behavior)
			.Setup(c => c.GetCoroutine(It.IsAny<Func<Vector3>>()))
			.Returns(execution);
		this.newActionFnTaskTokens[0].SetResult(InputAction.Chain);

		this.game.WaitFrames(1);

		Mock
			.Get(this.scheduler)
			.Verify(b => b.Enqueue(execution), Times.Once);
	}

	[Fact]
	public void DoNotRunBehaviorWithNoTarget() {
		_ = Mock.Get(this.getTarget)
			.Setup(c => c.GetTarget())
			.Returns(Result.SystemError("ERROR"));
		this.newActionFnTaskTokens[0].SetResult(InputAction.Chain);

		this.game.WaitFrames(1);

		Mock
			.Get(this.behavior)
			.Verify(b => b.GetCoroutine(It.IsAny<Func<Vector3>>()), Times.Never);
	}

	[Fact]
	public void LogGetTargetSystemError() {
		_ = Mock
			.Get(this.getTarget)
			.Setup(c => c.GetTarget())
			.Returns(Result.SystemError("ERROR"));

		this.newActionFnTaskTokens[0].SetResult(InputAction.Chain);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log("ERROR"), Times.Once);
	}

	[Fact]
	public void LogGetTargetPlayerError() {
		_ = Mock.Get(this.getTarget)
			.Setup(c => c.GetTarget())
			.Returns(Result.PlayerError("ERROR"));
		this.newActionFnTaskTokens[0].SetResult(InputAction.Chain);

		this.game.WaitFrames(1);

		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log("ERROR"), Times.Once);
	}

	[Fact]
	public void MissingGetTarget() {
		_ = this.scene.Entities.Remove(this.controller.Entity);

		this.controller.getTarget = Maybe.None<IGetTarget>();

		this.scene.Entities.Add(this.controller.Entity);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(s => s.Log(this.controller.MissingField(nameof(this.controller.getTarget))), Times.Once);
	}

	[Fact]
	public void MissingBehavior() {
		_ = this.scene.Entities.Remove(this.controller.Entity);

		this.controller.behavior = Maybe.None<IBehavior>();

		this.scene.Entities.Add(this.controller.Entity);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(s => s.Log(this.controller.MissingField(nameof(this.controller.behavior))), Times.Once);
	}

	[Fact]
	public void MissingScheduler() {
		_ = this.scene.Entities.Remove(this.controller.Entity);

		this.controller.scheduler = Maybe.None<IScheduler>();

		this.scene.Entities.Add(this.controller.Entity);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(s => s.Log(this.controller.MissingField(nameof(this.controller.scheduler))), Times.Once);
	}


	[Fact]
	public void InputNotSet() {
		_ = this.scene.Entities.Remove(this.controller.Entity);

		this.controller.input = null;

		this.scene.Entities.Add(this.controller.Entity);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(s => s.Log(this.controller.MissingField(nameof(this.controller.input))), Times.Once);
	}

	[Fact]
	public void AllFieldsMissing() {
		_ = this.scene.Entities.Remove(this.controller.Entity);

		this.controller.input = null;
		this.controller.getTarget = Maybe.None<IGetTarget>();
		this.controller.behavior = Maybe.None<IBehavior>();
		this.controller.scheduler = Maybe.None<IScheduler>();

		this.scene.Entities.Add(this.controller.Entity);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(s => s.Log(
				this.controller.MissingField(nameof(this.controller.getTarget)),
				this.controller.MissingField(nameof(this.controller.behavior)),
				this.controller.MissingField(nameof(this.controller.scheduler)),
				this.controller.MissingField(nameof(this.controller.input))
			), Times.Once);
	}

	[Fact]
	public void LogCoroutineError() {
		_ = Mock
			.Get(this.behavior)
			.Setup(b => b.GetCoroutine(It.IsAny<Func<Vector3>>()))
			.Returns(Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" })));

		this.newActionFnTaskTokens[0].SetResult(InputAction.Run);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log("AAA"), Times.Once);
		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log("aaa"), Times.Once);
	}

	[Fact]
	public void LogActionError() {
		_ = Mock
			.Get(this.behavior)
			.Setup(b => b.GetCoroutine(It.IsAny<Func<Vector3>>()))
			.Returns((() => Enumerable.Empty<Result<IWait>>(), () => Result.Ok()));

		this.newActionFnTaskTokens[0].SetResult(Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" })));

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log("AAA"), Times.Once);
		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log("aaa"), Times.Once);
	}

	[Fact]
	public void LogCoroutineRunError() {
		_ = Mock
			.Get(this.behavior)
			.Setup(b => b.GetCoroutine(It.IsAny<Func<Vector3>>()))
			.Returns((() => Enumerable.Empty<Result<IWait>>(), () => Result.Ok()));

		_ = Mock
			.Get(this.scheduler)
			.Setup(s => s.Run(It.IsAny<(Func<IEnumerable<Result<IWait>>>, Cancel)>()))
			.Returns(Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" })));

		this.newActionFnTaskTokens[0].SetResult(InputAction.Run);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log("AAA"), Times.Once);
		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log("aaa"), Times.Once);
	}

	[Fact]
	public void LogCoroutineEnqueError() {
		_ = Mock
			.Get(this.behavior)
			.Setup(b => b.GetCoroutine(It.IsAny<Func<Vector3>>()))
			.Returns((() => Enumerable.Empty<Result<IWait>>(), () => Result.Ok()));

		_ = Mock
			.Get(this.scheduler)
			.Setup(s => s.Enqueue(It.IsAny<(Func<IEnumerable<Result<IWait>>>, Cancel)>()))
			.Returns(Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" })));

		this.newActionFnTaskTokens[0].SetResult(InputAction.Chain);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log("AAA"), Times.Once);
		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log("aaa"), Times.Once);
	}

	[Fact]
	public void LogInputDispatcherAddErrors() {
		_ = Mock
			.Get(this.dispatcher)
			.Setup(d => d.Add(It.IsAny<IInputStream>()))
			.Returns(Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" })));

		_ = this.scene.Entities.Remove(this.controller.Entity);
		this.scene.Entities.Add(this.controller.Entity);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log("AAA"), Times.Once);
		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log("aaa"), Times.Once);
	}

	[Fact]
	public void LogInputDispatcherRemoveErrors() {
		_ = Mock
			.Get(this.dispatcher)
			.Setup(d => d.Remove(It.IsAny<IInputStream>()))
			.Returns(Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" })));

		this.game.WaitFrames(1);

		_ = this.scene.Entities.Remove(this.controller.Entity);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log("AAA"), Times.Once);
		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log("aaa"), Times.Once);
	}
}
