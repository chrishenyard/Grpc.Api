using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

namespace Grpc.Service.Client.Testing;

public record TestResult(string TestName, bool Passed, TimeSpan Duration, string? ErrorMessage = null, string? StackTrace = null, string? Category = null);

public class TestRunner
{
    private readonly List<TestResult> _results = new();
    private readonly SemaphoreSlim _consoleLock = new(1, 1);
    private readonly int _maxDegreeOfParallelism;

    public TestRunner(int maxDegreeOfParallelism = 4)
    {
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    public async Task<TestResult> RunTestAsync(string testName, Func<Task> testAction, string? category = null)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await testAction();
            stopwatch.Stop();
            var result = new TestResult(testName, true, stopwatch.Elapsed, Category: category);
            lock (_results)
            {
                _results.Add(result);
            }
            await WriteLineAsync($"✓ {testName} ({stopwatch.ElapsedMilliseconds}ms)", ConsoleColor.Green);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var result = new TestResult(testName, false, stopwatch.Elapsed, ex.Message, ex.StackTrace, category);
            lock (_results)
            {
                _results.Add(result);
            }
            await WriteLineAsync($"✗ {testName} ({stopwatch.ElapsedMilliseconds}ms)", ConsoleColor.Red);
            await WriteLineAsync($"  Error: {ex.Message}", ConsoleColor.Red);
            return result;
        }
    }

    public async Task DiscoverAndRunTestsAsync(object testFixture, TestContext context)
    {
        var type = testFixture.GetType();
        var testMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<TestAttribute>() != null)
            .ToList();

        var setupMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.GetCustomAttribute<SetupAttribute>() != null);

        var teardownMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.GetCustomAttribute<TeardownAttribute>() != null);

        if (testMethods.Count == 0)
        {
            await WriteLineAsync("No test methods found.", ConsoleColor.Yellow);
            return;
        }

        await WriteLineAsync($"Found {testMethods.Count} test(s) in {type.Name}", ConsoleColor.Cyan);
        Console.WriteLine();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxDegreeOfParallelism
        };

        await Parallel.ForEachAsync(testMethods, parallelOptions, async (method, ct) =>
        {
            var testAttr = method.GetCustomAttribute<TestAttribute>()!;

            if (testAttr.Skip)
            {
                await WriteLineAsync($"⊘ {method.Name} (Skipped: {testAttr.SkipReason})", ConsoleColor.DarkGray);
                return;
            }

            // Run setup
            if (setupMethod != null)
            {
                try
                {
                    var setupTask = setupMethod.Invoke(testFixture, new object[] { context });
                    if (setupTask is Task task)
                        await task;
                }
                catch (Exception ex)
                {
                    await WriteLineAsync($"✗ {method.Name} - Setup failed: {ex.Message}", ConsoleColor.Red);
                    lock (_results)
                    {
                        _results.Add(new TestResult(method.Name, false, TimeSpan.Zero, $"Setup failed: {ex.Message}", ex.StackTrace, testAttr.Category));
                    }
                    return;
                }
            }

            // Run test
            await RunTestAsync(method.Name, async () =>
            {
                var result = method.Invoke(testFixture, new object[] { context });
                if (result is Task task)
                    await task;
            }, testAttr.Category);

            // Run teardown
            if (teardownMethod != null)
            {
                try
                {
                    var teardownTask = teardownMethod.Invoke(testFixture, new object[] { context });
                    if (teardownTask is Task task)
                        await task;
                }
                catch (Exception ex)
                {
                    await WriteLineAsync($"⚠ {method.Name} - Teardown failed: {ex.Message}", ConsoleColor.Yellow);
                }
            }
        });
    }

    public void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("Test Summary");
        Console.WriteLine("═══════════════════════════════════════════════════════");

        var passed = _results.Count(r => r.Passed);
        var failed = _results.Count(r => !r.Passed);
        var totalDuration = TimeSpan.FromMilliseconds(_results.Sum(r => r.Duration.TotalMilliseconds));

        Console.WriteLine($"Total:    {_results.Count}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Passed:   {passed}");
        Console.ResetColor();

        if (failed > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed:   {failed}");
            Console.ResetColor();
        }

        Console.WriteLine($"Duration: {totalDuration.TotalSeconds:F2}s");
        Console.WriteLine("═══════════════════════════════════════════════════════");

        if (failed > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failed Tests:");
            Console.ResetColor();
            foreach (var result in _results.Where(r => !r.Passed))
            {
                Console.WriteLine($"  - {result.TestName}: {result.ErrorMessage}");
            }
        }

        // Print category summary
        var categories = _results.Where(r => !string.IsNullOrEmpty(r.Category))
            .GroupBy(r => r.Category)
            .ToList();

        if (categories.Any())
        {
            Console.WriteLine();
            Console.WriteLine("By Category:");
            foreach (var group in categories)
            {
                var categoryPassed = group.Count(r => r.Passed);
                var categoryFailed = group.Count(r => !r.Passed);
                Console.WriteLine($"  {group.Key}: {categoryPassed} passed, {categoryFailed} failed");
            }
        }
    }

    public void ExportJUnitXml(string filePath)
    {
        var totalTests = _results.Count;
        var failures = _results.Count(r => !r.Passed);
        var totalTime = _results.Sum(r => r.Duration.TotalSeconds);

        var testSuite = new XElement("testsuite",
            new XAttribute("name", "GrpcServiceTests"),
            new XAttribute("tests", totalTests),
            new XAttribute("failures", failures),
            new XAttribute("errors", 0),
            new XAttribute("time", totalTime.ToString("F3")),
            new XAttribute("timestamp", DateTime.UtcNow.ToString("o")),
            _results.Select(r => new XElement("testcase",
                new XAttribute("name", r.TestName),
                new XAttribute("classname", r.Category ?? "General"),
                new XAttribute("time", r.Duration.TotalSeconds.ToString("F3")),
                !r.Passed ? new XElement("failure",
                    new XAttribute("message", r.ErrorMessage ?? "Test failed"),
                    new XCData(r.StackTrace ?? string.Empty)) : null
            ))
        );

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            testSuite
        );

        doc.Save(filePath);
        Console.WriteLine();
        Console.WriteLine($"JUnit XML report saved to: {filePath}");
    }

    public int GetExitCode() => _results.Any(r => !r.Passed) ? 1 : 0;

    private async Task WriteLineAsync(string message, ConsoleColor? color = null)
    {
        await _consoleLock.WaitAsync();
        try
        {
            if (color.HasValue)
                Console.ForegroundColor = color.Value;
            Console.WriteLine(message);
            if (color.HasValue)
                Console.ResetColor();
        }
        finally
        {
            _consoleLock.Release();
        }
    }
}
