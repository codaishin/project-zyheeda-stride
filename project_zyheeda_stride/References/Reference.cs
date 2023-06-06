namespace ProjectZyheeda;

using Stride.Core;

[DataContract]
public abstract class Reference<TTarget, TInterface> :
	IMaybe<TInterface>
	where TTarget :
		TInterface {

	public TTarget? target;

	public TReturn Switch<TReturn>(System.Func<TInterface, TReturn> some, System.Func<TReturn> none) {
		return this.target is not null ? some(this.target) : none();
	}
}

/// <summary>
/// 	Base class for all interface references that need to be assignable as components in the
/// 	editor.
///
/// 	Stride seemingly can't handle (de)serialization of generic fields/properties that remain
/// 	generic for more than one level of inheritance. Thus, we cannot declare the Target property
/// 	(the component we are referencing) here. The base class for each type of interface must do
/// 	that instead.
///
/// 	However, we expose some quasi setter/getter methods that can be used for the get and set on
/// 	the children's Target property.
/// </summary>
/// <typeparam name="TTarget"></typeparam>
[DataContract(Inherited = true)]
public abstract class Reference<TTarget> {
	protected Result<TTarget> target;

	public Reference() {
		this.target = Result.SystemError(this.MissingField(nameof(this.target)));
	}

	protected TTarget? GetRef() {
		return this.target.Switch<TTarget?>(
			_ => default,
			v => v
		);
	}

	protected void SetRef(TTarget? value) {
		this.target =
			value is null
				? Result.SystemError(this.MissingField(nameof(this.target)))
				: Result.Ok(value);
	}
}
