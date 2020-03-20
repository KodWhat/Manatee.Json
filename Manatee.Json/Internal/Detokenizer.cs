﻿using System.Text.RegularExpressions;

namespace Manatee.Json.Internal
{
	internal static class Detokenizer
	{
		private static readonly Regex _tokenPattern = new Regex(@"\{\{(?<key>[a-z,0-9]*)(?<format>:.*?)?\}\}", RegexOptions.IgnoreCase);

		public static string ResolveTokens(this string template, JsonObject settings)
		{
			var matches = _tokenPattern.Matches(template);

			foreach (Match? match in matches)
			{
				var key = match!.Groups["key"].Value;
				var format = match.Groups["format"]?.Value;

				if (!settings.TryGetValue(key, out var setting)) continue;

				var formatString = format == null ? "{0}" : $"{{0:{format}}}";
				var replacement = string.Format(formatString, setting);

				template = template.Replace(match.Value, replacement);
			}

			return template;
		}
	}
}