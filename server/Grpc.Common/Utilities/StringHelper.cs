namespace Grpc.Common.Utilities;

public static class StringHelper
{
    public static string GenerateRandomString(int length, string? prefix)
    {
        if (length <= 0)
        {
            throw new ArgumentException("Length must be greater than zero.", nameof(length));
        }

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        Span<char> stringChars = stackalloc char[length + (prefix == null ? 0 : prefix.Length)];

        prefix?.AsSpan().CopyTo(stringChars);

        for (int i = (prefix == null ? 0 : prefix.Length); i < stringChars.Length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }

        return new string(stringChars);
    }
}
