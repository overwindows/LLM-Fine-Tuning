

using System;

namespace VanillaLib
{

    public class HTMLParserConstants_Fields
    {
		public const int EOF = 0;
		public const int ScriptStart = 1;
		public const int TagName = 2;
		public const int DeclName = 3;
		public const int Comment1 = 4;
		public const int Comment2 = 5;
		public const int Word = 6;
		public const int LET = 7;
		public const int NUM = 8;
		public const int HEX = 9;
		public const int Entity = 10;
		public const int Space = 11;
		public const int SP = 12;
		public const int Punct = 13;
		public const int ScriptText = 14;
		public const int ScriptEnd = 15;
		public const int ArgName = 16;
		public const int ArgEquals = 17;
		public const int TagEnd = 18;
		public const int ArgValue = 19;
		public const int ArgQuote1 = 20;
		public const int ArgQuote2 = 21;
		public const int Quote1Text = 23;
		public const int CloseQuote1 = 24;
		public const int Quote2Text = 25;
		public const int CloseQuote2 = 26;
		public const int CommentText1 = 27;
		public const int CommentEnd1 = 28;
		public const int CommentText2 = 29;
		public const int CommentEnd2 = 30;
		public const int DEFAULT = 0;
		public const int WithinScript = 1;
		public const int WithinTag = 2;
		public const int AfterEquals = 3;
		public const int WithinQuote1 = 4;
		public const int WithinQuote2 = 5;
		public const int WithinComment1 = 6;
		public const int WithinComment2 = 7;
		public static System.String[] tokenImage = new System.String[]{"<EOF>", "\"<script\"", "<TagName>", "<DeclName>", "\"<!--\"", "\"<!\"", "<Word>", "<LET>", "<NUM>", "<HEX>", "<Entity>", "<Space>", "<SP>", "<Punct>", "<ScriptText>", "<ScriptEnd>", "<ArgName>", "\"=\"", "<TagEnd>", "<ArgValue>", "\"\\\'\"", "\"\\\"\"", "<token of kind 22>", "<Quote1Text>", "<CloseQuote1>", "<Quote2Text>", "<CloseQuote2>", "<CommentText1>", "\"-->\"", "<CommentText2>", "\">\""};
	}
}