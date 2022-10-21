namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Engine;
using TBehaviorAndEquipmentFn = System.Func<
	IEquipment,
	System.Func<
		Stride.Engine.Entity,
		IEither<
			IUnion<Requirement, System.Type[], Dependency>,
			(IBehaviorStateMachine, IEquipment)
		>
	>
>;
using TEquipErrorEvents = System.Collections.Generic.List<
	Reference<
		IEvent<
			IUnion<
				Requirement,
				System.Type[],
				Dependency
			>
		>
	>
>;
using TEquipEvents = System.Collections.Generic.List<
	Reference<
		IEvent<
			IEquipment
		>
	>
>;

[Flags]
public enum Dependency {
	Agent = 0b0001,
	Equipment = 0b0010,
}

public class BehaviorController : StartupScript, IBehavior {
	public readonly EventReference<Reference<IEquipment>, IEquipment> equipment;
	public readonly EventReference<Reference<Entity>, Entity> agent;

	public readonly TEquipErrorEvents onEquipError = new();
	public readonly TEquipEvents onEquip = new();

	private IMaybe<IBehaviorStateMachine> behavior =
		Maybe.None<IBehaviorStateMachine>();

	private static void Idle() { }

	private static IEither<IEnumerable<Dependency>, TBehaviorAndEquipmentFn> GetBehaviorAndEquipmentFn() {
		var func = (IEquipment equipment) => (Entity agent) => equipment
			.GetBehaviorFor(agent)
			.MapError(Union.Expand<Requirement, Type[], Dependency>)
			.Map(behavior => (behavior, equipment));

		return Either
			.New(func)
			.WithNoError<IEnumerable<Dependency>>();
	}

	private void OnError(IUnion<Requirement, System.Type[], Dependency> error) {
		foreach (var @event in this.onEquipError) {
			@event.Switch(
				some: value => value.Invoke(error),
				none: BehaviorController.Idle
			);
		}
	}

	private void OnEquip(IEquipment equipment) {
		foreach (var @event in this.onEquip) {
			@event.Switch(
				some: value => value.Invoke(equipment),
				none: BehaviorController.Idle
			);
		}
	}

	private void ResetBehavior(IUnion<Requirement, System.Type[], Dependency> error) {
		this.behavior = Maybe.None<IBehaviorStateMachine>();
		this.OnError(error);
	}

	private void SetNewBehavior(
		(IBehaviorStateMachine, IEquipment) behaviorAndEquipment
	) {
		var (behavior, equipment) = behaviorAndEquipment;
		this.behavior = Maybe.Some(behavior);
		this.OnEquip(equipment);
	}

	private static Dependency DependencyEnumerableToFlag(
		IEnumerable<Dependency> dependencies
	) {
		return dependencies.Aggregate((fst, snd) => fst | snd);
	}

	private Action UpdateBehavior(
		Reference<IEquipment> equipment,
		Reference<Entity> agent
	) {
		return () => BehaviorController
			.GetBehaviorAndEquipmentFn()
			.Apply(equipment.ToEither(error: Dependency.Equipment))
			.Apply(agent.ToEither(error: Dependency.Agent))
			.MapError(BehaviorController.DependencyEnumerableToFlag)
			.MapError(Union.New<Requirement, Type[], Dependency>)
			.Flatten()
			.Switch(
				error: this.ResetBehavior,
				value: this.SetNewBehavior
			);
	}

	public BehaviorController() {
		var equipment = new Reference<IEquipment>();
		var agent = new Reference<Entity>();
		var onSet = this.UpdateBehavior(equipment, agent);

		this.equipment = new(equipment, onSet);
		this.agent = new(agent, onSet);
	}

	public override void Start() { }

	public void Run() {
		this.behavior.Switch(
			some: b => b.ExecuteNext(),
			none: BehaviorController.Idle
		);
	}

	public void Reset() {
		this.behavior.Switch(
			some: b => b.ResetAndIdle(),
			none: BehaviorController.Idle
		);
	}
}
