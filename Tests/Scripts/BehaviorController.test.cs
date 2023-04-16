namespace Tests;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;

public class BehaviorControllerTest : GameTestCollection {
	private ISystemMessage systemMessage = Mock.Of<ISystemMessage>();
	private IPlayerMessage playerMessage = Mock.Of<IPlayerMessage>();
	private BehaviorController controller = new();

	private static Either<IEnumerable<U<SystemString, PlayerString>>, IBehaviorStateMachine> EitherWithSystemErrors(
		params string[] errors
	) {
		return new Either<IEnumerable<U<SystemString, PlayerString>>, IBehaviorStateMachine>(
			errors.Select(e => (U<SystemString, PlayerString>)new SystemString(e))
		);
	}

	private static Either<IEnumerable<U<SystemString, PlayerString>>, IBehaviorStateMachine> EitherWithPlayerErrors(
		params string[] errors
	) {
		return new Either<IEnumerable<U<SystemString, PlayerString>>, IBehaviorStateMachine>(
			errors.Select(e => (U<SystemString, PlayerString>)new PlayerString(e))
		);
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

		this.scene.Entities.Add(new Entity { this.controller });

		this.game.WaitFrames(2);
	}

	private static Either<IEnumerable<U<SystemString, PlayerString>>, IBehaviorStateMachine> EitherWithBehavior(
		IBehaviorStateMachine behavior
	) {
		return new Either<IEnumerable<U<SystemString, PlayerString>>, IBehaviorStateMachine>(behavior);
	}

	[Test]
	public void PassAgentToGetBehaviorFor() {
		var behavior = Mock.Of<IBehaviorStateMachine>();
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var agent = new Entity();

		_ = mEquipment
			.Setup(e => e.GetBehaviorFor(agent))
			.Returns(BehaviorControllerTest.EitherWithBehavior(behavior));

		this.controller.agent.Entity = agent;
		this.controller.equipment.Entity = new Entity {
			(EntityComponent)mEquipment.Object,
		};

		mEquipment.Verify(e => e.GetBehaviorFor(agent), Times.Once());
	}

	[Test]
	public void OnRunExecute() {
		var behavior = Mock.Of<IBehaviorStateMachine>();
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var target = new Vector3(1, 2, 3);

		_ = mEquipment
			.Setup(e => e.GetBehaviorFor(It.IsAny<Entity>()))
			.Returns(BehaviorControllerTest.EitherWithBehavior(behavior));

		this.controller.agent.Entity = new();
		this.controller.equipment.Entity = new Entity {
			(EntityComponent)mEquipment.Object,
		};

		_ = this.controller.Run(target);

		Mock.Get(behavior).Verify(b => b.Execute(target), Times.Once());
	}

