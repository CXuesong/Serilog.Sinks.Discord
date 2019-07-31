using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CXuesong.Uel.Serilog.Sinks.Discord
{
    internal class MarkdownUtility
    {

        private static readonly char[] markdownEscapedCharacters = @"|*#{}[]()\".ToCharArray();
        private static readonly Regex markdownEscapedCharactersMatcher = new Regex("[" + Regex.Escape(new string(markdownEscapedCharacters)) + "]");

        public static string Escape(string text)
        {
            if (text.IndexOfAny(markdownEscapedCharacters) < 0) return text;
            return markdownEscapedCharactersMatcher.Replace(text, @"\$0");
        }

        public static string MakeLink(string text, string url)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            sb.Append(Escape(text));
            sb.Append("](<");
            // Discord will simply ignore `\)` in the link content.
            sb.Append(url.Replace("(", "%28").Replace(")", "%29"));
            sb.Append(">)");
            return sb.ToString();
        }

    }
}
