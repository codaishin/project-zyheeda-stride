namespace ProjectZyheeda;

using Stride.Core;

[DataContract(Inherited = true)]
[Display(Expand = ExpandRule.Always)]
public abstract class ReferenceBehavior<TBehavior> : IReference<TBehavior>, IBehaviorEditor
	where TBehavior :
		class,
		IBehavior {

	private Result<TBehavior> target;

	protected ReferenceBehavior() {
		this.target = Result.SystemError(this.MissingTarget());
	}

	public TBehavior? Target {
		get => this.target.UnpackOrDefault();
		set => this.target = value.OkOrSystemError(this.MissingTarget());
	}

	public Result<(Coroutine, Cancel)> GetExecution() {
		return this.target.FlatMap(b => b.GetExecution());
	}
}

public class ReferenceBehaviorController : ReferenceBehavior<BehaviorController> { }
