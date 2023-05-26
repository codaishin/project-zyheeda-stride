namespace Tests;

using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;




public class BehaviorControllerTest : GameTestCollection {
	private ISystemMessage systemMessage = Mock.Of<ISystemMessage>();
	private IPlayerMessage playerMessage = Mock.Of<IPlayerMessage>();
	private BehaviorController controller = new();

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

		_ = this.controller.GetCoroutine(Vector3.Zero);

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

		this.controller.agent = new();
		this.controller.equipment = Maybe.Some(equipment);

		_ = this.controller.GetCoroutine(target);

		Mock.Get(getCoroutine).Verify(func => func(target), Times.Once());
	}

	[Test]
	public void EquipmentMissingOnUse() {
		var equipment = Mock.Of<IEquipment>();
		var target = Vector3.UnitZ;

		this.controller.agent = new Entity("Player");

		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log(new PlayerStr("nothing equipped")), Times.Never);

		var (run, _) = this.controller.GetCoroutine(target);
		var coroutine = run().GetEnumerator();

		_ = coroutine.MoveNext();

		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log(new PlayerStr("nothing equipped")), Times.Once);
	}

	[Test]
	public void EquipmentMissingOnUseBeforeAnythingIsSet() {
		var equipment = Mock.Of<IEquipment>();
		var target = Vector3.UnitX;

		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log(new PlayerStr("nothing equipped")), Times.Never);

		var (run, _) = this.controller.GetCoroutine(target);
		var coroutine = run().GetEnumerator();

		_ = coroutine.MoveNext();

		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log(new PlayerStr("nothing equipped")), Times.Once);
	}

	[Test]
	public void RequirementsMissing() {
		var equipment = Mock.Of<IEquipment>();
		var message = new PlayerStr("can't use gun");

		_ = Mock
			.Get(equipment)
			.Setup(e => e.PrepareCoroutineFor(It.IsAny<Entity>()))
			.Returns(Result.Error(message));

		this.controller.agent = new();
		this.controller.equipment = Maybe.Some(equipment);

		_ = this.controller.GetCoroutine(Vector3.Zero);

		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log(message), Times.Once);
	}

	[Test]
	public void ReturnBehaviorExecution() {
		var getCoroutine = Mock.Of<FGetCoroutine>();
		var equipment = Mock.Of<IEquipment>();
		var target = new Vector3(1, 2, 3);
		(Func<IEnumerable<IWait>>, Action) execution = (
			() => Array.Empty<IWait>(),
			() => { }
		);

		_ = Mock
			.Get(equipment)
			.Setup(e => e.PrepareCoroutineFor(It.IsAny<Entity>()))
			.Returns(Result.Ok(getCoroutine));

		_ = Mock.Get(getCoroutine)
			.Setup(func => func(It.IsAny<U<Vector3, Entity>>()))
			.Returns(execution);

		this.controller.agent = new();
		this.controller.equipment = Maybe.Some(equipment);

		Assert.That(this.controller.GetCoroutine(target), Is.EqualTo(execution));
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
