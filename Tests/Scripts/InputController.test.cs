namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;

public class TestInputController : GameTestCollection, IDisposable {
	private class MockInput : IInput {
		public Func<IInputManagerWrapper, InputAction> getAction = (_) => InputAction.None;

		public InputAction GetAction(IInputManagerWrapper input) {
			return this.getAction(input);
		}
	}

	private class MockController : BaseInputController<MockInput> { }

	private Entity controllerEntity = new();
	private Entity behaviorEntity = new();
	private Entity getTargetEntity = new();
	private Entity schedulerEntity = new();

	[SetUp]
	public void Setup() {
		var inputManagerWrapper = Mock.Of<IInputManagerWrapper>();
		var mGetTarget = new Mock<EntityComponent>().As<IGetTarget>();
		var mBehavior = new Mock<EntityComponent>().As<IBehavior>();
		var mScheduler = new Mock<EntityComponent>().As<IScheduler>();
		var controller = new MockController();

		Mock.Get(inputManagerWrapper).SetReturnsDefault<bool>(false);

		this.game.WaitFrames(1);

		this.game.Services.AddService<IInputManagerWrapper>(inputManagerWrapper);

		this.Scene.Entities.Add(
			this.controllerEntity = new Entity { controller }
		);
		this.Scene.Entities.Add(
			this.behaviorEntity = new Entity { (EntityComponent)mBehavior.Object }
		);
		this.Scene.Entities.Add(
			this.getTargetEntity = new Entity { (EntityComponent)mGetTarget.Object }
		);
		this.Scene.Entities.Add(
			this.schedulerEntity = new Entity { (EntityComponent)mScheduler.Object }
		);

		controller.getTarget.Entity = this.getTargetEntity;
		controller.behavior.Entity = this.behaviorEntity;
		controller.scheduler.Entity = this.schedulerEntity;
	}

	[TearDown]
	public void RemoveInputWrapper() {
		this.game.Services.RemoveService<IInputManagerWrapper>();
	}

	[Test]
	public void RunBehaviorWithTarget() {
		static IEnumerable<IWait> run() {
			yield break;
		}

		var controller = this.controllerEntity.Get<MockController>();
		var getTarget = this.getTargetEntity.Components.OfType<IGetTarget>().First();
		var behavior = this.behaviorEntity.Components.OfType<IBehavior>().First();
		(Func<IEnumerable<IWait>>, Action) execution = (run, () => { });
		var calls = 0;

		controller.input.getAction = (_) => calls++ == 0 ? InputAction.Run : InputAction.None;

		_ = Mock.Get(getTarget)
			.Setup(c => c.GetTarget())
			.Returns(Maybe.Some<U<Vector3, Entity>>(new Vector3(1, 2, 3)));
		_ = Mock.Get(behavior)
			.Setup(c => c.GetCoroutine(new Vector3(1, 2, 3)))
			.Returns(execution);

		this.game.WaitFrames(2);

		Mock
			.Get(this.schedulerEntity.Components.OfType<IScheduler>().First())
			.Verify(b => b.Run(execution), Times.Once);
	}

	[Test]
	public void EnqueueBehaviorWithTarget() {
		static IEnumerable<IWait> run() {
			yield break;
		}

		var controller = this.controllerEntity.Get<MockController>();
		var getTarget = this.getTargetEntity.Components.OfType<IGetTarget>().First();
		var behavior = this.behaviorEntity.Components.OfType<IBehavior>().First();
		(Func<IEnumerable<IWait>>, Action) execution = (run, () => { });
		var calls = 0;

		controller.input.getAction = (_) => calls++ == 0 ? InputAction.Chain : InputAction.None;

		_ = Mock.Get(getTarget)
			.Setup(c => c.GetTarget())
			.Returns(Maybe.Some<U<Vector3, Entity>>(new Vector3(1, 2, 3)));
		_ = Mock.Get(behavior)
			.Setup(c => c.GetCoroutine(new Vector3(1, 2, 3)))
			.Returns(execution);

		this.game.WaitFrames(2);

		Mock
			.Get(this.schedulerEntity.Components.OfType<IScheduler>().First())
			.Verify(b => b.Enqueue(execution), Times.Once);
	}

	[Test]
	public void DoNotRunBehaviorWithNoTarget() {
		var controller = this.controllerEntity.Get<MockController>();
		var getTarget = this.getTargetEntity.Components.OfType<IGetTarget>().First();
		var calls = 0;

		controller.input.getAction = (_) => calls++ == 0 ? InputAction.Run : InputAction.None;

		_ = Mock.Get(getTarget)
			.Setup(c => c.GetTarget())
			.Returns(Maybe.None<U<Vector3, Entity>>());

		this.game.WaitFrames(2);

		Mock
			.Get(this.behaviorEntity.Components.OfType<IBehavior>().First())
			.Verify(b => b.GetCoroutine(It.IsAny<U<Vector3, Entity>>()), Times.Never);
	}

	[Test]
	public void MissingGetTarget() {
		var controller = this.controllerEntity.Get<MockController>();
		_ = this.Scene.Entities.Remove(controller.Entity);

		controller.getTarget.Entity = null;
		controller.input.getAction = (_) => InputAction.Run;

		controller.Start();

		Assert.Multiple(() => {
			var error = Assert.Throws<MissingField>(controller.Update);
			Assert.That(
				error?.ToString(),
				Does.Contain(new MissingField(controller, nameof(controller.getTarget)).ToString())
			);
		});
	}

	[Test]
	public void MissingBehavior() {
		var controller = this.controllerEntity.Get<MockController>();
		_ = this.Scene.Entities.Remove(controller.Entity);

		controller.behavior.Entity = null;
		controller.input.getAction = (_) => InputAction.Run;

		controller.Start();

		Assert.Multiple(() => {
			var error = Assert.Throws<MissingField>(controller.Update);
			Assert.That(
				error?.ToString(),
				Does.Contain(new MissingField(controller, nameof(controller.behavior)).ToString())
			);
		});
	}

	[Test]
	public void MissingScheduler() {
		var controller = this.controllerEntity.Get<MockController>();
		_ = this.Scene.Entities.Remove(controller.Entity);

		controller.scheduler.Entity = null;
		controller.input.getAction = (_) => InputAction.Run;

		controller.Start();

		Assert.Multiple(() => {
			var error = Assert.Throws<MissingField>(controller.Update);
			Assert.That(
				error?.ToString(),
				Does.Contain(new MissingField(controller, nameof(controller.scheduler)).ToString())
			);
		});
	}

	[Test]
	public void MissingGetTargetAndBehavior() {
		var controller = this.controllerEntity.Get<MockController>();
		_ = this.Scene.Entities.Remove(controller.Entity);

		controller.getTarget.Entity = null;
		controller.behavior.Entity = null;
		controller.input.getAction = (_) => InputAction.Run;

		controller.Start();

		Assert.Multiple(() => {
			var error = Assert.Throws<MissingField>(controller.Update);
			Assert.That(
				error?.ToString(),
				Does.Contain(new MissingField(controller, nameof(controller.getTarget), nameof(controller.behavior)).ToString())
			);
		});
	}

	public void Dispose() {
		GC.SuppressFinalize(this);
	}
}
