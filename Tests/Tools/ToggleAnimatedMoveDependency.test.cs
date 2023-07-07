namespace Tests;

using System.Collections.Generic;
using System.Linq
;
using Moq;
using ProjectZyheeda;
using Xunit;
using Xunit.Sdk;

public class TestToggleAnimatedMoveDependency {
	private readonly CharacterDependencies characterDependencies = new() {
		move = Mock.Of<IAnimatedMoveEditor>()
	};
	private readonly ToggleAnimatedMoveDependency toggleMoveController = new() {
		toggleSpeed = new UnitsPerSecond(0),
	};

	public TestToggleAnimatedMoveDependency() : base() {
		_ = Mock
			.Get(this.characterDependencies.move!)
			.Setup(m => m.SetSpeed(It.IsAny<ISpeedEditor>()))
			.Returns(Result.Ok<ISpeedEditor>(new UnitsPerSecond(3)));
		_ = Mock
			.Get(this.characterDependencies.move!)
			.Setup(m => m.SetAnimation(It.IsAny<string>()))
			.Returns(Result.Ok("hover"));

		this.toggleMoveController.target = this.characterDependencies;
	}

	private static (IEnumerable<Result<IWait>> coroutine, Cancel cancel) Fail(string message) {
		throw new XunitException(message);
	}

	[Fact]
	public void ToggleOnce() {
		this.toggleMoveController.toggleSpeed = new UnitsPerSecond(42);
		this.toggleMoveController.toggleAnimationKey = "fly";

		var (coroutine, _) = this.toggleMoveController.GetExecution().Switch(
			_ => TestToggleAnimatedMoveDependency.Fail("no execution"),
			e => e
		);

		var wait = coroutine
			.First()
			.UnpackOr(new WaitFrame());

		Assert.Multiple(
			() => Mock.Get(this.characterDependencies.move!).Verify(m => m.SetSpeed(new UnitsPerSecond(42)), Times.Once),
			() => Mock.Get(this.characterDependencies.move!).Verify(m => m.SetAnimation("fly"), Times.Once),
			() => Assert.Equal(new UnitsPerSecond(3), this.toggleMoveController.toggleSpeed),
			() => Assert.Equal("hover", this.toggleMoveController.toggleAnimationKey),
			() => Assert.IsType<NoWait>(wait)
		);
	}

	[Fact]
	public void MissingMoveController() {
		this.toggleMoveController.target = null;

		var (coroutine, _) = this.toggleMoveController.GetExecution().Switch(
			_ => TestToggleAnimatedMoveDependency.Fail("no execution"),
			e => e
		);

		var error = coroutine
			.First()
			.Switch(
				errors => (string)errors.system.FirstOrDefault(),
				_ => "no error"
			);

		Assert.Equal(
			this.toggleMoveController.MissingField(nameof(this.toggleMoveController.target)),
			error
		);
	}

	[Fact]
	public void MissingMoveOnMoveController() {
		this.toggleMoveController.target!.move = null;

		var (coroutine, _) = this.toggleMoveController.GetExecution().Switch(
			_ => TestToggleAnimatedMoveDependency.Fail("no execution"),
			e => e
		);

		var error = coroutine
			.First()
			.Switch(
				errors => (string)errors.system.FirstOrDefault(),
				_ => "no error"
			);

		Assert.Equal(
			this.characterDependencies.MissingField(nameof(this.characterDependencies.move)),
			error
		);
	}

	[Fact]
	public void NoSpeedSet() {
		this.toggleMoveController.toggleSpeed = null;

		var (coroutine, _) = this.toggleMoveController.GetExecution().Switch(
			_ => TestToggleAnimatedMoveDependency.Fail("no execution"),
			e => e
		);

		var error = coroutine
			.First()
			.Switch(
				errors => (string)errors.system.FirstOrDefault(),
				_ => "no errors"
			);

		Assert.Equal(this.toggleMoveController.MissingField(nameof(this.toggleMoveController.toggleSpeed)), error);
	}

	[Fact]
	public void SetSpeedError() {
		_ = Mock
			.Get(this.characterDependencies.move!)
			.Setup(m => m.SetSpeed(It.IsAny<ISpeedEditor>()))
			.Returns(Result.PlayerError("OO"));

		var (coroutine, _) = this.toggleMoveController.GetExecution().Switch(
			_ => TestToggleAnimatedMoveDependency.Fail("no execution"),
			e => e
		);

		var error = coroutine
			.First()
			.Switch(
				errors => (string)errors.player.FirstOrDefault(),
				_ => "no error"
			);

		Assert.Equal("OO", error);
	}

	[Fact]
	public void SetAnimationError() {
		_ = Mock
			.Get(this.characterDependencies.move!)
			.Setup(m => m.SetAnimation(It.IsAny<string>()))
			.Returns(Result.PlayerError("II"));

		var (coroutine, _) = this.toggleMoveController.GetExecution().Switch(
			_ => TestToggleAnimatedMoveDependency.Fail("no execution"),
			e => e
		);

		var error = coroutine
			.First()
			.Switch(
				errors => (string)errors.player.FirstOrDefault(),
				_ => "no error"
			);

		Assert.Equal("II", error);
	}

	[Fact]
	public void CancelOk() {
		var (_, cancel) = this.toggleMoveController.GetExecution().Switch(
			_ => TestToggleAnimatedMoveDependency.Fail("no execution"),
			e => e
		);

		var ok = cancel().Switch(
			_ => false,
			() => true
		);

		Assert.True(ok);
	}
}
