namespace Tests;

using System;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Engine;

public class BehaviorControllerTest : GameTestCollection {
	private class MockBehavior : IBehaviorStateMachine {
		public Action executeNext = () => { };
		public Action resetAndIdle = () => { };

		public void ExecuteNext() {
			this.executeNext();
		}

		public void ResetAndIdle() {
			this.resetAndIdle();
		}
	}

	private class MockEquipment : EntityComponent, IEquipment {
		public delegate IMaybe<IBehaviorStateMachine> GetBehaviorFn(Entity agent);

		public GetBehaviorFn getBehaviorFor =
			_ => Maybe.Some<IBehaviorStateMachine>(new MockBehavior());

		public IMaybe<IBehaviorStateMachine> GetBehaviorFor(Entity agent) {
			return this.getBehaviorFor(agent);
		}
	}

	private static void AssignTo(
		BehaviorController controller,
		MockEquipment equipment,
		Entity agent,
		bool reversed
	) {
		var equipmentEntity = new Entity { equipment };
		if (reversed) {
			controller.Agent.Entity = agent;
			controller.Equipment.Entity = equipmentEntity;
		} else {
			controller.Equipment.Entity = equipmentEntity;
			controller.Agent.Entity = agent;
		}
	}

	[Test]
	public void PassAgentToGetBehaviorFor([Values(false, true)] bool reversed) {
		var gotAgent = null as Entity;
		var equipment = new MockEquipment {
			getBehaviorFor = agentArg => {
				gotAgent = agentArg;
				return Maybe.Some<IBehaviorStateMachine>(new MockBehavior());
			}
		};
		var controller = new BehaviorController();
		var expectedAgent = new Entity();

		BehaviorControllerTest.AssignTo(controller, equipment, expectedAgent, reversed);

		Assert.That(gotAgent, Is.SameAs(expectedAgent));
	}

	[Test]
	public void OnRunExecuteNext([Values(false, true)] bool reversed) {
		var called = 0;
		var equipment = new MockEquipment {
			getBehaviorFor = _ => Maybe.Some<IBehaviorStateMachine>(
				new MockBehavior { executeNext = () => ++called }
			)
		};
		var controller = new BehaviorController();

		BehaviorControllerTest.AssignTo(controller, equipment, new(), reversed);

		controller.Run();

		Assert.That(called, Is.EqualTo(1));
	}

	[Test]
	public void OnCancelResetAndIdle([Values(false, true)] bool reversed) {
		var called = 0;
		var equipment = new MockEquipment {
			getBehaviorFor = _ => Maybe.Some<IBehaviorStateMachine>(
				new MockBehavior { resetAndIdle = () => ++called }
			)
		};
		var controller = new BehaviorController();

		BehaviorControllerTest.AssignTo(controller, equipment, new(), reversed);

		controller.Reset();

		Assert.That(called, Is.EqualTo(1));
	}

	[Test]
	public void NullEquipmentWhenAssigningInvalidAgent() {
		var called = 0;
		var validAgent = new Entity();
		var equipment = new MockEquipment {
			getBehaviorFor = (a) => a != validAgent
				? Maybe.None<IBehaviorStateMachine>()
				: Maybe.Some<IBehaviorStateMachine>(new MockBehavior {
					executeNext = () => ++called
				})
		};
		var controller = new BehaviorController();
		controller.Equipment.Entity = new Entity { equipment };
		controller.Agent.Entity = validAgent;

		controller.Agent.Entity = new Entity();

		controller.Run();
		Assert.Multiple(() => {
			Assert.That(controller.Equipment.Entity, Is.Null);
			Assert.That(called, Is.EqualTo(0));
		});
	}

	[Test]
	public void NullEquipmentWhenAssigningInvalidEquipment() {
		var called = 0;
		var validEquipment = new MockEquipment {
			getBehaviorFor = _ => Maybe.Some<IBehaviorStateMachine>(
				new MockBehavior { executeNext = () => ++called }
			)
		};
		var invalidEquipment = new MockEquipment {
			getBehaviorFor = _ => Maybe.None<IBehaviorStateMachine>()
		};
		var controller = new BehaviorController();
		controller.Agent.Entity = new Entity();
		controller.Equipment.Entity = new Entity { validEquipment };

		controller.Equipment.Entity = new Entity { invalidEquipment };

		controller.Run();
		Assert.Multiple(() => {
			Assert.That(controller.Equipment.Entity, Is.Null);
			Assert.That(called, Is.EqualTo(0));
		});
	}
}
