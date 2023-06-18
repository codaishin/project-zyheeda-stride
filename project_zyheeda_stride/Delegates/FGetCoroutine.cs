namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;


public delegate Result<IWait> Cancel();

public delegate (Func<Coroutine>, Cancel) FGetCoroutine(Func<Vector3> getTarget);
