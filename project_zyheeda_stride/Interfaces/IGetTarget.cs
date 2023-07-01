namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;

public interface IGetTarget {
	Result<Func<Result<Vector3>>> GetTarget();
}

public interface IGetTargetEditor : IGetTarget { }
