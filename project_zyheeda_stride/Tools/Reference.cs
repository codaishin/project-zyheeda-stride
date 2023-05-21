namespace ProjectZyheeda;

using System;
using System.Linq;
using Stride.Core;
using Stride.Engine;

[DataContract]
public class Reference<T> : IReference, IMaybe<T>
	where T : class {
	private static Entity? EntityOnly((Entity entity, T target) data) {
		return data.entity;
	}

	private static T TargetOnly((Entity entity, T target) data) {
		return data.target;
	}

	private IMaybe<(Entity, T)> GetFirstMatch(Entity entity) {
		var first = (entity as T) ?? entity.Components
			.OfType<T>()
			.FirstOrDefault();
		return first is null
			? this.data
			: Maybe.Some((entity, first));
	}

	private IMaybe<(Entity entity, T target)> data = Maybe.None<(Entity, T)>();

	[DataMember]
	public Entity? Entity {
		get => this.data
			.Map(Reference<T>.EntityOnly)
			.UnpackOr(null);
		set => this.data = value is null
			? Maybe.None<(Entity, T)>()
			: this.GetFirstMatch(value);
	}

	public TReturn Switch<TReturn>(Func<T, TReturn> some, Func<TReturn> none) {
		return this.data
			.Map(Reference<T>.TargetOnly)
			.Switch(some, none);
	}
}

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
