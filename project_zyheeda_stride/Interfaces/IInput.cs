namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;

public interface IInput {
	IMaybe<Func<IGetTarget, ScriptSystem, IAsyncEnumerable<U<Vector3, Entity>>>> GetTargets(
		IInputManagerWrapper input
	);
}
