namespace ProjectZyheeda;

using Stride.Engine;

public static class Messages {
	private static string EntityName(Entity entity) {
		return entity is null
			? "!!!Missing Entity!!!"
			: entity.Name;
	}

	public static string MissingField(this EntityComponent component, string fieldName) {
		var name = Messages.EntityName(component.Entity);
		var type = component.GetType().Name;
		return $"{name} ({type}): '{fieldName}' not assigned";
	}

	public static string MissingField(this object obj, string fieldName) {
		return $"{obj}: '{fieldName}' not assigned";
	}

	public static string MissingComponent(this Entity entity, string componentName) {
		var name = Messages.EntityName(entity);
		return $"{name}: misses component of type {componentName}";
	}

	public static string MissingComponentOnPrefab(this EntityComponent component, string prefabField, string componentName) {
		var name = Messages.EntityName(component.Entity);
		var type = component.GetType().Name;
		return $"{name} ({type}): '{prefabField}' misses component of type {componentName}";
	}

	public static string MissingEntitiesOnPrefab(this EntityComponent component, string prefabField) {
		var name = Messages.EntityName(component.Entity);
		var type = component.GetType().Name;
		return $"{name} ({type}): '{prefabField}' has no entities";
	}

	public static string EntitiesCountMismatchOnPrefab(
		this EntityComponent component,
		string prefabField,
		int expected,
		int actual
	) {
		var name = Messages.EntityName(component.Entity);
		var type = component.GetType().Name;
		return $"{name} ({type}): '{prefabField}' had {actual} number of entities, but we expected only {expected}";
	}

	public static string MissingService<T>(this EntityComponent component) {
		var name = Messages.EntityName(component.Entity);
		var type = component.GetType().Name;
		return $"{name} ({type}): Needed '{typeof(T).Name}' Service, but it was missing"; ;
	}

	public static string KeyNotFound(this AnimationComponent animation, string key) {
		var name = Messages.EntityName(animation.Entity);
		return $"{name} ({nameof(AnimationComponent)}): key '{key}' not found"; ;
	}
}
