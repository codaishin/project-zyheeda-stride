namespace ProjectZyheeda;

using System.Threading.Tasks;
using Stride.Core;

[DataContract]
public class InputStream : IInputStream {
	public InputKeys activationKey;
	public InputActivation activation;
	public InputKeys chainKey;

	private TaskCompletionSource<InputAction> actionToken;
	private bool primedForChaining;

	public InputStream() {
		this.activationKey = InputKeys.None;
		this.chainKey = InputKeys.None;
		this.activation = InputActivation.OnPress;
		this.actionToken = new();
		this.primedForChaining = false;
	}

	public Task<InputAction> NewAction() {
		return this.actionToken.Task;
	}

	public void ProcessEvent(InputKeys key, bool isDown) {
		if (key == this.chainKey) {
			this.ToggleChain(isDown);
			return;
		}
		if (key != this.activationKey) {
			return;
		}
		this.ResolveTask(isDown);
	}

	private void ToggleChain(bool isDown) {
		this.primedForChaining = InputStream.Matches(InputActivation.OnPress, isDown);
	}

	private void ResolveTask(bool isDown) {
		if (!InputStream.Matches(this.activation, isDown)) {
			return;
		}
		this.actionToken.SetResult(this.primedForChaining ? InputAction.Chain : InputAction.Run);
		this.actionToken = new();
	}

	private static bool Matches(InputActivation activation, bool isDown) {
		return isDown
			? activation is InputActivation.OnPress
			: activation is InputActivation.OnRelease;
	}
}
