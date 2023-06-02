namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;

public class TestInputController : GameTestCollection, IDisposable {

	private InputController controller = new();
	private IInputStream inputStream = Mock.Of<IInputStream>();
	private IBehavior behavior = Mock.Of<IBehavior>();
	private IGetTarget getTarget = Mock.Of<IGetTarget>();
	private IScheduler scheduler = Mock.Of<IScheduler>();
	private List<TaskCompletionSource<Result<InputAction>>> newActionFnTaskTokens = new();
	private ISystemMessage systemMessage = Mock.Of<ISystemMessage>();
	private IPlayerMessage playerMessage = Mock.Of<IPlayerMessage>();
	private IInputDispatcher dispatcher = Mock.Of<IInputDispatcher>();

	[SetUp]
	public void Setup() {
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

		this.Scene.Entities.Add(new Entity { this.controller });

		this.game.WaitFrames(2);
	}

	[Test]
	public void AddInputStreamToDispatcher() {
		Mock
			.Get(this.game.Services.GetService<IInputDispatcher>())
			.Verify(d => d.Add(this.inputStream), Times.Once);
	}

	[Test]
	public void RemoveInputStreamFromDispatcher() {
		_ = this.Scene.Entities.Remove(this.controller.Entity);

		this.game.WaitFrames(1);

		this.Scene.Entities.Add(this.controller.Entity);

		this.game.WaitFrames(1);

		_ = this.Scene.Entities.Remove(this.controller.Entity);

		this.game.WaitFrames(1);

		Mock
			.Get(this.game.Services.GetService<IInputDispatcher>())
			.Verify(d => d.Remove(this.inputStream), Times.Exactly(2));
	}

	[Test]
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
				Assert.That(getTarget(), Is.EqualTo(new Vector3(1, 2, 3)));
				return execution;
			});
		this.newActionFnTaskTokens[0].SetResult(InputAction.Run);

		this.game.WaitFrames(1);

		Mock
			.Get(this.behavior)
			.Verify(b => b.GetCoroutine(It.IsAny<Func<Vector3>>()), Times.Once);
	}

	[Test]
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

		this.game.WaitFrames(1);

		Mock
			.Get(this.scheduler)
			.Verify(b => b.Run(execution), Times.Exactly(2));
	}

	[Test]
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

	[Test]
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

	[Test]
	public void LogGetTargetSystemError() {
		_ = Mock.Get(this.getTarget)
			.Setup(c => c.GetTarget())
			.Returns(Result.SystemError("ERROR"));
		this.newActionFnTaskTokens[0].SetResult(InputAction.Chain);

		this.game.WaitFrames(1);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log("ERROR"), Times.Once);
	}

	[Test]
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

	[Test]
	public void MissingGetTarget() {
		_ = this.Scene.Entities.Remove(this.controller.Entity);

		this.controller.getTarget = Maybe.None<IGetTarget>();

		this.Scene.Entities.Add(this.controller.Entity);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(s => s.Log(this.controller.MissingField(nameof(this.controller.getTarget))), Times.Once);
	}

	[Test]
	public void MissingBehavior() {
		_ = this.Scene.Entities.Remove(this.controller.Entity);

		this.controller.behavior = Maybe.None<IBehavior>();

		this.Scene.Entities.Add(this.controller.Entity);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(s => s.Log(this.controller.MissingField(nameof(this.controller.behavior))), Times.Once);
	}

	[Test]
	public void MissingScheduler() {
		_ = this.Scene.Entities.Remove(this.controller.Entity);

		this.controller.scheduler = Maybe.None<IScheduler>();

		this.Scene.Entities.Add(this.controller.Entity);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(s => s.Log(this.controller.MissingField(nameof(this.controller.scheduler))), Times.Once);
	}


	[Test]
	public void InputNotSet() {
		_ = this.Scene.Entities.Remove(this.controller.Entity);

		this.controller.input = null;

		this.Scene.Entities.Add(this.controller.Entity);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(s => s.Log(this.controller.MissingField(nameof(this.controller.input))), Times.Once);
	}

	[Test]
	public void AllFieldsMissing() {
		_ = this.Scene.Entities.Remove(this.controller.Entity);

		this.controller.input = null;
		this.controller.getTarget = Maybe.None<IGetTarget>();
		this.controller.behavior = Maybe.None<IBehavior>();
		this.controller.scheduler = Maybe.None<IScheduler>();

		this.Scene.Entities.Add(this.controller.Entity);

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

	[Test]
	public void LogCoroutineError() {
		_ = Mock
			.Get(this.behavior)
			.Setup(b => b.GetCoroutine(It.IsAny<Func<Vector3>>()))
			.Returns(Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" })));

		this.newActionFnTaskTokens[0].SetResult(InputAction.Run);

		this.game.WaitFrames(1);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log("AAA"), Times.Once);
		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log("aaa"), Times.Once);
	}

	[Test]
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

	[Test]
	public void LogInputDispatcherAddErrors() {
		_ = Mock
			.Get(this.dispatcher)
			.Setup(d => d.Add(It.IsAny<IInputStream>()))
			.Returns(Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" })));

		_ = this.Scene.Entities.Remove(this.controller.Entity);
		this.Scene.Entities.Add(this.controller.Entity);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log("AAA"), Times.Once);
		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log("aaa"), Times.Once);
	}

	[Test]
	public void LogInputDispatcherRemoveErrors() {
		_ = Mock
			.Get(this.dispatcher)
			.Setup(d => d.Remove(It.IsAny<IInputStream>()))
			.Returns(Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" })));

		this.game.WaitFrames(1);

		_ = this.Scene.Entities.Remove(this.controller.Entity);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log("AAA"), Times.Once);
		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log("aaa"), Times.Once);
	}

	public void Dispose() {
		GC.SuppressFinalize(this);
	}
}
