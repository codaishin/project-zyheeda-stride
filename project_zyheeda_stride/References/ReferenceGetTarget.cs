namespace ProjectZyheeda;

using Stride.Core.Serialization;

public class NoGetTarget : ReferenceNone<IGetTarget> { }

[DataSerializer(typeof(ReferenceSerializer<ReferenceGetMousePosition, GetMousePosition, IGetTarget>))]
public class ReferenceGetMousePosition : Reference<GetMousePosition, IGetTarget> { }
