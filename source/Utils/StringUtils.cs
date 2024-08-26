using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NowPlaying.Utils
{
    public static class StringUtils
    {

        /// <summary>
        /// QuotedSplit() options:
        ///   Split-on-quotes mode (stickyQuotes = false, default) - splits input string at non-quoted white spice and at quotation marks.
        ///   Sticky-quotes mode (stickyQuotes = true) - splits input string at non-quoted white space.
        ///   
        ///   Examples: 
        ///   QuotedSplit("a \"b c\" 'd e' -f='g h':i -j=\"k l\"")      ==>     { "a", "\"b c\"", "'d e'", "-f=", "'g h'", ":i", "-j=", "\"k l\"" }
        ///   QuotedSplit("a \"b c\" 'd e' -f='g h':i -j=\"k l\"", stickyQuotes=true)  ==>  { "a", "\"b c\"", "'d e'", "-f='g h':i", "-j=\"k l\"" }
        /// </summary>
        public static List<string> QuotedSplit(string str, bool stickyQuotes = false)
        {
            var defaultPattern = @"[^\s""']+|""([^""]*)""|'([^']*)'";
            var stickyPattern = @"([^\s""]*""[^""]*""[^\s""]*)|([^\s']*'[^']*'[^\s']*)|(\S+)";
            string pattern = stickyQuotes ? stickyPattern : defaultPattern;

            //var testInp = "a \"b c\" 'd e' -f='g h':i -j=\"k l\"";
            //var testOut = Regex.Matches(testInp, defaultPattern).Cast<Match>().Select(m => m.Value).ToList(); // QuotedSplit(testInp);
            //var testOut2 = Regex.Matches(testInp, stickyPattern).Cast<Match>().Select(m => m.Value).ToList(); // QuotedSplit(testInp, stickyQuotes = true);

            return Regex.Matches(str, pattern).Cast<Match>().Select(m => m.Value).ToList();
        }

        public static string Unquote(string str)
        {
            var first = str[0];
            var last = str[str.Length - 1];
            if (first == '\"' && last == '\"' || first == '\'' && last == '\'')
            {
                return str.Substring(1, str.Length - 2);
            }
            else { return str; }
        }

        public static List<string> Unquote(List<string> strList)
        {
            return strList?.Select(s => StringUtils.Unquote(s)).ToList();
        }
    }
}
