namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Engine;

public class TestMoveController : GameTestCollection, System.IDisposable {
	private Entity agent = new();
	private MockMoveController moveController = new();

	private class MockMove : IAnimatedMove {
		public Func<Entity, FSpeedToDelta, Action<string>, FGetCoroutine> prepareCoroutineFor;

		public MockMove() {
			this.prepareCoroutineFor = (_, __, ___) => Mock.Of<FGetCoroutine>();
		}

		public FGetCoroutine PrepareCoroutineFor(Entity agent, FSpeedToDelta delta, Action<string> playAnimation) {
			return this.prepareCoroutineFor(agent, delta, playAnimation);
		}
	}

	private class MockMoveController : BaseMoveController<MockMove> { }

	private static string ErrorsToString(IEnumerable<U<SystemStr, PlayerStr>> errors) {
		var errorsUnpacked = errors.Select(error => error.Switch(
			v => $"{v.value} ({v.GetType().Name})",
			v => $"{v.value} ({v.GetType().Name})"
		));
		return string.Join(", ", errorsUnpacked);
	}

	[SetUp]
	public void SetUp() {
		var animation = Mock.Of<IAnimation>();
		this.game.Services.RemoveService<IAnimation>();
		this.game.Services.AddService<IAnimation>(animation);

		Mock
			.Get(animation)
			.SetReturnsDefault(false);
		Mock
			.Get(animation)
			.SetReturnsDefault(Maybe.None<IPlayingAnimation>());

		this.agent = new Entity();
		this.agent.AddChild(new Entity { new AnimationComponent() });
		this.moveController = new MockMoveController();

		this.Scene.Entities.Add(new Entity { this.moveController });
		this.Scene.Entities.Add(this.agent);

		this.game.WaitFrames(1);
	}

	[Test]
	public void ReturnMoveResult() {
		var mockGetCoroutine = Mock.Of<FGetCoroutine>();
		var prepareCoroutineFor = Mock.Of<Func<Entity, FSpeedToDelta, Action<string>, FGetCoroutine>>();
		this.moveController.move.prepareCoroutineFor = prepareCoroutineFor;

		_ = Mock
			.Get(prepareCoroutineFor)
			.Setup(func => func(this.agent, It.IsAny<FSpeedToDelta>(), It.IsAny<Action<string>>()))
			.Returns(mockGetCoroutine);

		var getCoroutine = this.moveController
			.PrepareCoroutineFor(this.agent)
			.Switch(
				_ => throw new AssertionException("got no coroutine function"),
				getCoroutine => getCoroutine
			);

		Assert.That(getCoroutine, Is.SameAs(mockGetCoroutine));
	}

	[Test]
	public void PlayAnimation() {
		var runPlay = null as Action<string>;
		var prepareCoroutineFor = Mock.Of<Func<Entity, FSpeedToDelta, Action<string>, FGetCoroutine>>();
		var animation = this.game.Services.GetService<IAnimation>();
		var animator = this.agent.GetChild(0).Get<AnimationComponent>();

		this.moveController.move.prepareCoroutineFor = (_, __, play) => {
			runPlay = play;
			return Mock.Of<FGetCoroutine>();
		};

		_ = this.moveController.PrepareCoroutineFor(this.agent);

		runPlay!("walk");

		Mock
			.Get(animation)
			.Verify(a => a.Play(animator, "walk"), Times.Once);
	}

	[Test]
	public void DoNotPlayActiveAnimations() {
		var runPlay = null as Action<string>;
		var prepareCoroutineFor = Mock.Of<Func<Entity, FSpeedToDelta, Action<string>, FGetCoroutine>>();
		var animation = this.game.Services.GetService<IAnimation>();
		var animator = this.agent.GetChild(0).Get<AnimationComponent>();

		this.moveController.move.prepareCoroutineFor = (_, __, play) => {
			runPlay = play;
			return Mock.Of<FGetCoroutine>();
		};

		_ = this.moveController.PrepareCoroutineFor(this.agent);

		runPlay!("walk");

		_ = Mock
			.Get(animation)
			.Setup(g => g.IsPlaying(animator, "walk"))
			.Returns(true);

		runPlay!("walk");

		Mock
			.Get(animation)
			.Verify(a => a.Play(animator, "walk"), Times.Once);
	}

	[Test]
	public void MultiplySpeedWithUpdateTimeElapsed() {
		var runDelta = null as FSpeedToDelta;
		var prepareCoroutineFor = Mock.Of<Func<Entity, FSpeedToDelta, Action<string>, FGetCoroutine>>();

		this.moveController.move.prepareCoroutineFor = (_, delta, __) => {
			runDelta = delta;
			return Mock.Of<FGetCoroutine>();
		};

		_ = this.moveController.PrepareCoroutineFor(this.agent);

		var deltaElapsed = (float)this.game.UpdateTime.Elapsed.TotalSeconds;
		Assert.That(runDelta!(42), Is.EqualTo(42 * deltaElapsed));
	}

	[Test]
	public void MissingAnimationComponent() {
		this.agent.Name = "Agent";
		this.agent.GetChild(0).RemoveAll<AnimationComponent>();

		var error = this.moveController
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.ErrorsToString, _ => "no error, got actual behavior");

		Assert.That(error, Is.EqualTo($"Missing AnimationComponent on Agent ({nameof(SystemStr)})"));
	}

	[Test]
	public void NoGetAnimationServiceBeforeStart() {
		this.game.Services.RemoveService<IAnimation>();

		var moveComponent = new MoveController();
		this.Scene.Entities.Add(new Entity { moveComponent });

		var error = moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.ErrorsToString, _ => "no error, got actual behavior");

		Assert.That(error, Is.EqualTo($"No IGetAnimation assigned ({nameof(SystemStr)})"));
	}

	[Test]
	public void MissingGetAnimationService() {
		this.game.Services.RemoveService<IAnimation>();

		var moveComponent = new MoveController();
		this.Scene.Entities.Add(new Entity { moveComponent });

		this.game.WaitFrames(1);

		var error = moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.ErrorsToString, _ => "no error, got actual behavior");

		Assert.That(error, Is.EqualTo($"Missing IGetAnimation Service ({nameof(SystemStr)})"));
	}

	[Test]
	public void MissingGetAnimationServiceAndAnimationComponent() {
		this.game.Services.RemoveService<IAnimation>();
		this.agent.Name = "Agent";
		this.agent.GetChild(0).RemoveAll<AnimationComponent>();

		var moveComponent = new MoveController();
		this.Scene.Entities.Add(new Entity { moveComponent });

		this.game.WaitFrames(1);

		var error = moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.ErrorsToString, _ => "no error, got actual behavior");

		Assert.Multiple(() => {
			Assert.That(error, Contains.Substring($"Missing AnimationComponent on Agent ({nameof(SystemStr)})"));
			Assert.That(error, Contains.Substring($"Missing AnimationComponent on Agent ({nameof(SystemStr)})"));
		});
	}

	public void Dispose() {
		GC.SuppressFinalize(this);
	}
}
