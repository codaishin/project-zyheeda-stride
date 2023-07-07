namespace ProjectZyheeda;

using System;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Core.MicroThreading;
using Stride.Engine;

public class KineticController : ProjectZyheedaAsyncScript, IProjectile {
	public PhysicsComponent? collider;
	public float baseRange;
	public IMoveEditor? move;
	public event Action<PhysicsComponent>? OnHit;
	public event Action? OnRangeLimit;

	private MicroThread? thread;
	private Cancel cancel = () => Result.Ok();

	private Task LogErrors((SystemErrors system, PlayerErrors player) errors) {
		this.EssentialServices.playerMessage.Log(errors.player.ToArray());
		this.EssentialServices.systemMessage.Log(errors.system.ToArray());
		return Task.CompletedTask;
	}

	private Task WaitPause(IWait pause) {
		return pause.Wait(this.Script).Task;
	}

	public override async Task Execute() {
		var systemLogger = this.Game.Services.GetService<ISystemMessage>().ToMaybe();

		if (this.collider is null) {
			this.EssentialServices.systemMessage.Log(this.MissingField(nameof(this.collider)));
			return;
		}

		while (this.Game.IsRunning) {
			var collision = await this.collider.NewCollision();
			this.thread?.Cancel();
			this.OnHit?.Invoke(collision.Other(this.collider));
			this.cancel().Switch(
				errors => this.LogErrors(errors),
				() => { }
			);
		}
	}

	private float Delta(ISpeed speed) {
		return (float)this.Game.UpdateTime.Elapsed.TotalSeconds * speed.ToUnitsPerSecond();
	}

	private Vector3 ProjectTargetOntoRange(Vector3 start, Vector3 target, float rangeMultiplier) {
		var direction = Vector3.Normalize(target - start);
		return start + (direction * this.baseRange * rangeMultiplier);
	}

	private Result Follow(Vector3 start, Vector3 target, float rangeMultiplier, FGetCoroutine getCoroutine) {
		target = this.ProjectTargetOntoRange(start, target, rangeMultiplier);
		(var coroutine, this.cancel) = getCoroutine(() => target);

		this.thread?.Cancel();
		this.thread = this.Script.AddTask(async () => {
			foreach (var step in coroutine) {
				await step.Switch(this.LogErrors, this.WaitPause);
			}
			this.OnRangeLimit?.Invoke();
		});
		this.Entity.Transform.Position = start;
		return Result.Ok();
	}

	public Result Follow(Vector3 start, Func<Result<Vector3>> getTarget, float rangeMultiplier) {
		var follow =
			(FGetCoroutine getCoroutine) =>
			(Vector3 target) =>
				this.Follow(start, target, rangeMultiplier, getCoroutine);

		var getCoroutine = this.move
			.OkOrSystemError(this.MissingField(nameof(this.move)))
			.FlatMap(m => m.PrepareCoroutineFor(this.Entity, this.Delta));

		return follow
			.Apply(getCoroutine)
			.Apply(getTarget());
	}
}
