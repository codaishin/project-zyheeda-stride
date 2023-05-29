namespace ProjectZyheeda;

using System;
using Stride.Core;

using Stride.Core.Annotations;
using Stride.Core.Serialization;

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

	public class Serializer<TReference> :
		DataSerializer<TReference>
		where TReference :
			Reference<TTarget, TInterface>,
			new() {

		private DataSerializer<TTarget?> targetSerializer = new EmptyDataSerializer<TTarget?>();

		public override void Initialize(SerializerSelector serializerSelector) {
			this.targetSerializer = MemberSerializer<TTarget?>.Create(serializerSelector);
		}

		public override void PreSerialize(ref object obj, ArchiveMode mode, SerializationStream stream) {
			var objT = obj as TReference ?? new TReference();
			this.PreSerialize(ref objT, mode, stream);
			obj = objT;
		}

		public override void Serialize(
			ref TReference obj,
			ArchiveMode mode,
			[NotNull] SerializationStream stream
		) {
			if (mode == ArchiveMode.Deserialize) {
				this.targetSerializer.Serialize(ref obj.target, mode, stream);
			} else {
				this.targetSerializer.Serialize(obj.target, stream);
			}
		}
	}
}

[DataContract(Inherited = true)]
public abstract class ReferenceNone<TInterface> : IMaybe<TInterface> {
	public TReturn Switch<TReturn>(Func<TInterface, TReturn> some, Func<TReturn> none) {
		return none();
	}
}
