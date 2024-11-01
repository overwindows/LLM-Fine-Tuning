import argparse
import logging
import os
import random
import re
import sys
import tokenize
from io import BytesIO

"""
sudo apt-get install libclang-dev  # for clang.cindex
sudo apt-get install clang
"""
import clang
import javalang_tokenizer as javalang_tok
from clang.cindex import TokenKind
from timeout import timeout, TimeoutError
import traceback
import re

def simple_tokenize(text):  
    # Split the text into words using whitespace  
    words = re.split(r'\s+', text)  
  
    # Join the words with a single space  
    tokenized_text = ' '.join(words)  
  
    return tokenized_text  

clang.cindex.Config.set_library_file('/usr/lib/llvm-14/lib/libclang.so')

TOK_NO_SPACE_BEFORE = {',', ';'}
STRINGS_AND_COMMENTS_TOKEN_KINDS = {TokenKind.LITERAL, TokenKind.COMMENT}
logging.basicConfig(
    filename='timeout_cpp_tokenizer_examples.log', level=logging.DEBUG)

idx = clang.cindex.Index.create()

JAVA_TOKEN2CHAR = {'STOKEN0': "//",
                   'STOKEN1': "/*",
                   'STOKEN2': "*/",
                   'STOKEN3': "/**",
                   'STOKEN4': "**/",
                   'STOKEN5': '"""',
                   'STOKEN6': '\\n'
                   }
JAVA_CHAR2TOKEN = {"//": ' STOKEN0 ',
                   "/*": ' STOKEN1 ',
                   "*/": ' STOKEN2 ',
                   "/**": ' STOKEN3 ',
                   "**/": ' STOKEN4 ',
                   '"""': ' STOKEN5 ',
                   '\\n': ' STOKEN6 '
                   }

CPP_TOKEN2CHAR = JAVA_TOKEN2CHAR.copy()
CPP_CHAR2TOKEN = JAVA_CHAR2TOKEN.copy()

PYTHON_TOKEN2CHAR = {'STOKEN0': '#',
                     'STOKEN1': "\\n",
                     'STOKEN2': '"""',
                     'STOKEN3': "'''"
                     }

PYTHON_CHAR2TOKEN = {'#': ' STOKEN0 ',
                     "\\n": ' STOKEN1 ',
                     '"""': ' STOKEN2 ',
                     "'''": ' STOKEN3 '
                     }

# from GraphCodeBERT.parser import DFG_csharp
from tree_sitter import Language, Parser

csharp_parser = Parser()
csharp_parser.set_language(Language('parser/my-languages.so', 'c_sharp'))

c_parser = Parser()
c_parser.set_language(Language('parser/my-languages.so', 'c'))

php_parser = Parser()
php_parser.set_language(Language('parser/my-languages.so', 'php'))

ruby_parser = Parser()
ruby_parser.set_language(Language('parser/my-languages.so', 'ruby'))

scala_parser = Parser()
scala_parser.set_language(Language('parser/my-languages.so', 'scala'))

bash_parser = Parser()
bash_parser.set_language(Language('parser/my-languages.so', 'bash'))

json_parser = Parser()
json_parser.set_language(Language('parser/my-languages.so', 'json'))

go_parser = Parser()
go_parser.set_language(Language('parser/my-languages.so', 'go'))

typescript_parser = Parser()
typescript_parser.set_language(Language('parser/my-languages.so', 'typescript'))

def tree_to_token_index(root_node):
    if (len(root_node.children)==0 or root_node.type=='string') and root_node.type!='comment':
        return [(root_node.start_point,root_node.end_point)]
    else:
        code_tokens=[]
        for child in root_node.children:
            code_tokens+=tree_to_token_index(child)
        return code_tokens

def index_to_code_token(index,code):
    start_point=index[0]
    end_point=index[1]
    if start_point[0]==end_point[0]:
        s=code[start_point[0]][start_point[1]:end_point[1]]
    else:
        s=""
        s+=code[start_point[0]][start_point[1]:]
        for i in range(start_point[0]+1,end_point[0]):
            s+=code[i]
        s+=code[end_point[0]][:end_point[1]]   
    return s

class ind_iter(object):
    def __init__(self, len):
        self.i = 0
        self.len = len

    def next(self):
        self.i += 1
        if self.i > (self.len - 1):
            raise StopIteration

    def prev(self):
        self.i -= 1
        if self.i < 0:
            raise StopIteration


