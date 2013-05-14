using System;
using System.Collections.Generic;

using Document = Lucene.Net.Documents.Document;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using TermEnum = Lucene.Net.Index.TermEnum;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using VanillaLib;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Security.Cryptography;
//using CJKAnalyzer = Lucene.Net.Analysis.CJK.CJKAnalyzer;

namespace Vanilla
{
	
	/// <summary>Indexer for HTML files. </summary>
	public class IndexHTML
	{
        public delegate void ShowDocPath(string docPath);
        public event ShowDocPath OnShowDocPath;

        private SortedList<string, Dictionary<string,int>> mapLnkTxt;
        private Hashtable DictUrls = new Hashtable();

        private string Host;
		public IndexHTML()
		{
            mapLnkTxt = new SortedList<string, Dictionary<string,int>>();
           
		}
		
		private static bool deleting = false; // true during deletion pass
		private static IndexReader reader; // existing index
		private static IndexWriter writer; // new index being built
		private static TermEnum uidIter; // document id iterator
        private HTMLDocument htmlDoc;
		/// <summary>Indexer for HTML files.</summary>
        public void StartIndexing(string Downloadfolder, string host)
		{
            htmlDoc = new HTMLDocument(host);

            FileStream fs = new FileStream(Downloadfolder + "\\" + host + "_LinkDesc", FileMode.Open);
            BinaryFormatter bf = new BinaryFormatter();
            mapLnkTxt = bf.Deserialize(fs) as SortedList<string, Dictionary<string,int>>;
            fs.Close();

			try
			{
                System.IO.DirectoryInfo index = new System.IO.DirectoryInfo(
                     System.IO.Path.Combine(Downloadfolder, "Index", host));
				bool create = true;
                System.IO.FileInfo root = new System.IO.FileInfo(System.IO.Path.Combine(Downloadfolder, host));
				
				//System.String usage = "IndexHTML [-create] [-index <index>] <root_directory>";	               
				
				if (root == null)
				{
					//System.Console.Error.WriteLine("Specify directory to index");
					//System.Console.Error.WriteLine("Usage: " + usage);
					return ;
				}
				System.DateTime start = System.DateTime.Now;
				
				if (!create)
				{
					// delete stale docs
					deleting = true;
					IndexDocs(root, index, create);
				}
                
				writer = new IndexWriter(FSDirectory.Open(index), new VanillaLib.ChineseAnalyzer(), create, new IndexWriter.MaxFieldLength(1000000));
				//writer.SetMaxFieldLength(1000000);
				IndexDocs(root, index, create); // add new docs
				
				System.Console.Out.WriteLine("Optimizing index...");
				writer.Optimize();
                writer.Dispose();
				
				System.DateTime end = System.DateTime.Now;
				
				System.Console.Out.Write(end.Millisecond - start.Millisecond);
				System.Console.Out.WriteLine(" total milliseconds");
			}
			catch (System.Exception e)
			{
				System.Console.Error.WriteLine(e.StackTrace);                
			}
		}
		
		/* Walk directory hierarchy in uid order, while keeping uid iterator from
		/* existing index in sync.  Mismatches indicate one of: (a) old documents to
		/* be deleted; (b) unchanged documents, to be left alone; or (c) new
		/* documents, to be indexed.
		*/

        private  void IndexDocs(System.IO.FileInfo file, System.IO.DirectoryInfo index, bool create)
		{
			if (!create)
			{
				// incrementally update				
				reader = IndexReader.Open(FSDirectory.Open(index), false); // open existing index
				uidIter = reader.Terms(new Term("uid", "")); // init uid iterator
				
				IndexDocs(file);
				
				if (deleting)
				{
					// delete rest of stale docs
					while (uidIter.Term() != null && (System.Object) uidIter.Term().Field == (System.Object) "uid")
					{
						System.Console.Out.WriteLine("deleting " + htmlDoc.Uid2url(uidIter.Term().Text));
						reader.DeleteDocuments(uidIter.Term());
						uidIter.Next();
					}
					deleting = false;
				}
				
				uidIter.Dispose(); // close uid iterator
                reader.Dispose(); // close existing index
			}
			// don't have exisiting
			else
				IndexDocs(file);
		}        

		private void IndexDocs(System.IO.FileInfo file)
		{
			if (System.IO.Directory.Exists(file.FullName))
			{
				// if a directory
				System.String[] files = System.IO.Directory.GetFileSystemEntries(file.FullName); // list its files
				System.Array.Sort(files); // sort the files
				for (int i = 0; i < files.Length; i++)
                    // recursively index them
					IndexDocs(new System.IO.FileInfo(System.IO.Path.Combine(file.FullName, files[i])));
			}
            else if (file.FullName.EndsWith(".html") 
                || file.FullName.EndsWith(".htm") 
                || file.FullName.EndsWith(".shtml") )//|| file.FullName.EndsWith(".shtml"))
			{							
				if (uidIter != null)
				{
                    System.String uid = htmlDoc.Uid(file); // construct uid for doc
					
					while (uidIter.Term() != null && (System.Object) uidIter.Term().Field == (System.Object) "uid" && String.CompareOrdinal(uidIter.Term().Text, uid) < 0)
					{
						if (deleting)
						{
							// delete stale docs
                            System.Console.Out.WriteLine("deleting " + htmlDoc.Uid2url(uidIter.Term().Text));
							reader.DeleteDocuments(uidIter.Term());
						}
						uidIter.Next();
					}
					if (uidIter.Term() != null && (System.Object) uidIter.Term().Field == (System.Object) "uid" && String.CompareOrdinal(uidIter.Term().Text, uid) == 0)
					{
						uidIter.Next(); // keep matching docs
					}
					else if (!deleting)
					{
						// add new docs
                        Document doc = htmlDoc.Document(file);
						//System.Console.Out.WriteLine("adding " + doc.Get("path"));
                        OnShowDocPath(doc.Get("path"));
						writer.AddDocument(doc);
					}
				}
				else
				{
					// creating a new index
                    Document doc = htmlDoc.Document(file);
					//System.Console.Out.WriteLine("adding " + doc.Get("path"));
                    OnShowDocPath(doc.Get("path"));
					writer.AddDocument(doc); // add docs unconditionally
				}
			}
		}
	}
}