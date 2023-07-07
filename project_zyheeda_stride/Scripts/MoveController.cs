namespace ProjectZyheeda;

using System.Linq;
using Stride.Engine;


public class MoveController : ProjectZyheedaStartupScript, IEquipment {
	public IAnimatedMoveEditor? move;

	private static Result<AnimationComponent> AnimatorOnChildOf(Entity agent) {
		return agent
			.GetChildren()
			.Select(c => c.Get<AnimationComponent>())
			.FirstOrDefault(a => a is not null)
			.OkOrSystemError($"Missing AnimationComponent on {agent.Name}");
	}

	private Result<IAnimatedMoveEditor> EitherMoveOrError() {
		return this.move is null
			? Result.SystemError(this.MissingField(nameof(this.move)))
			: Result.Ok(this.move);
	}

	public Result<FGetCoroutine> PrepareCoroutineFor(Entity agent) {
		var prepareCoroutine =
			(IAnimatedMoveEditor move) =>
			(AnimationComponent agentAnimator) =>
				move.PrepareCoroutineFor(
					agent,
					speed => (float)this.Game.UpdateTime.Elapsed.TotalSeconds * speed.ToUnitsPerSecond(),
					key => this.Play(key, agentAnimator)
				);

		var agentAnimatorOrError = MoveController.AnimatorOnChildOf(agent);
		return prepareCoroutine
			.Apply(this.EitherMoveOrError())
			.Apply(agentAnimatorOrError)
			.Flatten();
	}

	private Result Play(string animationKey, AnimationComponent agentAnimator) {
		var animation = this.EssentialServices.animation;
		return animation
			.IsPlaying(agentAnimator, animationKey)
			.FlatMap(
				isPlaying => isPlaying
					? Result.Ok()
					: animation.Play(agentAnimator, animationKey)
			);
	}
}
