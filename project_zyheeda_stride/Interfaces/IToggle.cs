namespace ProjectZyheeda;

public interface IToggle {
	Result<(Coroutine coroutine, Cancel cancel)> GetToggle();
}

public interface IToggleEditor : IToggle { }
