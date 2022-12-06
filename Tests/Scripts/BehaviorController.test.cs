namespace Tests;

using System;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;
using TMissing = ProjectZyheeda.U<ProjectZyheeda.Requirement, System.Type[]>;

public class BehaviorControllerTest : GameTestCollection {
	private class MockBehavior : IBehaviorStateMachine {
		public Action<IMaybe<U<Vector3, Entity>>> executeNext = _ => { };
		public Action resetAndIdle = () => { };

		public void ExecuteNext(IMaybe<U<Vector3, Entity>> target) {
			this.executeNext(target);
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
		var called = Maybe.None<U<Vector3, Entity>>();
		var vector = new Vector3(1, 2, 3);
		var mockTarget = new U<Vector3, Entity>(vector).Apply(Maybe.Some);
		var equipment = new MockEquipment {
			getBehaviorFor = _ => Either
				.New(new MockBehavior { executeNext = (target) => called = target })
				.WithNoError<TMissing>()
		};
		var controller = new BehaviorController();

		controller.agent.Entity = new();
		controller.equipment.Entity = new Entity { equipment };

		controller.Run(mockTarget);

		Assert.That(called, Is.SameAs(mockTarget));
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
	public void OnErrorEventEquipmentMissing() {
		var called = (U<Requirement, Type[], DependencyError>)DependencyError.Agent;
		var controller = new BehaviorController();
		var onErrorEntity = new Entity {
			new MockEvent<U<Requirement, Type[], DependencyError>> {
				invoke = data => called = data
			},
		};
		controller.onEquipError.Add(
			new Reference<IEvent<U<Requirement, Type[], DependencyError>>> {
				Entity = onErrorEntity
			}
		);

		controller.agent.Entity = new();

		var dependency = called.Switch(
			(Requirement _) => DependencyError.Agent,
			(Type[] _) => DependencyError.Agent,
			(DependencyError d) => d
		);
		Assert.That(dependency, Is.EqualTo(DependencyError.Equipment));
	}

	[Test]
	public void OnErrorEventAgentMissing() {
		var called = (U<Requirement, Type[], DependencyError>)DependencyError.Equipment;
		var equipment = new MockEquipment();
		var controller = new BehaviorController();
		var onErrorEntity = new Entity {
			new MockEvent<U<Requirement, Type[], DependencyError>> {
				invoke = data => called = data
			},
		};
		controller.onEquipError.Add(
			new Reference<IEvent<U<Requirement, Type[], DependencyError>>> {
				Entity = onErrorEntity
			}
		);

		controller.equipment.Entity = new Entity { new MockEquipment() };

		var dependency = called.Switch(
			(Requirement _) => DependencyError.Equipment,
			(Type[] _) => DependencyError.Equipment,
			(DependencyError d) => d
		);
		Assert.That(dependency, Is.EqualTo(DependencyError.Agent));
	}

	[Test]
	public void OnErrorEventAgentAndEquipmentMissing() {
		var called = (U<Requirement, Type[], DependencyError>)(DependencyError)0;
		var controller = new BehaviorController();
		var onErrorEntity = new Entity {
			new MockEvent<U<Requirement, Type[], DependencyError>> {
				invoke = data => called = data
			},
		};
		controller.onEquipError.Add(
			new Reference<IEvent<U<Requirement, Type[], DependencyError>>> {
				Entity = onErrorEntity
			}
		);

		controller.equipment.Entity = new Entity { new MockEquipment() };

		controller.equipment.Entity = null;

		var dependency = called.Switch(
			(Requirement _) => (DependencyError)0,
			(Type[] _) => (DependencyError)0,
			(DependencyError d) => d
		);

		Assert.That(dependency, Is.EqualTo(DependencyError.Agent | DependencyError.Equipment));
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
