namespace ProjectZyheeda;

using Stride.Core.Serialization;

public class NoScheduler : ReferenceNone<IScheduler> { }

[DataSerializer(typeof(ReferenceSchedularController.Serializer<ReferenceSchedularController>))]
public class ReferenceSchedularController : Reference<SchedulerController, IScheduler> { }
