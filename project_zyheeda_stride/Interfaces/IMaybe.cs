namespace ProjectZyheeda;

using System;

public interface IMaybe<T> {
	TReturn Switch<TReturn>(Func<T, TReturn> some, Func<TReturn> none);
}
