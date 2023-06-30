namespace Tests;

using System.Collections.Generic;
using System.Linq
;
using Moq;
using ProjectZyheeda;
using Xunit;
using Xunit.Sdk;

public class TestToggleMoveController {
	private readonly MoveController moveController = new() {
		move = Mock.Of<IAnimatedMoveEditor>()
	};
	private readonly ToggleMoveController toggleMoveController = new();

	public TestToggleMoveController() : base() {
		_ = Mock
			.Get(this.moveController.move!)
			.Setup(m => m.SetSpeed(It.IsAny<float>()))
			.Returns(Result.Ok(3f));
		_ = Mock
			.Get(this.moveController.move!)
			.Setup(m => m.SetAnimation(It.IsAny<string>()))
			.Returns(Result.Ok("hover"));

		this.toggleMoveController.target = this.moveController;
	}

	private static (IEnumerable<Result<IWait>> coroutine, Cancel cancel) Fail(string message) {
		throw new XunitException(message);
	}

	[Fact]
	public void ToggleOnce() {
		this.toggleMoveController.toggleSpeed = 42;
		this.toggleMoveController.toggleAnimationKey = "fly";

		var (coroutine, _) = this.toggleMoveController.GetExecution().Switch(
			_ => TestToggleMoveController.Fail("no execution"),
			e => e
		);

		var wait = coroutine
			.First()
			.UnpackOr(new WaitFrame());

		Assert.Multiple(
			() => Mock.Get(this.moveController.move!).Verify(m => m.SetSpeed(42), Times.Once),
			() => Mock.Get(this.moveController.move!).Verify(m => m.SetAnimation("fly"), Times.Once),
			() => Assert.Equal(3, this.toggleMoveController.toggleSpeed),
			() => Assert.Equal("hover", this.toggleMoveController.toggleAnimationKey),
			() => Assert.IsType<NoWait>(wait)
		);
	}

	[Fact]
	public void MissingMoveController() {
		this.toggleMoveController.target = null;

		var (coroutine, _) = this.toggleMoveController.GetExecution().Switch(
			_ => TestToggleMoveController.Fail("no execution"),
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
			_ => TestToggleMoveController.Fail("no execution"),
			e => e
		);

		var error = coroutine
			.First()
			.Switch(
				errors => (string)errors.system.FirstOrDefault(),
				_ => "no error"
			);

		Assert.Equal(
			this.moveController.MissingField(nameof(this.moveController.move)),
			error
		);
	}

	[Fact]
	public void SetSpeedError() {
		_ = Mock
			.Get(this.moveController.move!)
			.Setup(m => m.SetSpeed(It.IsAny<float>()))
			.Returns(Result.PlayerError("OO"));

		var (coroutine, _) = this.toggleMoveController.GetExecution().Switch(
			_ => TestToggleMoveController.Fail("no execution"),
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
			.Get(this.moveController.move!)
			.Setup(m => m.SetAnimation(It.IsAny<string>()))
			.Returns(Result.PlayerError("II"));

		var (coroutine, _) = this.toggleMoveController.GetExecution().Switch(
			_ => TestToggleMoveController.Fail("no execution"),
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
			_ => TestToggleMoveController.Fail("no execution"),
			e => e
		);

		var ok = cancel().Switch(
			_ => false,
			() => true
		);

		Assert.True(ok);
	}
}
