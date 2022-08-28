namespace ProjectZyheeda;

using System;
using Stride.Core;
using Stride.Engine;

[DataContract]
public class Reference<T> : IMaybe<T> {
	private static Entity? EntityOnly((Entity entity, T target) data) {
		return data.entity;
	}

	private static T TargetOnly((Entity entity, T target) data) {
		return data.target;
	}

	private IMaybe<(Entity, T)> GetData(Entity entity) {
		foreach (var component in entity.Components) {
			if (component is T target) {
				return Maybe.Some((entity, target));
			}
		}
		return this.data;
	}

	private IMaybe<(Entity entity, T target)> data = Maybe.None<(Entity, T)>();

	[DataMember]
	public Entity? Entity {
		get => this.data
			.Map(Reference<T>.EntityOnly)
			.Unpack(fallback: null);
		set => this.data = value is null
			? Maybe.None<(Entity entity, T target)>()
			: this.GetData(value);
	}

	public void Match(Action<T> some, Action? none = null) {
		this.data
			.Map(Reference<T>.TargetOnly)
			.Match(some, none);
	}
}