def process_string(tok, char2tok, tok2char, is_comment):
    if is_comment:
        tok = re.sub(' +', ' ', tok)
        tok = re.sub(r"(.)\1\1\1\1+", r"\1\1\1\1\1", tok)
        if len(re.sub(r'\W', '', tok)) < 2:
            return ''
    tok = tok.replace(' ', ' ▁ ')
    for char, special_token in char2tok.items():
        tok = tok.replace(char, special_token)
    if tok.startswith(' STOKEN0'):
        if tok.endswith('\n'):
            tok = tok[:-1]
        tok += ' ENDCOM'
    tok = tok.replace('\n', ' STRNEWLINE ')
    tok = tok.replace('\t', ' TABSYMBOL ')
    tok = re.sub(' +', ' ', tok)
    tok = simple_tokenize(tok)
    tok = re.sub(' +', ' ', tok)
    for special_token, char in tok2char.items():
        tok = tok.replace(special_token, char)
    tok = tok.replace('\r', '')

    return tok


import re  
  
def tokenize_javascript(s, keep_comments=False):  
    tokens = []  
    assert isinstance(s, str)  
  
    # Define a regular expression pattern for JavaScript tokens  
    pattern = r'''(?x)          # Enable verbose mode  
        (?:[A-Za-z_]\w*)        # Identifiers and keywords  
        |(?:\d+\.\d*|\.\d+|\d+)  # Numeric literals  
        |(?:\+|-|\*|\/|%|==)    # Operators  
        |(?:\'[^\']*\'|"[^"]*")  # String literals  
        |(?:\s+)                # Whitespace  
        |(?:\/\/.*|\/\*[\s\S]*?\*\/)  # Comments  
        |(?:[(){}\[\],;])       # Punctuation  
    '''  
  
    # Find all tokens in the input string  
    for match in re.finditer(pattern, s):  
        token = match.group(0)  
  
        # Ignore comments if keep_comments is False  
        if not keep_comments and (token.startswith("//") or token.startswith("/*")):  
            continue  
        token = token.strip()
        if not token:
            continue
        tokens.append(token)  
  
    return tokens  



def detokenize_javascript(s):
    # print(s)
    return ' '.join(s)

def tokenize_scala(s, keep_comments=True):
    code = s.replace('. ', '.')
    tree = scala_parser.parse(bytes(code, 'utf8'))

    if keep_comments:
        root_node = tree.root_node
        tokens_index = tree_to_token_index(root_node)
        code = code.split('\n')
        code_tokens = [index_to_code_token(x, code) for x in tokens_index]
        return code_tokens
    else:
        root_node = tree.root_node
        tokens_index = tree_to_token_index(root_node)
        code = code.split('\n')
        code_tokens = [index_to_code_token(x, code) for x in tokens_index if code[x[0]][x[1]:x[1]+2]!='//']
        return code_tokens

def tokenize_ruby(s, keep_comments=False):
    code = s.replace('. ', '.')
    tree = ruby_parser.parse(bytes(code, 'utf8'))

    root_node = tree.root_node
    root_node = tree.root_node
    tokens_index = tree_to_token_index(root_node)
    code = code.split('\n')
    code_tokens = [index_to_code_token(x, code) for x in tokens_index]
    return code_tokens

def tokenize_php(s, keep_comments=False):
    code = s.replace('. ', '.')
    tree = php_parser.parse(bytes(code, 'utf8'))

    root_node = tree.root_node
    tokens_index = tree_to_token_index(root_node)
    code = code.split('\n')
    code_tokens = [index_to_code_token(x, code) for x in tokens_index]
    return code_tokens

def tokenize_c(s, keep_comments=False):
    code = s.replace('. ', '.')
    tree = c_parser.parse(bytes(code, 'utf8'))

    root_node = tree.root_node
    tokens_index = tree_to_token_index(root_node)
    code = code.split('\n')
    code_tokens = [index_to_code_token(x, code) for x in tokens_index]
    return code_tokens

def tokenize_csharp(s, keep_comments=False):
    code = s.replace('. ', '.')
    tree = csharp_parser.parse(bytes(code, 'utf8'))

    root_node = tree.root_node
    tokens_index = tree_to_token_index(root_node)
    code = code.split('\n')
    code_tokens = [index_to_code_token(x, code) for x in tokens_index]
    return code_tokens

