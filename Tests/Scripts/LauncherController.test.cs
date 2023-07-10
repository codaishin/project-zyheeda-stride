namespace Tests;

using System;
using System.Linq;
using Moq;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;
using Xunit.Sdk;

public class LauncherControllerTests : GameTestCollection {
	private readonly LauncherController controller = new();
	private readonly IProjectile projectile = Mock.Of<IProjectile>();
	private readonly IMagazineEditor magazine = Mock.Of<IMagazineEditor>();
	private readonly IAnimation animation = Mock.Of<IAnimation>();
	private readonly Entity agent = new();
	private readonly AnimationComponent agentAnimator = new();

	public LauncherControllerTests(GameFixture fixture) : base(fixture) {
		this.controller.magazine = this.magazine;
		this.controller.spawnProjectileAt = new Entity().Transform;
		this.agent.AddChild(new Entity { this.agentAnimator });

		this.scene.Entities.Add(new Entity { this.controller });
		this.scene.Entities.Add(this.controller.spawnProjectileAt.Entity);

		this.game.Services.RemoveService<IAnimation>();
		this.game.Services.AddService<IAnimation>(this.animation);

		_ = Mock
			.Get(this.magazine)
			.Setup(m => m.GetProjectile())
			.Returns(Result.Ok(this.projectile));

		_ = Mock
			.Get(this.projectile)
			.Setup(p => p.Follow(It.IsAny<Vector3>(), It.IsAny<Func<Result<Vector3>>>(), It.IsAny<float>()))
			.Returns(Result.Ok(this.projectile));

		_ = Mock
			.Get(this.animation)
			.Setup(a => a.Play(It.IsAny<AnimationComponent>(), It.IsAny<string>()))
			.Returns(Result.Ok(Mock.Of<IPlayingAnimation>()));

		this.game.Frames(2).Wait();
	}

	private static FGetCoroutine Fail(string message) {
		throw new XunitException(message);
	}

	[Fact]
	public void WaitPreCast() {
		this.controller.preCastMilliseconds = 42;
		var getCoroutine = this.controller.PrepareCoroutineFor(this.agent).Switch(
			_ => LauncherControllerTests.Fail("got errors"),
			v => v
		);
		var (coroutine, _) = getCoroutine(() => Vector3.Zero);

		var enumerator = coroutine.GetEnumerator();

		Assert.Multiple(
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.True(enumerator.MoveNext()),
			() => {
				var wait = enumerator.Current.UnpackOr(new NoWait());

				var waitMilliSeconds = Assert.IsType<WaitMilliSeconds>(wait);
				Assert.Equal(42, waitMilliSeconds.milliSeconds);
			}
		);
	}

	[Fact]
	public void WaitAfterCast() {
		this.controller.afterCastMilliseconds = 33;
		var getCoroutine = this.controller.PrepareCoroutineFor(this.agent).Switch(
			_ => LauncherControllerTests.Fail("got errors"),
			v => v
		);
		var (coroutine, _) = getCoroutine(() => Vector3.Zero);

		var enumerator = coroutine.GetEnumerator();

		Assert.Multiple(
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.True(enumerator.MoveNext()),
			() => {
				var wait = enumerator.Current.UnpackOr(new NoWait());

				var waitMilliSeconds = Assert.IsType<WaitMilliSeconds>(wait);
				Assert.Equal(33, waitMilliSeconds.milliSeconds);
			}
		);
	}

	[Fact]
	public void PlayAnimation() {
		this.controller.animationKey = "shoot";
		var getCoroutine = this.controller.PrepareCoroutineFor(this.agent).Switch(
			_ => LauncherControllerTests.Fail("got errors"),
			v => v
		);
		var (coroutine, _) = getCoroutine(() => Vector3.Zero);

		var enumerator = coroutine.GetEnumerator();

		Assert.Multiple(
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.True(enumerator.MoveNext()),
			() => Mock.Get(this.animation).Verify(a => a.Play(this.agentAnimator, "shoot"))
		);
	}

	[Fact]
	public void PlayAnimationError() {
		_ = Mock
			.Get(this.animation)
			.Setup(a => a.Play(It.IsAny<AnimationComponent>(), It.IsAny<string>()))
			.Returns(Result.SystemError("BBB"));

		var getCoroutine = this.controller.PrepareCoroutineFor(this.agent).Switch(
			_ => LauncherControllerTests.Fail("got errors"),
			v => v
		);
		var (coroutine, _) = getCoroutine(() => Vector3.Zero);

		var enumerator = coroutine.GetEnumerator();

		Assert.Multiple(
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.Equal("BBB", enumerator.Current.Switch(e => (string)e.system.FirstOrDefault(), _ => "no errors"))
		);
	}

