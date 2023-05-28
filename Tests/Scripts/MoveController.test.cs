namespace Tests;

using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Engine;

public class TestMoveController : GameTestCollection, System.IDisposable {
	private Entity agent = new();
	private MoveController moveController = new();
	private IAnimatedMove move = Mock.Of<IAnimatedMove>();

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
		this.move = Mock.Of<IAnimatedMove>();
		this.moveController = new() { move = this.move };

		this.Scene.Entities.Add(new Entity { this.moveController });
		this.Scene.Entities.Add(this.agent);

		this.game.WaitFrames(1);
	}

	[Test]
	public void ReturnMoveResult() {
		var mockGetCoroutine = Mock.Of<FGetCoroutine>();

		_ = Mock
			.Get(this.move)
			.Setup(m => m.PrepareCoroutineFor(this.agent, It.IsAny<FSpeedToDelta>(), It.IsAny<Action<string>>()))
			.Returns(Result.Ok(mockGetCoroutine));

		var getCoroutine = this.moveController
			.PrepareCoroutineFor(this.agent)
			.Switch(
				_ => throw new AssertionException("got no coroutine function"),
				getCoroutine => getCoroutine
			);

		Assert.That(getCoroutine, Is.EqualTo(mockGetCoroutine));
	}

	[Test]
	public void PlayAnimation() {
		var runPlay = null as Action<string>;
		var prepareCoroutineFor = Mock.Of<Func<Entity, FSpeedToDelta, Action<string>, FGetCoroutine>>();
		var animation = this.game.Services.GetService<IAnimation>();
		var animator = this.agent.GetChild(0).Get<AnimationComponent>();

		_ = Mock
			.Get(this.move)
			.Setup(m => m.PrepareCoroutineFor(this.agent, It.IsAny<FSpeedToDelta>(), It.IsAny<Action<string>>()))
			.Returns((Entity _, FSpeedToDelta _, Action<string> play) => {
				runPlay = play;
				return Result.Ok(Mock.Of<FGetCoroutine>());
			});

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

		_ = Mock
			.Get(this.move)
			.Setup(m => m.PrepareCoroutineFor(this.agent, It.IsAny<FSpeedToDelta>(), It.IsAny<Action<string>>()))
			.Returns((Entity _, FSpeedToDelta _, Action<string> play) => {
				runPlay = play;
				return Result.Ok(Mock.Of<FGetCoroutine>());
			});

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

		_ = Mock
			.Get(this.move)
			.Setup(m => m.PrepareCoroutineFor(this.agent, It.IsAny<FSpeedToDelta>(), It.IsAny<Action<string>>()))
			.Returns((Entity _, FSpeedToDelta delta, Action<string> _) => {
				runDelta = delta;
				return Result.Ok(Mock.Of<FGetCoroutine>());
			});

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
			.Switch<string>(errors => errors.system.First(), _ => "no error, got actual behavior");

		Assert.That(error, Is.EqualTo($"Missing AnimationComponent on Agent"));
	}

	[Test]
	public void MissingMoveComponent() {
		this.moveController.move = null;
		var error = this.moveController
			.PrepareCoroutineFor(this.agent)
			.Switch<string>(errors => errors.system.First(), _ => "no error, got actual behavior");

		var missingMove = this.moveController.MissingField(nameof(this.moveController.move));
		Assert.That(error, Is.EqualTo(missingMove));
	}

	public void Dispose() {
		GC.SuppressFinalize(this);
	}
}
