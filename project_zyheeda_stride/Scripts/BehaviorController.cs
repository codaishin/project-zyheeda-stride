namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine;
using BehaviorError = U<Requirement, System.Type[], DependencyError>;
using TBehaviorAndEquipmentFn = System.Func<
	IEquipment,
	System.Func<
		Stride.Engine.Entity,
		IEither<
			U<Requirement, System.Type[], DependencyError>,
			(IBehaviorStateMachine, IEquipment)
		>
	>
>;
using TEquipErrorEvents = System.Collections.Generic.List<
	Reference<
		IEvent<
			U<
				Requirement,
				System.Type[],
				DependencyError
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
public enum DependencyError {
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

	private static IEither<IEnumerable<DependencyError>, TBehaviorAndEquipmentFn> GetBehaviorAndEquipmentFn() {
		var getBehaviorAndEquipment =
			(IEquipment equipment) =>
			(Entity agent) =>
				equipment
					.GetBehaviorFor(agent)
					.MapError(e => (BehaviorError)e)
					.Map(behavior => (behavior, equipment));

		return EitherTools
			.New(getBehaviorAndEquipment)
			.WithNoError<IEnumerable<DependencyError>>();
	}

	private void OnError(BehaviorError error) {
		foreach (var onEquipErrorEvent in this.onEquipError) {
			onEquipErrorEvent.Switch(
				some: value => value.Invoke(error),
				none: BehaviorController.Idle
			);
		}
	}

	private void OnEquip(IEquipment equipment) {
		foreach (var onEquipEvent in this.onEquip) {
			onEquipEvent.Switch(
				some: value => value.Invoke(equipment),
				none: BehaviorController.Idle
			);
		}
	}

	private void ResetBehavior(BehaviorError error) {
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

	private static BehaviorError CombineDependencyErrors(
		IEnumerable<DependencyError> dependencies
	) {
		return dependencies.Aggregate((fst, snd) => fst | snd);
	}

	private Action UpdateBehavior(
		Reference<IEquipment> equipment,
		Reference<Entity> agent
	) {
		return () => BehaviorController
			.GetBehaviorAndEquipmentFn()
			.ApplyWeak(equipment.ToEither(error: DependencyError.Equipment))
			.ApplyWeak(agent.ToEither(error: DependencyError.Agent))
			.MapError(BehaviorController.CombineDependencyErrors)
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

	public void Run(U<Vector3, Entity>[] targets) {
		this.behavior.Switch(
			some: b => b.ExecuteNext(targets),
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
