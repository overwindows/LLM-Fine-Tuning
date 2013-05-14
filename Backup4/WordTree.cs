using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.IO;

namespace Lucene.Fanswo
{
    /// <summary>
    /// �ʿ��࣬�������δʿ�
    /// </summary>
    public class WordTree
    {
        /// <summary>
        /// �ֵ��ļ���·��
        /// </summary>
        //private static string DictPath = Application.StartupPath + "\\data\\sDict.txt";
        private static string DictPath = Environment.CurrentDirectory + "\\data\\sDict.txt";
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
        public string strChinese = "[\u4e00-\u9fa5]";
        public string strNumber = "[0-9]";
        public string strEnglish = "[a-zA-Z]";


        /// <summary>
        /// ��ȡ�ַ�����
        /// </summary>
        /// <param name="Char"></param>
        /// <returns>
        /// 0: ����,1:Ӣ��,2:����
        ///</returns>
        public  int GetCharType(string Char)
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
        public  void LoadDict()
        {
            if (DictLoaded) return;
            BuidDictTree();
            DictLoaded = true;
            return;
        }

        /// <summary>
        /// ������
        /// </summary>
        private void BuidDictTree()
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
            System.Console.Out.WriteLine("��ȡ�ֵ��ļ����õ�ʱ��: " + DictLoad_Span+"s");
        }

    }
}
