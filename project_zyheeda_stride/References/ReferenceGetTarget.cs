namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;

public abstract class ReferenceGetTarget<TGetTarget> : Reference<TGetTarget>, IGetTargetEditor
	where TGetTarget :
		IGetTarget {

	public TGetTarget? Target {
		get => this.GetRef();
		set => this.SetRef(value);
	}

	public Result<Func<Vector3>> GetTarget() {
		return this.target.FlatMap(g => g.GetTarget());
	}
}

public class ReferenceGetMousePosition : ReferenceGetTarget<GetMousePosition> { }
