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

	private Action<(IEquipment equipment, Entity agent)> SetOrClearIncompatible(
		Reference<IEquipment> equipment
	) {
		return pair => pair
			.equipment
			.GetBehaviorFor(pair.agent)
			.Match(
				some: behavior => this.behavior = Maybe.Some(behavior),
				none: () => {
					equipment.Entity = null;
					this.Clear();
				}
			);
	}

	private void Clear() {
		this.behavior = Maybe.None<IBehaviorStateMachine>();
	}

	private static IMaybe<(IEquipment, Entity)> BothOrNone(
		Reference<IEquipment> equipment,
		Reference<Entity> agent
	) {
		return agent.Bind(a => equipment.Map(e => (e, a)));
	}

	private Action SetBehavior(
		Reference<IEquipment> equipment,
		Reference<Entity> agent
	) {
		return () => BehaviorController
			.BothOrNone(equipment, agent)
			.Match(
				some: this.SetOrClearIncompatible(equipment),
				none: this.Clear
			);
	}

	public BehaviorController() {
		var equipment = new Reference<IEquipment>();
		var agent = new Reference<Entity>();
		var setBehavior = this.SetBehavior(equipment, agent);

		this.Equipment = new(equipment, onSet: setBehavior);
		this.Agent = new(agent, onSet: setBehavior);
	}

	public override void Start() { }

	public void Run() {
		this.behavior.Match(b => b.ExecuteNext());
	}

	public void Reset() {
		this.behavior.Match(b => b.ResetAndIdle());
	}
}
