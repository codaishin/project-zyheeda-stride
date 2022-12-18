namespace ProjectZyheeda;

using System;

public class MissingService : Exception { }
public class MissingService<T> : MissingService { }
