namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;
using Xunit.Sdk;

public class BehaviorControllerTest : GameTestCollection {

	private readonly BehaviorController controller;
	private readonly IGetTargetEditor getTarget;

	public BehaviorControllerTest(GameFixture fixture) : base(fixture) {
		this.controller = new BehaviorController {
			getTarget = this.getTarget = Mock.Of<IGetTargetEditor>()
		};
		this.scene.Entities.Add(new Entity { this.controller });

		_ = Mock
			.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Result.Ok(() => Result.Ok(Vector3.Zero)));

		this.game.WaitFrames(2);
	}

	private static (IEnumerable<Result<IWait>>, Cancel) Fail(
		(IEnumerable<SystemError> system, IEnumerable<PlayerError> player) errors
	) {
		throw new XunitException((
			string.Join(", ", errors.system.Select(e => (string)e)),
			string.Join(", ", errors.player.Select(e => (string)e))
		).ToString());
	}


	[Fact]
	public void PassAgentToGetBehaviorFor() {
		var getCoroutine = Mock.Of<FGetCoroutine>();
		var equipment = Mock.Of<IEquipmentEditor>();
		var agent = new Entity();

		_ = Mock
			.Get(equipment)
			.Setup(e => e.PrepareCoroutineFor(agent))
			.Returns(Result.Ok(getCoroutine));

		this.controller.agent = agent;
		this.controller.equipment = equipment;

		_ = this.controller.GetExecution();

		Mock
			.Get(equipment)
			.Verify(e => e.PrepareCoroutineFor(agent), Times.Once());
	}

	[Fact]
	public void ExecuteWithTarget() {
		var getCoroutine = Mock.Of<FGetCoroutine>();
		var equipment = Mock.Of<IEquipmentEditor>();
		var target = Result.Ok(new Vector3(1, 2, 3));

		_ = Mock
			.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Result.Ok(() => target));

		_ = Mock
			.Get(equipment)
			.Setup(e => e.PrepareCoroutineFor(It.IsAny<Entity>()))
			.Returns(Result.Ok(getCoroutine));

		_ = Mock
			.Get(getCoroutine)
			.Setup(getCoroutine => getCoroutine(It.IsAny<Func<Result<Vector3>>>()))
			.Returns((Func<Result<Vector3>> getTarget) => {
				Assert.Equal(target, getTarget());
				return (Enumerable.Empty<Result<IWait>>(), () => Result.Ok());
			});

		this.controller.agent = new();
		this.controller.equipment = equipment;

		_ = this.controller.GetExecution();

		Mock
			.Get(getCoroutine)
			.Verify(func => func(It.IsAny<Func<Result<Vector3>>>()), Times.Once());
	}

	[Fact]
	public void MissingGetTarget() {
		this.controller.agent = new Entity();
		this.controller.equipment = Mock.Of<IEquipmentEditor>();
		this.controller.getTarget = null;

		var result = this.controller.GetExecution();
		var error = result.Switch(
			errors => (string)errors.system.FirstOrDefault(),
			_ => "no error"
		);

		Assert.Equal(this.controller.MissingField(nameof(this.controller.getTarget)), error);
	}

	[Fact]
	public void GetTargetError() {
		this.controller.agent = new Entity();
		this.controller.equipment = Mock.Of<IEquipmentEditor>();

		_ = Mock
			.Get(this.controller.equipment)
			.Setup(e => e.PrepareCoroutineFor(It.IsAny<Entity>()))
			.Returns(Result.Ok(Mock.Of<FGetCoroutine>()));

		_ = Mock
			.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Result.SystemError("AAA"));

		var result = this.controller.GetExecution();
		var error = result.Switch(
			errors => (string)errors.system.FirstOrDefault(),
			_ => "no error"
		);

		Assert.Equal("AAA", error);
	}

	[Fact]
	public void EquipmentMissingOnUse() {
		var target = Vector3.UnitX;
		this.controller.agent = new Entity("Player");

		var (coroutine, _) = this.controller.GetExecution().Switch(
			errors => BehaviorControllerTest.Fail(errors),
			runAndCancel => runAndCancel
		);
		var enumerator = coroutine.GetEnumerator();

		_ = enumerator.MoveNext();
		var error = enumerator.Current.Switch(
			errors => errors.player.First(),
			_ => (PlayerError)"no error"
		);

		Assert.Equal((PlayerError)"nothing equipped", error);
	}

	[Fact]
	public void RequirementsMissing() {
		var equipment = Mock.Of<IEquipmentEditor>();
		var message = "can't use gun";

		_ = Mock
			.Get(equipment)
			.Setup(e => e.PrepareCoroutineFor(It.IsAny<Entity>()))
			.Returns(Result.PlayerError(message));

		this.controller.agent = new();
		this.controller.equipment = equipment;

		var error = this.controller.GetExecution().Switch<string>(
			errors => errors.player.First(),
			_ => "no errors"
		);

		Assert.Equal(message, error);
	}

	[Fact]
	public void AgentMissing() {
		var equipment = Mock.Of<IEquipmentEditor>();
		var message = this.controller.MissingField(nameof(this.controller.agent));

		this.controller.agent = null;
		this.controller.equipment = equipment;

		var error = this.controller.GetExecution().Switch<string>(
			errors => errors.system.First(),
			_ => "no errors"
		);

		Assert.Equal(message, error);
	}

	[Fact]
	public void ReturnBehaviorExecution() {
		var getCoroutine = Mock.Of<FGetCoroutine>();
		var equipment = Mock.Of<IEquipmentEditor>();
		var target = new Vector3(1, 2, 3);
		(IEnumerable<Result<IWait>>, Cancel) execution = (
			Enumerable.Empty<Result<IWait>>(),
			() => Result.Ok()
		);

		_ = Mock
			.Get(equipment)
			.Setup(e => e.PrepareCoroutineFor(It.IsAny<Entity>()))
			.Returns(Result.Ok(getCoroutine));

		_ = Mock.Get(getCoroutine)
			.Setup(func => func(It.IsAny<Func<Result<Vector3>>>()))
			.Returns(execution);

		this.controller.agent = new();
		this.controller.equipment = equipment;

		var gotExecution = this.controller.GetExecution().Switch(
			errors => BehaviorControllerTest.Fail(errors),
			runAndCancel => runAndCancel
		);

		Assert.Equal(execution, gotExecution);
	}
}

public class BehaviorControllerNonGameTest {
	[Fact]
	public void NoEquipmentAssignErrorWhenNotInRunningGame() {
		var controller = new BehaviorController();
		var equipment = Mock.Of<IEquipmentEditor>();
		controller.agent = new Entity();

		var record = Record.Exception(() => controller.equipment = equipment);
		Assert.Null(record);
	}
}
