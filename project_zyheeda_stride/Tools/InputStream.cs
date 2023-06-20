namespace ProjectZyheeda;

using System.Threading.Tasks;
using Stride.Core;

public enum InputActivation { OnPress, OnRelease }

public enum InputKeys {
	None = default,
	ShiftLeft,
	MouseLeft,
	MouseRight,
}

[DataContract]
public class InputStream : IInputStreamEditor {
	public InputKeys activationKey = InputKeys.None;
	public InputActivation activation = InputActivation.OnPress;
	public InputKeys enqueueKey = InputKeys.None;

	private TaskCompletionSource<Result<InputAction>> actionToken = new();
	private bool primedForChaining;

	public Task<Result<InputAction>> NewAction() {
		return this.actionToken.Task;
	}

	public Result ProcessEvent(InputKeys key, bool isDown) {
		return key == this.enqueueKey
			? this.TryPrimeForChaining(isDown)
			: key == this.activationKey
			? this.TryRunOrChain(isDown)
			: Result.Ok();
	}

	private Result TryPrimeForChaining(bool isDown) {
		this.primedForChaining = InputStream.Matches(InputActivation.OnPress, isDown);
		return Result.Ok();
	}

	private Result TryRunOrChain(bool isDown) {
		var ok = Result.Ok();
		if (!InputStream.Matches(this.activation, isDown)) {
			return ok;
		}
		this.actionToken.SetResult(this.primedForChaining ? InputAction.Enqueue : InputAction.Run);
		this.actionToken = new();
		return ok;
	}

	private static bool Matches(InputActivation activation, bool isDown) {
		return isDown
			? activation is InputActivation.OnPress
			: activation is InputActivation.OnRelease;
	}
}
