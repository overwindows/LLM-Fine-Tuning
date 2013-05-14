using System;

using Analyzer = Lucene.Net.Analysis.Analyzer;
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using Document = Lucene.Net.Documents.Document;
using FilterIndexReader = Lucene.Net.Index.FilterIndexReader;
using IndexReader = Lucene.Net.Index.IndexReader;
using QueryParser = Lucene.Net.QueryParsers.QueryParser;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using Version = Lucene.Net.Util.Version;

using Collector = Lucene.Net.Search.Collector;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using Query = Lucene.Net.Search.Query;
using ScoreDoc = Lucene.Net.Search.ScoreDoc;
using Scorer = Lucene.Net.Search.Scorer;
using Searcher = Lucene.Net.Search.Searcher;
using TopScoreDocCollector = Lucene.Net.Search.TopScoreDocCollector;

using System.IO;
using System.Collections;
using VanillaLib;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using Lucene.Net.Index;
using System.Text;
using System.Collections.Generic;
using SpellChecker.Net.Search.Spell;

namespace Vanilla
{
    public class SearchUrls
    {
        private class AnonymousClassCollector : Collector
        {
            private Scorer scorer;
            private int docBase;

            // simply print docId and score of every matching document
            public override void Collect(int doc)
            {
                System.Console.Out.WriteLine("doc=" + doc + docBase + " score=" + scorer.Score());
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return true; }
            }

