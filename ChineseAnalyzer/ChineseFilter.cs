using System;
using System.IO;
using System.Collections;
using System.Globalization;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace VanillaLib
{
    // TODO: convert this XML code to valid .NET
    /// <summary>
    /// A {@link TokenFilter} with a stop word table.  
    /// <ul>
    /// <li>Numeric tokens are removed.</li>
    /// <li>English tokens must be larger than 1 char.</li>
    /// <li>One Chinese char as one Chinese word.</li>
    /// </ul>
    /// TO DO:
    /// <ol>
    /// <li>Add Chinese stop words, such as \ue400</li>
    /// <li>Dictionary based Chinese word extraction</li>
    /// <li>Intelligent Chinese word extraction</li>
    /// </ol>
    /// </summary>
    public sealed class ChineseFilter : TokenFilter
    {
        // Only English now, Chinese to be added later.
        // 考虑配入自定义Stop Words
        public static readonly System.String[] STOP_WORDS =
            new System.String[] { "a", "an", "and", "are", "as", "at", "be", 
                "but", "by", "for", "if", "in", "into", "is", "it", "no", "not", "of",
                "on", "or", "s", "such", "t", "that", "the", "their", "then", "there", 
                "these", "they", "this", "to", "was", "will", "with", "我", "我们", "的" };
        
        private CharArraySet stopTable;
        private ITermAttribute termAtt;

        public ChineseFilter(TokenStream _in)
            : base(_in)
        {
            stopTable = new CharArraySet(STOP_WORDS, false);
            termAtt = AddAttribute<ITermAttribute>();
        }

        public override bool IncrementToken()
        {
            while (input.IncrementToken())
            {
                char[] text = termAtt.TermBuffer();
                int termLength = termAtt.TermLength();

                // why not key off token type here assuming ChineseTokenizer comes first?
                if (!stopTable.Contains(text, 0, termLength))
                {
                    switch (char.GetUnicodeCategory(text[0]))
                    {
                        case UnicodeCategory.LowercaseLetter:
                        case UnicodeCategory.UppercaseLetter:
                        //case UnicodeCategory.DecimalDigitNumber:
                            // English word/token should larger than 1 char.
                            if (termLength > 1)
                            {
                                return true;
                            }
                            break;
                        case UnicodeCategory.OtherLetter:
                            // One Chinese char as one Chinese word.
                            // Chinese word extraction to be added later here.
                            return true;
                    }
                }
            }
            return false;
        }
    }
}
