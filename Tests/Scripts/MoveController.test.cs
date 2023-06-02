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
	private IAnimation animation = Mock.Of<IAnimation>();

	[SetUp]
	public void SetUp() {
		;
		this.game.Services.RemoveService<IAnimation>();
		this.game.Services.AddService<IAnimation>(this.animation = Mock.Of<IAnimation>());

		Mock
			.Get(this.animation)
			.SetReturnsDefault<Result<bool>>(false);
		Mock
			.Get(this.animation)
			.SetReturnsDefault<Result<IPlayingAnimation>>(Result.Ok(Mock.Of<IPlayingAnimation>()));
		Mock
			.Get(this.move)
			.SetReturnsDefault<Result<bool>>(false);

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
			.Setup(m => m.PrepareCoroutineFor(this.agent, It.IsAny<FSpeedToDelta>(), It.IsAny<Func<string, Result>>()))
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
		var prepareCoroutineFor = Mock.Of<Func<Entity, FSpeedToDelta, Func<string, Result>, FGetCoroutine>>();
		var animation = this.game.Services.GetService<IAnimation>();
		var animator = this.agent.GetChild(0).Get<AnimationComponent>();

		_ = Mock
			.Get(this.move)
			.Setup(m => m.PrepareCoroutineFor(this.agent, It.IsAny<FSpeedToDelta>(), It.IsAny<Func<string, Result>>()))
			.Returns((Entity _, FSpeedToDelta _, Func<string, Result> play) => {
				_ = play("walk");
				return Result.Ok(Mock.Of<FGetCoroutine>());
			});

		_ = this.moveController.PrepareCoroutineFor(this.agent);

		Mock
			.Get(animation)
			.Verify(a => a.Play(animator, "walk"), Times.Once);
	}

	[Test]
	public void DoNotPlayActiveAnimations() {
		var prepareCoroutineFor = Mock.Of<Func<Entity, FSpeedToDelta, Action<string>, FGetCoroutine>>();
		var animation = this.game.Services.GetService<IAnimation>();
		var animator = this.agent.GetChild(0).Get<AnimationComponent>();
		_ = Mock
			.Get(animation)
			.SetupSequence(g => g.IsPlaying(animator, "walk"))
			.Returns(false)
			.Returns(true);

		_ = Mock
			.Get(this.move)
			.Setup(m => m.PrepareCoroutineFor(this.agent, It.IsAny<FSpeedToDelta>(), It.IsAny<Func<string, Result>>()))
			.Returns((Entity _, FSpeedToDelta _, Func<string, Result> play) => {
				_ = play("walk");
				_ = play("walk");

				Mock
					.Get(animation)
					.Verify(a => a.Play(animator, "walk"), Times.Once);

				return Result.Ok(Mock.Of<FGetCoroutine>());
			});

		_ = this.moveController.PrepareCoroutineFor(this.agent);
	}

	[Test]
	public void MultiplySpeedWithUpdateTimeElapsed() {
		var prepareCoroutineFor = Mock.Of<Func<Entity, FSpeedToDelta, Func<string, Result>, FGetCoroutine>>();

		_ = Mock
			.Get(this.move)
			.Setup(m => m.PrepareCoroutineFor(this.agent, It.IsAny<FSpeedToDelta>(), It.IsAny<Func<string, Result>>()))
			.Returns((Entity _, FSpeedToDelta delta, Func<string, Result> _) => {
				var deltaElapsed = (float)this.game.UpdateTime.Elapsed.TotalSeconds;
				Assert.That(delta(42), Is.EqualTo(42 * deltaElapsed));
				return Result.Ok(Mock.Of<FGetCoroutine>());
			});

		_ = this.moveController.PrepareCoroutineFor(this.agent);
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

	[Test]
	public void MoveError() {
		_ = Mock
			.Get(this.move)
			.Setup(m => m.PrepareCoroutineFor(this.agent, It.IsAny<FSpeedToDelta>(), It.IsAny<Func<string, Result>>()))
			.Returns(Result.SystemError("LLL"));

		var error = this.moveController
			.PrepareCoroutineFor(this.agent)
			.Switch<string>(errors => errors.system.First(), _ => "no error, got actual behavior");

		Assert.That(error, Is.EqualTo("LLL"));
	}

	[Test]
	public void CheckAnimationError() {
		_ = Mock
			.Get(this.animation)
			.Setup(a => a.IsPlaying(It.IsAny<AnimationComponent>(), It.IsAny<string>()))
			.Returns(Result.SystemError("LLL"));
		_ = Mock
			.Get(this.move)
			.Setup(m => m.PrepareCoroutineFor(It.IsAny<Entity>(), It.IsAny<FSpeedToDelta>(), It.IsAny<Func<string, Result>>()))
			.Returns((Entity _, FSpeedToDelta _, Func<string, Result> play) => {
				var error = play("").Switch<string>(
					errors => errors.system.First(),
					() => "no error, got actual behavior"
				);

				Assert.That(error, Is.EqualTo("LLL"));

				return Result.Ok(Mock.Of<FGetCoroutine>());
			});

		_ = this.moveController.PrepareCoroutineFor(this.agent);

	}

	[Test]
	public void PlayAnimationError() {
		_ = Mock
			.Get(this.animation)
			.Setup(a => a.Play(It.IsAny<AnimationComponent>(), It.IsAny<string>()))
			.Returns(Result.SystemError("LLL"));
		_ = Mock
			.Get(this.move)
			.Setup(m => m.PrepareCoroutineFor(It.IsAny<Entity>(), It.IsAny<FSpeedToDelta>(), It.IsAny<Func<string, Result>>()))
			.Returns((Entity _, FSpeedToDelta _, Func<string, Result> play) => {
				var error = play("").Switch<string>(
					errors => errors.system.First(),
					() => "no error, got actual behavior"
				);

				Assert.That(error, Is.EqualTo("LLL"));

				return Result.Ok(Mock.Of<FGetCoroutine>());
			});

		_ = this.moveController.PrepareCoroutineFor(this.agent);
	}

	public void Dispose() {
		GC.SuppressFinalize(this);
	}
}
