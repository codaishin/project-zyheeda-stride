namespace ProjectZyheeda;

public static class ReferenceExtensions {

	public static SystemError MissingTarget<TTarget>(this IReference<TTarget> reference) {
		return reference.MissingField(nameof(reference.Target));
	}
}
