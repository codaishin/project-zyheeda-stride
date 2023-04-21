namespace ProjectZyheeda;

using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine;


public class MoveController : StartupScript, IEquipment {
	public static readonly string fallbackAnimationKey = "default";

	public float speed;
	public string animationKey = "";

	private Either<U<SystemStr, PlayerStr>, IAnimation> animation = new U<SystemStr, PlayerStr>(
		new SystemStr("No IGetAnimation assigned")
	);

	private static Either<U<SystemStr, PlayerStr>, AnimationComponent> AnimatorOnChildOf(Entity agent) {
		return agent
			.GetChildren()
			.Select(c => c.Get<AnimationComponent>())
			.FirstOrDefault()
			.ToEither(new U<SystemStr, PlayerStr>(new SystemStr($"Missing AnimationComponent on {agent.Name}")));
	}

	private Either<U<SystemStr, PlayerStr>, IAnimation> AnimationFromService() {
		return this
			.Game
			.Services
			.GetService<IAnimation>()
			.ToEither(new U<SystemStr, PlayerStr>(new SystemStr("Missing IGetAnimation Service")));
	}

	public override void Start() {
		this.animation = this.AnimationFromService();
	}

	public Either<IEnumerable<U<SystemStr, PlayerStr>>, FGetCoroutine> PrepareCoroutineFor(Entity agent) {
		var prepareCoroutine =
			(IAnimation animation) =>
			(AnimationComponent agentAnimator) => this.PrepareCoroutineFor(
				agent,
				animation,
				agentAnimator
			);

		var agentAnimator = MoveController.AnimatorOnChildOf(agent);
		return prepareCoroutine
			.ApplyWeak(this.animation)
			.ApplyWeak(agentAnimator);
	}

	private static void Play(IAnimation animation, string animationKey, AnimationComponent agentAnimator) {
		if (!animation.IsPlaying(agentAnimator, animationKey)) {
			_ = animation.Play(agentAnimator, animationKey);
		}
	}

	private Coroutine MoveTowards(TransformComponent agent, U<Vector3, Entity> target) {
		var direction = target.Position() - agent.Position;

		if (direction != Vector3.Zero) {
			direction.Normalize();
			agent.Rotation = Quaternion.LookRotation(direction, Vector3.UnitY);
		}

		while (agent.Position != target.Position()) {
			var delta = (float)this.Game.UpdateTime.Elapsed.TotalSeconds * this.speed;
			agent.Position = agent.Position.MoveTowards(target.Position(), delta);
			yield return new WaitFrame();
		}
	}

	private FGetCoroutine PrepareCoroutineFor(Entity agent, IAnimation animation, AnimationComponent agentAnimator) {
		void playAnimation(string animationKey) {
			MoveController.Play(animation, animationKey, agentAnimator);
		}

		void cancel() {
			playAnimation(MoveController.fallbackAnimationKey);
		};

		return (U<Vector3, Entity> target) => {
			Coroutine run() {
				playAnimation(this.animationKey);
				foreach (var wait in this.MoveTowards(agent.Transform, target)) {
					yield return wait;
				}
				playAnimation(MoveController.fallbackAnimationKey);
			};

			return (run, cancel);
		};
	}
}
