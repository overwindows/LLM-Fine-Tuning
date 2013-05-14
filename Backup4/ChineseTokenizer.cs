using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Analysis;
using System.Collections;
using System.Text.RegularExpressions;
using System.IO;

namespace Lucene.Fanswo
{
    class ChineseTokenizer : Tokenizer
    {

        private int offset = 0, bufferIndex = 0, dataLen = 0;//ƫ��������ǰ�ַ���λ�ã��ַ�����

        private int start;//��ʼλ��
        /// <summary>
        /// �����ַ�����
        /// </summary>
        private string text;
       
        /// <summary>
        /// �д������ѵ�ʱ��
        /// </summary>
        public double TextSeg_Span = 0;
   
  		/// <summary>Constructs a tokenizer for this Reader. </summary>
		public ChineseTokenizer(System.IO.TextReader reader)
		{
			this.input = reader;
            text = input.ReadToEnd();
            dataLen = text.Length;
        }

        /// <summary>�����дʣ���������������һ��token����������Ϊ��ʱ����null
		/// </summary>
        /// 
        public override Token Next()
        {
            Token token = null;
            WordTree tree = new WordTree();
            //��ȡ�ʿ�
            tree.LoadDict();
            //��ʼ���ʿ⣬Ϊ����
            Hashtable t_chartable = WordTree.chartable;
            string ReWord = "";
            string char_s;
            start = offset;
            bufferIndex = start;

            while (true)
            {
                //��ʼλ�ó����ַ������˳�ѭ��
                if (start >= dataLen)
                {
                    break;
                }
                //��ȡһ����
                char_s = text.Substring(start, 1);
                if (string.IsNullOrEmpty(char_s.Trim()))
                {
                    start++;
                    continue;
                }
                //�ַ������ֵ���
                if (!t_chartable.Contains(char_s))
                {
                    if (ReWord == "")
                    {
                        int j = start + 1;
                        switch (tree.GetCharType(char_s))
                        {
                            case 0://���ĵ���
                                ReWord += char_s;
                                break;
                            case 1://Ӣ�ĵ���
                                j = start + 1;
                                while (j < dataLen)
                                {
                                    if (tree.GetCharType(text.Substring(j, 1)) != 1)
                                        break;

                                    j++;
                                }
                                ReWord += text.Substring(start, j - offset);

                                break;
                            case 2://����
                                j = start + 1;
                                while (j < dataLen)
                                {
                                    if (tree.GetCharType(text.Substring(j, 1)) != 2)
                                        break;

                                    j++;
                                }
                                ReWord += text.Substring(start, j - offset);

                                break;

                            default:
                                ReWord += char_s;//�����ַ�����
                                break;
                        }

                        offset = j;//����ȡ��һ���ʵĿ�ʼλ��
                    }
                    else
                    {
                        offset = start;//����ȡ��һ���ʵĿ�ʼλ��
                    }
                    
                    //����token����
                    return new Token(ReWord, bufferIndex, bufferIndex + ReWord.Length - 1);
                }
                //�ַ����ֵ���
                ReWord += char_s;
                //ȡ�����ڵ�ǰ�ַ��Ĵʵ���
                t_chartable = (Hashtable)t_chartable[char_s];
                //������һѭ��ȡ��һ���ʵĿ�ʼλ��
                start++;
                if (start == dataLen)
                {
                    offset = dataLen;
                    return new Token(ReWord, bufferIndex, bufferIndex + ReWord.Length - 1);
                }
            }
            return token;
        }

    }
}
