/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using Lucene.Net.Documents;
using HTMLParser = VanillaLib.HTMLParser;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;

namespace VanillaLib
{
	
	/// <summary>A utility for making Lucene Documents for HTML documents. </summary>
	
	public class HTMLDocument
	{
		internal  char dirSep = System.IO.Path.DirectorySeparatorChar.ToString()[0];

		
		public  System.String Uid(System.IO.FileInfo f)
		{
			// Append path and date into a string in such a way that lexicographic
			// sorting gives the same results as a walk of the file hierarchy.  Thus
			// null (\u0000) is used both to separate directory components and to
			// separate the path from the date.
			return f.FullName.Replace(dirSep, '\u0000') + "\u0000" + DateTools.TimeToString(f.LastWriteTime.Millisecond, DateTools.Resolution.SECOND);
		}
		
		public  System.String Uid2url(System.String uid)
		{
			System.String url = uid.Replace('\u0000', '/'); // replace nulls with slashes
			return url.Substring(0, (url.LastIndexOf('/')) - (0)); // remove date from end
		}

        private  string GetMD5Code(string Str)
        {
            MD5CryptoServiceProvider MD5CSP = new MD5CryptoServiceProvider();
            Byte[] bHashTable = MD5CSP.ComputeHash(System.Text.Encoding.Unicode.GetBytes(Str));
            return System.BitConverter.ToString(bHashTable).Replace("-", "");
        }

        public  Document Document(System.IO.FileInfo f)
		{
			// make a new, empty document
			Document doc = new Document();
			
			// Add the url as a field named "path".  Use a field that is 
			// indexed (i.e. searchable), but don't tokenize the field into words.
			doc.Add(new Field("path", f.FullName.Replace(dirSep, '/'), Field.Store.YES, Field.Index.NOT_ANALYZED));
			
			// Add the last modified date of the file a field named "modified".  
			// Use a field that is indexed (i.e. searchable), but don't tokenize
			// the field into words.
			doc.Add(new Field("modified", DateTools.TimeToString(f.LastWriteTime.Millisecond, DateTools.Resolution.MINUTE), Field.Store.YES, Field.Index.NOT_ANALYZED));
			
			// Add the uid as a field, so that index can be incrementally maintained.
			// This field is not stored with document, it is indexed, but it is not
			// tokenized prior to indexing.
			doc.Add(new Field("uid", Uid(f), Field.Store.NO, Field.Index.NOT_ANALYZED));
			
			System.IO.FileStream fis = new System.IO.FileStream(f.FullName, System.IO.FileMode.Open, System.IO.FileAccess.Read);
           // StreamReader reader = new StreamReader(fis, System.Text.Encoding.Unicode);
            HTMLParser parser = new HTMLParser(fis);
			
			// Add the tag-stripped contents as a Reader-valued Text field so it will
			// get tokenized and indexed.
			doc.Add(new Field("contents", parser.GetReader()));
			
			// Add the summary as a field that is stored and returned with
			// hit documents for display.
			doc.Add(new Field("summary", parser.GetSummary(), Field.Store.YES, Field.Index.NO));
			
			// Add the title as a field that it can be searched and that is stored.
		    doc.Add(new Field("title", parser.GetTitle(), Field.Store.YES, Field.Index.ANALYZED));

            #region ¡¥Ω”√Ë ˆ–≈œ¢
            String linkDesciption = String.Empty;
            StringBuilder descContent = new StringBuilder();
            String Url;
            int Cnt = 0;

            String Dummy = "&nbsp;";

            Url = DictUrls[GetMD5Code(f.FullName)] as string;

            try
            {
                if (!String.IsNullOrEmpty(Url) && mapLnkTxt.ContainsKey(Url))
                {
                    foreach (KeyValuePair<string, int> desc in mapLnkTxt[Url])
                    {
                        if (desc.Value > Cnt)
                        {
                            linkDesciption = desc.Key;
                            Cnt = desc.Value;
                        }
                    }
                
                    descContent.Append(linkDesciption.Replace(Dummy,string.Empty));
                }
            }
            catch(Exception e)
            {
                throw e;
            }

            doc.Add(new Field("description", descContent.ToString(), Field.Store.NO, Field.Index.ANALYZED));
            #endregion

            #region Debug
            //System.Threading.Thread.Sleep(5000);
            //System.Console.Out.WriteLine("Title: " + parser.GetTitle());
            //System.Console.Out.WriteLine("Summary: " + parser.GetSummary());
            //System.Console.Out.WriteLine(f.DirectoryName);
            //System.IO.StreamReader reader = new System.IO.StreamReader(parser.GetReader().BaseStream, parser.GetReader().CurrentEncoding);
            //for (System.String l = reader.ReadLine(); l != null; l = reader.ReadLine())
            //    System.Console.Out.WriteLine(l);
#endregion
			return doc;
		}

        Hashtable DictUrls;
        SortedList<string, Dictionary<string, int>> mapLnkTxt;

		public HTMLDocument(string Host)
		{           
            DictUrls = new Hashtable();
            mapLnkTxt = new SortedList<string, Dictionary<string, int>>();

            string Downloadfolder = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Vanilla");
            FileStream fs = new FileStream(Downloadfolder + "\\" + Host + "_DB", FileMode.Open);
            FileStream fs0 = new FileStream(Downloadfolder + "\\" + Host + "_LinkDesc", FileMode.Open);
            BinaryFormatter bf = new BinaryFormatter();
            DictUrls = (Hashtable)bf.Deserialize(fs);
            mapLnkTxt = bf.Deserialize(fs0) as SortedList<string, Dictionary<string, int>>;
            fs.Close();
            fs0.Close();
		}
	}
}