namespace ProjectZyheeda;

using System;

public interface IMaybe<T> {
	void Match(Action<T> some, Action? none = null);
}
