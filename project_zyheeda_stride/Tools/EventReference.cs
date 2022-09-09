namespace ProjectZyheeda;

using System;
using Stride.Core;
using Stride.Engine;

[DataContract]
public class EventReference<TWrapped, T> :
	IReference,
	IMaybe<T>
	where TWrapped :
		IReference,
		IMaybe<T> {
	public Entity? Entity {
		get => this.wrapped.Entity;
		set {
			this.wrapped.Entity = value;
			this.onSet?.Invoke();
		}
	}

	private readonly TWrapped wrapped;
	private readonly Action? onSet;

	public EventReference(TWrapped wrapped, Action? onSet = null) {
		this.wrapped = wrapped;
		this.onSet = onSet;
	}

	public TReturn Switch<TReturn>(Func<T, TReturn> some, Func<TReturn> none) {
		return this.wrapped.Switch(some, none);
	}
}
