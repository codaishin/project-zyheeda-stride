namespace ProjectZyheeda;

using Stride.Core.Serialization;

public class NoGetTarget : ReferenceNone<IGetTarget> { }

[DataSerializer(typeof(ReferenceGetMousePosition.Serializer<ReferenceGetMousePosition>))]
public class ReferenceGetMousePosition : Reference<GetMousePosition, IGetTarget> { }
