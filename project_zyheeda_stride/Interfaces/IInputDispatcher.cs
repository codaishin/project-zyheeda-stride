namespace ProjectZyheeda;

using System.Collections.Generic;

public interface IInputDispatcher {
	List<IInputStream> Streams { get; }
}
