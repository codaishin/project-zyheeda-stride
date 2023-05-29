namespace ProjectZyheeda;

using Stride.Core.Annotations;
using Stride.Core.Serialization;

public class ReferenceSerializer<TReference, TTarget, TInterface>
	: DataSerializer<Reference<TTarget, TInterface>>
	where TTarget : TInterface
	where TReference :
		Reference<TTarget, TInterface>,
		new() {

	private DataSerializer<TTarget?> targetSerializer = new EmptyDataSerializer<TTarget?>();

	public override void Initialize(SerializerSelector serializerSelector) {
		this.targetSerializer = MemberSerializer<TTarget?>.Create(serializerSelector);
	}

	public override void PreSerialize(ref object obj, ArchiveMode mode, SerializationStream stream) {
		Reference<TTarget, TInterface> objT = obj as TReference ?? new TReference();
		this.PreSerialize(ref objT, mode, stream);
		obj = objT;
	}

	public override void Serialize(
		ref Reference<TTarget, TInterface> obj,
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