	[Fact]
	public void FallbackAnimationWhenDone() {
		var getCoroutine = this.controller.PrepareCoroutineFor(this.agent).Switch(
			_ => LauncherControllerTests.Fail("got errors"),
			v => v
		);
		var (coroutine, _) = getCoroutine(() => Vector3.Zero);

		var last = Result.Ok(Mock.Of<IWait>());
		foreach (var step in coroutine) {
			last = step;
		}

		Assert.Multiple(
			() => Mock.Get(this.animation).Verify(a => a.Play(this.agentAnimator, LauncherController.fallbackAnimationKey)),
			() => Assert.IsType<NoWait>(last.UnpackOr(new WaitFrame()))
		);
	}

	[Fact]
	public void FallbackAnimationError() {
		_ = Mock
			.Get(this.animation)
			.Setup(a => a.Play(It.IsAny<AnimationComponent>(), LauncherController.fallbackAnimationKey))
			.Returns(Result.SystemError("CCC"));

		var getCoroutine = this.controller.PrepareCoroutineFor(this.agent).Switch(
			_ => LauncherControllerTests.Fail("got errors"),
			v => v
		);
		var (coroutine, _) = getCoroutine(() => Vector3.Zero);

		var last = Result.Ok(Mock.Of<IWait>());
		foreach (var step in coroutine) {
			last = step;
		}

		var error = last.Switch(
			errors => (string)errors.system.FirstOrDefault(),
			_ => "no errors"
		);

		Assert.Equal("CCC", error);
	}

	[Fact]
	public void FallbackAnimationOnCancel() {
		var getCoroutine = this.controller.PrepareCoroutineFor(this.agent).Switch(
			_ => LauncherControllerTests.Fail("got errors"),
			v => v
		);
		var (_, cancel) = getCoroutine(() => Vector3.Zero);

		var result = cancel();

		Assert.Multiple(
			() => Mock.Get(this.animation).Verify(a => a.Play(this.agentAnimator, LauncherController.fallbackAnimationKey)),
			() => Assert.Equal(Result.Ok(), result)
		);
	}

	[Fact]
	public void FallbackAnimationErrorOnCancel() {
		_ = Mock
			.Get(this.animation)
			.Setup(a => a.Play(It.IsAny<AnimationComponent>(), LauncherController.fallbackAnimationKey))
			.Returns(Result.SystemError("OOO"));

		var getCoroutine = this.controller.PrepareCoroutineFor(this.agent).Switch(
			_ => LauncherControllerTests.Fail("got errors"),
			v => v
		);
		var (_, cancel) = getCoroutine(() => Vector3.Zero);

		var error = cancel().Switch(
			errors => (string)errors.system.FirstOrDefault(),
			() => "no errors"
		);

		Assert.Equal("OOO", error);
	}

	[Fact]
	public async void FollowTargetOk() {
		var target = () => Result.Ok(new Vector3(1, 2, 3));
		var spawn = new Vector3(5, 6, 7);
		this.controller.rangeModifier = 2f;
		this.controller.spawnProjectileAt!.Position = spawn;

		await this.game.Frames(1);

		var getCoroutine = this.controller.PrepareCoroutineFor(this.agent).Switch(
			_ => LauncherControllerTests.Fail("got errors"),
			v => v
		);
		var (coroutine, _) = getCoroutine(target);

		var enumerator = coroutine.GetEnumerator();

		Assert.Multiple(
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.True(enumerator.MoveNext()),
			() => Mock.Get(this.projectile).Verify(p => p.Follow(spawn, target, this.controller.rangeModifier), Times.Once),
			() => Assert.IsType<NoWait>(enumerator.Current.UnpackOr(new WaitFrame()))
		);
	}

	[Fact]
	public async void FollowTargetSpawnWorldPosition() {
		var target = () => Result.Ok(new Vector3(1, 2, 3));
		var spawn = new Vector3(5, 6, 7);
		var spawnEntityParent = new Entity();
		spawnEntityParent.AddChild(new Entity());

		this.scene.Entities.Add(spawnEntityParent);

		this.controller.rangeModifier = 2f;
		this.controller.spawnProjectileAt = spawnEntityParent.GetChild(0).Transform;
		spawnEntityParent.Transform.Position = spawn;

		await this.game.Frames(1);

		var getCoroutine = this.controller.PrepareCoroutineFor(this.agent).Switch(
			_ => LauncherControllerTests.Fail("got errors"),
			v => v
		);
		var (coroutine, _) = getCoroutine(target);

		var enumerator = coroutine.GetEnumerator();

		Assert.Multiple(
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.True(enumerator.MoveNext()),
			() => Mock.Get(this.projectile).Verify(p => p.Follow(spawn, target, this.controller.rangeModifier), Times.Once),
			() => Assert.IsType<NoWait>(enumerator.Current.UnpackOr(new WaitFrame()))
		);
	}

