namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;

public class BehaviorControllerTest : GameTestCollection {
	private ISystemMessage systemMessage = Mock.Of<ISystemMessage>();
	private IPlayerMessage playerMessage = Mock.Of<IPlayerMessage>();
	private BehaviorController controller = new();

	private static (Func<IEnumerable<Result<IWait>>>, Cancel) Fail(
		(IEnumerable<SystemError> system, IEnumerable<PlayerError> player) errors
	) {
		throw new AssertionException((
			string.Join(", ", errors.system.Select(e => (string)e)),
			string.Join(", ", errors.player.Select(e => (string)e))
		).ToString());
	}

	[SetUp]
	public void Setup() {
		this.systemMessage = Mock.Of<ISystemMessage>();
		this.playerMessage = Mock.Of<IPlayerMessage>();
		this.controller = new();

		this.game.Services.RemoveService<ISystemMessage>();
		this.game.Services.RemoveService<IPlayerMessage>();
		this.game.Services.AddService(this.systemMessage);
		this.game.Services.AddService(this.playerMessage);

		this.Scene.Entities.Add(new Entity { this.controller });

		this.game.WaitFrames(2);
	}

	[Test]
	public void PassAgentToGetBehaviorFor() {
		var getCoroutine = Mock.Of<FGetCoroutine>();
		var equipment = Mock.Of<IEquipment>();
		var agent = new Entity();

		_ = Mock
			.Get(equipment)
			.Setup(e => e.PrepareCoroutineFor(agent))
			.Returns(Result.Ok(getCoroutine));

		this.controller.agent = agent;
		this.controller.equipment = Maybe.Some(equipment);

		_ = this.controller.GetCoroutine(() => Vector3.Zero);

		Mock
			.Get(equipment)
			.Verify(e => e.PrepareCoroutineFor(agent), Times.Once());
	}

	[Test]
	public void OnRunExecute() {
		var getCoroutine = Mock.Of<FGetCoroutine>();
		var equipment = Mock.Of<IEquipment>();
		var target = new Vector3(1, 2, 3);

		_ = Mock
			.Get(equipment)
			.Setup(e => e.PrepareCoroutineFor(It.IsAny<Entity>()))
			.Returns(Result.Ok(getCoroutine));

		_ = Mock
			.Get(getCoroutine)
			.Setup(getCoroutine => getCoroutine(It.IsAny<Func<Vector3>>()))
			.Returns((Func<Vector3> getTarget) => {
				Assert.That(getTarget(), Is.EqualTo(target));
				return (() => Array.Empty<Result<IWait>>(), () => Result.Ok());
			});

		this.controller.agent = new();
		this.controller.equipment = Maybe.Some(equipment);

		_ = this.controller.GetCoroutine(() => target);

		Mock
			.Get(getCoroutine)
			.Verify(func => func(It.IsAny<Func<Vector3>>()), Times.Once());
	}

	[Test]
	public void EquipmentMissingOnUse() {
		var target = Vector3.UnitX;
		this.controller.agent = new Entity("Player");

		var (run, _) = this.controller.GetCoroutine(() => target).Switch(
			errors => BehaviorControllerTest.Fail(errors),
			runAndCancel => runAndCancel
		); ;
		var coroutine = run().GetEnumerator();

		_ = coroutine.MoveNext();
		var error = coroutine.Current.Switch(
			errors => errors.player.First(),
			_ => (PlayerError)"no error"
		);

		Assert.That(error, Is.EqualTo((PlayerError)"nothing equipped"));
	}

	[Test]
	public void RequirementsMissing() {
		var equipment = Mock.Of<IEquipment>();
		var message = "can't use gun";

		_ = Mock
			.Get(equipment)
			.Setup(e => e.PrepareCoroutineFor(It.IsAny<Entity>()))
			.Returns(Result.PlayerError(message));

		this.controller.agent = new();
		this.controller.equipment = Maybe.Some(equipment);

		var error = this.controller.GetCoroutine(() => Vector3.Zero).Switch<string>(
			errors => errors.player.First(),
			_ => "no errors"
		);

		Assert.That(error, Is.EqualTo(message));
	}

	[Test]
	public void AgentMissing() {
		var equipment = Mock.Of<IEquipment>();
		var message = this.controller.MissingField(nameof(this.controller.agent));

		this.controller.agent = null;
		this.controller.equipment = Maybe.Some(equipment);

		var error = this.controller.GetCoroutine(() => Vector3.Zero).Switch<string>(
			errors => errors.system.First(),
			_ => "no errors"
		);

		Assert.That(error, Is.EqualTo(message));
	}

	[Test]
	public void ReturnBehaviorExecution() {
		var getCoroutine = Mock.Of<FGetCoroutine>();
		var equipment = Mock.Of<IEquipment>();
		var target = new Vector3(1, 2, 3);
		(Func<IEnumerable<Result<IWait>>>, Cancel) execution = (
			() => Array.Empty<Result<IWait>>(),
			() => Result.Ok()
		);

		_ = Mock
			.Get(equipment)
			.Setup(e => e.PrepareCoroutineFor(It.IsAny<Entity>()))
			.Returns(Result.Ok(getCoroutine));

		_ = Mock.Get(getCoroutine)
			.Setup(func => func(It.IsAny<Func<Vector3>>()))
			.Returns(execution);

		this.controller.agent = new();
		this.controller.equipment = Maybe.Some(equipment);

		var gotExecution = this.controller.GetCoroutine(() => target).Switch(
			errors => BehaviorControllerTest.Fail(errors),
			runAndCancel => runAndCancel
		);

		Assert.That(gotExecution, Is.EqualTo(execution));
	}
}

[TestFixture]
public class BehaviorControllerNonGameTest {
	[Test]
	public void NoEquipmentAssignErrorWhenNotInRunningGame() {
		var controller = new BehaviorController();
		var equipment = Mock.Of<IEquipment>();
		controller.agent = new Entity();

		Assert.DoesNotThrow(
			() => {
				controller.equipment = Maybe.Some(equipment);
			}
		);
	}
}
