namespace Tests;

using System;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Engine;

using TMissing = ProjectZyheeda.IUnion<ProjectZyheeda.Requirement, System.Type[]>;

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
		public delegate IEither<TMissing, MockBehavior> GetBehaviorFn(Entity agent);

		public GetBehaviorFn getBehaviorFor =
			_ => Either
				.New(new MockBehavior())
				.WithNoError<TMissing>();

		public IEither<TMissing, IBehaviorStateMachine> GetBehaviorFor(Entity agent) {
			return this.getBehaviorFor(agent).Switch(
				error => Either
					.New(error)
					.WithNoValue<IBehaviorStateMachine>(),
				value => Either
					.New<IBehaviorStateMachine>(value)
					.WithNoError<TMissing>()
			);
		}
	}

	[Test]
	public void PassAgentToGetBehaviorFor() {
		var gotAgent = null as Entity;
		var equipment = new MockEquipment {
			getBehaviorFor = agentArg => {
				gotAgent = agentArg;
				return Either.New(new MockBehavior()).WithNoError<TMissing>();
			}
		};
		var controller = new BehaviorController();
		var expectedAgent = new Entity();

		controller.Agent.Entity = expectedAgent;
		controller.Equipment.Entity = new Entity { equipment };

		Assert.That(gotAgent, Is.SameAs(expectedAgent));
	}

	[Test]
	public void OnRunExecuteNext() {
		var called = 0;
		var equipment = new MockEquipment {
			getBehaviorFor = _ => Either
				.New(new MockBehavior { executeNext = () => ++called })
				.WithNoError<TMissing>()
		};
		var controller = new BehaviorController();

		controller.Agent.Entity = new();
		controller.Equipment.Entity = new Entity { equipment };

		controller.Run();

		Assert.That(called, Is.EqualTo(1));
	}

	[Test]
	public void OnCancelResetAndIdle() {
		var called = 0;
		var equipment = new MockEquipment {
			getBehaviorFor = _ => Either
				.New(new MockBehavior { resetAndIdle = () => ++called })
				.WithNoError<TMissing>()
		};
		var controller = new BehaviorController();

		controller.Agent.Entity = new();
		controller.Equipment.Entity = new Entity { equipment };

		controller.Reset();

		Assert.That(called, Is.EqualTo(1));
	}

	[Test]
	public void NullEquipmentWhenAssigningInvalidAgent() {
		var called = 0;
		var validAgent = new Entity();
		var equipment = new MockEquipment {
			getBehaviorFor = (a) => a != validAgent
				? Either
					.New(Union.New<Requirement, Type[]>(new[] { typeof(int) }))
					.WithNoValue<MockBehavior>()
				: Either
					.New(new MockBehavior { executeNext = () => ++called })
					.WithNoError<TMissing>()
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
			getBehaviorFor = _ => Either
				.New(new MockBehavior { executeNext = () => ++called })
				.WithNoError<TMissing>()
		};
		var invalidEquipment = new MockEquipment {
			getBehaviorFor = _ => Either
				.New(Union.New<Requirement, Type[]>(new[] { typeof(int) }))
				.WithNoValue<MockBehavior>()
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
