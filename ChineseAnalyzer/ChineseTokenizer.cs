using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Analysis;
using System.Collections;
using System.Text.RegularExpressions;
using System.IO;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;
using System.Globalization;

namespace VanillaLib
{
    class ChineseTokenizer : Tokenizer
    {
        public ChineseTokenizer(TextReader _in)
            : base(_in)
        {
            Init();
        }

        public ChineseTokenizer(AttributeSource source, TextReader _in)
            : base(source, _in)
        {
            Init();
        }

        public ChineseTokenizer(AttributeFactory factory, TextReader _in)
            : base(factory, _in)
        {
            Init();
        }

        private void Init()
        {
            termAtt = AddAttribute<ITermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        private static readonly int MAX_WORD_LEN = 255;
        private static readonly int IO_BUFFER_SIZE = 1024;
        private readonly char[] buffer = new char[MAX_WORD_LEN];
        private readonly char[] ioBuffer = new char[IO_BUFFER_SIZE];

        private int length;

        private ITermAttribute termAtt;
        private IOffsetAttribute offsetAtt;
        private Boolean isChinese = false;
        private int offset = 0, bufferIndex = 0, dataLen = 0;//偏移量，当前字符的位置，字符长度

        private int start;//开始位置
        /// <summary>
        /// 存在字符内容
        /// </summary>
        private string text;

        /// <summary>
        /// 切词所花费的时间
        /// </summary>
        public double TextSeg_Span = 0;

        private void Push(char c)
        {
            if (length == 0) start = offset - 1; // start of token
            buffer[length++] = Char.ToLower(c); // buffer it
        }

        private bool Flush()
        {
            if (length > 0)
            {
                termAtt.SetTermBuffer(buffer, 0, length);
                offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(start + length));
                return true;
            }
            else
                return false;
        }

        public override bool IncrementToken()
        {
            ClearAttributes();
            WordTree tree = new WordTree();
            tree.LoadDict();

            while (true)
            {
                Hashtable t_chartable = WordTree.chartable;
                length = 0;
                // offset = 0;
                start = offset;

                while (true)
                {
                    char c;
                    offset++;

                    if (bufferIndex >= dataLen)
                    {
                        dataLen = input.Read(ioBuffer, 0, ioBuffer.Length);
                        bufferIndex = 0;
                    }

                    if (dataLen == 0)
                    {
                        if (length > 0)
                        {
                            offset--;
                            break;
                        }
                        else
                        {
                            offset--;
                            return false;
                        }
                    }
                    else
                    {
                        c = ioBuffer[bufferIndex++];
                    }

                    if (char.IsLetterOrDigit(c))
                    {
                        #region Chinese Word
                        if (tree.GetCharType(c) == 0)
                        {
                            if (length > 0 && !isChinese)
                            {
                                isChinese = true;
                                offset--;
                                bufferIndex--;
                                break;
                            }
                            else
                                isChinese = true;

                            //字符不在字典中
                            if (!t_chartable.Contains(c.ToString()))
                            {
                                if (length == 0)
                                {
                                    start = offset - 1;
                                    buffer[length++] = c;
                                }
                                else
                                {
                                    offset--;
                                    bufferIndex--;
                                }
                                break;
                            }
                            else
                            {
                                if (length == 0)
                                    start = offset - 1;

                                //字符在字典中
                                buffer[length++] = c;

                                //取得属于当前字符的词典树
                                t_chartable = (Hashtable)t_chartable[c.ToString()];
                            }
                        }
                        #endregion
                        #region English and Other Words
                        else
                        {
                            if (length > 0 && isChinese)
                            {
                                isChinese = false;
                                offset--;
                                bufferIndex--;
                                break;
                            }
                            else
                            {
                                isChinese = false;
                            }

                            if (length == 0)
                                start = offset - 1;

                            buffer[length++] = c;
                        }
                        #endregion
                    }
                    else if (length > 0)
                    {
                        break;
                    }
                }

                if (length > 0)
                {
                    termAtt.SetTermBuffer(buffer, 0, length);
                    offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(start + length));
                    return true;
                }
                else if (dataLen == 0)
                {
                    offset--;
                    return false;
                }
            }
        }

        public override sealed void End()
        {
            // set final offset
            int finalOffset = CorrectOffset(offset);
            this.offsetAtt.SetOffset(finalOffset, finalOffset);
        }

        public override void Reset()
        {
            base.Reset();
            offset = bufferIndex = dataLen = 0;
        }

        public override void Reset(TextReader input)
        {
            base.Reset(input);
            Reset();
        }



    }
}
