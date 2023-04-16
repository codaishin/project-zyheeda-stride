namespace ProjectZyheeda;

using System.Linq;
using Stride.Engine;

public abstract class BaseInputController<IInput> : SyncScript where IInput : ProjectZyheeda.IInput, new() {
	private IInputManagerWrapper? inputWrapper;

	public readonly IInput input = new();
	public Reference<IGetTarget> getTarget = new();
	public Reference<IBehavior> behavior = new();

	public override void Start() {
		var service = this.Services.GetService<IInputManagerWrapper>() ?? throw new MissingService<IInputManagerWrapper>();
		this.inputWrapper = service;
	}

	private void RunBehavior() {
		var runBehavior =
			(IGetTarget getTarget) =>
			(IBehavior behavior) =>
			() => getTarget.GetTarget().Switch(target => behavior.Run(target), () => { });

		runBehavior
			.ApplyWeak(this.getTarget.MaybeToEither(nameof(this.getTarget)))
			.ApplyWeak(this.behavior.MaybeToEither(nameof(this.behavior)))
			.Switch(
				missingFields => throw new MissingField(this, missingFields.ToArray()),
				action => action()
			);
	}

	public override void Update() {
		if (this.input.GetAction(this.inputWrapper!) is InputAction.None) {
			return;
		}
		this.RunBehavior();
	}
}

public class InputController : BaseInputController<Input> { }
