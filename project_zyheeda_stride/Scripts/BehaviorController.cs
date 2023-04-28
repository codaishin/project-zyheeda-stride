namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;

public class BehaviorController : ProjectZyheedaStartupScript, IBehavior {
	private U<SystemStr, PlayerStr> NoAgentMessage => new SystemStr(this.MissingField(nameof(this.agent)));
	private FGetCoroutine getCoroutine;

	public readonly EventReference<Reference<IEquipment>, IEquipment> equipment;
	public readonly EventReference<Reference<Entity>, Entity> agent;

	private void LogMessage(U<SystemStr, PlayerStr> error) {
		error.Switch(
			this.EssentialServices.systemMessage.Log,
			this.EssentialServices.playerMessage.Log
		);
	}

	private Either<Errors, Func<Entity, Either<Errors, FGetCoroutine>>> GetBehavior {
		get {
			var getBehaviorAndEquipment =
				(Entity agent) =>
					this.equipment.Switch(
						equipment => equipment.PrepareCoroutineFor(agent),
						() => (FGetCoroutine)this.NothingEquipped
					);
			return getBehaviorAndEquipment;
		}
	}

	private (Func<Coroutine>, Cancel) NothingEquipped(U<Vector3, Entity> target) {
		Coroutine run() {
			this.LogMessage(new PlayerStr("nothing equipped"));
			yield break;
		}
		void cancel() { }
		return (run, cancel);
	}

	private void ResetBehavior(IEnumerable<U<SystemStr, PlayerStr>> errors) {
		this.getCoroutine = this.NothingEquipped;
		foreach (var error in errors) {
			this.LogMessage(error);
		}
	}

	private void SetNewBehavior(FGetCoroutine getCoroutine) {
		this.getCoroutine = getCoroutine;
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
		this.UpdateBehavior();
	}

	public BehaviorController() {
		var equipment = new Reference<IEquipment>();
		var agent = new Reference<Entity>();

		this.equipment = new(equipment, this.UpdateBehavior);
		this.agent = new(agent, this.UpdateBehavior);
		this.getCoroutine = this.NothingEquipped;
	}

	public (Func<Coroutine>, Cancel) GetCoroutine(U<Vector3, Entity> target) {
		return this.getCoroutine(target);
	}
}