def tokenize_go(s, keep_comments=False):
    code = s.replace('. ', '.')
    tree = go_parser.parse(bytes(code, 'utf8'))

    root_node = tree.root_node
    tokens_index = tree_to_token_index(root_node)
    code = code.split('\n')
    code_tokens = [index_to_code_token(x, code) for x in tokens_index]
    return code_tokens

def detokenize_csharp(s):
    # offset0 = len('<DOCUMENT_ID="None">')
    # offset1 = len('</DOCUMENT>')
    # s = s[offset0:-offset1]
    # print(s)
    return s.strip()


def tokenize_python(s, keep_comments=False):
    try:
        assert isinstance(s, str)
        s = s.replace(r'\r', '')
        tokens = []

        try:
            iterator = tokenize.tokenize(BytesIO(s.encode('utf-8')).readline)
        except SyntaxError as excep:
            raise SyntaxError(excep)

        removed_docstr = 0
        while True:
            try:
                toktype, tok, _, _, line = next(iterator)
            except (tokenize.TokenError, IndentationError, SyntaxError, UnicodeDecodeError):
                raise Exception(
                    f"Impossible to parse tokens because icorrect source code \"{s[0:30]}\" ...")
            except StopIteration:
                raise Exception(f"End of iterator before ENDMARKER token.")

            if toktype == tokenize.ENCODING or toktype == tokenize.NL:
                continue

            elif toktype == tokenize.NEWLINE:
                if removed_docstr == 1:
                    removed_docstr = 0
                    continue
                tokens.append('NEW_LINE')

            elif toktype == tokenize.COMMENT:
                if keep_comments:
                    com = process_string(
                        tok, PYTHON_CHAR2TOKEN, PYTHON_TOKEN2CHAR, True)
                    if len(com) > 0:
                        tokens.append(com)
                else:
                    continue

            elif toktype == tokenize.STRING:
                if tok == line.strip():  # docstring
                    if not keep_comments:
                        removed_docstr = 1
                        continue
                    else:
                        coms = process_string(
                            tok, PYTHON_CHAR2TOKEN, PYTHON_TOKEN2CHAR, True)
                        if len(coms) > 0:
                            tokens.append(coms)
                        else:
                            removed_docstr = 1
                else:
                    tokens.append(process_string(
                        tok, PYTHON_CHAR2TOKEN, PYTHON_TOKEN2CHAR, False))

            elif toktype == tokenize.INDENT:
                tokens.append('INDENT')

            elif toktype == tokenize.DEDENT:
                # empty block
                if tokens[-1] == 'INDENT':
                    tokens = tokens[:-1]
                else:
                    tokens.append('DEDENT')

            elif toktype == tokenize.ENDMARKER:
                tokens.append('ENDMARKER')
                break

            else:
                tokens.append(tok)

        assert (tokens[-1] == 'ENDMARKER'), "Error, no end marker"
        return tokens[:-1]
    except KeyboardInterrupt:
        raise
    except:
        return []


def detokenize_python(s):
    try:
        assert isinstance(s, str) or isinstance(s, list)
        if isinstance(s, list):
            s = ' '.join(s)
        s = s.replace('ENDCOM', 'NEW_LINE')
        s = s.replace('▁', 'SPACETOKEN')

        lines = s.split('NEW_LINE')
        tabs = ''
        for i, line in enumerate(lines):
            line = line.strip()
            if line.startswith('INDENT '):
                tabs += '    '
                line = line.replace('INDENT ', tabs)
            elif line.startswith('DEDENT'):
                number_dedent = line.count('DEDENT')
                tabs = tabs[4 * number_dedent:]
                line = line.replace("DEDENT", '')
                line = line.strip()
                line = tabs + line
            elif line == 'DEDENT':
                line = ''
            else:
                line = tabs + line
            lines[i] = line
        untok_s = '\n'.join(lines)

        # find string and comment with parser and detokenize string correctly
        try:
            for toktype, tok, _, _, line in tokenize.tokenize(BytesIO(untok_s.encode('utf-8')).readline):
                if toktype == tokenize.STRING or toktype == tokenize.COMMENT:
                    tok_ = tok.replace('STRNEWLINE', '\n').replace(
                        'TABSYMBOL', '\t').replace(' ', '').replace('SPACETOKEN', ' ')
                    untok_s = untok_s.replace(tok, tok_)
        except KeyboardInterrupt:
            raise
        except:
            pass

        # detokenize imports
        untok_s = untok_s.replace('. ', '.').replace(' .', '.').replace(
            'import.', 'import .').replace('from.', 'from .')
        untok_s = untok_s.replace('> >', '>>').replace('< <', '<<')
        return untok_s
    except KeyboardInterrupt:
        raise
    except:
        return ''


