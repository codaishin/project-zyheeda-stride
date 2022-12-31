namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Input;

public enum InputMode { OnPress, OnRelease }

public abstract class Input<TKey> : IInput
	where TKey : struct, Enum {

	public TKey key;
	public InputMode mode;

	public IMaybe<Func<IGetTarget, ScriptSystem, IAsyncEnumerable<U<Vector3, Entity>>>> GetTargets(
		IInputManagerWrapper input
	) {
		static async IAsyncEnumerable<U<Vector3, Entity>> GetInputTargets(
			IGetTarget getTarget,
			ScriptSystem script
		) {
			_ = await script.NextFrame();
			var target = getTarget
				.GetTarget()
				.Map(t => (U<Vector3, Entity>?)t)
				.UnpackOr(null);
			if (target is not null) {
				yield return target.Value;
			}
			yield break;
		}

		return this.IsTriggered(input) ?
			Maybe.Some(GetInputTargets) :
			Maybe.None<Func<IGetTarget, ScriptSystem, IAsyncEnumerable<U<Vector3, Entity>>>>();
	}

	protected abstract bool IsPressed(IInputManagerWrapper input);
	protected abstract bool IsReleased(IInputManagerWrapper input);

	private bool IsTriggered(IInputManagerWrapper inputManager) {
		return this.mode switch {
			InputMode.OnPress => this.IsPressed(inputManager),
			InputMode.OnRelease => this.IsReleased(inputManager),
			_ => false,
		};
	}
}

public class MouseInput : Input<MouseButton> {
	protected override bool IsPressed(IInputManagerWrapper input) {
		return input.IsMouseButtonPressed(this.key);
	}

	protected override bool IsReleased(IInputManagerWrapper input) {
		return input.IsMouseButtonReleased(this.key);
	}
}

public class KeyInput : Input<Keys> {
	protected override bool IsPressed(IInputManagerWrapper input) {
		return input.IsKeyPressed(this.key);
	}

	protected override bool IsReleased(IInputManagerWrapper input) {
		return input.IsKeyReleased(this.key);
	}
}
