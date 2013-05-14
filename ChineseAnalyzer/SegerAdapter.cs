using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;

namespace Lucene.Fanswo
{
    /// <summary>
    ///�д���
    /// </summary>
    public class SegerAdapter
    {
        /// <summary>
        /// �ֵ��ļ���·��
        /// </summary>
        //private static string DictPath = Application.StartupPath + "\\data\\sDict.txt";
        private static string DictPath =  "E:\\jobs\\Fanswo V2.0\\LucuneSearch\\Fanswo.ChinaneseAnalyer\\data\\sDict.txt";
        /// <summary>
        /// �����ֵ�Ķ���
        /// </summary>
        public static Hashtable chartable = new Hashtable();

        /// <summary>
        /// �ֵ��ļ���ȡ��״̬
        /// </summary>
        private static bool DictLoaded = false;
        /// <summary>
        /// ��ȡ�ֵ��ļ����õ�ʱ��
        /// </summary>
        public static double DictLoad_Span = 0;

        /// <summary>
        /// ������ʽ
        /// </summary>
        private string strChinese = "[\u4e00-\u9fa5]";
        private string strNumber = "[0-9]";
        private string strEnglish = "[a-zA-Z]";

        /// <summary>
        /// �д������ѵ�ʱ��
        /// </summary>
        public double TextSeg_Span = 0;

        /// <summary>
        /// ��ȡ�ַ�����
        /// </summary>
        /// <param name="Char"></param>
        /// <returns>
        /// 0: ����,1:Ӣ��,2:����
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
        /// ��ȡ�ֵ��ļ�
        /// </summary>
        private void LoadDict()
        {
            if (DictLoaded) return;
            BuidDictTree();
            DictLoaded = true;
            return;
        }

        /// <summary>
        /// ������
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
        /// �д�
        /// </summary>
        /// <param name="text">Ҫ�дʵ��ַ���</param>
        /// <returns></returns>
        public string SegText(string text)
        {
            //��ȡ�ʿ�
            this.LoadDict();
            long dt_s = DateTime.Now.Ticks;
            string ReText = "";
            Hashtable t_chartable = chartable;
            string ReWord = "";
            string char_s;
            for (int i = 0; i < text.Length; i++)
            {
                char_s = text.Substring(i, 1);
                //��ȡһ����
                if (!t_chartable.Contains(char_s))
                {
                    if (ReWord == "")
                    {
                        int j = i + 1;
                        switch (GetCharType(char_s))
                        {
                            case 0://���ĵ���
                                ReWord += char_s;
                                break;
                            case 1://Ӣ�ĵ���
                                j = i + 1;
                                while (j < text.Length)
                                {
                                    if (GetCharType(text.Substring(j, 1)) != 1)
                                        break;

                                    j++;
                                }
                                ReWord += text.Substring(i, j - i);

                                break;
                            case 2://����
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
                                ReWord += char_s;//�����ַ�����
                                break;
                        }

                        i = j - 1;
                    }
                    else
                    {
                        i--;
                    }

                    ReText += ReWord + "|";

                    //��һ����
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
