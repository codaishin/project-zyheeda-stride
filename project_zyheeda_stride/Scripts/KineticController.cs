namespace ProjectZyheeda;

using System;
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
	private Action cancel = () => { };

	public BaseKineticController(TMove move) : base() {
		this.move = move;
	}

	private void SystemLog(string message) {
		this.EssentialServices.systemMessage.Log(new SystemError(message));
	}

	public override async Task Execute() {
		var systemLogger = this.Game.Services.GetService<ISystemMessage>().ToMaybe();

		if (this.collider is null) {
			this.SystemLog(this.MissingField(nameof(this.collider)));
			return;
		}

		while (this.Game.IsRunning) {
			var collision = await this.collider.NewCollision();
			this.thread?.Cancel();
			this.OnHit?.Invoke(collision.Other(this.collider));
			this.cancel();
		}
	}

	private float Delta(float speed) {
		return (float)this.Game.UpdateTime.Elapsed.TotalSeconds * speed;
	}

	public void Follow(Vector3 start, U<Vector3, Entity> target, float rangeMultiplier) {
		var getCoroutine = this.move.PrepareCoroutineFor(this.Entity, this.Delta);
		var scaledDirection = (target.Position() - start) * this.baseRange * rangeMultiplier;
		(var run, this.cancel) = getCoroutine(start + scaledDirection);

		this.thread?.Cancel();
		this.thread = this.Script.AddTask(async () => {
			foreach (var pause in run()) {
				await pause.Wait(this.Script);
			}
		});
		this.Entity.Transform.Position = start;
	}
}

public class KineticController : BaseKineticController<StraightMove> {
	public KineticController() : base(new StraightMove()) { }
}
