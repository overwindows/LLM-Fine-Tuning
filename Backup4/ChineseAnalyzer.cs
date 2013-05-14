using System;
using System.Collections.Generic;
using System.Text;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;

namespace Lucene.Fanswo
{
    /// <summary>
    /// 
    /// </summary>
    public class ChineseAnalyzer:Analyzer
    {
        //private System.Collections.Hashtable stopSet;
        public static readonly System.String[] CHINESE_ENGLISH_STOP_WORDS = new System.String[] { "a", "an", "and", "are", "as", "at", "be", "but", "by", "for", "if", "in", "into", "is", "it", "no", "not", "of", "on", "or", "s", "such", "t", "that", "the", "their", "then", "there", "these", "they", "this", "to", "was", "will", "with", "Œ“", "Œ“√«" };

      
        /// <summary>Constructs a {@link StandardTokenizer} filtered by a {@link
        /// StandardFilter}, a {@link LowerCaseFilter} and a {@link StopFilter}. 
        /// </summary>
        public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
        {
            TokenStream result = new ChineseTokenizer(reader);
            result = new StandardFilter(result);
            result = new LowerCaseFilter(result);
            result = new StopFilter(result, CHINESE_ENGLISH_STOP_WORDS);
            return result;
        }

    }
}
