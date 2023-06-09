namespace ProjectZyheeda;

using System;

public interface IBehavior {
	Result<(Func<Coroutine>, Cancel)> GetCoroutine();
}

public interface IBehaviorEditor : IBehavior { }
