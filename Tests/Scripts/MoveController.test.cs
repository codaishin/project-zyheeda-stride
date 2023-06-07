namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using ProjectZyheeda;
using Stride.Engine;
using Xunit;
using Xunit.Sdk;

public class TestMoveController : GameTestCollection {
	private readonly Entity agent;
	private readonly MoveController moveController;
	private readonly IAnimatedMoveEditor move;
	private readonly IAnimation animation;

	public TestMoveController(GameFixture fixture) : base(fixture) {
		this.game.Services.RemoveService<IAnimation>();
		this.game.Services.AddService<IAnimation>(this.animation = Mock.Of<IAnimation>());

		IEnumerable<Result<IWait>> Run() {
			yield break;
		}
		Result Cancel() {
			return Result.Ok();
		}

		this.agent = new Entity();
		this.agent.AddChild(new Entity { new AnimationComponent() });
		this.move = Mock.Of<IAnimatedMoveEditor>();
		this.moveController = new() { move = this.move };

		Mock
			.Get(this.animation)
			.SetReturnsDefault<Result<bool>>(false);
		Mock
			.Get(this.animation)
			.SetReturnsDefault<Result<IPlayingAnimation>>(Result.Ok(Mock.Of<IPlayingAnimation>()));
		Mock
			.Get(this.move)
			.SetReturnsDefault<Result<FGetCoroutine>>(Result.Ok<FGetCoroutine>(_ => (Run, Cancel)));

		this.scene.Entities.Add(new Entity { this.moveController });
		this.scene.Entities.Add(this.agent);

		this.game.WaitFrames(1);
	}

	[Fact]
	public void ReturnMoveResult() {
		var mockGetCoroutine = Mock.Of<FGetCoroutine>();

		_ = Mock
			.Get(this.move)
			.Setup(m => m.PrepareCoroutineFor(this.agent, It.IsAny<FSpeedToDelta>(), It.IsAny<Func<string, Result>>()))
			.Returns(Result.Ok(mockGetCoroutine));

		var getCoroutine = this.moveController
			.PrepareCoroutineFor(this.agent)
			.Switch(
				_ => throw new XunitException("got no coroutine function"),
				getCoroutine => getCoroutine
			);

		Assert.Equal(mockGetCoroutine, getCoroutine);
	}

	[Fact]
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

	[Fact]
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

	[Fact]
	public void MultiplySpeedWithUpdateTimeElapsed() {
		var prepareCoroutineFor = Mock.Of<Func<Entity, FSpeedToDelta, Func<string, Result>, FGetCoroutine>>();

		_ = Mock
			.Get(this.move)
			.Setup(m => m.PrepareCoroutineFor(this.agent, It.IsAny<FSpeedToDelta>(), It.IsAny<Func<string, Result>>()))
			.Returns((Entity _, FSpeedToDelta delta, Func<string, Result> _) => {
				var deltaElapsed = (float)this.game.UpdateTime.Elapsed.TotalSeconds;
				Assert.Equal(42 * deltaElapsed, delta(42));
				return Result.Ok(Mock.Of<FGetCoroutine>());
			});

		_ = this.moveController.PrepareCoroutineFor(this.agent);
	}

	[Fact]
	public void MissingAnimationComponent() {
		this.agent.Name = "Agent";
		this.agent.GetChild(0).RemoveAll<AnimationComponent>();

		var error = this.moveController
			.PrepareCoroutineFor(this.agent)
			.Switch<string>(errors => errors.system.First(), _ => "no error, got actual behavior");

		Assert.Equal("Missing AnimationComponent on Agent", error);
	}

	[Fact]
	public void MissingMoveComponent() {
		this.moveController.move = null;
		var error = this.moveController
			.PrepareCoroutineFor(this.agent)
			.Switch<string>(errors => errors.system.First(), _ => "no error, got actual behavior");

		var missingMove = this.moveController.MissingField(nameof(this.moveController.move));
		Assert.Equal(missingMove, error);
	}

	[Fact]
	public void MoveError() {
		_ = Mock
			.Get(this.move)
			.Setup(m => m.PrepareCoroutineFor(this.agent, It.IsAny<FSpeedToDelta>(), It.IsAny<Func<string, Result>>()))
			.Returns(Result.SystemError("LLL"));

		var error = this.moveController
			.PrepareCoroutineFor(this.agent)
			.Switch<string>(errors => errors.system.First(), _ => "no error, got actual behavior");

		Assert.Equal("LLL", error);
	}

	[Fact]
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

				Assert.Equal("LLL", error);

				return Result.Ok(Mock.Of<FGetCoroutine>());
			});

		_ = this.moveController.PrepareCoroutineFor(this.agent);

	}

	[Fact]
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

				Assert.Equal("LLL", error);

				return Result.Ok(Mock.Of<FGetCoroutine>());
			});

		_ = this.moveController.PrepareCoroutineFor(this.agent);
	}
}
