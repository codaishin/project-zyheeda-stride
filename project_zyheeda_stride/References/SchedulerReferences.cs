namespace ProjectZyheeda;

using Stride.Core;

[DataContract(Inherited = true)]
[Display(Expand = ExpandRule.Always)]
public abstract class ReferenceScheduler<TScheduler> : IReference<TScheduler>, ISchedulerEditor
	where TScheduler :
		class,
		IScheduler {

	private Result<TScheduler> target;

	protected ReferenceScheduler() {
		this.target = Result.SystemError(this.MissingTarget());
	}

	public TScheduler? Target {
		get => this.target.UnpackOrDefault();
		set => this.target = value.OkOrSystemError(this.MissingTarget());
	}

	public Result Clear() {
		return this.target.FlatMap(s => s.Clear());
	}

	public Result Run(Coroutine coroutine, Cancel? cancel = null) {
		return this.target.FlatMap(s => s.Run(coroutine, cancel));
	}

	public Result Enqueue(Coroutine coroutine, Cancel? cancel = null) {
		return this.target.FlatMap(s => s.Enqueue(coroutine, cancel));
	}
}

public class ReferenceSchedularController : ReferenceScheduler<SchedulerController> { }