	[Fact]
	public void LookAtTargetXZ() {
		var target = () => Result.Ok(new Vector3(1, 2, 3));
		this.controller.rangeModifier = 2f;

		var getCoroutine = this.controller.PrepareCoroutineFor(this.agent).Switch(
			_ => LauncherControllerTests.Fail("got errors"),
			v => v
		);
		var (coroutine, _) = getCoroutine(target);

		var enumerator = coroutine.GetEnumerator();

		var lookVector = new Vector3(1, 0, 3);
		lookVector.Normalize();

		Assert.Multiple(
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.IsType<NoWait>(enumerator.Current.UnpackOr(new WaitMilliSeconds(1))),
			() => Assert.Equal(Quaternion.LookRotation(lookVector, Vector3.UnitY), this.agent.Transform.Rotation)
		);
	}

	[Fact]
	public void LookAtTargetXZWithAgentY() {
		var target = () => Result.Ok(new Vector3(1, 2, 3));
		this.controller.rangeModifier = 2f;
		this.agent.Transform.Position = Vector3.UnitY;

		var getCoroutine = this.controller.PrepareCoroutineFor(this.agent).Switch(
			_ => LauncherControllerTests.Fail("got errors"),
			v => v
		);
		var (coroutine, _) = getCoroutine(target);

		var enumerator = coroutine.GetEnumerator();
		_ = enumerator.MoveNext();

		var lookVector = new Vector3(1, 0, 3);
		lookVector.Normalize();

		Assert.Equal(
			Quaternion.LookRotation(lookVector, Vector3.UnitY),
			this.agent.Transform.Rotation
		);
	}

	[Fact]
	public void LookAtTargetXZWithAgentNotAtZero() {
		var target = () => Result.Ok(new Vector3(5, 6, 7));
		this.controller.rangeModifier = 2f;
		this.agent.Transform.Position = Vector3.One;

		var getCoroutine = this.controller.PrepareCoroutineFor(this.agent).Switch(
			_ => LauncherControllerTests.Fail("got errors"),
			v => v
		);
		var (coroutine, _) = getCoroutine(target);

		var enumerator = coroutine.GetEnumerator();
		_ = enumerator.MoveNext();

		var lookVector = new Vector3(4, 0, 6);
		lookVector.Normalize();

		Assert.Equal(
			Quaternion.LookRotation(lookVector, Vector3.UnitY),
			this.agent.Transform.Rotation
		);
	}

	[Fact]
	public void LookAtTargetError() {
		this.controller.rangeModifier = 2f;
		this.agent.Transform.Position = Vector3.UnitY;

		var getCoroutine = this.controller.PrepareCoroutineFor(this.agent).Switch(
			_ => LauncherControllerTests.Fail("got errors"),
			v => v
		);
		var (coroutine, _) = getCoroutine(() => Result.SystemError("AAA"));

		var enumerator = coroutine.GetEnumerator();

		Assert.Multiple(
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.Equal("AAA", enumerator.Current.Switch(e => (string)e.system.FirstOrDefault(), _ => "no error"))
		);

	}

	[Fact]
	public void FollowTargetError() {
		_ = Mock
			.Get(this.projectile)
			.Setup(p => p.Follow(It.IsAny<Vector3>(), It.IsAny<Func<Result<Vector3>>>(), It.IsAny<float>()))
			.Returns(Result.PlayerError("AAA"));

		var target = () => Result.Ok(new Vector3(1, 2, 3));
		var getCoroutine = this.controller.PrepareCoroutineFor(this.agent).Switch(
			_ => LauncherControllerTests.Fail("got errors"),
			v => v
		);
		var (coroutine, _) = getCoroutine(target);

		var enumerator = coroutine.GetEnumerator();

		Assert.Multiple(
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.True(enumerator.MoveNext()),
			() => Assert.Equal("AAA", enumerator.Current.Switch(e => (string)e.player.FirstOrDefault(), _ => "no error"))
		);
	}

	[Fact]
	public void MagazineMissing() {
		this.controller.magazine = null;

		var error = this.controller.PrepareCoroutineFor(this.agent).Switch(
			errors => (string)errors.system.FirstOrDefault(),
			v => "no errors"
		);

		Assert.Equal(this.controller.MissingField(nameof(this.controller.magazine)), error);
	}

	[Fact]
	public void MissingSpawn() {
		this.controller.spawnProjectileAt = null;
		var error = this.controller.PrepareCoroutineFor(this.agent).Switch(
			errors => (string)errors.system.FirstOrDefault(),
			v => "no errors"
		);

		Assert.Equal(this.controller.MissingField(nameof(this.controller.spawnProjectileAt)), error);
	}

	[Fact]
	public void MissingAnimationComponentOnAgent() {
		var agent = new Entity();
		var error = this.controller.PrepareCoroutineFor(agent).Switch(
			errors => (string)errors.system.FirstOrDefault(),
			v => "no errors"
		);

		Assert.Equal(agent.MissingComponent(nameof(AnimationComponent)), error);
	}

	[Fact]
	public void CanHandleAnimationComponentNotFirstChildOfAgent() {
		var agent = new Entity();
		agent.AddChild(new Entity());
		agent.AddChild(new Entity { new AnimationComponent() });

		var ok = this.controller.PrepareCoroutineFor(agent).Switch(
			_ => false,
			_ => true
		);

		Assert.True(ok);
	}
}
