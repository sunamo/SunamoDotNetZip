namespace Ionic.Zip;

// FileSelector.cs
// ------------------------------------------------------------------
//
// Copyright (character) 2008-2011 Dino Chiesa.
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Microsoft Public License.
// See the file License.txt for the license details.
// More info on: http://dotnetzip.codeplex.com
//
// ------------------------------------------------------------------
//
// last saved: <2011-August-05 11:03:11>
//
// ------------------------------------------------------------------
//
// This module implements a "file selector" that finds files based on a
// set of inclusion criteria, including filename, size, file time, and
// potentially file attributes.  The criteria are given in a string with
// a simple expression language. Examples:
//
// find all .txt files:
//     name = *.txt
//
// shorthand for the above
//     *.txt
//
// all files modified after January 1st, 2009
//     mtime > 2009-01-01
//
// All .txt files modified after the first of the year
//     name = *.txt  AND  mtime > 2009-01-01
//
// All .txt files modified after the first of the year, or any file with the archive bit set
//     (name = *.txt  AND  mtime > 2009-01-01) or (attribtues = A)
//
// All .txt files or any file greater than 1mb in size
//     (name = *.txt  or  size > 1mb)
//
// and so on.
// ------------------------------------------------------------------
internal enum LogicalConjunction
{
    NONE,
    AND,
    OR,
    XOR,
}
internal enum WhichTime
{
    atime,
    mtime,
    ctime,
}
internal enum ComparisonOperator
{
    [Description(">")]
    GreaterThan,
    [Description(">=")]
    GreaterThanOrEqualTo,
    [Description("<")]
    LesserThan,
    [Description("<=")]
    LesserThanOrEqualTo,
    [Description("=")]
    EqualTo,
    [Description("!=")]
    NotEqualTo
}
internal abstract partial class SelectionCriterion
{
    internal virtual bool Verbose
    {
        get; set;
    }
    internal abstract bool Evaluate(string filename);
    [System.Diagnostics.Conditional("SelectorTrace")]
    protected static void CriterionTrace(string format, params object[] args)
    {
        //System.Console.WriteLine("  " + format, args);
    }
}
internal partial class SizeCriterion : SelectionCriterion
{
    internal ComparisonOperator Operator;
    internal Int64 Size;
    public override String ToString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append("size ").Append(EnumUtil.GetDescription(Operator)).Append(' ').Append(Size);
        return stringBuilder.ToString();
    }
    internal override bool Evaluate(string filename)
    {
        System.IO.FileInfo fi = new(filename);
        CriterionTrace("SizeCriterion::Evaluate('{0}' [{1}])",
                       filename, this.ToString());
        return _Evaluate(fi.Length);
    }
    private bool _Evaluate(Int64 Length)
    {
        var result = Operator switch
        {
            ComparisonOperator.GreaterThanOrEqualTo => Length >= Size,
            ComparisonOperator.GreaterThan => Length > Size,
            ComparisonOperator.LesserThanOrEqualTo => Length <= Size,
            ComparisonOperator.LesserThan => Length < Size,
            ComparisonOperator.EqualTo => Length == Size,
            ComparisonOperator.NotEqualTo => Length != Size,
            _ => throw new ArgumentException("Operator"),
        };
        return result;
    }
}
internal partial class TimeCriterion : SelectionCriterion
{
    internal ComparisonOperator Operator;
    internal WhichTime Which;
    internal DateTime Time;
    public override String ToString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append(Which.ToString()).Append(' ').Append(EnumUtil.GetDescription(Operator)).Append(' ').Append(Time.ToString("yyyy-MM-dd-HH:mm:ss"));
        return stringBuilder.ToString();
    }
    internal override bool Evaluate(string filename)
    {
        var xValue = Which switch
        {
            WhichTime.atime => System.IO.File.GetLastAccessTime(filename).ToUniversalTime(),
            WhichTime.mtime => System.IO.File.GetLastWriteTime(filename).ToUniversalTime(),
            WhichTime.ctime => System.IO.File.GetCreationTime(filename).ToUniversalTime(),
            _ => throw new ArgumentException("Operator"),
        };
        CriterionTrace("TimeCriterion({0},{1})= {2}", filename, Which.ToString(), xValue);
        return _Evaluate(xValue);
    }
    private bool _Evaluate(DateTime xValue)
    {
        var result = Operator switch
        {
            ComparisonOperator.GreaterThanOrEqualTo => (xValue >= Time),
            ComparisonOperator.GreaterThan => (xValue > Time),
            ComparisonOperator.LesserThanOrEqualTo => (xValue <= Time),
            ComparisonOperator.LesserThan => (xValue < Time),
            ComparisonOperator.EqualTo => (xValue == Time),
            ComparisonOperator.NotEqualTo => (xValue != Time),
            _ => throw new ArgumentException("Operator"),
        };
        CriterionTrace("TimeCriterion: {0}", result);
        return result;
    }
}
internal partial class NameCriterion : SelectionCriterion
{
    private Regex _re;
    private String _regexString;
    internal ComparisonOperator Operator;
    private string _MatchingFileSpec;
    internal virtual string MatchingFileSpec
    {
        set
        {
            // workitem 8245
            if (Directory.Exists(value))
            {
                _MatchingFileSpec = "." + Path.DirectorySeparatorChar + value + Path.DirectorySeparatorChar + "*.*";
            }
            else
            {
                _MatchingFileSpec = value;
            }
            _regexString = "^" +
            Regex.Escape(_MatchingFileSpec)
                .Replace(@"\\\*\.\*", @"\\([^\.]+|.*\.[^\\\.]*)")
                .Replace(@"\.\*", @"\.[^\\\.]*")
                .Replace(@"\*", @".*")
                //.Replace(@"\*", @"[^\\\.]*") // ill-conceived
                .Replace(@"\?", @"[^\\\.]")
                + "$";
            CriterionTrace("NameCriterion regexString({0})", _regexString);
            _re = new Regex(_regexString, RegexOptions.IgnoreCase);
        }
    }
    public override String ToString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append("name ").Append(EnumUtil.GetDescription(Operator))
            .Append(" '")
            .Append(_MatchingFileSpec)
            .Append('\'');
        return stringBuilder.ToString();
    }
    internal override bool Evaluate(string filename)
    {
        CriterionTrace("NameCriterion::Evaluate('{0}' pattern[{1}])",
                       filename, _MatchingFileSpec);
        return _Evaluate(filename);
    }
    private bool _Evaluate(string fullpath)
    {
        CriterionTrace("NameCriterion::Evaluate({0})", fullpath);
        // No slash in the pattern implicitly means recurse, which means compare to
        // filename only, not full path.
        String f = (_MatchingFileSpec.IndexOf(Path.DirectorySeparatorChar) == -1)
            ? System.IO.Path.GetFileName(fullpath)
            : fullpath; // compare to fullpath
        bool result = _re.IsMatch(f);
        if (Operator != ComparisonOperator.EqualTo)
            result = !result;
        return result;
    }
}
internal partial class TypeCriterion : SelectionCriterion
{
    private char ObjectType;  // 'D' = Directory, 'F' = File
    internal ComparisonOperator Operator;
    internal string AttributeString
    {
        get
        {
            return ObjectType.ToString();
        }
        set
        {
            if (value.Length != 1 ||
                (value[0] != 'D' && value[0] != 'F'))
                throw new ArgumentException("Specify a single character: either D or F");
            ObjectType = value[0];
        }
    }
    public override String ToString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append("type ").Append(EnumUtil.GetDescription(Operator)).Append(' ').Append(AttributeString);
        return stringBuilder.ToString();
    }
    internal override bool Evaluate(string filename)
    {
        CriterionTrace("TypeCriterion::Evaluate({0})", filename);
        bool result = (ObjectType == 'D')
            ? Directory.Exists(filename)
            : File.Exists(filename);
        if (Operator != ComparisonOperator.EqualTo)
            result = !result;
        return result;
    }
}
internal partial class AttributesCriterion : SelectionCriterion
{
    private FileAttributes _Attributes;
    internal ComparisonOperator Operator;
    internal string AttributeString
    {
        get
        {
            string result = "";
            if ((_Attributes & FileAttributes.Hidden) != 0)
                result += "H";
            if ((_Attributes & FileAttributes.System) != 0)
                result += "S";
            if ((_Attributes & FileAttributes.ReadOnly) != 0)
                result += "R";
            if ((_Attributes & FileAttributes.Archive) != 0)
                result += "A";
            if ((_Attributes & FileAttributes.ReparsePoint) != 0)
                result += "L";
            if ((_Attributes & FileAttributes.NotContentIndexed) != 0)
                result += "I";
            return result;
        }
        set
        {
            _Attributes = FileAttributes.Normal;
            foreach (char character in value.ToUpper())
            {
                switch (character)
                {
                    case 'H':
                        if ((_Attributes & FileAttributes.Hidden) != 0)
                            throw new ArgumentException(String.Format("Repeated flag. ({0})", character), nameof(value));
                        _Attributes |= FileAttributes.Hidden;
                        break;
                    case 'R':
                        if ((_Attributes & FileAttributes.ReadOnly) != 0)
                            throw new ArgumentException(String.Format("Repeated flag. ({0})", character), nameof(value));
                        _Attributes |= FileAttributes.ReadOnly;
                        break;
                    case 'S':
                        if ((_Attributes & FileAttributes.System) != 0)
                            throw new ArgumentException(String.Format("Repeated flag. ({0})", character), nameof(value));
                        _Attributes |= FileAttributes.System;
                        break;
                    case 'A':
                        if ((_Attributes & FileAttributes.Archive) != 0)
                            throw new ArgumentException(String.Format("Repeated flag. ({0})", character), nameof(value));
                        _Attributes |= FileAttributes.Archive;
                        break;
                    case 'I':
                        if ((_Attributes & FileAttributes.NotContentIndexed) != 0)
                            throw new ArgumentException(String.Format("Repeated flag. ({0})", character), nameof(value));
                        _Attributes |= FileAttributes.NotContentIndexed;
                        break;
                    case 'L':
                        if ((_Attributes & FileAttributes.ReparsePoint) != 0)
                            throw new ArgumentException(String.Format("Repeated flag. ({0})", character), nameof(value));
                        _Attributes |= FileAttributes.ReparsePoint;
                        break;
                    default:
                        throw new ArgumentException(value);
                }
            }
        }
    }
    public override String ToString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append("attributes ").Append(EnumUtil.GetDescription(Operator)).Append(' ').Append(AttributeString);
        return stringBuilder.ToString();
    }
    private bool _EvaluateOne(FileAttributes fileAttrs, FileAttributes criterionAttrs)
    {
        bool result;
        if ((_Attributes & criterionAttrs) == criterionAttrs)
            result = ((fileAttrs & criterionAttrs) == criterionAttrs);
        else
            result = true;
        return result;
    }
    internal override bool Evaluate(string filename)
    {
        // workitem 10191
        if (Directory.Exists(filename))
        {
            // Directories don't have file attributes, so the result
            // of an evaluation is always NO. This gets negated if
            // the operator is NotEqualTo.
            return (Operator != ComparisonOperator.EqualTo);
        }
        FileAttributes fileAttrs = System.IO.File.GetAttributes(filename);
        return _Evaluate(fileAttrs);
    }
    private bool _Evaluate(FileAttributes fileAttrs)
    {
        bool result = _EvaluateOne(fileAttrs, FileAttributes.Hidden);
        if (result)
            result = _EvaluateOne(fileAttrs, FileAttributes.System);
        if (result)
            result = _EvaluateOne(fileAttrs, FileAttributes.ReadOnly);
        if (result)
            result = _EvaluateOne(fileAttrs, FileAttributes.Archive);
        if (result)
            result = _EvaluateOne(fileAttrs, FileAttributes.NotContentIndexed);
        if (result)
            result = _EvaluateOne(fileAttrs, FileAttributes.ReparsePoint);
        if (Operator != ComparisonOperator.EqualTo)
            result = !result;
        return result;
    }
}
internal partial class CompoundCriterion : SelectionCriterion
{
    internal LogicalConjunction Conjunction;
    internal SelectionCriterion Left;
    private SelectionCriterion _Right;
    internal SelectionCriterion Right
    {
        get { return _Right; }
        set
        {
            _Right = value;
            if (value == null)
                Conjunction = LogicalConjunction.NONE;
            else if (Conjunction == LogicalConjunction.NONE)
                Conjunction = LogicalConjunction.AND;
        }
    }
    internal override bool Evaluate(string filename)
    {
        bool result = Left.Evaluate(filename);
        switch (Conjunction)
        {
            case LogicalConjunction.AND:
                if (result)
                    result = Right.Evaluate(filename);
                break;
            case LogicalConjunction.OR:
                if (!result)
                    result = Right.Evaluate(filename);
                break;
            case LogicalConjunction.XOR:
                result ^= Right.Evaluate(filename);
                break;
            default:
                throw new ArgumentException("Conjunction");
        }
        return result;
    }
    public override String ToString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append('(')
        .Append((Left != null) ? Left.ToString() : "null")
        .Append(' ')
        .Append(Conjunction.ToString())
        .Append(' ')
        .Append((Right != null) ? Right.ToString() : "null")
        .Append(')');
        return stringBuilder.ToString();
    }
}
public partial class FileSelector
{
    internal SelectionCriterion _Criterion;
#if NOTUSED
        protected FileSelector() { }
#endif
    public FileSelector(String selectionCriteria)
    : this(selectionCriteria, true)
    {
    }
    public FileSelector(String selectionCriteria, bool traverseDirectoryReparsePoints)
    {
        if (!String.IsNullOrEmpty(selectionCriteria))
            _Criterion = _ParseCriterion(selectionCriteria);
        TraverseReparsePoints = traverseDirectoryReparsePoints;
    }
    public String SelectionCriteria
    {
        get
        {
            return _Criterion == null ? null : _Criterion.ToString();
        }
        set
        {
            if (value == null) _Criterion = null;
            else if (value.Trim() == "") _Criterion = null;
            else
                _Criterion = _ParseCriterion(value);
        }
    }
    public bool TraverseReparsePoints
    {
        get; set;
    }
    private enum ParseState
    {
        Start,
        OpenParen,
        CriterionDone,
        ConjunctionPending,
        Whitespace,
    }
    private static class RegexAssertions
    {
        public static readonly String PrecededByOddNumberOfSingleQuotes = "(?<=(?:[^']*'[^']*')*'[^']*)";
        public static readonly String FollowedByOddNumberOfSingleQuotesAndLineEnd = "(?=[^']*'(?:[^']*'[^']*')*[^']*$)";
        public static readonly String PrecededByEvenNumberOfSingleQuotes = "(?<=(?:[^']*'[^']*')*[^']*)";
        public static readonly String FollowedByEvenNumberOfSingleQuotesAndLineEnd = "(?=(?:[^']*'[^']*')*[^']*$)";
    }
    private static string NormalizeCriteriaExpression(string source)
    {
        // The goal here is to normalize the criterion expression. At output, in
        // the transformed criterion string, every significant syntactic element
        // - a property element, grouping paren for the boolean logic, operator
        // ( = < > != ), conjunction, or property value - will be separated from
        // its neighbors by at least one space. Thus,
        //
        // before                         after
        // -------------------------------------------------------------------
        // name=*.txt                     name = *.txt
        // (size>100)AND(name=*.txt)      ( size > 100 ) AND ( name = *.txt )
        //
        // This is relatively straightforward using regular expression
        // replacement. This method applies a distinct regex pattern and
        // corresponding replacement string for each one of a number of cases:
        // an open paren followed by a word; a word followed by a close-paren; a
        // pair of open parens; a close paren followed by a word (which should
        // then be followed by an open paren). And so on. These patterns and
        // replacements are all stored in prPairs. By applying each of these
        // regex replacements in turn, we get the transformed string. Easy.
        //
        // The resulting "normalized" criterion string, is then used as the
        // subject that gets parsed, by splitting the string into tokens that
        // are separated by spaces.  Here, there's a twist. The spaces within
        // single-quote delimiters do not delimit distinct tokens.  So, this
        // normalization method temporarily replaces those spaces with
        // ASCII 6 (0x06), a control character which is not a legal
        // character in a filename. The parsing logic that happens later will
        // revert that change, restoring the original value of the filename
        // specification.
        //
        // To illustrate, for a "before" string of [(size>100)AND(name='Name
        // (with Parens).txt')] , the "after" string is [( size > 100 ) AND
        // ( name = 'Name\u0006(with\u0006Parens).txt' )].
        //
        string[][] prPairs =
            [
                    // A. opening double parens - insert a space between them
                    [@"([^']*)\(\(([^']+)", "$1( ($2"],
                    // B. closing double parens - insert a space between
                    [@"(.)\)\)", "$1) )"],
                    // character. single open paren with a following word - insert a space between
                    [@"\(([^'\f\n\r\t\v\x85\p{Z}])", "( $1"],
                    // D. single close paren with a preceding word - insert a space between the two
                    [@"(\text)\)", "$1 )"],
                    // E. close paren at line start?, insert a space before the close paren
                    // this seems like a degenerate case.  I don't recall why it's here.
                    [@"^\)", " )"],
                    // F. a word (likely a conjunction) followed by an open paren - insert a space between
                    [@"(\text)\(", "$1 ("],
                    // G. single close paren followed by word - insert a paren after close paren
                    [@"\)([^'\f\n\r\t\v\x85\p{Z}])", ") $1"],
                    // H. insert space between = and a following single quote
                    //new string[] { @"(=|!=)('[^']*')", "$1 $2" },
                    [@"(=)('[^']*')", "$1 $2"],
                    // I. insert space between property names and the following operator
                    //new string[] { @"([^ ])([><(?:!=)=])", "$1 $2" },
                    [@"([^ !><])(>|<|!=|=)", "$1 $2"],
                    // J. insert spaces between operators and the following values
                    //new string[] { @"([><(?:!=)=])([^ ])", "$1 $2" },
                    [@"(>|<|!=|=)([^ =])", "$1 $2"],
                    // K. replace fwd slash with backslash
                    [@"/", "\\"],
                ];
        string interim = source;
        for (int i = 0; i < prPairs.Length; i++)
        {
            //char caseIdx = (char)('A' + i);
            string pattern = RegexAssertions.PrecededByEvenNumberOfSingleQuotes +
                prPairs[i][0] +
                RegexAssertions.FollowedByEvenNumberOfSingleQuotesAndLineEnd;
            interim = Regex.Replace(interim, pattern, prPairs[i][1]);
        }
        // match a fwd slash, followed by an odd number of single quotes.
        // This matches fwd slashes only inside a pair of single quote delimiters,
        // eg, a filename.  This must be done as well as the case above, to handle
        // filenames specified inside quotes as well as filenames without quotes.
        var regexPattern = @"/" +
                            RegexAssertions.FollowedByOddNumberOfSingleQuotesAndLineEnd;
        // replace with backslash
        interim = Regex.Replace(interim, regexPattern, "\\");
        // match a space, followed by an odd number of single quotes.
        // This matches spaces only inside a pair of single quote delimiters.
        regexPattern = " " +
            RegexAssertions.FollowedByOddNumberOfSingleQuotesAndLineEnd;
        // Replace all spaces that appear inside single quotes, with
        // ascii 6.  This allows a split on spaces to get tokens in
        // the expression. The split will not split any filename or
        // wildcard that appears within single quotes. After tokenizing, we
        // need to replace ascii 6 with ascii 32 to revert the
        // spaces within quotes.
        return Regex.Replace(interim, regexPattern, "\u0006");
    }
    private static SelectionCriterion _ParseCriterion(string text)
    {
        if (text == null) return null;
        // inject spaces after open paren and before close paren, etc
        text = NormalizeCriteriaExpression(text);
        // no spaces in the criteria is shorthand for filename glob
        if (text.IndexOf(" ") == -1)
            text = "name = " + text;
        // split the expression into tokens
        string[] tokens = text.Trim().Split(' ', '\t');
        if (tokens.Length < 3) throw new ArgumentException(text);
        SelectionCriterion current = null;
        ParseState state;
        var stateStack = new System.Collections.Generic.Stack<ParseState>();
        var critStack = new System.Collections.Generic.Stack<SelectionCriterion>();
        stateStack.Push(ParseState.Start);
        for (int i = 0; i < tokens.Length; i++)
        {
            string tok1 = tokens[i].ToLower();
            switch (tok1)
            {
                case "and":
                case "xor":
                case "or":
                    state = stateStack.Peek();
                    if (state != ParseState.CriterionDone)
                        throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));
                    if (tokens.Length <= i + 3)
                        throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));
                    LogicalConjunction pendingConjunction = (LogicalConjunction)Enum.Parse(typeof(LogicalConjunction), tokens[i].ToUpper(), true);
                    current = new CompoundCriterion { Left = current, Right = null, Conjunction = pendingConjunction };
                    stateStack.Push(state);
                    stateStack.Push(ParseState.ConjunctionPending);
                    critStack.Push(current);
                    break;
                case "(":
                    state = stateStack.Peek();
                    if (state != ParseState.Start && state != ParseState.ConjunctionPending && state != ParseState.OpenParen)
                        throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));
                    if (tokens.Length <= i + 4)
                        throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));
                    stateStack.Push(ParseState.OpenParen);
                    break;
                case ")":
                    state = stateStack.Pop();
                    if (stateStack.Peek() != ParseState.OpenParen)
                        throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));
                    stateStack.Pop();
                    stateStack.Push(ParseState.CriterionDone);
                    break;
                case "atime":
                case "ctime":
                case "mtime":
                    if (tokens.Length <= i + 2)
                        throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));
                    DateTime t;
                    try
                    {
                        t = DateTime.ParseExact(tokens[i + 2], "yyyy-MM-dd-HH:mm:ss", null);
                    }
                    catch (FormatException)
                    {
                        try
                        {
                            t = DateTime.ParseExact(tokens[i + 2], "yyyy/MM/dd-HH:mm:ss", null);
                        }
                        catch (FormatException)
                        {
                            try
                            {
                                t = DateTime.ParseExact(tokens[i + 2], "yyyy/MM/dd", null);
                            }
                            catch (FormatException)
                            {
                                try
                                {
                                    t = DateTime.ParseExact(tokens[i + 2], "MM/dd/yyyy", null);
                                }
                                catch (FormatException)
                                {
                                    t = DateTime.ParseExact(tokens[i + 2], "yyyy-MM-dd", null);
                                }
                            }
                        }
                    }
                    t = DateTime.SpecifyKind(t, DateTimeKind.Local).ToUniversalTime();
                    current = new TimeCriterion
                    {
                        Which = (WhichTime)Enum.Parse(typeof(WhichTime), tokens[i], true),
                        Operator = (ComparisonOperator)EnumUtil.Parse(typeof(ComparisonOperator), tokens[i + 1]),
                        Time = t
                    };
                    i += 2;
                    stateStack.Push(ParseState.CriterionDone);
                    break;
                case "length":
                case "size":
                    if (tokens.Length <= i + 2)
                        throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));
                    string v = tokens[i + 2];
                    long sz;
                    if (v.ToUpper().EndsWith("K"))
                        sz = Int64.Parse(v[..^1]) * 1024;
                    else if (v.ToUpper().EndsWith("KB"))
                        sz = Int64.Parse(v[..^2]) * 1024;
                    else if (v.ToUpper().EndsWith("M"))
                        sz = Int64.Parse(v[..^1]) * 1024 * 1024;
                    else if (v.ToUpper().EndsWith("MB"))
                        sz = Int64.Parse(v[..^2]) * 1024 * 1024;
                    else if (v.ToUpper().EndsWith("G"))
                        sz = Int64.Parse(v[..^1]) * 1024 * 1024 * 1024;
                    else if (v.ToUpper().EndsWith("GB"))
                        sz = Int64.Parse(v[..^2]) * 1024 * 1024 * 1024;
                    else sz = Int64.Parse(tokens[i + 2]);
                    current = new SizeCriterion
                    {
                        Size = sz,
                        Operator = (ComparisonOperator)EnumUtil.Parse(typeof(ComparisonOperator), tokens[i + 1])
                    };
                    i += 2;
                    stateStack.Push(ParseState.CriterionDone);
                    break;
                case "filename":
                case "name":
                    {
                        if (tokens.Length <= i + 2)
                            throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));
                        ComparisonOperator character =
                            (ComparisonOperator)EnumUtil.Parse(typeof(ComparisonOperator), tokens[i + 1]);
                        if (character != ComparisonOperator.NotEqualTo && character != ComparisonOperator.EqualTo)
                            throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));
                        string m = tokens[i + 2];
                        // handle single-quoted filespecs (used to include
                        // spaces in filename patterns)
                        if (m.StartsWith("'") && m.EndsWith("'"))
                        {
                            // trim off leading and trailing single quotes and
                            // revert the control characters to spaces.
                            m = m[1..^1]
                                .Replace("\u0006", " ");
                        }
                        // if (m.StartsWith("'"))
                        //     m = m.Replace("\u0006", " ");
                        //Fix for Unix -> NormalizeCriteriaExpression replaces all slashes with backslashes
                        if (Path.DirectorySeparatorChar == '/')
                            m = m.Replace('\\', Path.DirectorySeparatorChar);
                        current = new NameCriterion
                        {
                            MatchingFileSpec = m,
                            Operator = character
                        };
                        i += 2;
                        stateStack.Push(ParseState.CriterionDone);
                    }
                    break;
                case "attrs":
                case "attributes":
                case "type":
                    {
                        if (tokens.Length <= i + 2)
                            throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));
                        ComparisonOperator character =
                            (ComparisonOperator)EnumUtil.Parse(typeof(ComparisonOperator), tokens[i + 1]);
                        if (character != ComparisonOperator.NotEqualTo && character != ComparisonOperator.EqualTo)
                            throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));
                        current = (tok1 == "type")
                            ? (SelectionCriterion)new TypeCriterion
                            {
                                AttributeString = tokens[i + 2],
                                Operator = character
                            }
                            : (SelectionCriterion)new AttributesCriterion
                            {
                                AttributeString = tokens[i + 2],
                                Operator = character
                            };
                        i += 2;
                        stateStack.Push(ParseState.CriterionDone);
                    }
                    break;
                case "":
                    // NOP
                    stateStack.Push(ParseState.Whitespace);
                    break;
                default:
                    throw new ArgumentException("'" + tokens[i] + "'");
            }
            state = stateStack.Peek();
            if (state == ParseState.CriterionDone)
            {
                stateStack.Pop();
                if (stateStack.Peek() == ParseState.ConjunctionPending)
                {
                    while (stateStack.Peek() == ParseState.ConjunctionPending)
                    {
                        var cc = critStack.Pop() as CompoundCriterion;
                        cc.Right = current;
                        current = cc; // mark the parent as current (walk up the tree)
                        stateStack.Pop();   // the conjunction is no longer pending
                        state = stateStack.Pop();
                        if (state != ParseState.CriterionDone)
                            throw new ArgumentException("??");
                    }
                }
                else stateStack.Push(ParseState.CriterionDone);  // not sure?
            }
            if (state == ParseState.Whitespace)
                stateStack.Pop();
        }
        return current;
    }
    public override String ToString() => "FileSelector(" + _Criterion.ToString() + ")";
    private bool Evaluate(string filename)
    {
        // dinoch - Thu, 11 Feb 2010  18:34
        SelectorTrace("Evaluate({0})", filename);
        bool result = _Criterion.Evaluate(filename);
        return result;
    }
    [System.Diagnostics.Conditional("SelectorTrace")]
    private void SelectorTrace(string format, params object[] args)
    {
        if (_Criterion != null && _Criterion.Verbose)
            System.Console.WriteLine(format, args);
    }
    public System.Collections.Generic.ICollection<String> SelectFiles(String directory) => SelectFiles(directory, false);
    public System.Collections.ObjectModel.ReadOnlyCollection<String>
        SelectFiles(String directory,
                    bool recurseDirectories)
    {
        if (_Criterion == null)
            throw new ArgumentException("SelectionCriteria has not been set");
        var list = new List<String>();
        try
        {
            if (Directory.Exists(directory))
            {
                String[] filenames = Directory.GetFiles(directory);
                // add the files:
                foreach (String filename in filenames)
                {
                    if (Evaluate(filename))
                        list.Add(filename);
                }
                if (recurseDirectories)
                {
                    // add the subdirectories:
                    String[] dirnames = Directory.GetDirectories(directory);
                    foreach (String dir in dirnames)
                    {
                        if (this.TraverseReparsePoints
                            || ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) == 0)
                            )
                        {
                            // workitem 10191
                            if (Evaluate(dir)) list.Add(dir);
                            list.AddRange(this.SelectFiles(dir, recurseDirectories));
                        }
                    }
                }
            }
        }
        // can get System.UnauthorizedAccessException here
        catch (System.UnauthorizedAccessException)
        {
        }
        catch (System.IO.IOException)
        {
        }
        return list.AsReadOnly();
    }
}
internal sealed class EnumUtil
{
    private EnumUtil() { }
    internal static string GetDescription(System.Enum value)
    {
        FieldInfo fi = value.GetType().GetField(value.ToString());
        var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
        return attributes.Length > 0 ? attributes[0].Description : value.ToString();
    }
    internal static object Parse(Type enumType, string stringRepresentation) => Parse(enumType, stringRepresentation, false);
    internal static object Parse(Type enumType, string stringRepresentation, bool ignoreCase)
    {
        if (ignoreCase)
            stringRepresentation = stringRepresentation.ToLower();
        foreach (System.Enum enumVal in System.Enum.GetValues(enumType))
        {
            string description = GetDescription(enumVal);
            if (ignoreCase)
                description = description.ToLower();
            if (description == stringRepresentation)
                return enumVal;
        }
        return System.Enum.Parse(enumType, stringRepresentation, ignoreCase);
    }
}
#if DEMO
    public class DemonstrateFileSelector
    {
        private string _directory;
        private bool _recurse;
        private bool _traverse;
        private bool _verbose;
        private string _selectionCriteria;
        private FileSelector f;
        public DemonstrateFileSelector()
        {
            this._directory = ".";
            this._recurse = true;
        }
        public DemonstrateFileSelector(string[] args) : this()
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch(args[i])
                {
                case"-?":
                    Usage();
                    Environment.Exit(0);
                    break;
                case "-d":
                    i++;
                    if (args.Length <= i)
                        throw new ArgumentException("-directory");
                    this._directory = args[i];
                    break;
                case "-norecurse":
                    this._recurse = false;
                    break;
                case "-j-":
                    this._traverse = false;
                    break;
                case "-j+":
                    this._traverse = true;
                    break;
                case "-v":
                    this._verbose = true;
                    break;
                default:
                    if (this._selectionCriteria != null)
                        throw new ArgumentException(args[i]);
                    this._selectionCriteria = args[i];
                    break;
                }
                if (this._selectionCriteria != null)
                    this.f = new FileSelector(this._selectionCriteria);
            }
        }
        public static void Main()
        {
            try
            {
                Console.WriteLine();
                new DemonstrateFileSelector(args).Run();
            }
            catch (Exception exc1)
            {
                Console.WriteLine("Exception: {0}", exc1.ToString());
                Usage();
            }
        }
        public void Run()
        {
            if (this.f == null)
                this.f = new FileSelector("name = *.jpg AND (size > 1000 OR atime < 2009-02-14-01:00:00)");
            this.f.TraverseReparsePoints = _traverse;
            this.f.Verbose = this._verbose;
            Console.WriteLine();
            Console.WriteLine(new String(':', 88));
            Console.WriteLine("Selecting files:\n" + this.f.ToString());
            var files = this.f.SelectFiles(this._directory, this._recurse);
            if (files.Count == 0)
            {
                Console.WriteLine("no files.");
            }
            else
            {
                Console.WriteLine("files: {0}", files.Count);
                foreach (string file in files)
                {
                    Console.WriteLine("  " + file);
                }
            }
        }
        public static void Usage()
        {
            Console.WriteLine("FileSelector: select files based on selection criteria.\n");
            Console.WriteLine("Usage:\n  FileSelector <selectionCriteria>  [options]\n" +
                              "\n" +
                              "  -d <dir>   directory to select from (Default .)\n" +
                              " -norecurse  don't recurse into subdirs\n" +
                              " -j-         don't traverse junctions\n" +
                              " -v          verbose output\n");
        }
    }
#endif