namespace ProjectZyheeda;

using System;

public class MissingService : Exception {
	public MissingService() : base() { }
	public MissingService(string msg) : base(msg) { }
}

public class MissingService<T> : MissingService {
	public MissingService() : base() { }
	public MissingService(string msg) : base(msg) { }
}
