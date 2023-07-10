namespace ProjectZyheeda;

using System;
using Stride.Core;
using Stride.Core.Mathematics;

[DataContract(Inherited = true)]
[Display(Expand = ExpandRule.Always)]
public abstract class ReferenceGetTarget<TGetTarget> : IReference<TGetTarget>, IGetTargetEditor
	where TGetTarget :
		class,
		IGetTarget {

	private Result<TGetTarget> target;

	protected ReferenceGetTarget() {
		this.target = Result.SystemError(this.MissingTarget());
	}

	public TGetTarget? Target {
		get => this.target.UnpackOrDefault();
		set => this.target = value.OkOrSystemError(this.MissingTarget());
	}

	public Result<Func<Result<Vector3>>> GetTarget() {
		return this.target.FlatMap(g => g.GetTarget());
	}
}

public class ReferenceGetMousePosition : ReferenceGetTarget<GetMousePosition> { }
