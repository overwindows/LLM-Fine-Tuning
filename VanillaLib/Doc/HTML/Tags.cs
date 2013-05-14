
using System;

namespace VanillaLib
{
	
	
	public sealed class Tags
	{
		
		/// <summary> contains all tags for which whitespaces have to be inserted for proper tokenization</summary>
		public static readonly System.Collections.Hashtable WS_ELEMS = new System.Collections.Hashtable();
		static Tags()
		{
			{
                WS_ELEMS.Add("<hr", "<hr");
                WS_ELEMS.Add("<hr/", "<hr/"); // note that "<hr />" does not need to be listed explicitly
                WS_ELEMS.Add("<br", "<br");
                WS_ELEMS.Add("<br/", "<br/");
                WS_ELEMS.Add("<p", "<p");
                WS_ELEMS.Add("</p", "</p");
                WS_ELEMS.Add("<div", "<div");
                WS_ELEMS.Add("</div", "</div");
                WS_ELEMS.Add("<td", "<td");
                WS_ELEMS.Add("</td", "</td");
                WS_ELEMS.Add("<li", "<li");
                WS_ELEMS.Add("</li", "</li");
                WS_ELEMS.Add("<q", "<q");
                WS_ELEMS.Add("</q", "</q");
                WS_ELEMS.Add("<blockquote", "<blockquote");
                WS_ELEMS.Add("</blockquote", "</blockquote");
                WS_ELEMS.Add("<dt", "<dt");
                WS_ELEMS.Add("</dt", "</dt");
                WS_ELEMS.Add("<h1", "<h1");
                WS_ELEMS.Add("</h1", "</h1");
                WS_ELEMS.Add("<h2", "<h2");
                WS_ELEMS.Add("</h2", "</h2");
                WS_ELEMS.Add("<h3", "<h3");
                WS_ELEMS.Add("</h3", "</h3");
                WS_ELEMS.Add("<h4", "<h4");
                WS_ELEMS.Add("</h4", "</h4");
                WS_ELEMS.Add("<h5", "<h5");
                WS_ELEMS.Add("</h5", "</h5");
                WS_ELEMS.Add("<h6", "<h6");
                WS_ELEMS.Add("</h6", "</h6");
			}
		}
	}
}