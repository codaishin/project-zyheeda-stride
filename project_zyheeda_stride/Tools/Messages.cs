namespace ProjectZyheeda;

using Stride.Engine;

public static class Messages {
	public static string MissingField(this EntityComponent component, string fieldName) {
		var name = component.Entity?.Name;  //it is possible for Entity to be null
		var type = component.GetType().Name;
		return $"{name} ({type}): '{fieldName}' not assigned";
	}

	public static string MissingField(this object obj, string fieldName) {
		return $"{obj}: '{fieldName}' not assigned";
	}

	public static string MissingService<T>(this EntityComponent component) {
		var name = component.Entity?.Name;  //it is possible for Entity to be null
		var type = component.GetType().Name;
		return $"{name} ({type}): Needed '{typeof(T).Name}' Service, but it was missing"; ;
	}
}