	[Test]
	public void UpdateBehaviorOnStart() {
		var behavior = Mock.Of<IBehaviorStateMachine>();
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 2, 3) }.ToAsyncEnumerable();
		var agent = new Entity();

		_ = mEquipment
			.Setup(e => e.GetBehaviorFor(It.IsAny<Entity>()))
			.Returns(BehaviorControllerTest.EitherWithBehavior(behavior));

		this.controller.agent.Entity = agent;
		this.controller.equipment.Entity = new Entity {
			(EntityComponent)mEquipment.Object,
		};

		this.controller.Start();

		mEquipment.Verify(e => e.GetBehaviorFor(agent), Times.Exactly(2));
	}

	[Test]
	public void OnCancelResetAndIdle() {
		var behavior = Mock.Of<IBehaviorStateMachine>();
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();

		_ = mEquipment
			.Setup(e => e.GetBehaviorFor(It.IsAny<Entity>()))
			.Returns(BehaviorControllerTest.EitherWithBehavior(behavior));

		this.controller.agent.Entity = new();
		this.controller.equipment.Entity = new Entity {
			(EntityComponent)mEquipment.Object,
		};

		this.controller.Reset();

		Mock.Get(behavior).Verify(b => b.ResetAndIdle(), Times.Once());
	}

	[Test]
	public void NullBehaviorWhenAssigningInvalidAgent() {
		var mBehavior = new Mock<IBehaviorStateMachine>();
		var agent = new Entity();
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var target = Vector3.UnitX;

		_ = mEquipment
			.Setup(e => e.GetBehaviorFor(agent))
			.Returns(BehaviorControllerTest.EitherWithBehavior(mBehavior.Object));
		_ = mEquipment
			.Setup(e => e.GetBehaviorFor(It.IsNotIn(agent)))
			.Returns(BehaviorControllerTest.EitherWithSystemErrors(""));

		this.controller.equipment.Entity = new Entity { (EntityComponent)mEquipment.Object };
		this.controller.agent.Entity = agent;
		this.controller.agent.Entity = new Entity();

		_ = this.controller.Run(target);

		mBehavior.Verify(b => b.Execute(target), Times.Never());
	}

	[Test]
	public void NullBehaviorInvalidEquipment() {
		var mBehavior = new Mock<IBehaviorStateMachine>();
		var agent = new Entity();
		var mValidEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var mInvalidEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var target = Vector3.UnitZ;

		_ = mValidEquipment
			.Setup(e => e.GetBehaviorFor(It.IsAny<Entity>()))
			.Returns(BehaviorControllerTest.EitherWithBehavior(mBehavior.Object));
		_ = mInvalidEquipment
			.Setup(e => e.GetBehaviorFor(It.IsAny<Entity>()))
			.Returns(BehaviorControllerTest.EitherWithSystemErrors(""));

		this.controller.agent.Entity = new Entity();
		this.controller.equipment.Entity = new Entity {
			(EntityComponent)mValidEquipment.Object,
		};
		this.controller.equipment.Entity = new Entity {
			(EntityComponent)mInvalidEquipment.Object,
		};

		_ = this.controller.Run(target);

		mBehavior.Verify(b => b.Execute(target), Times.Never());
	}

	[Test]
	public void AgentMissingOnEquip() {
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();

		this.controller.equipment.Entity = new Entity {
			(EntityComponent)mEquipment.Object,
		};

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log(new SystemString(this.controller.MissingField(nameof(this.controller.agent)))), Times.Exactly(2));  //twice from Start and Equip
	}

	[Test]
	public void EquipmentMissingOnUse() {
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var target = Vector3.UnitZ;

		this.controller.agent.Entity = new Entity("Player");

		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log(new PlayerString("nothing equipped")), Times.Never);

		_ = this.controller.Run(target);

		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log(new PlayerString("nothing equipped")), Times.Once);
	}

	[Test]
	public void EquipmentMissingOnUseBeforeAnythingIsSet() {
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var target = Vector3.UnitX;

		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log(new PlayerString("nothing equipped")), Times.Never);

		_ = this.controller.Run(target);

		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log(new PlayerString("nothing equipped")), Times.Once);
	}

	[Test]
	public void RequirementsMissingOnEquip() {
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var message = new PlayerString("can't use gun");

		_ = mEquipment
			.Setup(e => e.GetBehaviorFor(It.IsAny<Entity>()))
			.Returns(BehaviorControllerTest.EitherWithPlayerErrors(message.value));

		this.controller.agent.Entity = new();
		this.controller.equipment.Entity = new Entity {
			(EntityComponent)mEquipment.Object,
		};

		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log(message), Times.Once);
	}

	[Test]
	public void ReturnBehaviorTask() {
		var behavior = Mock.Of<IBehaviorStateMachine>();
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var target = new Vector3(1, 2, 3);
		var task = Task.FromResult(true);

		_ = mEquipment
			.Setup(e => e.GetBehaviorFor(It.IsAny<Entity>()))
			.Returns(BehaviorControllerTest.EitherWithBehavior(behavior));

		_ = Mock.Get(behavior)
			.Setup(b => b.Execute(It.IsAny<U<Vector3, Entity>>()))
			.Returns(task);

		this.controller.agent.Entity = new();
		this.controller.equipment.Entity = new Entity {
			(EntityComponent)mEquipment.Object,
		};

		Assert.That(this.controller.Run(target), Is.SameAs(task));
	}
}

[TestFixture]
public class BehaviorControllerNonGameTest {
	[Test]
	public void NoEquipmentAssignErrorWhenNotInRunningGame() {
		var controller = new BehaviorController();
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		controller.agent.Entity = new Entity();

		Assert.DoesNotThrow(
			() => {
				controller.equipment.Entity = new Entity {
					(EntityComponent)mEquipment.Object,
				};
			}
		);
	}
}
