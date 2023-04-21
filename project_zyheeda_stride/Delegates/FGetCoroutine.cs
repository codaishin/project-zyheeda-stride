namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;
using Stride.Engine;

public delegate (Func<Coroutine>, Cancel) FGetCoroutine(U<Vector3, Entity> target);
