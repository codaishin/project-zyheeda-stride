namespace ProjectZyheeda;

using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Core.MicroThreading;
using Stride.Engine;

public abstract class BaseProjectileController<TMove> :
	ProjectZyheedaAsyncScript,
	IProjectile
	where TMove : IMove {

	public PhysicsComponent? collider;

	public readonly TMove move;
	public event Action<PhysicsComponent>? OnHit;

	private MicroThread? thread;
	private Action cancel = () => { };

	public BaseProjectileController(TMove move) : base() {
		this.move = move;
	}

	private void SystemLog(IMaybe<ISystemMessage> systemLogger, string message) {
		systemLogger.Switch(
			l => l.Log(new SystemStr(message)),
			() => this.Log.Error(message)
		);
	}

	public override async Task Execute() {
		var systemLogger = this.Game.Services.GetService<ISystemMessage>().ToMaybe();

		if (this.collider is null) {
			this.SystemLog(systemLogger, this.MissingField(nameof(this.collider)));
			return;
		}

		while (this.Game.IsRunning) {
			var collision = await this.collider.NewCollision();
			this.thread?.Cancel();
			this.OnHit?.Invoke(collision.Other(this.collider));
			this.cancel();
		}
	}

	private FSpeedToDelta ScaleSpeed(float factor) {
		return (speed) => (float)this.Game.UpdateTime.Elapsed.TotalSeconds * speed * factor;
	}

	public void Follow(Vector3 start, U<Vector3, Entity> target, float speedFactor) {
		var getCoroutine = this.move.PrepareCoroutineFor(this.Entity, this.ScaleSpeed(speedFactor));
		(var run, this.cancel) = getCoroutine(target);

		this.thread?.Cancel();
		this.thread = this.Script.AddTask(async () => {
			foreach (var pause in run()) {
				await pause.Wait(this.Script);
			}
		});
		this.Entity.Transform.Position = start;
	}
}

public class ProjectileController : BaseProjectileController<StraightMove> {
	public ProjectileController() : base(new StraightMove()) { }
}
