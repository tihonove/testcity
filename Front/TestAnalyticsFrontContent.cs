using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kontur.TestAnalytics.Front;

public static class TestAnalyticsFrontContent
{
	private static string ResourcesPrefix => typeof(TestAnalyticsFrontContent).Namespace + ".dist";

	public static IEnumerable<string> EnumerateFiles()
	{
		return typeof(TestAnalyticsFrontContent).Assembly.GetManifestResourceNames().Select(x => x.Replace(ResourcesPrefix + ".", ""));
	}

	public static Stream GetFile(string relativePath) =>
		typeof(TestAnalyticsFrontContent).Assembly.GetManifestResourceStream(
			ResourcesPrefix + "." + relativePath.Replace("/", ".").Replace("\\", ".")
		)
		?? throw new Exception($"Content file not found '{relativePath}'");
}
