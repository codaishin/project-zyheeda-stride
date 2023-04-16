namespace ProjectZyheeda;

using Stride.Core;

public enum InputActivation { OnPress, OnRelease }

[DataContract]
public class Input : IInput {
	public InputKeys activationKey;
	public InputActivation activation;
	public InputKeys chainKey;

	public InputAction GetAction(IInputManagerWrapper input) {
		return this.Inactive(input) ?? this.RunOrChain(input);
	}

	private InputAction? Inactive(IInputManagerWrapper input) {
		var active = this.activation switch {
			InputActivation.OnPress => input.IsPressed(this.activationKey),
			InputActivation.OnRelease => input.IsReleased(this.activationKey),
			_ => false,
		};
		return active ? null : InputAction.None;
	}

	private InputAction RunOrChain(IInputManagerWrapper input) {
		return input.IsDown(this.chainKey)
			? InputAction.Chain
			: InputAction.Run;
	}
}
