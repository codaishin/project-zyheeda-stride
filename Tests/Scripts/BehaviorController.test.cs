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

	private class MockEvent<T> : EntityComponent, IEvent<T> {
		public Action<T> invoke = _ => { };
		public void Invoke(T data) {
			this.invoke(data);
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

		controller.agent.Entity = expectedAgent;
		controller.equipment.Entity = new Entity { equipment };

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

		controller.agent.Entity = new();
		controller.equipment.Entity = new Entity { equipment };

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

		controller.agent.Entity = new();
		controller.equipment.Entity = new Entity { equipment };

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
		controller.equipment.Entity = new Entity { equipment };
		controller.agent.Entity = validAgent;

		controller.agent.Entity = new Entity();

		controller.Run();
		Assert.Multiple(() => {
			Assert.That(controller.equipment.Entity, Is.Null);
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
		controller.agent.Entity = new Entity();
		controller.equipment.Entity = new Entity { validEquipment };

		controller.equipment.Entity = new Entity { invalidEquipment };

		controller.Run();
		Assert.Multiple(() => {
			Assert.That(controller.equipment.Entity, Is.Null);
			Assert.That(called, Is.EqualTo(0));
		});
	}

	[Test]
	public void OnErrorEventEquipmentMissing() {
		var called = Union.New<Requirement, Type[], Dependency>(Dependency.Agent);
		var controller = new BehaviorController();
		var onErrorEntity = new Entity {
			new MockEvent<IUnion<Requirement, Type[], Dependency>> {
				invoke = data => called = data
			},
		};
		controller.onEquipError.Add(
			new Reference<IEvent<IUnion<Requirement, Type[], Dependency>>> {
				Entity = onErrorEntity
			}
		);

		controller.agent.Entity = new();

		var dependency = called.Switch(
			(Requirement _) => Dependency.Agent,
			(Type[] _) => Dependency.Agent,
			(Dependency d) => d
		);
		Assert.That(dependency, Is.EqualTo(Dependency.Equipment));
	}

	[Test]
	public void OnErrorEventAgentMissing() {
		var called = Union.New<Requirement, Type[], Dependency>(Dependency.Equipment);
		var equipment = new MockEquipment();
		var controller = new BehaviorController();
		var onErrorEntity = new Entity {
			new MockEvent<IUnion<Requirement, Type[], Dependency>> {
				invoke = data => called = data
			},
		};
		controller.onEquipError.Add(
			new Reference<IEvent<IUnion<Requirement, Type[], Dependency>>> {
				Entity = onErrorEntity
			}
		);

		controller.equipment.Entity = new Entity { new MockEquipment() };

		var dependency = called.Switch(
			(Requirement _) => Dependency.Equipment,
			(Type[] _) => Dependency.Equipment,
			(Dependency d) => d
		);
		Assert.That(dependency, Is.EqualTo(Dependency.Agent));
	}

	[Test]
	public void OnErrorEventAgentAndEquipmentMissing() {
		var called = Union.New<Requirement, Type[], Dependency>((Dependency)0);
		var controller = new BehaviorController();
		var onErrorEntity = new Entity {
			new MockEvent<IUnion<Requirement, Type[], Dependency>> {
				invoke = data => called = data
			},
		};
		controller.onEquipError.Add(
			new Reference<IEvent<IUnion<Requirement, Type[], Dependency>>> {
				Entity = onErrorEntity
			}
		);

		controller.equipment.Entity = new Entity { new MockEquipment() };

		controller.equipment.Entity = null;

		var dependency = called.Switch(
			(Requirement _) => (Dependency)0,
			(Type[] _) => (Dependency)0,
			(Dependency d) => d
		);

		Assert.That(dependency, Is.EqualTo(Dependency.Agent | Dependency.Equipment));
	}

	[Test]
	public void OnEquip() {
		var called = null as IEquipment;
		var equipment = new MockEquipment();
		var controller = new BehaviorController();
		var onEquipEntity = new Entity {
			new MockEvent<IEquipment> {
				invoke = data => called = data
			},
		};
		controller.onEquip.Add(
			new Reference<IEvent<IEquipment>> {
				Entity = onEquipEntity
			}
		);
		controller.agent.Entity = new();
		controller.equipment.Entity = new Entity { equipment };

		Assert.That(called, Is.SameAs(equipment));
	}
}
