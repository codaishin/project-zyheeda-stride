namespace Tests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;

public class TestInputController : GameTestCollection, IDisposable {
	private class MockController : BaseInputController<IInputStream> {
		public MockController() : base(Mock.Of<IInputStream>()) { }
	}

	private MockController controller = new();
	private IBehavior behavior = Mock.Of<IBehavior>();
	private IGetTarget getTarget = Mock.Of<IGetTarget>();
	private IScheduler scheduler = Mock.Of<IScheduler>();
	private List<TaskCompletionSource<InputAction>> newActionFnTaskTokens = new();
	private ISystemMessage systemMessage = Mock.Of<ISystemMessage>();

	[SetUp]
	public void Setup() {
		var newActionCallCount = 0;
		var dispatcher = Mock.Of<IInputDispatcher>();
		this.controller = new MockController {
			behavior = Maybe.Some(this.behavior = Mock.Of<IBehavior>()),
			getTarget = Maybe.Some(this.getTarget = Mock.Of<IGetTarget>()),
			scheduler = Maybe.Some(this.scheduler = Mock.Of<IScheduler>())
		};

		this.newActionFnTaskTokens = new List<TaskCompletionSource<InputAction>> {
			new TaskCompletionSource<InputAction>(),
			new TaskCompletionSource<InputAction>(),
			new TaskCompletionSource<InputAction>(),
			new TaskCompletionSource<InputAction>(),
			new TaskCompletionSource<InputAction>(),
		};

		_ = Mock
			.Get(this.controller.input)
			.Setup(i => i.NewAction())
			.Returns(() => this.newActionFnTaskTokens[newActionCallCount++].Task);

		this.game.Services.RemoveService<ISystemMessage>();
		this.game.Services.AddService(this.systemMessage = Mock.Of<ISystemMessage>());
		this.game.Services.RemoveService<IInputDispatcher>();
		this.game.Services.AddService(dispatcher);

		this.Scene.Entities.Add(new Entity { this.controller });

		this.game.WaitFrames(2);
	}

	[Test]
	public void AddInputStreamToDispatcher() {
		Mock
			.Get(this.game.Services.GetService<IInputDispatcher>())
			.Verify(d => d.Add(this.controller.input), Times.Once);
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
			.Verify(d => d.Remove(this.controller.input), Times.Exactly(2));
	}

	[Test]
	public void RunBehaviorWithTarget() {
		static IEnumerable<IWait> run() {
			yield break;
		}

		(Func<IEnumerable<IWait>>, Action) execution = (run, () => { });

		_ = Mock.Get(this.getTarget)
			.Setup(c => c.GetTarget())
			.Returns(Maybe.Some<U<Vector3, Entity>>(new Vector3(1, 2, 3)));
		_ = Mock.Get(this.behavior)
			.Setup(c => c.GetCoroutine(new Vector3(1, 2, 3)))
			.Returns(execution);
		this.newActionFnTaskTokens[0].SetResult(InputAction.Run);

		this.game.WaitFrames(1);

		Mock
			.Get(this.scheduler)
			.Verify(b => b.Run(execution), Times.Once);
	}

	[Test]
	public void RunBehaviorWithTargetTwice() {
		static IEnumerable<IWait> run() {
			yield break;
		}

		(Func<IEnumerable<IWait>>, Action) execution = (run, () => { });

		_ = Mock.Get(this.getTarget)
			.Setup(c => c.GetTarget())
			.Returns(Maybe.Some<U<Vector3, Entity>>(new Vector3(1, 2, 3)));
		_ = Mock.Get(this.behavior)
			.Setup(c => c.GetCoroutine(new Vector3(1, 2, 3)))
			.Returns(execution);
		this.newActionFnTaskTokens[0].SetResult(InputAction.Run);
		this.newActionFnTaskTokens[1].SetResult(InputAction.Run);

		this.game.WaitFrames(1);

		Mock
			.Get(this.scheduler)
			.Verify(b => b.Run(execution), Times.Exactly(2));
	}

	[Test]
	public void EnqueueBehaviorWithTarget() {
		static IEnumerable<IWait> run() {
			yield break;
		}

		(Func<IEnumerable<IWait>>, Action) execution = (run, () => { });

		_ = Mock.Get(this.getTarget)
			.Setup(c => c.GetTarget())
			.Returns(Maybe.Some<U<Vector3, Entity>>(new Vector3(1, 2, 3)));
		_ = Mock.Get(this.behavior)
			.Setup(c => c.GetCoroutine(new Vector3(1, 2, 3)))
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
			.Returns(Maybe.None<U<Vector3, Entity>>());
		this.newActionFnTaskTokens[0].SetResult(InputAction.Chain);

		this.game.WaitFrames(1);

		Mock
			.Get(this.behavior)
			.Verify(b => b.GetCoroutine(It.IsAny<U<Vector3, Entity>>()), Times.Never);
	}

	[Test]
	public void MissingGetTarget() {
		_ = this.Scene.Entities.Remove(this.controller.Entity);

		this.controller.getTarget = Maybe.None<IGetTarget>();

		this.Scene.Entities.Add(this.controller.Entity);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(s => s.Log(new SystemStr(this.controller.MissingField(nameof(this.controller.getTarget)))), Times.Once);
	}

	[Test]
	public void MissingBehavior() {
		_ = this.Scene.Entities.Remove(this.controller.Entity);

		this.controller.behavior = Maybe.None<IBehavior>();

		this.Scene.Entities.Add(this.controller.Entity);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(s => s.Log(new SystemStr(this.controller.MissingField(nameof(this.controller.behavior)))), Times.Once);
	}

	[Test]
	public void MissingScheduler() {
		_ = this.Scene.Entities.Remove(this.controller.Entity);

		this.controller.scheduler = Maybe.None<IScheduler>();

		this.Scene.Entities.Add(this.controller.Entity);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(s => s.Log(new SystemStr(this.controller.MissingField(nameof(this.controller.scheduler)))), Times.Once);
	}

	[Test]
	public void MissingGetTargetAndBehavior() {
		_ = this.Scene.Entities.Remove(this.controller.Entity);

		this.controller.getTarget = Maybe.None<IGetTarget>();
		this.controller.behavior = Maybe.None<IBehavior>();
		this.controller.scheduler = Maybe.None<IScheduler>();

		this.Scene.Entities.Add(this.controller.Entity);

		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(s => s.Log(new SystemStr(this.controller.MissingField(nameof(this.controller.getTarget)))), Times.Once);
		Mock
			.Get(this.systemMessage)
			.Verify(s => s.Log(new SystemStr(this.controller.MissingField(nameof(this.controller.behavior)))), Times.Once);
		Mock
			.Get(this.systemMessage)
			.Verify(s => s.Log(new SystemStr(this.controller.MissingField(nameof(this.controller.scheduler)))), Times.Once);
	}

	public void Dispose() {
		GC.SuppressFinalize(this);
	}
}
