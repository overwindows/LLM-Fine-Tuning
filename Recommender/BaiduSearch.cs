using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Vanilla
{
    public class BaiduSearch
    {

        //static void Main(string[] args)
        //{
        //    BaiduSearch baidu = new BaiduSearch();
        //    Console.WriteLine(baidu.Search("新浪"));
        //}


        protected string uri = "http://www.baidu.com/s?wd=";
        protected Encoding queryEncoding = Encoding.GetEncoding("gb2312");
        protected Encoding pageEncoding = Encoding.GetEncoding("gb2312");
        protected string resultPattern = @"(?<=找到相关结果[约]?)[0-9,]*?(?=个)";

        public int Search(string word)
        {
            string html = string.Empty;
            string uriString = uri + System.Web.HttpUtility.UrlEncode(word, queryEncoding);
            try
            {
                html = WebFunc.GetHtml(uriString, pageEncoding);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            int count = 0;
            count = GetSearchCount(html);

            return count;
        }

        public virtual int GetSearchCount(string html)
        {
            int result = 0;
            string searchcount = string.Empty;

            Regex regex = new Regex(resultPattern);
            Match match = regex.Match(html);

            if (match.Success)
            {
                searchcount = match.Value;
            }
            else
            {
                searchcount = "0";
            }

            if (searchcount.IndexOf(",") > 0)
            {
                searchcount = searchcount.Replace(",", string.Empty);
            }

            int.TryParse(searchcount, out result);

            return result;
        }
    }

    static class WebFunc
    {
        public static string GetHtml(string url)
        {
            return GetHtml(url, Encoding.UTF8);
        }

        public static string GetHtml(string url, Encoding encoding)
        {
            WebRequest request;
            request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            WebResponse response;
            response = request.GetResponse();
            return new StreamReader(response.GetResponseStream(), encoding).ReadToEnd();
        }
    }
}
