namespace ProjectZyheeda;

using Stride.Engine;

public static class Messages {
	public static string MissingField(this EntityComponent component, string fieldName) {
		var name = component.Entity?.Name;  //it is possible for Entity to be null
		var type = component.GetType().Name;
		return $"{name} ({type}): '{fieldName}' not assigned";
	}
}
