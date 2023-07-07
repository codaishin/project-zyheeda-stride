namespace Tests;

using System;
using System.Linq;
using Moq;
using ProjectZyheeda;
using Stride.Engine;
using Xunit;

public class TestReferenceAnimatedMove {
	private readonly CharacterDependencies dependencies = new() {
		move = Mock.Of<IAnimatedMoveEditor>(),
	};
	private readonly ReferenceAnimatedMoveDependency reference;

	public TestReferenceAnimatedMove() {
		this.reference = new() {
			Target = this.dependencies,
		};
	}

	[Fact]
	public void PrepareCoroutineFor() {
		var agent = new Entity();
		var delta = Mock.Of<FSpeedToDelta>();
		var play = Mock.Of<Func<string, Result>>();
		var getCoroutine = Mock.Of<FGetCoroutine>();

		_ = Mock
			.Get(this.dependencies.move!)
			.Setup(m => m.PrepareCoroutineFor(agent, delta, play))
			.Returns(Result.Ok(getCoroutine));

		Assert.Same(getCoroutine, this.reference.PrepareCoroutineFor(agent, delta, play).UnpackOr(Mock.Of<FGetCoroutine>()));
	}

	[Fact]
	public void PrepareCoroutineForMissingMove() {
		this.dependencies.move = null;

		var error = this.reference.PrepareCoroutineFor(new Entity(), s => s.ToUnitsPerSecond(), _ => Result.Ok()).Switch(
			errors => (string)errors.system.FirstOrDefault(),
			_ => "no error"
		);

		Assert.Equal(this.dependencies.MissingField(nameof(this.dependencies.move)), error);
	}

	[Fact]
	public void SetSpeed() {
		_ = Mock
			.Get(this.dependencies.move!)
			.Setup(m => m.SetSpeed(new UnitsPerSecond(42f)))
			.Returns(Result.Ok<ISpeedEditor>(new UnitsPerSecond(100f)));

		Assert.Equal(
			new UnitsPerSecond(100f),
			this.reference.SetSpeed(new UnitsPerSecond(42f)).UnpackOr(new UnitsPerSecond(-1f))
		);
	}

	[Fact]
	public void SetSpeedMissingMove() {
		this.dependencies.move = null;

		var error = this.reference.SetSpeed(new UnitsPerSecond(42f)).Switch(
			errors => (string)errors.system.FirstOrDefault(),
			_ => "no error"
		);

		Assert.Equal(this.dependencies.MissingField(nameof(this.dependencies.move)), error);
	}

	[Fact]
	public void SetAnimation() {
		_ = Mock
			.Get(this.dependencies.move!)
			.Setup(m => m.SetAnimation("jjj"))
			.Returns(Result.Ok("aaa"));

		Assert.Equal("aaa", this.reference.SetAnimation("jjj").UnpackOr("___"));
	}

	[Fact]
	public void SetAnimationMissingMove() {
		this.dependencies.move = null;

		var error = this.reference.SetAnimation("jjj").Switch(
			errors => (string)errors.system.FirstOrDefault(),
			_ => "no error"
		);

		Assert.Equal(this.dependencies.MissingField(nameof(this.dependencies.move)), error);
	}
}
