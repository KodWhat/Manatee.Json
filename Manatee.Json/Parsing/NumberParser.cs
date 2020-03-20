﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Manatee.Json.Internal;

namespace Manatee.Json.Parsing
{
	internal class NumberParser : IJsonParser
	{
		public bool Handles(char c)
		{
			return c == '0' || c == '1' || c == '2' || c == '3' || c == '4' ||
			       c == '5' || c == '6' || c == '7' || c == '8' || c == '9' ||
			       c == '-';
		}

		public bool TryParse(string source, ref int index, [NotNullWhen(true)] out JsonValue? value, [NotNullWhen(false)] out string? errorMessage, bool allowExtraChars)
		{
			if (index >= source.Length)
				throw new ArgumentOutOfRangeException(nameof(index));

			value = null;

			var originalIndex = index;
			while (index < source.Length)
			{
				var c = source[index];
				if (char.IsWhiteSpace(c) || c == ',' || c == ']' || c == '}') break;

				var isNumber = _IsNumberChar(c);
				if (!isNumber && allowExtraChars) break;
				if (!isNumber)
				{
					errorMessage = "Expected ',', ']', or '}'.";
					return false;
				}

				index++;
			}

			var result = source.Substring(originalIndex, index - originalIndex);
			if (!double.TryParse(result, NumberStyles.Any, CultureInfo.InvariantCulture, out double dbl))
			{
				errorMessage = $"Value not recognized: '{result}'";
				return false;
			}

			value = dbl;
			errorMessage = null;
			return true;
		}

		public bool TryParse(TextReader stream, [NotNullWhen(true)] out JsonValue? value, [NotNullWhen(false)] out string? errorMessage)
		{
			value = null;

			var buffer = StringBuilderCache.Acquire();
			while (stream.Peek() != -1)
			{
				var c = (char)stream.Peek();
				if (char.IsWhiteSpace(c) || c == ',' || c == ']' || c == '}') break;

				stream.Read(); // eat the character

				if (!_IsNumberChar(c))
				{
					StringBuilderCache.Release(buffer);
					errorMessage = "Expected ',', ']', or '}'.";
					return false;
				}

				buffer.Append(c);
			}

			var result = StringBuilderCache.GetStringAndRelease(buffer);
			if (!double.TryParse(result, NumberStyles.Any, CultureInfo.InvariantCulture, out double dbl))
			{
				errorMessage = $"Value not recognized: '{result}'";
				return false;
			}

			value = dbl;
			errorMessage = null;
			return true;
		}

		public async Task<(string? errorMessage, JsonValue? value)> TryParseAsync(TextReader stream, CancellationToken token)
		{
			var buffer = StringBuilderCache.Acquire();
			var scratch = SmallBufferCache.Acquire(1);

			string? errorMessage = null;
			while (stream.Peek() != -1)
			{
				if (token.IsCancellationRequested)
				{
					errorMessage = "Parsing incomplete. The task was cancelled.";
					break;
				}

				var c = (char)stream.Peek();
				if (char.IsWhiteSpace(c) || c == ',' || c == ']' || c == '}') break;

				await stream.TryRead(scratch, 0, 1, token); // eat the character

				if (!_IsNumberChar(c))
				{
					errorMessage = "Expected ',', ']', or '}'.";
					break;
				}

				buffer.Append(c);
			}

			SmallBufferCache.Release(scratch);

			if (errorMessage != null)
			{
				StringBuilderCache.Release(buffer);
				return (errorMessage, null);
			}

			var result = StringBuilderCache.GetStringAndRelease(buffer);
			if (!double.TryParse(result, NumberStyles.Any, CultureInfo.InvariantCulture, out double dbl))
				return ($"Value not recognized: '{result}'", null);

			return (null, dbl);
		}

		private static bool _IsNumberChar(char c)
		{
			return c == '0' || c == '1' || c == '2' || c == '3' || c == '4' ||
			       c == '5' || c == '6' || c == '7' || c == '8' || c == '9' ||
			       c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E';
		}
	}
}
