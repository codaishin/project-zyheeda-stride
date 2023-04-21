namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine;


public class MoveController : StartupScript, IEquipment {
	public static readonly string fallbackAnimationKey = "default";

	public float speed;
	public string playAnimation = "";

	private Either<U<SystemStr, PlayerStr>, IGetAnimation> animationGetter = new U<SystemStr, PlayerStr>(
		new SystemStr("No IGetAnimation assigned")
	);

	private static Either<U<SystemStr, PlayerStr>, AnimationComponent> AnimationComponentFromChildren(Entity agent) {
		return agent
			.GetChildren()
			.Select(c => c.Get<AnimationComponent>())
			.FirstOrDefault()
			.ToEither(new U<SystemStr, PlayerStr>(new SystemStr($"Missing AnimationComponent on {agent.Name}")));
	}

	private Either<U<SystemStr, PlayerStr>, IGetAnimation> AnimationGetterFromService() {
		return this
			.Game
			.Services
			.GetService<IGetAnimation>()
			.ToEither(new U<SystemStr, PlayerStr>(new SystemStr("Missing IGetAnimation Service")));
	}

	public override void Start() {
		this.animationGetter = this.AnimationGetterFromService();
	}

	public Either<IEnumerable<U<SystemStr, PlayerStr>>, FGetCoroutine> PrepareCoroutineFor(Entity agent) {
		var getBehavior =
			(IGetAnimation getAnimation) =>
			(AnimationComponent animationComponent) => (FGetCoroutine)new Behavior(
				this,
				getAnimation,
				agent.Transform,
				animationComponent
			).GetExecution;

		var animationComponent = MoveController.AnimationComponentFromChildren(agent);
		return getBehavior
			.ApplyWeak(this.animationGetter)
			.ApplyWeak(animationComponent);
	}

	private class Behavior {
		private readonly MoveController move;
		private readonly IGetAnimation animationGetter;
		private readonly TransformComponent agentTransform;
		private readonly AnimationComponent agentAnimation;

		public Behavior(
			MoveController move,
			IGetAnimation animationGetter,
			TransformComponent agentTransform,
			AnimationComponent agentAnimation
		) {
			this.move = move;
			this.animationGetter = animationGetter;
			this.agentTransform = agentTransform;
			this.agentAnimation = agentAnimation;
		}

		private void Play(string animationKey) {
			if (!this.animationGetter.IsPlaying(this.agentAnimation, animationKey)) {
				_ = this.animationGetter.Play(this.agentAnimation, animationKey);
			}
		}

		private Coroutine MoveTowards(TransformComponent agent, U<Vector3, Entity> target) {
			var direction = target.Position() - agent.Position;

			if (direction != Vector3.Zero) {
				direction.Normalize();
				agent.Rotation = Quaternion.LookRotation(direction, Vector3.UnitY);
			}

			while (agent.Position != target.Position()) {
				var delta = (float)this.move.Game.UpdateTime.Elapsed.TotalSeconds * this.move.speed;
				agent.Position = agent.Position.MoveTowards(target.Position(), delta);
				yield return new WaitFrame();
			}
		}

		public (Func<Coroutine>, Cancel) GetExecution(U<Vector3, Entity> target) {
			Coroutine run() {
				this.Play(this.move.playAnimation);
				foreach (var yield in this.MoveTowards(this.agentTransform, target)) {
					yield return yield;
				}
				this.Play(fallbackAnimationKey);
			};
			void cancel() {
				this.Play(fallbackAnimationKey);
			};
			return (run, cancel);
		}
	}
}
