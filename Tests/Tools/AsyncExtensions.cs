namespace Tests;

using System.Collections.Generic;
using System.Threading.Tasks;

public static class AsyncExtensions {
	public static IEnumerable<Task<T>> ToTasks<T>(this IEnumerable<T> items) {
		foreach (var item in items) {
			yield return Task.FromResult(item);
		}
	}
}