def extract_functions_python_with_docstring(function):
    ds = re.findall(
        "[:][ ][N][E][W][_][L][I][N][E][ ][I][N][D][E][N][T][ ]['][']['].*?['][']['][ ][N][E][W][_][L][I][N][E]|[:][ ][N][E][W][_][L][I][N][E][ ][I][N][D][E][N][T][ ][\"][\"][\"].*?[\"][\"][\"][ ][N][E][W][_][L][I][N][E]",
        function, re.DOTALL)
    if len(ds) > 0:
        for d in ds:
            function = function.replace(d[18:-9], '')
        coms = ' '.join([d[18:-9] for d in ds])
        inline_coms = re.findall('[#].*?[E][N][D][C][O][M]', function)
        for inline_com in inline_coms:
            function = function.replace(inline_com, '')
            coms += ' <INLINE> '
            coms += inline_com
        if len(re.sub(r'\W', '', coms.replace('<INLINE>', '').replace('ENDCOM', ''))) < 5:
            return '', ''
        else:
            return re.sub('\s+', ' ', function), coms
    else:
        return '', ''


def extract_functions_python(s):
    tokens = iter(s.split())
    functions_standalone = []
    functions_class = []
    number_indent = 0
    try:
        token = next(tokens)
    except StopIteration:
        return [], []
    while True:
        try:
            if token == 'def':
                function = ['def']
                while not (token == 'DEDENT' and number_indent == 0):
                    token = next(tokens)
                    if token == 'INDENT':
                        number_indent += 1
                    elif token == 'DEDENT':
                        number_indent -= 1
                    function.append(token)
                try:
                    if function[function.index('(') + 1] == 'self':
                        function = filter_functions_python_2_3(
                            ' '.join(function))
                        if function is not None:
                            functions_class.append(function)
                    else:
                        function = filter_functions_python_2_3(
                            ' '.join(function))
                        if function is not None:
                            functions_standalone.append(function)
                except:
                    print(function)
                    token = next(tokens)
            else:
                token = next(tokens)
        except StopIteration:
            break
    return functions_standalone, functions_class


def filter_functions_python_2_3(function):
    if (re.search("print [^(]", function) is None and
            re.search("raise \w+ ,", function) is None and
            re.search("except \w+ ,", function) is None and
            re.search("[^ ]+ = \d+ L", function) is None and
            ". iterkeys ( )" not in function and
            ". itervalues ( )" not in function and
            ". iteritems ( )" not in function and
            "xrange (" not in function and
            "imap (" not in function):
        return function
    else:
        return None


def get_function_name_python(s):
    assert isinstance(s, str) or isinstance(s, list)
    if isinstance(s, str):
        s = s.split()
    return s[s.index('def') + 1]


# @timeout(10)
def get_cpp_tokens_and_types(s):
    # print(s)
    tokens = []
    assert isinstance(s, str)
    s = s.replace(r'\r', '')
    hash = str(random.getrandbits(128))
    parsed_code = idx.parse(
        hash + '_tmp.cpp', args=['-std=c++11'], unsaved_files=[(hash + '_tmp.cpp', s)], options=0)
    # print(parsed_code)
    for tok in parsed_code.get_tokens(extent=parsed_code.cursor.extent):
        tokens.append((tok.spelling, tok.kind))
    return tokens


def tokenize_cpp(s, keep_comments=False):
    tokens = []
    assert isinstance(s, str)
    try:
        tokens_and_types = get_cpp_tokens_and_types(s)
        for tok, typ in tokens_and_types:
            if not keep_comments and typ == TokenKind.COMMENT:
                continue
            if typ in STRINGS_AND_COMMENTS_TOKEN_KINDS:
                if typ == TokenKind.COMMENT:
                    com = process_string(
                        tok, CPP_CHAR2TOKEN, CPP_TOKEN2CHAR, True)
                    if len(com) > 0:
                        tokens.append(com)
                else:
                    tokens.append(process_string(
                        tok, CPP_CHAR2TOKEN, CPP_TOKEN2CHAR, False))
            else:
                tokens.append(tok)
        return tokens
    except KeyboardInterrupt:
        raise
    except TimeoutError:
        print(f'TimeOut Error')
        logging.info('*' * 20)
        logging.info(f'TimeOut Error for string {s}')
        return []
    except:
        return []


