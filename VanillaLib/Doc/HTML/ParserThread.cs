

using System;
using Lucene.Net.Support;

namespace VanillaLib
{
	
	class ParserThread : ThreadClass
	{
		internal HTMLParser parser;
		
		internal ParserThread(HTMLParser p)
		{
			parser = p;
		}
		
		override public void  Run()
		{
			// convert pipeOut to pipeIn
			try
			{
				try
				{
					// parse document to pipeOut
					parser.HTMLDocument();
				}
				catch (ParseException e)
				{
					System.Console.Out.WriteLine("Parse Aborted: " + e.Message);
				}
				catch (TokenMgrError e)
				{
					System.Console.Out.WriteLine("Parse Aborted: " + e.Message);
				}
				finally
				{                   
					parser.pipeOut.Close();
					lock (parser)
					{
						parser.summary.Length = HTMLParser.SUMMARY_LENGTH;
                        parser.summaryComplete = true;
						parser.titleComplete = true;
						System.Threading.Monitor.PulseAll(parser);
					}
				}
			}
			catch (System.IO.IOException e)
			{
				System.Console.Error.WriteLine(e.StackTrace);
			}
		}
	}
}