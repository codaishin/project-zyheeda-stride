namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;


public delegate Result Cancel();

public delegate (Coroutine, Cancel) FGetCoroutine(Func<Result<Vector3>> getTarget);
