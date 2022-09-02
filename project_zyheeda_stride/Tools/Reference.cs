namespace ProjectZyheeda;

using System;
using System.Linq;
using Stride.Core;
using Stride.Engine;

[DataContract]
public class Reference<T> : IReference, IMaybe<T> {
	private static Entity? EntityOnly((Entity entity, T target) data) {
		return data.entity;
	}

	private static T TargetOnly((Entity entity, T target) data) {
		return data.target;
	}

	private IMaybe<(Entity, T)> GetFirstMatch(Entity entity) {
		var first = entity.Components
			.OfType<T>()
			.FirstOrDefault();
		return first is not null
			? Maybe.Some((entity, first))
			: this.data;
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

	public void Match(Action<T> some, Action? none = null) {
		this.data
			.Map(Reference<T>.TargetOnly)
			.Match(some, none);
	}
}
