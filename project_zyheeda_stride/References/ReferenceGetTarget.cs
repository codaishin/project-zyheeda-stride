namespace ProjectZyheeda;

using Stride.Core.Serialization;

[DataSerializer(typeof(ReferenceSerializer<ReferenceGetMousePosition, GetMousePosition, IGetTarget>))]
public class ReferenceGetMousePosition : Reference<GetMousePosition, IGetTarget> { }
