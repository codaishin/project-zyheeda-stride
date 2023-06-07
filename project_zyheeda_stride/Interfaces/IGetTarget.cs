namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;

public interface IGetTarget {
	Result<Func<Vector3>> GetTarget();
}

public interface IGetTargetEditor : IGetTarget { }
