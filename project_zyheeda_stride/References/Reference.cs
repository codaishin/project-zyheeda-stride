namespace ProjectZyheeda;

using System;
using Stride.Core;

[DataContract(Inherited = true)]
[Display(Expand = ExpandRule.Always)]
public abstract class Reference<TTarget, TInterface> :
	IMaybe<TInterface>
	where TTarget :
		TInterface {

	public TTarget? target;

	public TReturn Switch<TReturn>(Func<TInterface, TReturn> some, Func<TReturn> none) {
		return this.target is not null ? some(this.target) : none();
	}
}

[DataContract(Inherited = true)]
public abstract class ReferenceNone<TInterface> : IMaybe<TInterface> {
	public TReturn Switch<TReturn>(Func<TInterface, TReturn> some, Func<TReturn> none) {
		return none();
	}
}
