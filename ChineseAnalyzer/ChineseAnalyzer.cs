using System;
using System.Collections.Generic;
using System.Text;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;

namespace VanillaLib
{
    /// <summary>
    /// 
    /// </summary>
    public class ChineseAnalyzer:Analyzer
    {
        /// <summary>Constructs a {@link StandardTokenizer} filtered by a {@link
        /// StandardFilter}, a {@link LowerCaseFilter} and a {@link StopFilter}. 
        /// </summary>
        public override sealed TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
        {
            TokenStream result = new ChineseTokenizer(reader);
            result = new ChineseFilter(result);
            //result = new StandardFilter(result);
            //result = new LowerCaseFilter(result);
            //result = new StopFilter(result, CHINESE_ENGLISH_STOP_WORDS);
            return result;
        }

        private class SavedStreams
        {
            protected internal Tokenizer source;
            protected internal TokenStream result;
        };

        public override TokenStream ReusableTokenStream(string fieldName, System.IO.TextReader reader)
        {
            SavedStreams streams = PreviousTokenStream as SavedStreams;
            if (streams == null)
            {
                streams = new SavedStreams();
                streams.source = new ChineseTokenizer(reader);
                streams.result = new ChineseFilter(streams.source);
                PreviousTokenStream = streams;
            }
            else
            {
                streams.source.Reset(reader);
            }
            return streams.result;
        }

    }
}
