namespace Tests;

using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;
using TBehaviorError = ProjectZyheeda.U<
	ProjectZyheeda.Requirement,
	System.Type[],
	ProjectZyheeda.DependencyError
>;
using TMissing = ProjectZyheeda.U<ProjectZyheeda.Requirement, System.Type[]>;

public class BehaviorControllerTest : GameTestCollection {
	[Test]
	public void PassAgentToGetBehaviorFor() {
		var behavior = Mock.Of<IBehaviorStateMachine>();
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var controller = new BehaviorController();
		var agent = new Entity();

		_ = mEquipment
			.Setup(e => e.GetBehaviorFor(agent))
			.Returns(new Either<TMissing, IBehaviorStateMachine>(behavior));

		controller.agent.Entity = agent;
		controller.equipment.Entity = new Entity {
			(EntityComponent)mEquipment.Object,
		};

		mEquipment.Verify(e => e.GetBehaviorFor(agent), Times.Once());
	}

	[Test]
	public void OnRunExecuteNext() {
		var behavior = Mock.Of<IBehaviorStateMachine>();
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var controller = new BehaviorController();
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 2, 3) }.ToAsyncEnumerable();

		_ = mEquipment
			.Setup(e => e.GetBehaviorFor(It.IsAny<Entity>()))
			.Returns(new Either<TMissing, IBehaviorStateMachine>(behavior));

		controller.agent.Entity = new();
		controller.equipment.Entity = new Entity {
			(EntityComponent)mEquipment.Object,
		};

		controller.Run(targets);

		Mock.Get(behavior).Verify(b => b.ExecuteNext(targets), Times.Once());
	}

	[Test]
	public void OnCancelResetAndIdle() {
		var behavior = Mock.Of<IBehaviorStateMachine>();
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var controller = new BehaviorController();

		_ = mEquipment
			.Setup(e => e.GetBehaviorFor(It.IsAny<Entity>()))
			.Returns(new Either<TMissing, IBehaviorStateMachine>(behavior));

		controller.agent.Entity = new();
		controller.equipment.Entity = new Entity {
			(EntityComponent)mEquipment.Object,
		};

		controller.Reset();

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
			.Returns(new Either<TMissing, IBehaviorStateMachine>(mBehavior.Object));
		_ = mEquipment
			.Setup(e => e.GetBehaviorFor(It.IsNotIn(agent)))
			.Returns(new Either<TMissing, IBehaviorStateMachine>(new[] { typeof(int) }));

		var controller = new BehaviorController();
		controller.equipment.Entity = new Entity { (EntityComponent)mEquipment.Object };
		controller.agent.Entity = agent;
		controller.agent.Entity = new Entity();

		controller.Run(targets);

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
			.Returns(new Either<TMissing, IBehaviorStateMachine>(mBehavior.Object));
		_ = mInvalidEquipment
			.Setup(e => e.GetBehaviorFor(It.IsAny<Entity>()))
			.Returns(new Either<TMissing, IBehaviorStateMachine>(new[] { typeof(int) }));

		var controller = new BehaviorController();
		controller.agent.Entity = new Entity();
		controller.equipment.Entity = new Entity {
			(EntityComponent)mValidEquipment.Object,
		};
		controller.equipment.Entity = new Entity {
			(EntityComponent)mInvalidEquipment.Object,
		};

		controller.Run(targets);

		mBehavior.Verify(b => b.ExecuteNext(targets), Times.Never());
	}

	[Test]
	public void OnErrorEventEquipmentMissing() {
		var controller = new BehaviorController();
		var mEvent = new Mock<EntityComponent>().As<IEvent<TBehaviorError>>();
		var eventRef = new Reference<IEvent<TBehaviorError>> {
			Entity = new Entity { (EntityComponent)mEvent.Object }
		};

		controller.onEquipError.Add(eventRef);
		controller.agent.Entity = new();

		mEvent.Verify(e => e.Invoke(DependencyError.Equipment), Times.Once());
	}

	[Test]
	public void OnErrorEventAgentMissing() {
		var controller = new BehaviorController();
		var mEvent = new Mock<EntityComponent>().As<IEvent<TBehaviorError>>();
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var eventRef = new Reference<IEvent<TBehaviorError>> {
			Entity = new Entity { (EntityComponent)mEvent.Object }
		};

		controller.onEquipError.Add(eventRef);
		controller.equipment.Entity = new Entity {
			(EntityComponent)mEquipment.Object,
		};

		mEvent.Verify(e => e.Invoke(DependencyError.Agent), Times.Once());
	}

	[Test]
	public void OnErrorEventAgentAndEquipmentMissing() {
		var controller = new BehaviorController();
		var mEvent = new Mock<EntityComponent>().As<IEvent<TBehaviorError>>();
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var eventRef = new Reference<IEvent<TBehaviorError>> {
			Entity = new Entity { (EntityComponent)mEvent.Object }
		};

		controller.onEquipError.Add(eventRef);
		controller.equipment.Entity = new Entity {
			(EntityComponent)mEquipment.Object,
		};
		controller.equipment.Entity = null;

		mEvent.Verify(
			e => e.Invoke(DependencyError.Equipment | DependencyError.Agent),
			Times.Once()
		);
	}

	[Test]
	public void OnEquip() {
		var behavior = Mock.Of<IBehaviorStateMachine>();
		var controller = new BehaviorController();
		var mEvent = new Mock<EntityComponent>().As<IEvent<IEquipment>>();
		var mEquipment = new Mock<EntityComponent>().As<IEquipment>();
		var eventRef = new Reference<IEvent<IEquipment>> {
			Entity = new Entity { (EntityComponent)mEvent.Object }
		};

		_ = mEquipment
			.Setup(e => e.GetBehaviorFor(It.IsAny<Entity>()))
			.Returns(new Either<TMissing, IBehaviorStateMachine>(behavior));

		controller.onEquip.Add(eventRef);
		controller.agent.Entity = new();
		controller.equipment.Entity = new Entity {
			(EntityComponent)mEquipment.Object,
		};

		mEvent.Verify(
			e => e.Invoke(mEquipment.Object),
			Times.Once()
		);
	}
}