def tokenize_java(s, keep_comments=False):
    try:
        tokens = []
        assert isinstance(s, str)
        s = s.replace(r'\r', '')
        tokens_generator = javalang_tok.tokenize(
            s, keep_comments=keep_comments)
        for token in tokens_generator:
            if isinstance(token, javalang_tok.String):
                tokens.append(process_string(
                    token.value, JAVA_CHAR2TOKEN, JAVA_TOKEN2CHAR, False))
            elif isinstance(token, javalang_tok.Comment):
                com = process_string(
                    token.value, JAVA_CHAR2TOKEN, JAVA_TOKEN2CHAR, True)
                if len(com) > 0:
                    tokens.append(com)
            else:
                tokens.append(token.value)
        return tokens
    except:
        print('tokenize_java Exception ', s)
        return []


def detokenize_cpp(s):
    print('detokenize_cpp')
    assert isinstance(s, str) or isinstance(s, list)
    if isinstance(s, list):
        s = ' '.join(s)
    # the ▁ character created bugs in the cpp tokenizer
    s = s.replace('ENDCOM', '\n').replace('▁', ' SPACETOKEN ')
    try:
        tokens_and_types = get_cpp_tokens_and_types(s)
    except Exception as e:
        print('Error Message: {}'.format(str(e)))
        # traceback.print_exc()
        # traceback.print_stack()
        # traceback.print_exception()
        return s
    new_tokens = []
    i = 0
    while i < len(tokens_and_types):
        token, type = tokens_and_types[i]
        if type in STRINGS_AND_COMMENTS_TOKEN_KINDS:
            new_tokens.append(token.replace('STRNEWLINE', '\n').replace(
                'TABSYMBOL', '\t').replace(' ', '').replace('SPACETOKEN', ' '))
            if type == TokenKind.COMMENT:
                new_tokens.append('NEW_LINE')
        elif token == '}':
            if i < len(tokens_and_types) - 1 and tokens_and_types[i + 1][0] == ';':
                new_tokens += ['CB_COLON', 'NEW_LINE']
                i += 2
                continue
            if i < len(tokens_and_types) - 1 and tokens_and_types[i + 1][0] == ',':
                new_tokens += ['CB_COMA', 'NEW_LINE']
                i += 2
                continue
            new_tokens += ['CB_', 'NEW_LINE']
        elif token == '{':
            new_tokens += ['OB_', 'NEW_LINE']
        elif token == '*/':
            new_tokens += ['*/', 'NEW_LINE']
        elif token == ';':
            new_tokens += [';', 'NEW_LINE']
        else:
            new_tokens.append(token)

        if i < len(tokens_and_types) - 1 and tokens_and_types[i + 1][0] in TOK_NO_SPACE_BEFORE:
            next_token = tokens_and_types[i + 1][0]
            new_tokens[len(new_tokens) - 1] += next_token
            if next_token == ';':
                new_tokens.append('NEW_LINE')
            i += 2
            continue
        i += 1

    lines = re.split('NEW_LINE', ' '.join(new_tokens))

    untok_s = indent_lines(lines)
    untok_s = untok_s.replace('CB_COLON', '};').replace(
        'CB_COMA', '},').replace('CB_', '}').replace('OB_', '{')
    return untok_s


def indent_lines(lines):
    prefix = ''
    for i, line in enumerate(lines):
        line = line.strip()
        if re.match('CB_COLON|CB_COMA|CB_', line):
            prefix = prefix[2:]
            line = prefix + line
        elif line.endswith('OB_'):
            line = prefix + line
            prefix += '  '
        else:
            line = prefix + line
        lines[i] = line
    untok_s = '\n'.join(lines)
    return untok_s