            public override void SetNextReader(IndexReader reader, int docBase)
            {
                this.docBase = docBase;
            }

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }
        }

        /// <summary>Use the norms from one field for all fields.  Norms are read into memory,
        /// using a byte of memory per document per searched field.  This can cause
        /// search of large collections with a large number of fields to run out of
        /// memory.  If all of the fields contain only a single token, then the norms
        /// are all identical, then single norm vector may be shared. 
        /// </summary>
        private class OneNormsReader : FilterIndexReader
        {
            private System.String field;

            public OneNormsReader(IndexReader in_Renamed, System.String field)
                : base(in_Renamed)
            {
                this.field = field;
            }

            public override byte[] Norms(System.String field)
            {
                return in_Renamed.Norms(this.field);
            }
        }

        private System.Collections.Generic.Dictionary<string, float> urlScore;
        private int offset;
        private Hashtable DictUrls;
        private const int PhraseSlop = 3;
        private string host;
        private SortedList<string, Dictionary<string, int>> mapLnkTxt;
        private Dictionary<string, string> dictDescUrl;
        SpellChecker.Net.Search.Spell.SpellChecker vanillaChecker;

        public SearchUrls(String WebSite, String Downloadfolder)
        {            
            urlScore = new System.Collections.Generic.Dictionary<string, float>();

            MyUri _uri = new MyUri(WebSite);
            host = _uri.AbsoluteUri;
            FileStream fs = new FileStream(Downloadfolder + "\\" + _uri.Host + "_DB", FileMode.Open);
            BinaryFormatter bf = new BinaryFormatter();
            DictUrls = (Hashtable)bf.Deserialize(fs);
            fs.Close();

            FileStream fs0 = new FileStream(Downloadfolder + "\\" + _uri.Host + "_LinkDesc", FileMode.Open);
            BinaryFormatter bf0 = new BinaryFormatter();
            mapLnkTxt = bf0.Deserialize(fs0) as SortedList<string, Dictionary<string, int>>;
            fs0.Close();

            string tempIndexPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "VanillaDescription.index"+_uri.Host);
            string tempIndexFileName =  Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "vanilla.description.index." + _uri.Host);

            if (!new FileInfo(tempIndexFileName).Exists)
            {
                dictDescUrl = new Dictionary<string, string>();

                FileStream fsLnkDesc = new FileStream(tempIndexFileName, FileMode.Create);
                StreamWriter swLnkDesc = new StreamWriter(fsLnkDesc, Encoding.Default);
                foreach (KeyValuePair<string, Dictionary<string, int>> kv in mapLnkTxt)
                {
                    int Cnt = 0;
                    string lnkDesc = string.Empty;
                    foreach (KeyValuePair<string, int> desc in kv.Value)
                    {
                        if (desc.Value > Cnt)
                        {
                            lnkDesc = desc.Key;
                            Cnt = desc.Value;
                        }
                    }
                    if (dictDescUrl.ContainsKey(lnkDesc))
                        continue;
                    dictDescUrl.Add(lnkDesc, kv.Key);
                    swLnkDesc.WriteLine(lnkDesc);
                }
                swLnkDesc.Close();
                fsLnkDesc.Close();
            }
            else
            {
                dictDescUrl = new Dictionary<string, string>();
                foreach (KeyValuePair<string, Dictionary<string, int>> kv in mapLnkTxt)
                {
                    int Cnt = 0;
                    string lnkDesc = string.Empty;
                    foreach (KeyValuePair<string, int> desc in kv.Value)
                    {
                        if (desc.Value > Cnt)
                        {
                            lnkDesc = desc.Key;
                            Cnt = desc.Value;
                        }
                    }
                    if (dictDescUrl.ContainsKey(lnkDesc))
                        continue;
                    dictDescUrl.Add(lnkDesc, kv.Key);                    
                }
            }

            vanillaChecker =
                new SpellChecker.Net.Search.Spell.SpellChecker(FSDirectory.Open(new DirectoryInfo(tempIndexPath)), new JaroWinklerDistance());
            
            vanillaChecker.IndexDictionary(new PlainTextDictionary(new FileInfo(tempIndexFileName)));
            

            // www.baidu.com site search
        }

        private class myComparerClass : IComparer<KeyValuePair<string, float>>
        {
            #region IComparer<KeyValuePair<string,float>> Members

            public int Compare(KeyValuePair<string, float> x, KeyValuePair<string, float> y)
            {
                return Comparer<float>.Default.Compare(y.Value, x.Value);
            }

            #endregion
        }


        public ArrayList UrlList
        {
            get
            {
                ArrayList aryLst = new ArrayList();
                List<KeyValuePair<string, float>> Lst = new List<KeyValuePair<string, float>>();

                foreach (KeyValuePair<string, float> kv in urlScore)
                {
                    //Filter Main Page
                    if (kv.Key.Equals(host))
                        continue;
                    Lst.Add(kv);
                }

                Lst.Sort(new myComparerClass());

                foreach (KeyValuePair<string, float> kv in Lst)
                {
                    aryLst.Add(kv.Key);
                }

                return aryLst;
            }
        }

        /// <summary>Simple command-line based search demo. </summary>
        public void StartQuerying(string indexPath, string keywords)
        {
            //System.String usage = "Usage:\t" + typeof(SearchUrls) + "[-index dir] [-field f] [-repeat n] [-queries file] [-raw] [-norms field] [-paging hitsPerPage]";
            //usage += "\n\tSpecify 'false' for hitsPerPage to use streaming instead of paging search.";
            #region 链接文字分析（短语相似度）
            urlScore.Clear();
            String[] Res = vanillaChecker.SuggestSimilar(keywords, 3);

            for (int i = 0; i < Res.Length; i++)
            {
                if (vanillaChecker.CorrespondScore[i] > 0.6)
                    urlScore.Add(dictDescUrl[Res[i]], vanillaChecker.CorrespondScore[i]);
            }
            #endregion

            if (urlScore.Count > 0)
                return;

            System.String index = indexPath;
            offset = indexPath.Length - 5;
            System.String fieldCntnt = "contents";
            String fieldDesc = "description";
            System.String queries = null;

            System.String normsField = null;
            bool paging = true;
            int hitsPerPage = 10;

            IndexReader reader = IndexReader.Open(FSDirectory.Open(new System.IO.DirectoryInfo(index)), true); // only searching, so read-only=true

            if (normsField != null)
                reader = new OneNormsReader(reader, normsField);

            Searcher searcher = new IndexSearcher(reader);
            Analyzer analyzer = new ChineseAnalyzer();

            System.IO.StreamReader in_Renamed = null;
            if (queries != null)
            {
                in_Renamed = new System.IO.StreamReader(new System.IO.StreamReader(queries, System.Text.Encoding.Default).BaseStream, new System.IO.StreamReader(queries, System.Text.Encoding.Default).CurrentEncoding);
            }
            else
            {
                in_Renamed = new System.IO.StreamReader(new System.IO.StreamReader(new MemoryStream(System.Text.Encoding.Default.GetBytes(keywords)), System.Text.Encoding.Default).BaseStream, new System.IO.StreamReader(System.Console.OpenStandardInput(), System.Text.Encoding.Default).CurrentEncoding);
            }

            QueryParser parserDesc = new QueryParser(Version.LUCENE_30, fieldDesc, analyzer);
            QueryParser parserContents = new QueryParser(Version.LUCENE_30, fieldCntnt, analyzer);

            //Inital
            Query queryDesc;
            int alreadyGotUrlsCnt = 0;
            System.String line = in_Renamed.ReadLine();
            if (line == null || line.Length == -1)
                return;
            line = line.Trim();
            if (line.Length == 0)
                return;

            while (parserDesc.PhraseSlop <= 1)
            {
                queryDesc = parserDesc.Parse(line);
                System.Console.Out.WriteLine("(Description) Searching for: " + queryDesc.ToString(fieldDesc));

                if (paging)
                {
                    DoPagingSearch(in_Renamed, searcher, queryDesc, hitsPerPage, 1);
                    if (this.urlScore.Keys.Count == 0)
                    {
                        parserDesc.PhraseSlop++;
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
            }

/*****************************************************************************************************
            if (this.urlScore.Keys.Count == 0)
            {
                queryDesc = parserDesc.Parse(line);
                if (queryDesc is Lucene.Net.Search.PhraseQuery)
                {
                    Term[] terms = (queryDesc as Lucene.Net.Search.PhraseQuery).GetTerms();
                    StringBuilder strBuilder = new StringBuilder();

                    if (terms == null)
                        return;

                    for (int ix = 0; ix < terms.GetLength(0); ix++)
                    {
                        strBuilder.Append(terms.GetValue(ix));
                        strBuilder.Append(" ");
                    }

                    queryDesc = parserDesc.Parse(strBuilder.ToString().TrimEnd());
                    DoPagingSearch(in_Renamed, searcher, queryDesc, hitsPerPage, 0);
                }
            }
*****************************************************************************************************/

            if (this.urlScore.Keys.Count < 2 )
            {
                #region TermQuery
                Query query = parserContents.Parse(line);
                if (query is Lucene.Net.Search.PhraseQuery)
                {
                    Term[] terms = (query as Lucene.Net.Search.PhraseQuery).GetTerms();

                    StringBuilder strBuilder = new StringBuilder();

                    if (terms == null)
                        return;

                    for (int ix = 0; ix < terms.GetLength(0); ix++)
                    {
                        strBuilder.Append(terms.GetValue(ix));
                        strBuilder.Append(" ");
                    }

                    query = parserContents.Parse(strBuilder.ToString().TrimEnd());
                    DoPagingSearch(in_Renamed, searcher, query, hitsPerPage, 0);
                }
                #endregion
                alreadyGotUrlsCnt = this.urlScore.Keys.Count;
                if (alreadyGotUrlsCnt > 8)
                    alreadyGotUrlsCnt = 0;
                #region PhraseQuery
                while (parserContents.PhraseSlop <= PhraseSlop)
                {
                    query = parserContents.Parse(line);
                    System.Console.Out.WriteLine("Searching for: " + query.ToString(fieldCntnt));

                    if (paging)
                    {
                        DoPagingSearch(in_Renamed, searcher, query, hitsPerPage, 1);
                        if (this.urlScore.Keys.Count == alreadyGotUrlsCnt)
                        {
                            parserContents.PhraseSlop++;
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }

                    System.Console.WriteLine("PhraseSlop: " + parserContents.PhraseSlop);
                }
                #endregion
            }
            reader.Dispose();

        }

        private string GetMD5Code(string Str)
        {
            MD5CryptoServiceProvider MD5CSP = new MD5CryptoServiceProvider();
            Byte[] bHashTable = MD5CSP.ComputeHash(System.Text.Encoding.Unicode.GetBytes(Str));
            return System.BitConverter.ToString(bHashTable).Replace("-", "");
        }
        /// <summary> This demonstrates a typical paging search scenario, where the search engine presents 
        /// pages of size n to the user. The user can then go to the next page if interested in
        /// the next hits.
        /// 
        /// When the query is executed for the first time, then only enough results are collected
        /// to fill 5 result pages. If the user wants to page beyond this limit, then the query
        /// is executed another time and all hits are collected.
        /// 
        /// </summary>
        public void DoPagingSearch(System.IO.StreamReader in_Renamed, Searcher searcher, Query query, int hitsPerPage, int type)
        {

            // Collect enough docs to show 5 pages
            TopScoreDocCollector collector = TopScoreDocCollector.Create(5 * hitsPerPage, false);
            searcher.Search(query, collector);
            ScoreDoc[] hits = collector.TopDocs().ScoreDocs;

            int numTotalHits = collector.TotalHits;
            //System.Console.Out.WriteLine(numTotalHits + " total matching pages");



            int start = 0;
            int end = System.Math.Min(numTotalHits, hitsPerPage);

            #region Comment
            //while (true)
            //{
            //if (end > hits.Length)
            //{
            //    System.Console.Out.WriteLine("Only results 1 - " + hits.Length + " of " + numTotalHits + " total matching documents collected.");
            //    System.Console.Out.WriteLine("Collect more (y/n) ?");
            //    System.String line = in_Renamed.ReadLine();
            //    if (line.Length == 0 || line[0] == 'n')
            //    {
            //        break;
            //    }

            //    collector = TopScoreDocCollector.Create(numTotalHits, false);
            //    searcher.Search(query, collector);
            //    hits = collector.TopDocs().ScoreDocs;
            //}
            #endregion

            //end = System.Math.Min(hits.Length, start + hitsPerPage);
            for (int i = start; i < end; i++)
            {
                //if (type == 1)
                //{
                //    hits[i].Score *= (float)(PhraseSlop / Math.Sqrt(PhraseSlop));
                //}

                System.Console.Out.WriteLine("doc=" + hits[i].Doc + " score=" + hits[i].Score);

                Document doc = searcher.Doc(hits[i].Doc);
                System.String path = doc.Get("path");

                string accessUrl = DictUrls[GetMD5Code(Path.GetFullPath(path))] as string;

                if (!urlScore.ContainsKey(accessUrl))
                {
                    urlScore.Add(accessUrl, hits[i].Score);
                }
                else
                {
                    if (hits[i].Score > urlScore[accessUrl])
                    {
                        urlScore[accessUrl] = hits[i].Score;
                    }
                }

                if (path != null)
                {
                    System.Console.Out.WriteLine((i + 1) + ". " + path);
                    #region Summary Info
                    //System.String summary = doc.Get("summary");
                    //if (summary != null)
                    //{
                    //    //System.Console.Out.WriteLine("summary: " + doc.Get("summary"));
                    //    //urlSummaryMap.Add(DictUrls[GetMD5Code(Path.GetFullPath(path))], doc.Get("summary"));
                    //}
                    #endregion
                }
                else
                {
                    System.Console.Out.WriteLine((i + 1) + ". " + "No path for this document");
                }
            }
        }
    }
}
