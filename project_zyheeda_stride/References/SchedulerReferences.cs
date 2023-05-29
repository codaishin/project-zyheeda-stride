namespace ProjectZyheeda;

using Stride.Core.Serialization;

public class NoScheduler : ReferenceNone<IScheduler> { }

[DataSerializer(typeof(ReferenceSerializer<ReferenceSchedularController, SchedulerController, IScheduler>))]
public class ReferenceSchedularController : Reference<SchedulerController, IScheduler> { }