def detokenize_java(s):
    assert isinstance(s, str) or isinstance(s, list)
    if isinstance(s, list):
        s = ' '.join(s)
    s = s.replace('ENDCOM', 'NEW_LINE')
    s = s.replace('▁', 'SPACETOKEN')

    s = s.replace('} "', 'CB_ "')
    s = s.replace('" {', '" OB_')
    s = s.replace('*/ ', '*/ NEW_LINE')
    s = s.replace('} ;', 'CB_COLON NEW_LINE')
    s = s.replace('} ,', 'CB_COMA')
    s = s.replace('}', 'CB_ NEW_LINE')
    s = s.replace('{', 'OB_ NEW_LINE')
    s = s.replace(';', '; NEW_LINE')
    lines = re.split('NEW_LINE', s)

    untok_s = indent_lines(lines)
    untok_s = untok_s.replace('CB_COLON', '};').replace(
        'CB_COMA', '},').replace('CB_', '}').replace('OB_', '{')
    untok_s = untok_s.replace('> > >', '>>>').replace('<< <', '<<<')
    untok_s = untok_s.replace('> >', '>>').replace('< <', '<<')

    try:
        # call parser of the tokenizer to find comments and string and detokenize them correctly
        tokens_generator = javalang_tok.tokenize(untok_s, keep_comments=True)
        for token in tokens_generator:
            if isinstance(token, javalang_tok.String) or isinstance(token, javalang_tok.Comment):
                token_ = token.value.replace('STRNEWLINE', '\n').replace('TABSYMBOL', '\t').replace(' ', '').replace(
                    'SPACETOKEN', ' ')
                untok_s = untok_s.replace(token.value, token_)
    except KeyboardInterrupt:
        raise
    except:
        pass
    return untok_s


def extract_functions_java(s):
    tokens = s.split()
    i = ind_iter(len(tokens))
    functions_standalone = []
    functions_class = []
    try:
        token = tokens[i.i]
    except KeyboardInterrupt:
        raise
    except:
        return [], []
    while True:
        try:
            # detect function
            if token == ')' and (tokens[i.i + 1] == '{' or (tokens[i.i + 1] == 'throws' and tokens[i.i + 3] == '{')):
                # go previous until the start of function
                while token not in [';', '}', '{', '*/', 'ENDCOM']:
                    i.prev()
                    token = tokens[i.i]

                if token == '*/':
                    while token != '/*':
                        i.prev()
                        token = tokens[i.i]
                    function = [token]
                    while token != '*/':
                        i.next()
                        token = tokens[i.i]
                        function.append(token)
                elif token == 'ENDCOM':
                    while token != '//':
                        i.prev()
                        token = tokens[i.i]
                    function = [token]
                    while token != 'ENDCOM':
                        i.next()
                        token = tokens[i.i]
                        function.append(token)
                else:
                    i.next()
                    token = tokens[i.i]
                    function = [token]

                while token != '{':
                    i.next()
                    token = tokens[i.i]
                    function.append(token)
                if token == '{':
                    number_indent = 1
                    while not (token == '}' and number_indent == 0):
                        try:
                            i.next()
                            token = tokens[i.i]
                            if token == '{':
                                number_indent += 1
                            elif token == '}':
                                number_indent -= 1
                            function.append(token)
                        except StopIteration:
                            break
                    if 'static' in function[0:function.index('{')]:
                        functions_standalone.append(
                            remove_java_annotation(' '.join(function)))
                    else:
                        functions_class.append(
                            remove_java_annotation(' '.join(function)))
            i.next()
            token = tokens[i.i]
        except KeyboardInterrupt:
            raise
        except:
            break
    return functions_standalone, functions_class


def extract_functions_java_with_docstring(function):
    ds = re.findall('[/][*].*?[*][/][ ]', function, re.DOTALL)
    if len(ds) > 0:
        for d in ds:
            function = function.replace(d, '')
        coms = ' '.join([d[:-1] for d in ds])
        inline_coms = re.findall('[/][/].*?[E][N][D][C][O][M]', function)
        for inline_com in inline_coms:
            function = function.replace(inline_com, '')
            coms += ' <INLINE> '
            coms += inline_com
        if len(re.sub(r'\W', '', coms.replace('<INLINE>', '').replace('ENDCOM', ''))) < 5:
            return '', ''
        else:
            return re.sub('\s+', ' ', function), coms
    else:
        return '', ''


