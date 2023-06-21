namespace ProjectZyheeda;

using System;

public interface IBehavior {
	Result<(Func<Coroutine> coroutine, Cancel cancel)> GetExecution();
}

public interface IBehaviorEditor : IBehavior { }
