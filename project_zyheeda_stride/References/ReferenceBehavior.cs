namespace ProjectZyheeda;

using System;

public abstract class ReferenceBehavior<TBehavior> : Reference<TBehavior>, IBehaviorEditor
	where TBehavior :
		IBehavior {

	public TBehavior? Target {
		get => this.GetRef();
		set => this.SetRef(value);
	}

	public Result<(Func<Coroutine>, Cancel)> GetCoroutine() {
		return this.target.FlatMap(b => b.GetCoroutine());
	}
}

public class ReferenceBehaviorController : ReferenceBehavior<BehaviorController> { }
