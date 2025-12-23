namespace Grpc.Service.Client.Testing;

public static class Assert
{
    public static void NotNull<T>(T? value, string? message = null)
    {
        if (value == null)
            throw new AssertionException(message ?? "Value should not be null");
    }

    public static void Null<T>(T? value, string? message = null)
    {
        if (value != null)
            throw new AssertionException(message ?? "Value should be null");
    }

    public static void True(bool condition, string? message = null)
    {
        if (!condition)
            throw new AssertionException(message ?? "Condition should be true");
    }

    public static void False(bool condition, string? message = null)
    {
        if (condition)
            throw new AssertionException(message ?? "Condition should be false");
    }

    public static void Equal<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new AssertionException(message ?? $"Expected: {expected}, Actual: {actual}");
    }

    public static void NotEqual<T>(T notExpected, T actual, string? message = null)
    {
        if (EqualityComparer<T>.Default.Equals(notExpected, actual))
            throw new AssertionException(message ?? $"Values should not be equal: {actual}");
    }

    public static void NotEmpty(string? value, string? message = null)
    {
        if (string.IsNullOrEmpty(value))
            throw new AssertionException(message ?? "String should not be empty");
    }

    public static void Contains(string expectedSubstring, string actualString, string? message = null)
    {
        if (actualString == null || !actualString.Contains(expectedSubstring))
            throw new AssertionException(message ?? $"String should contain: {expectedSubstring}");
    }

    public static async Task ThrowsAsync<TException>(Func<Task> action, string? message = null) where TException : Exception
    {
        try
        {
            await action();
            throw new AssertionException(message ?? $"Expected exception of type {typeof(TException).Name} but no exception was thrown");
        }
        catch (TException)
        {
            // Expected exception - test passes
        }
        catch (Exception ex)
        {
            throw new AssertionException(message ?? $"Expected exception of type {typeof(TException).Name} but got {ex.GetType().Name}: {ex.Message}");
        }
    }
}

public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}