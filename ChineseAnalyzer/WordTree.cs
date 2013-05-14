using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.IO;
using VanillaAnalyzer.Properties;

namespace VanillaLib
{
    /// <summary>
    /// �ʿ��࣬�������δʿ�
    /// </summary>
    public class WordTree
    {      
        /// <summary>
        /// �����ֵ�Ķ���
        /// </summary>
        public static Hashtable chartable = new Hashtable();
        /// <summary>
        /// �ֵ��ļ���ȡ��״̬
        /// </summary>
        private static bool DictLoaded = false;

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
        public  int GetCharType(char Char)
        {
            if (new Regex(strChinese).IsMatch(Char.ToString()))
                return 0;
            if (new Regex(strEnglish).IsMatch(Char.ToString()))
                return 1;
            if (new Regex(strNumber).IsMatch(Char.ToString()))
                return 2;
            return -1;
        }

        /// <summary>
        /// ��ȡ�ֵ��ļ�
        /// </summary>
        public void LoadDict()
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
            string char_s;
            //StreamReader reader = new StreamReader(DictPath, System.Text.Encoding.UTF8);
            StreamReader reader = new StreamReader(this.GetType().Assembly.GetManifestResourceStream("VanillaAnalyzer.Resources.Dict.txt"), System.Text.Encoding.Default);
           
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
        }

    }
}
