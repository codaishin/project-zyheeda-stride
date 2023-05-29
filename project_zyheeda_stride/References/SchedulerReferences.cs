namespace ProjectZyheeda;

using Stride.Core.Serialization;

[DataSerializer(typeof(ReferenceSerializer<ReferenceSchedularController, SchedulerController, IScheduler>))]
public class ReferenceSchedularController : Reference<SchedulerController, IScheduler> { }
