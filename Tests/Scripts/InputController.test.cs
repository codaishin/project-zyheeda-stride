namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;

public class TestInputController : GameTestCollection, IDisposable {
	private class MockInput : IInput {
		public Func<IInputManagerWrapper, IMaybe<Func<IGetTarget, ScriptSystem, IAsyncEnumerable<U<Vector3, Entity>>>>> getTargets =
			(_) => Maybe.None<Func<IGetTarget, ScriptSystem, IAsyncEnumerable<U<Vector3, Entity>>>>();

		public IMaybe<Func<IGetTarget, ScriptSystem, IAsyncEnumerable<U<Vector3, Entity>>>> GetTargets(IInputManagerWrapper input) {
			return this.getTargets(input);
		}
	}

	private class MockController : BaseInputController<MockInput> { }

	private Entity controllerEntity = new();
	private Entity behaviorEntity = new();
	private Entity getTargetEntity = new();

	[SetUp]
	public void Setup() {
		var inputManagerWrapper = Mock.Of<IInputManagerWrapper>();
		var mGetTargets = new Mock<EntityComponent>().As<IGetTarget>();
		var mBehavior = new Mock<EntityComponent>().As<IBehavior>();
		var controller = new MockController();

		Mock.Get(inputManagerWrapper).SetReturnsDefault<bool>(false);

		this.game.WaitFrames(1);

		this.game.Services.AddService<IInputManagerWrapper>(inputManagerWrapper);

		this.scene.Entities.Add(
			this.controllerEntity = new Entity { controller }
		);
		this.scene.Entities.Add(
			this.behaviorEntity = new Entity { (EntityComponent)mBehavior.Object }
		);
		this.scene.Entities.Add(
			this.getTargetEntity = new Entity { (EntityComponent)mGetTargets.Object }
		);

		controller.getTarget.Entity = this.getTargetEntity;
		controller.behavior.Entity = this.behaviorEntity;
	}

	[TearDown]
	public void RemoveInputWrapper() {
		this.game.Services.RemoveService<IInputManagerWrapper>();
	}

	public void Dispose() {
		GC.SuppressFinalize(this);
	}

	[Test]
	public void InjectInputManagerWrapperService() {
		var controller = this.controllerEntity.Get<MockController>();
		var getTargets = Mock.Of<Func<IInputManagerWrapper, IMaybe<Func<IGetTarget, ScriptSystem, IAsyncEnumerable<U<Vector3, Entity>>>>>>();

		_ = Mock.Get(getTargets)
			.Setup(f => f.Invoke(It.IsAny<IInputManagerWrapper>()))
			.Returns(Maybe.None<Func<IGetTarget, ScriptSystem, IAsyncEnumerable<U<Vector3, Entity>>>>());

		controller.input.getTargets = getTargets;

		this.game.WaitFrames(2);

		var inputManagerWrapper = this.game.Services.GetService<IInputManagerWrapper>();
		Mock
			.Get(getTargets)
			.Verify(f => f.Invoke(inputManagerWrapper));
	}

	[Test]
	public void InjectIGetTargetAndScriptSystem() {
		var controller = this.controllerEntity.Get<MockController>();
		var getTargets = Mock.Of<Func<IGetTarget, ScriptSystem, IAsyncEnumerable<U<Vector3, Entity>>>>();

		_ = Mock.Get(getTargets)
			.Setup(f => f.Invoke(It.IsAny<IGetTarget>(), It.IsAny<ScriptSystem>()))
			.Returns(Array.Empty<U<Vector3, Entity>>().ToAsyncEnumerable());

		controller.input.getTargets = (_) => Maybe.Some(getTargets);

		this.game.WaitFrames(2);

		var getTarget = this.getTargetEntity.Components.OfType<IGetTarget>().First();
		Mock
			.Get(getTargets)
			.Verify(f => f.Invoke(getTarget, this.game.Script));
	}

	[Test]
	public void RunBehaviorWithTargets() {
		var controller = this.controllerEntity.Get<MockController>();
		var getTargets = Mock.Of<Func<IGetTarget, ScriptSystem, IAsyncEnumerable<U<Vector3, Entity>>>>();
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 2, 3), new Entity() }.ToAsyncEnumerable();

		_ = Mock.Get(getTargets)
			.Setup(f => f.Invoke(It.IsAny<IGetTarget>(), It.IsAny<ScriptSystem>()))
			.Returns(targets);


		controller.input.getTargets = (_) => Maybe.Some(getTargets);

		this.game.WaitFrames(2);

		Mock
			.Get(this.behaviorEntity.Components.OfType<IBehavior>().First())
			.Verify(b => b.Run(targets));
	}

	[Test]
	public void MissingGetTarget() {
		var controller = this.controllerEntity.Get<MockController>();
		controller.getTarget.Entity = null;
		controller.input.getTargets = (_) => Maybe.Some(
			(IGetTarget _, ScriptSystem __) => Array.Empty<U<Vector3, Entity>>().ToAsyncEnumerable()
		);

		controller.Start();

		var error = Assert.Throws<MissingField>(controller.Update);
		Assert.That(
			error?.ToString(),
			Does.Contain(new MissingField(controller, nameof(controller.getTarget)).ToString())
		);
	}

	[Test]
	public void MissingBehavior() {
		var controller = this.controllerEntity.Get<MockController>();
		controller.behavior.Entity = null;
		controller.input.getTargets = (_) => Maybe.Some(
			(IGetTarget _, ScriptSystem __) => Array.Empty<U<Vector3, Entity>>().ToAsyncEnumerable()
		);

		controller.Start();

		var error = Assert.Throws<MissingField>(controller.Update);
		Assert.That(
			error?.ToString(),
			Does.Contain(new MissingField(controller, nameof(controller.behavior)).ToString())
		);
	}

	[Test]
	public void MissingGetTargetAndBehavior() {
		var controller = this.controllerEntity.Get<MockController>();
		controller.getTarget.Entity = null;
		controller.behavior.Entity = null;
		controller.input.getTargets = (_) => Maybe.Some(
			(IGetTarget _, ScriptSystem __) => Array.Empty<U<Vector3, Entity>>().ToAsyncEnumerable()
		);

		controller.Start();

		var error = Assert.Throws<MissingField>(controller.Update);
		Assert.That(
			error?.ToString(),
			Does.Contain(new MissingField(controller, nameof(controller.getTarget), nameof(controller.behavior)).ToString())
		);
	}
}
