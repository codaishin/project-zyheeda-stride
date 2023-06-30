namespace ProjectZyheeda;

public interface IBehavior {
	Result<(Coroutine coroutine, Cancel cancel)> GetExecution();
}

public interface IBehaviorEditor : IBehavior { }
