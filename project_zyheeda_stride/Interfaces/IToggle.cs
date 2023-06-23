namespace ProjectZyheeda;

using System;

public interface IToggle {
	Result<(Func<Coroutine> coroutine, Cancel cancel)> GetToggle();
}

public interface IToggleEditor : IToggle { }
