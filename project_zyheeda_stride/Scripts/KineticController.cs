namespace ProjectZyheeda;

using System;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Core.MicroThreading;
using Stride.Engine;

public abstract class BaseKineticController<TMove> :
	ProjectZyheedaAsyncScript,
	IProjectile
	where TMove : IMove {

	public PhysicsComponent? collider;
	public float baseRange;

	public readonly TMove move;
	public event Action<PhysicsComponent>? OnHit;

	private MicroThread? thread;
	private Cancel cancel = () => Result.Ok();

	public BaseKineticController(TMove move) : base() {
		this.move = move;
	}

	private Task LogErrors((SystemErrors system, PlayerErrors player) errors) {
		this.EssentialServices.playerMessage.Log(errors.player.ToArray());
		this.EssentialServices.systemMessage.Log(errors.system.ToArray());
		return Task.CompletedTask;
	}

	private Task WaitPause(IWait pause) {
		return pause.Wait(this.Script);
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

	private float Delta(float speed) {
		return (float)this.Game.UpdateTime.Elapsed.TotalSeconds * speed;
	}

	public void Follow(Vector3 start, Func<Vector3> getTarget, float rangeMultiplier) {
		var getCoroutine = this.move.PrepareCoroutineFor(this.Entity, this.Delta);
		var scaledDirection = (getTarget() - start) * this.baseRange * rangeMultiplier;
		(var run, this.cancel) = getCoroutine(() => start + scaledDirection);

		this.thread?.Cancel();
		this.thread = this.Script.AddTask(async () => {
			foreach (var pause in run()) {
				await pause.Switch(this.LogErrors, this.WaitPause);
			}
		});
		this.Entity.Transform.Position = start;
	}
}

public class KineticController : BaseKineticController<StraightMove> {
	public KineticController() : base(new StraightMove()) { }
}
