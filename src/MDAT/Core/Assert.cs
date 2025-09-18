namespace MDAT
{
    public class AssertFailedException : Exception
    {
        public AssertFailedException(string message) : base(message) { }
    }

    public static class Assert
    {
        public static void True(bool condition, string? because = null)
        {
            if (!condition) throw new AssertFailedException($"Expected true. {because}");
        }

        public static void False(bool condition, string? because = null)
        {
            if (condition) throw new AssertFailedException($"Expected false. {because}");
        }

        public static void Equal<T>(T expected, T actual, IEqualityComparer<T>? cmp = null)
        {
            cmp ??= EqualityComparer<T>.Default;
            if (!cmp.Equals(expected, actual))
                throw new AssertFailedException($"Expected: {expected}\nActual:   {actual}");
        }

        public static void NotNull(object? value, string? because = null)
        {
            if (value is null) throw new AssertFailedException($"Expected non-null. {because}");
        }

        public static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T>? cmp = null)
        {
            cmp ??= EqualityComparer<T>.Default;
            using var e1 = expected.GetEnumerator();
            using var e2 = actual.GetEnumerator();
            int i = 0;
            for (; ; i++)
            {
                bool m1 = e1.MoveNext(), m2 = e2.MoveNext();
                if (!m1 && !m2) return;
                if (m1 != m2) throw new AssertFailedException($"Sequences differ in length at index {i}.");
                if (!cmp.Equals(e1.Current!, e2.Current!))
                    throw new AssertFailedException($"Sequences differ at index {i}. Expected {e1.Current}, got {e2.Current}.");
            }
        }
    }
}