def clean_hashtags_functions_cpp(function):
    function = re.sub('[#][ ][i][n][c][l][u][d][e][ ]["].*?["]', "", function)
    function = re.sub('[#][ ][i][n][c][l][u][d][e][ ][<].*?[>]', "", function)
    function = re.sub('[#][ ][i][f][n][d][e][f][ ][^ ]*', "", function)
    function = re.sub('[#][ ][i][f][d][e][f][ ][^ ]*', "", function)
    function = re.sub(
        '[#][ ][d][e][f][i][n][e][ ][^ ]*[ ][(][ ].*?[ ][)][ ][(][ ].*[ ][)]', "", function)
    function = re.sub(
        '[#][ ][d][e][f][i][n][e][ ][^ ]*[ ][(][ ].*?[ ][)][ ][{][ ].*[ ][}]', "", function)
    function = re.sub(
        '[#][ ][d][e][f][i][n][e][ ][^ ]*[ ]([(][ ])?["].*?["]([ ][)])?', "", function)
    function = re.sub(
        '[#][ ][d][e][f][i][n][e][ ][^ ]*[ ]([(][ ])?\d*\.?\d*([ ][+-/*][ ]?\d*\.?\d*)?([ ][)])?', "", function)
    function = re.sub('[#][ ][d][e][f][i][n][e][ ][^ ]', "", function)
    function = re.sub(
        '[#][ ][i][f][ ][d][e][f][i][n][e][d][ ][(][ ].*?[ ][)]', "", function)
    function = re.sub('[#][ ][i][f][ ][^ ]*', "", function)
    function = function.replace('# else', '')
    function = function.replace('# endif', '')
    function = function.strip()
    return function


def extract_functions_javascript(s):
    # Only for CodeSearchNet Corpus!!!
    # print(s)
    return [s], [s]


def extract_functions_csharp(s):
    return [s], [s]


def extract_functions_php(s):
    # Only for CodeSearchNet Corpus!!!
    return [s], [s]


def extract_functions_cpp(s):
    try:
        s = clean_hashtags_functions_cpp(s)
        s = s.replace('ENDCOM', '\n').replace('▁', 'SPACETOKEN')
        tokens = get_cpp_tokens_and_types(s)
    except:
        return [], []
    i = ind_iter(len(tokens))
    functions_standalone = []
    functions_class = []
    try:
        token, token_type = tokens[i.i]
    except:
        return [], []
    while True:
        try:
            # detect function
            if token == ')' and ((tokens[i.i + 1][0] == '{' and tokens[i.i + 2][0] != '}') or (
                    tokens[i.i + 1][0] == 'throw' and tokens[i.i + 4][0] == '{' and tokens[i.i + 5][0] == '}')):
                # go previous until the start of function
                while token not in {';', '}', '{'}:
                    try:
                        i.prev()
                    except StopIteration:
                        break
                    token = tokens[i.i][0]

                i.next()
                token, token_type = tokens[i.i]
                if token_type == TokenKind.COMMENT:
                    token = token.strip()
                    token += " ENDCOM"
                function = [token]
                token_types = [token_type]
                while token != '{':
                    i.next()
                    token, token_type = tokens[i.i]
                    if token_type == TokenKind.COMMENT:
                        token = token.strip()
                        token += " ENDCOM"
                    function.append(token)
                    token_types.append(token_type)

                if token_types[function.index('(') - 1] != TokenKind.IDENTIFIER:
                    continue
                if token == '{':
                    number_indent = 1
                    while not (token == '}' and number_indent == 0):
                        try:
                            i.next()
                            token, token_type = tokens[i.i]
                            if token == '{':
                                number_indent += 1
                            elif token == '}':
                                number_indent -= 1
                            if token_type == TokenKind.COMMENT:
                                token = token.strip()
                                token += " ENDCOM"
                            function.append(token)
                        except StopIteration:
                            break
                    if 'static' in function[0:function.index('{')] or '::' not in function[0:function.index('(')]:
                        function = ' '.join(function)
                        function = re.sub(
                            "[<][ ][D][O][C][U][M][E][N][T].*?[>] ", "", function)
                        function = clean_hashtags_functions_cpp(function)
                        function = function.strip()
                        function = function.replace(
                            '\n', 'ENDCOM').replace('SPACETOKEN', '▁')
                        if not re.sub('[^ ]*[ ][(][ ]\w*([ ][,][ ]\w*)*[ ][)]', "", function[:function.index('{')]).strip().startswith('{') and not function.startswith('#'):
                            functions_standalone.append(function)
                    else:
                        function = ' '.join(function)
                        function = re.sub(
                            "[<][ ][D][O][C][U][M][E][N][T].*?[>] ", "", function)
                        function = clean_hashtags_functions_cpp(function)
                        function = function.strip()
                        function = function.replace(
                            '\n', 'ENDCOM').replace('SPACETOKEN', '▁')
                        if not re.sub('[^ ]*[ ][(][ ]\w*([ ][,][ ]\w*)*[ ][)]', "", function[:function.index('{')]).strip().startswith('{') and not function.startswith('#'):
                            functions_class.append(function)
            i.next()
            token = tokens[i.i][0]
        except:
            break
    return functions_standalone, functions_class


