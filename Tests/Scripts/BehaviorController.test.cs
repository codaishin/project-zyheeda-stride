namespace Tests;

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

	private static Either<IEnumerable<U<SystemString, PlayerString>>, IBehaviorStateMachine> Behavior(
		IBehaviorStateMachine behavior
	) {
		return new Either<IEnumerable<U<SystemString, PlayerString>>, IBehaviorStateMachine>(behavior);
	}

	private static Either<IEnumerable<U<SystemString, PlayerString>>, IBehaviorStateMachine> SystemErrors(
		params string[] errors
	) {
		return new Either<IEnumerable<U<SystemString, PlayerString>>, IBehaviorStateMachine>(
			errors.Select(e => (U<SystemString, PlayerString>)new SystemString(e))
		);
	}

	private static Either<IEnumerable<U<SystemString, PlayerString>>, IBehaviorStateMachine> PlayerErrors(
		params string[] errors
	) {
		return new Either<IEnumerable<U<SystemString, PlayerString>>, IBehaviorStateMachine>(
			errors.Select(e => (U<SystemString, PlayerString>)new PlayerString(e))
		);
	}

	[Test]
	public void PassAgentToGetBehaviorFor() {
		var behavior = Mock.Of<IBehaviorStateMachine>();
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var agent = new Entity();

		_ = mEquipment
			.Setup(e => e.GetBehaviorFor(agent))
			.Returns(BehaviorControllerTest.Behavior(behavior));

		this.controller.agent.Entity = agent;
		this.controller.equipment.Entity = new Entity {
			(EntityComponent)mEquipment.Object,
		};

		mEquipment.Verify(e => e.GetBehaviorFor(agent), Times.Once());
	}

	[Test]
	public void OnRunExecuteNext() {
		var behavior = Mock.Of<IBehaviorStateMachine>();
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 2, 3) }.ToAsyncEnumerable();

		_ = mEquipment
			.Setup(e => e.GetBehaviorFor(It.IsAny<Entity>()))
			.Returns(BehaviorControllerTest.Behavior(behavior));

		this.controller.agent.Entity = new();
		this.controller.equipment.Entity = new Entity {
			(EntityComponent)mEquipment.Object,
		};

		this.controller.Run(targets);

		Mock.Get(behavior).Verify(b => b.ExecuteNext(targets), Times.Once());
	}

	[Test]
	public void UpdateBehaviorOnStart() {
		var behavior = Mock.Of<IBehaviorStateMachine>();
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 2, 3) }.ToAsyncEnumerable();
		var agent = new Entity();

		_ = mEquipment
			.Setup(e => e.GetBehaviorFor(It.IsAny<Entity>()))
			.Returns(BehaviorControllerTest.Behavior(behavior));

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
			.Returns(BehaviorControllerTest.Behavior(behavior));

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
		var targets = System.Array.Empty<U<Vector3, Entity>>().ToAsyncEnumerable();

		_ = mEquipment
			.Setup(e => e.GetBehaviorFor(agent))
			.Returns(BehaviorControllerTest.Behavior(mBehavior.Object));
		_ = mEquipment
			.Setup(e => e.GetBehaviorFor(It.IsNotIn(agent)))
			.Returns(BehaviorControllerTest.SystemErrors(""));

		this.controller.equipment.Entity = new Entity { (EntityComponent)mEquipment.Object };
		this.controller.agent.Entity = agent;
		this.controller.agent.Entity = new Entity();

		this.controller.Run(targets);

		mBehavior.Verify(b => b.ExecuteNext(targets), Times.Never());
	}

	[Test]
	public void NullBehaviorInvalidEquipment() {
		var mBehavior = new Mock<IBehaviorStateMachine>();
		var agent = new Entity();
		var mValidEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var mInvalidEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var targets = System.Array.Empty<U<Vector3, Entity>>().ToAsyncEnumerable();

		_ = mValidEquipment
			.Setup(e => e.GetBehaviorFor(It.IsAny<Entity>()))
			.Returns(BehaviorControllerTest.Behavior(mBehavior.Object));
		_ = mInvalidEquipment
			.Setup(e => e.GetBehaviorFor(It.IsAny<Entity>()))
			.Returns(BehaviorControllerTest.SystemErrors(""));

		this.controller.agent.Entity = new Entity();
		this.controller.equipment.Entity = new Entity {
			(EntityComponent)mValidEquipment.Object,
		};
		this.controller.equipment.Entity = new Entity {
			(EntityComponent)mInvalidEquipment.Object,
		};

		this.controller.Run(targets);

		mBehavior.Verify(b => b.ExecuteNext(targets), Times.Never());
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
		var targets = System.Array.Empty<U<Vector3, Entity>>().ToAsyncEnumerable();

		this.controller.agent.Entity = new Entity("Player");

		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log(new PlayerString("nothing equipped")), Times.Never);

		this.controller.Run(targets);

		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log(new PlayerString("nothing equipped")), Times.Once);
	}

	[Test]
	public void EquipmentMissingOnUseBeforeAnythingIsSet() {
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var targets = System.Array.Empty<U<Vector3, Entity>>().ToAsyncEnumerable();

		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log(new PlayerString("nothing equipped")), Times.Never);

		this.controller.Run(targets);

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
			.Returns(BehaviorControllerTest.PlayerErrors(message.value));

		this.controller.agent.Entity = new();
		this.controller.equipment.Entity = new Entity {
			(EntityComponent)mEquipment.Object,
		};

		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log(message), Times.Once);
	}

	[Test]
	public void ErrorWhenInputMessageSystemsMissing() {
		var behavior = new BehaviorController();
		this.scene.Entities.Add(new Entity { behavior });

		this.game.Services.RemoveService<IPlayerMessage>();

		_ = Assert.Throws<MissingService<IPlayerMessage>>(() => behavior.Start());

		this.game.Services.RemoveService<ISystemMessage>();
		this.game.Services.AddService(this.playerMessage);

		_ = Assert.Throws<MissingService<ISystemMessage>>(() => behavior.Start());
	}
}
