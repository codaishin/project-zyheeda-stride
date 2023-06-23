namespace ProjectZyheeda;

using System;

public abstract class ReferenceToggle<TToggle> : Reference<TToggle>, IBehaviorEditor
	where TToggle :
		IToggle {

	public TToggle? Target {
		get => this.GetRef();
		set => this.SetRef(value);
	}

	public Result<(Func<Coroutine>, Cancel)> GetExecution() {
		return this.target.FlatMap(t => t.GetToggle());
	}
}

public class ReferenceBehaviorToggle : ReferenceToggle<ToggleBehaviorController> { }
