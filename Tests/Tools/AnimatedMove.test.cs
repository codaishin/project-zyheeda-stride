namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using Xunit;
using Xunit.Sdk;

public class TestAnimatedMove {
	private readonly IMove move;
	private readonly AnimatedMove animatedMove;
	private readonly FGetCoroutine getCoroutine;
	private readonly Func<IEnumerable<Result<IWait>>> run;
	private readonly Cancel cancel;

	private class MockWait : IWait {
		public Task<Result> Wait(ScriptSystem script) {
			throw new NotImplementedException();
		}
	}

	private static FGetCoroutine AssertFail((IEnumerable<SystemError> system, IEnumerable<PlayerError> player) errors) {
		throw new XunitException((
			string.Join(", ", errors.player),
			string.Join(", ", errors.system)
		).ToString());
	}

	public TestAnimatedMove() {
		this.animatedMove = new() { move = this.move = Mock.Of<IMove>() };
		this.getCoroutine = Mock.Of<FGetCoroutine>();
		this.run = Mock.Of<Func<IEnumerable<Result<IWait>>>>();
		this.cancel = Mock.Of<Cancel>();

		Mock
			.Get(this.animatedMove.move)
			.SetReturnsDefault<Result<FGetCoroutine>>(this.getCoroutine);

		Mock
			.Get(this.getCoroutine)
			.SetReturnsDefault<(Func<IEnumerable<Result<IWait>>>, Cancel)>((this.run, this.cancel));

		Mock
			.Get(this.run)
			.SetReturnsDefault<IEnumerable<Result<IWait>>>(new Result<IWait>[] { new MockWait(), new MockWait() });
	}

	[Fact]
	public void UseMovesGetCoroutine() {
		var agent = new Entity();
		var delta = Mock.Of<FSpeedToDelta>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(agent, delta, _ => Result.Ok()).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);

		Mock
			.Get(this.move)
			.Verify(m => m.PrepareCoroutineFor(agent, delta), Times.Once);

		var (run, cancel) = getCoroutine(() => new Vector3(1, 2, 3));

		Mock
			.Get(this.getCoroutine)
			.Verify(getCoroutine => getCoroutine(It.IsAny<Func<Vector3>>()), Times.Once);

		var waits = run().ToArray();
		var innerWaits = waits.Skip(1).Take(waits.Length - 2);

		Assert.All(
			innerWaits.Select(w => w.UnpackOr(new WaitMilliSeconds(0))),
			w => Assert.IsType<MockWait>(w)
		);

		_ = cancel();

