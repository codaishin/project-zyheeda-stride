namespace ProjectZyheeda;

public abstract class ReferenceBehavior<TBehavior> : Reference<TBehavior>, IBehaviorEditor
	where TBehavior :
		IBehavior {

	public TBehavior? Target {
		get => this.GetRef();
		set => this.SetRef(value);
	}

	public Result<(Coroutine, Cancel)> GetExecution() {
		return this.target.FlatMap(b => b.GetExecution());
	}
}

public class ReferenceBehaviorController : ReferenceBehavior<BehaviorController> { }
public class ReferenceTogglableBehavior : ReferenceBehavior<ToggleBehaviorController> { }
public class ReferenceToggleAnimatedMoveDependency : ReferenceBehavior<ToggleAnimatedMoveDependencyController> { }
