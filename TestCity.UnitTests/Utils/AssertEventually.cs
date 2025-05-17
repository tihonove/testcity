using System.Diagnostics;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace TestCity.UnitTests.Utils;

internal static class AssertEventually
{
    internal static async Task That<T>(Func<T> value, IResolveConstraint constraintExpression)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(60))
        {
            try
            {
                var constraint = constraintExpression.Resolve();
                var result = constraint.ApplyTo(value());
                if (!result.IsSuccess)
                {
                    await Task.Delay(1000);
                }
                else
                {
                    return;
                }
            }
            catch
            {
                await Task.Delay(1000);
            }
        }
        Assert.That(value(), constraintExpression);
    }
}
