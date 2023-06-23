namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using ProjectZyheeda;
using Xunit;

public class TestBehaviorToggleController {
	private (Func<IEnumerable<Result<IWait>>>, Cancel) executionA = (
		Mock.Of<Func<IEnumerable<Result<IWait>>>>(),
		Mock.Of<Cancel>()
	);
	private (Func<IEnumerable<Result<IWait>>>, Cancel) executionB = (
		Mock.Of<Func<IEnumerable<Result<IWait>>>>(),
		Mock.Of<Cancel>()
	);
	private readonly ToggleBehaviorController controller = new();

	public TestBehaviorToggleController() {
		var behaviorA = Mock.Of<IBehaviorEditor>();
		var behaviorB = Mock.Of<IBehaviorEditor>();

		_ = Mock
			.Get(behaviorA)
			.Setup(b => b.GetExecution())
			.Returns(Result.Ok(this.executionA));
		_ = Mock
			.Get(behaviorB)
			.Setup(b => b.GetExecution())
			.Returns(Result.Ok(this.executionB));

		this.controller.behaviorA = behaviorA;
		this.controller.behaviorB = behaviorB;
	}

	[Fact]
	public void ReturnExecutionA() {
		var got = this.controller
			.GetExecution()
			.UnpackOr((Mock.Of<Func<IEnumerable<Result<IWait>>>>(), Mock.Of<Cancel>()));

		Assert.Equal(this.executionA, got);
	}

	[Fact]
	public void ReturnExecutionBAfterOneToggle() {
		var toggle = this.controller.GetToggle().Switch(
			e => () => Enumerable.Empty<Result<IWait>>(),
			e => e.coroutine
		);

		foreach (var _ in toggle()) { }

		var got = this.controller
			.GetExecution()
			.UnpackOr((Mock.Of<Func<IEnumerable<Result<IWait>>>>(), Mock.Of<Cancel>()));

		Assert.Equal(this.executionB, got);
	}

	[Fact]
	public void ReturnExecutionAAfterTwoToggles() {
		var toggle = this.controller.GetToggle().Switch(
			e => () => Enumerable.Empty<Result<IWait>>(),
			e => e.coroutine
		);

		foreach (var _ in toggle()) { }
		foreach (var _ in toggle()) { }

		var got = this.controller
			.GetExecution()
			.UnpackOr((Mock.Of<Func<IEnumerable<Result<IWait>>>>(), Mock.Of<Cancel>()));

		Assert.Equal(this.executionA, got);
	}

	[Fact]
	public void CancelReturnsOkay() {
		var cancel = this.controller.GetToggle().Switch(
			e => () => Result.Ok(),
			e => e.cancel
		);

		var ok = cancel().Switch(
			_ => false,
			() => true
		);

		Assert.True(ok);
	}

	[Fact]
	public void BehaviorAMissing() {
		this.controller.behaviorA = null;

		var got = this.controller.GetExecution();
		var errors = got.Switch(
			errors => string.Join(", ", errors.system.Select(e => (string)e)),
			_ => "no errors"
		);

		Assert.Equal(this.controller.MissingField(nameof(this.controller.behaviorA)), errors);
	}

	[Fact]
	public void BehaviorBMissing() {
		this.controller.behaviorB = null;

		var toggle = this.controller.GetToggle().Switch(
			e => () => Enumerable.Empty<Result<IWait>>(),
			e => e.coroutine
		);
		foreach (var _ in toggle()) { }

		var got = this.controller.GetExecution();
		var errors = got.Switch(
			errors => string.Join(", ", errors.system.Select(e => (string)e)),
			_ => "no errors"
		);

		Assert.Equal(this.controller.MissingField(nameof(this.controller.behaviorB)), errors);
	}
}
