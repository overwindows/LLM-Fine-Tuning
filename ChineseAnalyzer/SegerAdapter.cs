using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;

namespace Lucene.Fanswo
{
    /// <summary>
    ///切词类
    /// </summary>
    public class SegerAdapter
    {
        /// <summary>
        /// 字典文件的路径
        /// </summary>
        //private static string DictPath = Application.StartupPath + "\\data\\sDict.txt";
        private static string DictPath =  "E:\\jobs\\Fanswo V2.0\\LucuneSearch\\Fanswo.ChinaneseAnalyer\\data\\sDict.txt";
        /// <summary>
        /// 缓存字典的对象
        /// </summary>
        public static Hashtable chartable = new Hashtable();

        /// <summary>
        /// 字典文件读取的状态
        /// </summary>
        private static bool DictLoaded = false;
        /// <summary>
        /// 读取字典文件所用的时间
        /// </summary>
        public static double DictLoad_Span = 0;

        /// <summary>
        /// 正则表达式
        /// </summary>
        private string strChinese = "[\u4e00-\u9fa5]";
        private string strNumber = "[0-9]";
        private string strEnglish = "[a-zA-Z]";

        /// <summary>
        /// 切词所花费的时间
        /// </summary>
        public double TextSeg_Span = 0;

        /// <summary>
        /// 获取字符类型
        /// </summary>
        /// <param name="Char"></param>
        /// <returns>
        /// 0: 中文,1:英文,2:数字
        ///</returns>
        private int GetCharType(string Char)
        {
            if (new Regex(strChinese).IsMatch(Char))
                return 0;
            if (new Regex(strEnglish).IsMatch(Char))
                return 1;
            if (new Regex(strNumber).IsMatch(Char))
                return 2;
            return -1;
        }

        /// <summary>
        /// 读取字典文件
        /// </summary>
        private void LoadDict()
        {
            if (DictLoaded) return;
            BuidDictTree();
            DictLoaded = true;
            return;
        }

        /// <summary>
        /// 建立树
        /// </summary>
        public void BuidDictTree()
        {
            long dt_s = DateTime.Now.Ticks;
            string char_s;
            StreamReader reader = new StreamReader(DictPath, System.Text.Encoding.UTF8);
            string word = reader.ReadLine();
            while (word != null && word.Trim() != "")
            {
                Hashtable t_chartable = chartable;
                for (int i = 0; i < word.Length; i++)
                {
                    char_s = word.Substring(i, 1);
                    if (!t_chartable.Contains(char_s))
                    {
                        t_chartable.Add(char_s, new Hashtable());
                    }
                    t_chartable = (Hashtable)t_chartable[char_s];
                }
                word = reader.ReadLine();
            }
            reader.Close();
            DictLoad_Span = (double)(DateTime.Now.Ticks - dt_s) / (1000 * 10000);
        }

        /// <summary>
        /// 切词
        /// </summary>
        /// <param name="text">要切词的字符串</param>
        /// <returns></returns>
        public string SegText(string text)
        {
            //读取词库
            this.LoadDict();
            long dt_s = DateTime.Now.Ticks;
            string ReText = "";
            Hashtable t_chartable = chartable;
            string ReWord = "";
            string char_s;
            for (int i = 0; i < text.Length; i++)
            {
                char_s = text.Substring(i, 1);
                //获取一个词
                if (!t_chartable.Contains(char_s))
                {
                    if (ReWord == "")
                    {
                        int j = i + 1;
                        switch (GetCharType(char_s))
                        {
                            case 0://中文单词
                                ReWord += char_s;
                                break;
                            case 1://英文单词
                                j = i + 1;
                                while (j < text.Length)
                                {
                                    if (GetCharType(text.Substring(j, 1)) != 1)
                                        break;

                                    j++;
                                }
                                ReWord += text.Substring(i, j - i);

                                break;
                            case 2://数字
                                j = i + 1;
                                while (j < text.Length)
                                {
                                    if (GetCharType(text.Substring(j, 1)) != 2)
                                        break;

                                    j++;
                                }
                                ReWord += text.Substring(i, j - i);

                                break;

                            default:
                                ReWord += char_s;//其他字符单词
                                break;
                        }

                        i = j - 1;
                    }
                    else
                    {
                        i--;
                    }

                    ReText += ReWord + "|";

                    //下一个词
                    ReWord = "";
                    t_chartable = chartable;
                    continue;
                }
                ReWord += char_s;
                if (i == text.Length - 1)
                {
                    ReText += ReWord + " ";
                    break;
                }
                t_chartable = (Hashtable)t_chartable[char_s];
            }
            TextSeg_Span = (double)(DateTime.Now.Ticks - dt_s) / (1000 * 10000);
            return ReText;
        }


    }
}