def extract_functions_cpp_with_docstring(function):
    function = re.sub("[<][ ][D][O][C][U][M][E][N][T].*?[>] ", "", function)
    ds = re.findall('[/][*].*?[*][/][ ]', function, re.DOTALL)
    if len(ds) > 0:
        for d in ds:
            function = function.replace(d, '')
        coms = ' '.join([d[:-1] for d in ds])
        inline_coms = re.findall('[/][/].*?[E][N][D][C][O][M]', function)
        for inline_com in inline_coms:
            function = function.replace(inline_com, '')
            coms += ' <INLINE> '
            coms += inline_com
        if len(re.sub(r'\W', '', coms.replace('<INLINE>', '').replace('ENDCOM', ''))) < 5:
            return '', ''
        else:
            return re.sub('\s+', ' ', function), coms
    else:
        return '', ''


def remove_java_annotation(function):
    return re.sub('^(@ (Override|Deprecated|SuppressWarnings) (\( .* \) )?)*', '', function)


def get_first_token_before_first_parenthesis(s):
    assert isinstance(s, str) or isinstance(s, list)
    if isinstance(s, str):
        s = s.split()
    return s[s.index('(') - 1]


def get_function_name_java(s):
    return get_first_token_before_first_parenthesis(s)


def get_function_name_cpp(s):
    return get_first_token_before_first_parenthesis(s)


def extract_arguments_java(f):
    return extract_arguments_java_using_parentheses(f)


def extract_arguments_cpp(f):
    return extract_arguments_java_using_parentheses(f)


def extract_arguments_java_using_parentheses(f):
    f = f.split(' ')
    types = []
    names = []
    par = 0
    arguments = []
    f = f[f.index('('):]
    for tok in f:
        if tok == '(':
            par += 1
        elif tok == ')':
            par -= 1
        arguments.append(tok)
        if par == 0:
            break
    arguments = ' '.join(arguments[1:-1])
    if arguments == '':
        return ['None'], ['None']
    arguments = arguments.split(',')
    for arg in arguments:
        bracks = re.findall('\[ \]', arg)
        bracks = ' '.join(bracks)
        arg = arg.replace(bracks, '')
        arg = arg.strip()
        arg = re.sub(' +', ' ', arg)
        t = ' '.join(arg.split(' ')[:-1] + [bracks])
        n = arg.split(' ')[-1]
        types.append(t)
        names.append(n)
    return types, names
  
def tokenize_swift(code):  
    keywords = set([  
        "class", "deinit", "enum", "extension", "func", "import", "init", "inout", "let", "operator", "precedencegroup", "protocol", "struct", "subscript", "typealias", "var", "fileprivate", "internal", "private", "public", "static", "defer", "if", "guard", "do", "repeat", "else", "for", "case", "in", "while", "return", "break", "continue", "fallthrough", "switch", "default", "where", "catch", "throw", "throws", "rethrows", "try", "as", "Any", "false", "is", "nil", "rethrows", "super", "self", "Self", "throw", "true", "try", "catch"  
    ])  
      
    symbols = set("(){}[].,;+-*/%=<>!&|^~?@:#")  
      
    tokens = []  
    current = ""  
      
    for char in code:  
        if char in symbols:  
            if current:  
                tokens.append(current)  
                current = ""  
            tokens.append(char)  
        elif char.isspace():  
            if current:  
                tokens.append(current)  
                current = ""  
        else:  
            current += char  
      
    if current:  
        tokens.append(current)  
      
    return tokens  


if __name__ == '__main__':
    # parser
    parser = argparse.ArgumentParser()
    parser.add_argument('--input_file', default='',
                        help='The file to strip comments from.')
    parser.add_argument('--l', default='python',
                        choices=['python', 'java'], help='language of input code')
    args = parser.parse_args()
    assert args.input_file == '' or os.path.isfile(args.input_file)

    # read from standard input, or from input file
    if args.input_file == '':
        source = sys.stdin.read()
    else:
        with io.open(args.input_file, encoding='utf-8') as f:
            source = f.read()

    tokenize = globals()[f"tokenize_{args.l}"]
    # tokenize
    print(tokenize(source), end='')