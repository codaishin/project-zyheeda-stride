namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;

public abstract class ReferenceBehavior<TBehavior> : Reference<TBehavior>, IBehaviorEditor
	where TBehavior :
		IBehavior {

	public TBehavior? Target {
		get => this.GetRef();
		set => this.SetRef(value);
	}

	public Result<(Func<Coroutine>, Cancel)> GetCoroutine(Func<Vector3> getTarget) {
		return this.target.FlatMap(b => b.GetCoroutine(getTarget));
	}
}

public class ReferenceBehaviorController : ReferenceBehavior<BehaviorController> { }
