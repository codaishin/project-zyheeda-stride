namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;

public abstract class InputController<IInput> : SyncScript
	where IInput :
		ProjectZyheeda.IInput,
		new() {

	private IInputManagerWrapper? inputWrapper;

	public readonly IInput input = new();
	public Reference<IGetTarget> getTarget = new();
	public Reference<IBehavior> behavior = new();

	public override void Start() {
		var service = this.Services.GetService<IInputManagerWrapper>();
		if (service is null) {
			throw new MissingService<IInputManagerWrapper>();
		}
		this.inputWrapper = service;
	}

	private void RunBehavior(
		Func<IGetTarget, ScriptSystem, IAsyncEnumerable<U<Vector3, Entity>>> getTargets
	) {
		var runBehavior =
			(IGetTarget getTarget) =>
			(IBehavior behavior) =>
			() => behavior.Run(getTargets(getTarget, this.Script));

		runBehavior
			.ApplyWeak(this.getTarget.MaybeToEither(nameof(this.getTarget)))
			.ApplyWeak(this.behavior.MaybeToEither(nameof(this.behavior)))
			.Switch(
				missingFields => throw new MissingField(this, missingFields.ToArray()),
				action => action()
			);
	}

	private void Idle() { }

	public override void Update() {
		this.input
			.GetTargets(this.inputWrapper!)
			.Switch(
				some: this.RunBehavior,
				none: this.Idle
			);
	}
}

public class MouseInputController : InputController<MouseInput> { }
public class KeyInputController : InputController<KeyInput> { }
