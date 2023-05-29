namespace ProjectZyheeda;

using Stride.Core;

[DataContract(Inherited = true)]
[Display(Expand = ExpandRule.Always)]
public abstract class Reference<TTarget, TInterface> :
	IMaybe<TInterface>
	where TTarget :
		TInterface {

	public TTarget? target;

	public TReturn Switch<TReturn>(System.Func<TInterface, TReturn> some, System.Func<TReturn> none) {
		return this.target is not null ? some(this.target) : none();
	}
}
