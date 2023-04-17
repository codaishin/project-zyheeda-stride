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
	private readonly struct VoidEquipment : IBehaviorStateMachine {
		private readonly Action<PlayerString> log;

		public VoidEquipment(Action<PlayerString> log) {
			this.log = log;
		}

		public static void ResetAndIdle() { }

		public (Func<Coroutine>, Cancel) GetExecution(U<Vector3, Entity> target) {
			var log = this.log;
			Coroutine run() {
				log(new PlayerString("nothing equipped"));
				yield break;
			}
			void cancel() { }
			return (run, cancel);
		}
	}

	private IMaybe<ISystemMessage> systemMessage = Maybe.None<ISystemMessage>();
	private IMaybe<IPlayerMessage> playerMessage = Maybe.None<IPlayerMessage>();
	private IBehaviorStateMachine behavior;
	private U<SystemString, PlayerString> NoAgentMessage => new SystemString(this.MissingField(nameof(this.agent)));

	public readonly EventReference<Reference<IEquipment>, IEquipment> equipment;
	public readonly EventReference<Reference<Entity>, Entity> agent;

	private void LogMessage(SystemString message) {
		this.systemMessage.Switch(
			l => l.Log(message),
			() => this.Log.Error($"missing system logger: {message.value}")
		);
	}

	private void LogMessage(PlayerString message) {
		this.playerMessage.Switch(
			l => l.Log(message),
			() => this.Log.Error($"missing player logger: {message.value}")
		);
	}

	private void LogMessage(U<SystemString, PlayerString> error) {
		error.Switch(this.LogMessage, this.LogMessage);
	}

	private Either<IEnumerable<U<SystemString, PlayerString>>, TBehaviorFn> GetBehavior {
		get {
			var getBehaviorAndEquipment =
				(Entity agent) =>
					this.equipment.Switch(
						equipment => equipment.GetBehaviorFor(agent),
						() => new VoidEquipment(this.LogMessage)
					);
			return getBehaviorAndEquipment;
		}
	}

	private void ResetBehavior(IEnumerable<U<SystemString, PlayerString>> errors) {
		this.behavior = new VoidEquipment(this.LogMessage);
		foreach (var error in errors) {
			this.LogMessage(error);
		}
	}

	private void SetNewBehavior(IBehaviorStateMachine behavior) {
		this.behavior = behavior;
	}

	private void UpdateBehavior() {
		if (this.Game == null) {
			return;
		}
		this.GetBehavior
			.ApplyWeak(this.agent.MaybeToEither(this.NoAgentMessage))
			.Flatten()
			.Switch(
				error: this.ResetBehavior,
				value: this.SetNewBehavior
			);
	}

	public override void Start() {
		this.systemMessage = this.Game.Services.GetService<ISystemMessage>().ToMaybe();
		this.playerMessage = this.Game.Services.GetService<IPlayerMessage>().ToMaybe();
		this.UpdateBehavior();
	}

	public BehaviorController() {
		var equipment = new Reference<IEquipment>();
		var agent = new Reference<Entity>();

		this.equipment = new(equipment, this.UpdateBehavior);
		this.agent = new(agent, this.UpdateBehavior);
		this.behavior = new VoidEquipment(this.LogMessage);
	}

	public (Func<Coroutine>, Cancel) GetExecution(U<Vector3, Entity> target) {
		return this.behavior.GetExecution(target);
	}
}
