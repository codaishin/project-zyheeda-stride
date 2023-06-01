namespace ProjectZyheeda;

using System.Linq;
using Stride.Engine;


public class MoveController : ProjectZyheedaStartupScript, IEquipment {
	public IAnimatedMove? move;

	private static Result<AnimationComponent> AnimatorOnChildOf(Entity agent) {
		return agent
			.GetChildren()
			.Select(c => c.Get<AnimationComponent>())
			.FirstOrDefault()
			.OkOrSystemError($"Missing AnimationComponent on {agent.Name}");
	}

	private Result<IAnimatedMove> EitherMoveOrError() {
		return this.move is null
			? Result.SystemError(this.MissingField(nameof(this.move)))
			: Result.Ok(this.move);
	}

	public Result<FGetCoroutine> PrepareCoroutineFor(Entity agent) {
		var prepareCoroutine =
			(IAnimatedMove move) =>
			(AnimationComponent agentAnimator) =>
				move.PrepareCoroutineFor(
					agent,
					speedPerSecond => (float)this.Game.UpdateTime.Elapsed.TotalSeconds * speedPerSecond,
					key => this.Play(key, agentAnimator)
				);

		var agentAnimatorOrError = MoveController.AnimatorOnChildOf(agent);
		return prepareCoroutine
			.ApplyWeak(this.EitherMoveOrError())
			.ApplyWeak(agentAnimatorOrError)
			.Flatten();
	}

	private void Play(string animationKey, AnimationComponent agentAnimator) {
		var animation = this.EssentialServices.animation;
		if (!animation.IsPlaying(agentAnimator, animationKey).UnpackOr(false)) {
			_ = animation.Play(agentAnimator, animationKey);
		}
	}
}
