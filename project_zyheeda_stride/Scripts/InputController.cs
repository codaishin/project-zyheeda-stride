namespace ProjectZyheeda;

using System;
using System.Linq;
using Stride.Engine;
using Stride.Input;

public enum InputMode { OnPress, OnRelease }

public abstract class InputController<T> : SyncScript
	where T : struct, Enum {
	private readonly Func<IGetTargets, Func<IBehavior, Action>> runBehavior =
		(IGetTargets getTargets) =>
		(IBehavior behavior) =>
		() => {
			var targets = getTargets.GetTargets();
			behavior.Run(targets);
		};

	private IInputWrapper? inputWrapper;

	public T button;
	public InputMode mode;
	public Reference<IGetTargets> getTarget = new();
	public Reference<IBehavior> behavior = new();

	protected abstract bool IsPressed(IInputWrapper input, T button);
	protected abstract bool IsReleased(IInputWrapper input, T button);

	public override void Start() {
		var service = this.Services.GetService<IInputWrapper>();
		if (service is null) {
			throw new MissingService<IInputWrapper>();
		}
		this.inputWrapper = service;
	}

	private bool IsTriggered() {
		return this.mode switch {
			InputMode.OnPress => this.IsPressed(this.inputWrapper!, this.button),
			InputMode.OnRelease => this.IsReleased(this.inputWrapper!, this.button),
			_ => false,
		};
	}

	public override void Update() {
		if (this.IsTriggered()) {
			this.runBehavior
				.ApplyWeak(this.getTarget.MaybeToEither(nameof(this.getTarget)))
				.ApplyWeak(this.behavior.MaybeToEither(nameof(this.behavior)))
				.Switch(
					missingFields => throw new MissingField(this, missingFields.ToArray()),
					action => action()
				);
		}
	}
}

public class MouseInputController : InputController<MouseButton> {
	protected override bool IsPressed(IInputWrapper input, MouseButton button) {
		return input.IsMouseButtonPressed(button);
	}

	protected override bool IsReleased(IInputWrapper input, MouseButton button) {
		return input.IsMouseButtonReleased(button);
	}
}

public class KeyInputController : InputController<Keys> {
	protected override bool IsPressed(IInputWrapper input, Keys button) {
		return input.IsKeyPressed(button);
	}

	protected override bool IsReleased(IInputWrapper input, Keys button) {
		return input.IsKeyReleased(button);
	}
}