		Mock
			.Get(this.cancel)
			.Verify(cancel => cancel(), Times.Once);
	}

	[Fact]
	public void UseNoWaitForStartingAnimations() {
		var agent = new Entity();
		var delta = Mock.Of<FSpeedToDelta>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(agent, delta, _ => Result.Ok()).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);

		Mock
			.Get(this.move)
			.Verify(m => m.PrepareCoroutineFor(agent, delta), Times.Once);

		var (run, cancel) = getCoroutine(() => new Vector3(1, 2, 3));

		Mock
			.Get(this.getCoroutine)
			.Verify(getCoroutine => getCoroutine(It.IsAny<Func<Vector3>>()), Times.Once);

		var waits = run().ToArray();

		Assert.Multiple(() => {
			_ = Assert.IsType<NoWait>(waits.First().UnpackOr(new WaitMilliSeconds(0)));
			_ = Assert.IsType<NoWait>(waits.Last().UnpackOr(new WaitMilliSeconds(0)));
		});
	}

	[Fact]
	public void PlayAnimationWalk() {
		var target = new Vector3(1, 0, 0);
		var play = Mock.Of<Func<string, Result>>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0f, play).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);
		var (run, _) = getCoroutine(() => target);
		var runner = run().GetEnumerator();

		this.animatedMove.animationKey = "walk";
		_ = runner.MoveNext();

		Mock
			.Get(play)
			.Verify(func => func("walk"), Times.Once);
	}

	[Fact]
	public void PlayAnimationRun() {
		var play = Mock.Of<Func<string, Result>>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0f, play).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);
		var (run, _) = getCoroutine(() => new Vector3(1, 0, 0));
		var runner = run().GetEnumerator();

		this.animatedMove.animationKey = "run";
		_ = runner.MoveNext();

		Mock
			.Get(play)
			.Verify(func => func("run"), Times.Once);
	}

	[Fact]
	public void PlayAnimationRunError() {
		var play = (string _) => (Result)Result.PlayerError("ZZZ");
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0f, play).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);
		var (run, _) = getCoroutine(() => new Vector3(1, 0, 0));
		var runner = run().GetEnumerator();

		this.animatedMove.animationKey = "run";
		_ = runner.MoveNext();

		var result = runner.Current;
		result.Switch(
			errors => Assert.Equal("ZZZ", (string)errors.player.First()),
			_ => Assert.Fail("NO ERRORS")
		);
	}

	[Fact(Timeout = 1000)]
	public void PlayAnimationIdleOnDone() {
		var target = new Vector3(0.3f, 0, 0);
		var play = Mock.Of<Func<string, Result>>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0.1f, play).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);
		var (run, _) = getCoroutine(() => target);
		var runner = run().GetEnumerator();

		while (runner.MoveNext()) { }

		Mock
			.Get(play)
			.Verify(func => func(AnimatedMove.fallbackAnimationKey), Times.Once);
	}

	[Fact(Timeout = 1000)]
	public void PlayAnimationErrorOnDone() {
		var target = new Vector3(0.3f, 0, 0);
		var play = Mock.Of<Func<string, Result>>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0.1f, play).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);
		var (run, _) = getCoroutine(() => target);

		_ = Mock
			.Get(play)
			.SetupSequence(p => p(It.IsAny<string>()))
			.Returns(Result.Ok())
			.Returns(Result.PlayerError("UUU"));

		var waits = run().ToArray();

		waits.Last().Switch(
			errors => Assert.Equal("UUU", (string)errors.player.First()),
			_ => Assert.Fail("NO ERRORS")
		);
	}

	[Fact]
	public void PlayIdleOnCancel() {
		var target = new Vector3(1, 0, 0);
		var play = Mock.Of<Func<string, Result>>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0, play).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);
		var (run, cancel) = getCoroutine(() => target);
		var runner = run().GetEnumerator();

		_ = runner.MoveNext();
		_ = cancel();

		Mock
			.Get(play)
			.Verify(func => func(AnimatedMove.fallbackAnimationKey), Times.Once);
	}

	[Fact]
	public void ReturnInnerCancelResult() {
		var target = new Vector3(1, 0, 0);
		var play = Mock.Of<Func<string, Result>>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0, play).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);

		_ = Mock
			.Get(this.cancel)
			.Setup(c => c()).Returns(Result.PlayerError("AAA"));

		var (_, cancel) = getCoroutine(() => target);

		var error = cancel().Switch<string>(
			errors => errors.player.First(),
			() => "no error"
		);
		Assert.Equal("AAA", error);
	}

	[Fact]
	public void ReturnPlayAnimationError() {
		var target = new Vector3(1, 0, 0);
		var play = (string _) => (Result)Result.PlayerError("AAA");
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0, play).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);

		var (_, cancel) = getCoroutine(() => target);

		var error = cancel().Switch<string>(
			errors => errors.player.First(),
			() => "no error"
		);
		Assert.Equal("AAA", error);
	}

	[Fact]
	public void NoMoveSet() {
		this.animatedMove.move = null;
		var play = Mock.Of<Func<string, Result>>();
		var (system, _) = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0, play).Switch(
			error => error,
			value => throw new XunitException("had a value, but shouldn't have had one")
		);

		Assert.Contains(
			(SystemError)this.animatedMove.MissingField(nameof(this.animatedMove.move)),
			system
		);
	}

	[Fact]
	public void ReturnMovePrepareErrors() {
		_ = Mock
			.Get(this.move)
			.Setup(m => m.PrepareCoroutineFor(It.IsAny<Entity>(), It.IsAny<FSpeedToDelta>()))
			.Returns(Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "BBB" })));

		var result = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 1, _ => Result.Ok());
		var errors = result.Switch(
			errors => $"{(string)errors.system.First()}, {(string)errors.player.First()}",
			_ => "no errors"
		);
		Assert.Equal("AAA, BBB", errors);
	}
}
