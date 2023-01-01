namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;

public enum InputMode { OnPress, OnRelease }

[DataContract]
public class Input : IInput {

	public InputKeys key;
	public InputMode mode;
	public InputKeys hold;

	public Logger? Log { get; set; }

	private bool holding;

	private static bool FoundTarget(IGetTarget getTarget, out U<Vector3, Entity> target) {
		var found = getTarget.GetTarget()
			.Map(t => (U<Vector3, Entity>?)t)
			.UnpackOr(null);
		if (found.HasValue) {
			target = found.Value;
			return true;
		}
		target = Vector3.Zero;
		return false;
	}

	public IMaybe<Func<IGetTarget, ScriptSystem, IAsyncEnumerable<U<Vector3, Entity>>>> GetTargets(
		IInputManagerWrapper input
	) {

		async IAsyncEnumerable<U<Vector3, Entity>> GetInputTargets(
			IGetTarget getTarget,
			ScriptSystem script
		) {
			this.holding = input.IsDown(this.hold);
			if (Input.FoundTarget(getTarget, out var target)) {
				yield return target;
			}
			_ = await script.NextFrame();

			while (input.IsDown(this.hold)) {
				if (Input.IsTriggered(input, this.mode, this.key) && Input.FoundTarget(getTarget, out target)) {
					yield return target;
				}
				_ = await script.NextFrame();
			}
			this.holding = false;
		}

		var triggered = Input.IsTriggered(input, this.mode, this.key);

		return triggered && !this.holding
			? Maybe.Some(GetInputTargets)
			: Maybe.None<Func<IGetTarget, ScriptSystem, IAsyncEnumerable<U<Vector3, Entity>>>>();
	}

	private static bool IsTriggered(IInputManagerWrapper inputManager, InputMode mode, InputKeys key) {
		return mode switch {
			InputMode.OnPress => inputManager.IsPressed(key),
			InputMode.OnRelease => inputManager.IsReleased(key),
			_ => false,
		};
	}
}
