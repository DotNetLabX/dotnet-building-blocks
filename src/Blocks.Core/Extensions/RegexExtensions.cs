using System.Text.RegularExpressions;

namespace Blocks.Core;

public static class RegexExtensions
{
    public static Match Match(this string content, string pattern)
    {
        Regex rg = new(pattern);
        return rg.Match(content);
    }
    public static MatchCollection Matches(this string content, string pattern)
    {
        Regex rg = new(pattern);
        return rg.Matches(content);
    }
}