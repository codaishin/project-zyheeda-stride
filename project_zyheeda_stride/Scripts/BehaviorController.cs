namespace ProjectZyheeda;

using System;
using Stride.Engine;

using TAgentReference = EventReference<
	Reference<Stride.Engine.Entity>,
	Stride.Engine.Entity
>;
using TEquipmentReference = EventReference<
	Reference<IEquipment>,
	IEquipment
>;

public class BehaviorController : StartupScript, IBehavior {
	public readonly TEquipmentReference Equipment;
	public readonly TAgentReference Agent;

	private IMaybe<IBehaviorStateMachine> behavior =
		Maybe.None<IBehaviorStateMachine>();

	private static void Idle() { }
	private static void Idle<T>(T _) { }

	private static Func<Entity, IMaybe<IBehaviorStateMachine>> GetBehavior(
		IEquipment equipment
	) {
		return agent => equipment.GetBehaviorFor(agent);
	}

	private static Action Empty(IReference reference) {
		return () => reference.Entity = null;
	}

	private Action UpdateBehavior(
		Reference<IEquipment> equipment,
		Reference<Entity> agent
	) {
		return () => {
			this.behavior = equipment
				.Map(BehaviorController.GetBehavior)
				.Apply(agent)
				.FlatMap();
			this.behavior.Switch(
				some: BehaviorController.Idle,
				none: BehaviorController.Empty(equipment)
			);
		};
	}

	public BehaviorController() {
		var equipment = new Reference<IEquipment>();
		var agent = new Reference<Entity>();
		var onSet = this.UpdateBehavior(equipment, agent);

		this.Equipment = new(equipment, onSet);
		this.Agent = new(agent, onSet);
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
