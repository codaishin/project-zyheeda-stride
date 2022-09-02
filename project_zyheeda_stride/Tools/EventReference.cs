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

	public void Match(Action<T> some, Action? none = null) {
		this.wrapped.Match(some, none);
	}
}
