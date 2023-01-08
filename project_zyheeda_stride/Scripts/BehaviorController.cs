namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using TBehaviorFn = System.Func<
	Stride.Engine.Entity,
	Either<
		System.Collections.Generic.IEnumerable<U<SystemString, PlayerString>>,
		IBehaviorStateMachine
	>
>;

public class BehaviorController : StartupScript, IBehavior {
	private class VoidEquipment : IBehaviorStateMachine {
		private readonly IPlayerMessage? playerMessage;

		public VoidEquipment(IPlayerMessage? playerMessage) {
			this.playerMessage = playerMessage;
		}

		public void ExecuteNext(IAsyncEnumerable<U<Vector3, Entity>> _) {
			this.playerMessage?.Log(new PlayerString("nothing equipped"));
		}
		public void ResetAndIdle() { }
	}

	private readonly Action updateBehavior;
	public readonly EventReference<Reference<IEquipment>, IEquipment> equipment;
	public readonly EventReference<Reference<Entity>, Entity> agent;
	private ISystemMessage? systemMessage;
	private IPlayerMessage? playerMessage;

	private IBehaviorStateMachine behavior = new VoidEquipment(null);

	private U<SystemString, PlayerString> NoAgentMessage => new SystemString(this.MissingField(nameof(this.agent)));

	private Either<IEnumerable<U<SystemString, PlayerString>>, TBehaviorFn> GetBehavior {
		get {
			var getBehaviorAndEquipment =
				(Entity agent) =>
					this.equipment.Switch(
						equipment => equipment.GetBehaviorFor(agent),
						() => new VoidEquipment(this.playerMessage)
					);
			return getBehaviorAndEquipment;
		}
	}

	private void ResetBehavior(IEnumerable<U<SystemString, PlayerString>> errors) {
		this.behavior = new VoidEquipment(this.playerMessage);
		foreach (var error in errors) {
			error
				.Switch<Action>(e => () => this.systemMessage?.Log(e), e => () => this.playerMessage?.Log(e))
				.Invoke();
		}
	}

	private void SetNewBehavior(IBehaviorStateMachine behavior) {
		this.behavior = behavior;
	}

	private Action UpdateBehaviorFor(Reference<Entity> agent) {
		return () => {
			this.GetBehavior
				.ApplyWeak(agent.MaybeToEither(this.NoAgentMessage))
				.Flatten()
				.Switch(
					error: this.ResetBehavior,
					value: this.SetNewBehavior
				);
		};
	}

	public override void Start() {
		this.systemMessage = this.Game.Services.GetService<ISystemMessage>();
		if (this.systemMessage is null) {
			throw new MissingService<ISystemMessage>();
		}
		this.playerMessage = this.Game.Services.GetService<IPlayerMessage>();
		if (this.playerMessage is null) {
			throw new MissingService<IPlayerMessage>();
		}
		this.updateBehavior();
	}

	public BehaviorController() {
		var equipment = new Reference<IEquipment>();
		var agent = new Reference<Entity>();
		this.updateBehavior = this.UpdateBehaviorFor(agent);

		this.equipment = new(equipment, this.updateBehavior);
		this.agent = new(agent, this.updateBehavior);
	}

	public void Run(IAsyncEnumerable<U<Vector3, Entity>> targets) {
		this.behavior.ExecuteNext(targets);
	}

	public void Reset() {
		this.behavior.ResetAndIdle();
	}
}
