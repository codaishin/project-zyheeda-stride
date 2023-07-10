namespace ProjectZyheeda;

using System;
using Stride.Core;
using Stride.Engine;

[DataContract(Inherited = true)]
[Display(Expand = ExpandRule.Always)]
public class ReferenceAnimatedMoveDependency : IReference<CharacterDependencies>, IAnimatedMoveEditor {

	private Result<CharacterDependencies> target;

	public CharacterDependencies? Target {
		get => this.target.UnpackOrDefault();
		set => this.target = value.OkOrSystemError(this.MissingTarget());
	}

	private Result<IAnimatedMove> Move =>
		this.target
			.FlatMap(t => t.move.OkOrSystemError(t.MissingField(nameof(t.move))));

	public ReferenceAnimatedMoveDependency() {
		this.target = Result.Error(this.MissingTarget());
	}

	public Result<FGetCoroutine> PrepareCoroutineFor(
		Entity agent,
		FSpeedToDelta delta,
		Func<string, Result> playAnimation
	) {
		return this.Move.FlatMap(m => m.PrepareCoroutineFor(agent, delta, playAnimation));
	}

	public Result<string> SetAnimation(string animationKey) {
		return this.Move.FlatMap(m => m.SetAnimation(animationKey));
	}

	public Result<OldSpeed> SetSpeed(ISpeedEditor speed) {
		return this.Move.FlatMap(m => m.SetSpeed(speed));
	}
}
