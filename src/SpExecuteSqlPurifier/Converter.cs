using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SpExecuteSqlPurifier
{
    public class Converter : IConvert
    {
        #region Patterns

        private readonly string patternMatch = @"
(?<!--[\ \t]*)
\b
exec
\s+
sp_executesql
\s+
(?:@[a-z@_][a-z@_0-9]*\s*=\s*)?
(?:
  N?'(?:''|[^'])*'
  |
  (?:[+-]\s*)?\$\s*?\d+(?:\.\d*)?
  |
  (?:[+-]\s*)?\d+(?:\.\d*)?(?:[\ \t]*[eE](?:[+\-]?\d+)?)?
)
(?:
  \s*,\s*
  (?:@[\w@_][\w@_0-9]*\s*=\s*)?
  (?:
    N?'(?:''|[^'])*'
    |
    (?:[+-]\s*)?\$\s*?\d+(?:\.\d*)?
    |
    (?:[+-]\s*)?\d+(?:\.\d*)?(?:[\ \t]*[eE](?:[+\-]?\d+)?)?
    |
    (?:NULL)
  )
)*
(?:\s*;[\ \t]*\r?\n?)?
";

//        private readonly string patternExtract = @"
//(?<!--[\ \t]*)
//\b
//exec
//\s+
//sp_executesql
//\s+
//(?<part>
//  (?:@[a-z@_][a-z@_0-9]*\s*=\s*)?
//  (?:
//    N?'(?:''|[^'])*'
//    |
//    \$\s*?\d+(?:\.\d*)?
//    |
//    \d+(?:\.\d*)?(?:[\ \t]*[eE](?:[+\-]?\d+)?)?
//  )
//)
//(?:
//  \s*,\s*
//  (?<part>
//    (?:@[a-z@_][a-z@_0-9]*\s*=\s*)?
//    (?:
//      N?'(?:''|[^'])*'
//      |
//      \$\s*?\d+(?:\.\d*)?
//      |
//      \d+(?:\.\d*)?(?:[\ \t]*[eE](?:[+\-]?\d+)?)?
//    )
//  )
//)*
//(?:\s*;[\ \t]*\r?\n?)?
//";

        private readonly string patternPart = @"
(?:(?<name>@[\w@_][\w@_0-9]*)\s*=\s*)?
(?<value>
  N?'(?:''|[^'])*'
  |
  (?:[+-]\s*)?\$\s*?\d+(?:\.\d*)?
  |
  (?:[+-]\s*)?\d+(?:\.\d*)?(?:[\ \t]*[eE](?:[+\-]?\d+)?)?
  |
  (?:NULL)
)
";

        private readonly string patternDefinition = @"
(?<name>@[\w@_][\w@_0-9]*)
\s+
(?<type>[\w_][\w_0-9]*(\s*\([^)]*\))?)?
\s*,?\s*
";

        private readonly string patternVariable = @"
--[^\n]*\r?\n?
|
\[(?:\]\]|[^\]])*\]
|
'(?:''|[^'])*'
|
""(?:""""|[^""])*""
|
(?<![\w0-9_])(?<name>@[\w@_][\w@_0-9]*)
";

        #endregion

        public bool Variables;

        public string Convert(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
            RegexOptions regexOptions = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft; 
            Regex regexMatch = new Regex(patternMatch, regexOptions);
            foreach (Match match in regexMatch.Matches(value))
            {
                value = (0 < match.Index ? value.Substring(0, match.Index - 1) : "")
                    + Partial(match.Value) + "\r\n"
                    + value.Substring(match.Index + match.Length)
                    ;
            }
            return value;
        }

        private string Partial(string input)
        {
            RegexOptions regexOptions = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant;
            Regex regexPart = new Regex(patternPart, regexOptions);
            int n = 0;
            StringBuilder sb = new StringBuilder();
            Dictionary<string, string> vars = new Dictionary<string, string>();
            Dictionary<string, string> vals = new Dictionary<string, string>();
            foreach (Match matchPart in regexPart.Matches(input))
            {
                n++;
                //string name = matchPart.Groups["name"];
                //string value = matchPart.Groups["value"];
                string name, value;
                (name, value) = (matchPart.Groups["name"].Value, matchPart.Groups["value"].Value);
                name += "";
                if (n == 1)
                {
                    sb.Append(Strip(value));
                    continue;
                }
                if (n == 2)
                {
                    foreach (KeyValuePair<string, string> item in Definitions(Strip(value)))
                    {
                        vars[item.Key] = item.Value;
                    }
                    continue;
                }
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }
                vals[name] = value;
            }

            string sql = "";

            if (Variables)
            {
                foreach (KeyValuePair<string, string> v in vars)
                {
                    string x = "";
                    if (vals.ContainsKey(v.Key))
                    {
                        x = " = " + vals[v.Key];
                    }
                    sql += "DECLARE " + v.Key + " " + v.Value + x + " ;" + "\r\n";
                }
                sql += sb.ToString().TrimEnd();
                sql += "\r\n" + "GO";
            }
            else
            {
                sql += Parse(sb.ToString(), vars, vals);
            }

            return sql;
        }

        private string Parse(string query, Dictionary<string, string> vars, Dictionary<string, string> vals)
        {
            RegexOptions regexOptions = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace;
            Regex regexVariable = new Regex(patternVariable, regexOptions);
            int Δ = 0;
            foreach (Match matchVariable in regexVariable.Matches(query))
            {
                string name = matchVariable.Groups["name"].Value;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }
                if (!vars.ContainsKey(name) || !vals.ContainsKey(name))
                {
                    continue;
                }
                string value = vals[name] ?? "";
                int p = matchVariable.Index;
                int l = matchVariable.Length;
                query = ""
                    + (p + Δ > 0 ? query.Substring(0, p + Δ) : "")
                    + value
                    + query.Substring(p + l + Δ)
                    ;
                Δ += value.Length - l;
            }
            return query;
        }

        private Dictionary<string, string> Definitions(string input)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            RegexOptions regexOptions = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace;
            Regex regexDefinition = new Regex(patternDefinition, regexOptions);
            foreach (Match matchDefinition in regexDefinition.Matches(input))
            {
                dictionary[matchDefinition.Groups["name"].Value] = matchDefinition.Groups["type"].Value;
            }
            return dictionary;
        }

        private string Strip(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
            else if (value.Length >= 3 && value.StartsWith("N'") && value.EndsWith("'"))
            {
                return value.Substring(2, value.Length - 3).Replace("''", "'");
            }
            if (value.Length >= 2 && value.StartsWith("'") && value.EndsWith("'"))
            {
                return value.Substring(1, value.Length - 2).Replace("''", "'");
            }
            else
            {
                return value;
            }
        }
    }
}
