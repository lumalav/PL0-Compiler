using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using PL0Resources;
using PL0VM;

namespace PL0Compiler
{
    public class Compiler
    {
        private const string _cleanCodeFilePath = "cleanInput.txt";
        private static string _outputPath;
        private static int _codeCounter;
        private static int _currentLevel;
        private static int _state;
        private static bool _eof;
        private static string _buffer;
        private static string _buffer2;
        private static NameRecord _currentProcedure;
        private static Instruction[] _generatedCode2;
        private static BufferBlock<Instruction> _generatedCode;
        private static BinaryReader _binaryReader;
        private static FileStream _fileStream;
        private static StreamWriter _lexemeTableWriter;
        private static StreamWriter _lexemeListWriter;
        private static StreamWriter _assemblyCodeWriter;
        private static StreamWriter _traceOutWriter;
        private static SymbolTable _symbolTable;
        private static BufferBlock<TokenValue> _tokens;
        private static TokenValue _currentToken;
        private static CompilerConfiguration _compilerConfiguration;
        private static int _rp;

        public Compiler(Action<CompilerConfiguration> configuration)
        {
            _compilerConfiguration = new CompilerConfiguration();
            configuration(_compilerConfiguration);

            if (string.IsNullOrWhiteSpace(_compilerConfiguration.SourceCodeFilePath) || !File.Exists(_compilerConfiguration.SourceCodeFilePath))
            {
                throw new Exception("Source code could not be found!");
            }
        }

        public void Start()
        {
            if (_compilerConfiguration.Execute && _compilerConfiguration.VmConfiguration == null)
            {
                throw new ArgumentNullException($"CompilerConfiguration.VmConfiguration");
            }

            _tokens = new BufferBlock<TokenValue>(new DataflowBlockOptions
            {
                EnsureOrdered = true
            });
            _symbolTable = new SymbolTable();
            _generatedCode2 = new Instruction[Constants.MaxCodeLength];

            if (_compilerConfiguration.Execute)
            {
                _generatedCode = new BufferBlock<Instruction>(new DataflowBlockOptions
                {
                    BoundedCapacity = Constants.MaxCodeLength,
                    EnsureOrdered = true
                });
            }

            _currentToken = new TokenValue();
            _currentProcedure = null;
            _rp = -1;
            CleanFile();
            
            Task codeGenerator;
            if (_compilerConfiguration.Execute)
            {
                var executor = new VM(_compilerConfiguration.VmConfiguration)
                    .StartDirectPipeLine(_generatedCode);
                codeGenerator = CodeParse();
                TokenizeCode();
                codeGenerator.Wait();
                _traceOutWriter.WriteLine($"Assembly code generated successfully\t{DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss:ff")}");
                executor.Wait();
                _traceOutWriter.WriteLine($"Code execution ended successfully\t{DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss:ff")}");
            }
            else
            {
                codeGenerator = CodeParse();
                TokenizeCode();
                codeGenerator.Wait();
                _traceOutWriter.WriteLine($"Assembly code generated successfully\t{DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss:ff")}");
            }

            _traceOutWriter.Close();
        }
        
        #region Code Scanner/Tokenizer
        private static void CleanFile()
        {
            FileStream fs = null;
            FileStream fs2 = null;
            StreamWriter sw = null;
            BinaryReader sr = null;

            try
            {
                _outputPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(),
                    $"PL_0_Compiler_ExecutionLog_{DateTime.Now.ToString("MM_dd_yyyy_hh_mm_ss")}.log");

                _traceOutWriter = new StreamWriter(_outputPath);
                fs = File.Open(_cleanCodeFilePath, FileMode.Create, FileAccess.Write);
                fs2 = File.Open(_compilerConfiguration.SourceCodeFilePath, FileMode.Open, FileAccess.Read);
                sw = new StreamWriter(fs);
                sr = new BinaryReader(fs2);

                var comment = false;
                var inLine = false;
                while (sr.BaseStream.Position != sr.BaseStream.Length)
                {
                    var ch = sr.ReadChar();
                    switch (ch)
                    {
                        case '\r':
                        case '\n':
                            if (comment && inLine)
                            {
                                comment = false;
                                inLine = false;
                            }

                            if (!(sr.PeekChar() == '\r' || sr.PeekChar() == '\n'))
                            {
                                sw.Write(Environment.NewLine);
                            }

                            break;
                        case '/':
                            if (sr.BaseStream.Position != sr.BaseStream.Length && !comment && (sr.PeekChar() == '*' || sr.PeekChar() == '/'))
                            {
                                comment = true;
                                if (sr.PeekChar() == '/')
                                {
                                    inLine = true;
                                }
                            }
                            else
                            {
                                sw.Write(ch);
                            }
                            break;
                        case '*':
                            if (sr.BaseStream.Position != sr.BaseStream.Length && comment && !inLine && sr.PeekChar() == '/')
                            {
                                comment = false;
                            }
                            else
                            {
                                sw.Write(ch);
                            }
                            break;
                        default:
                            if (!comment)
                            {
                                sw.Write(ch);
                            }
                            break;
                    }
                }

                _traceOutWriter.WriteLine($"Comments extracted from the source file\t{DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss:ff")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(Environment.NewLine + $"Something happened during pre-compilation process! {ex.Message} Check: {_outputPath}");
                _traceOutWriter.WriteLine(ex.Message + " " + ex.StackTrace);
                throw;
            }
            finally
            {
                sr?.Close();
                sw?.Close();
                fs?.Close();
                fs2?.Close();
            }

            if (!File.Exists(_cleanCodeFilePath))
            {
                throw new Exception("Clean code could not be found!");
            }
        }

        private static void TokenizeCode()
        {
            _fileStream = null;
            _binaryReader = null;
            _state = 0;
            _buffer = string.Empty;

            try
            {
                
                _assemblyCodeWriter = new StreamWriter("generatedCode.txt");
                _lexemeTableWriter = new StreamWriter("lexemeTable.txt");
                _lexemeListWriter = new StreamWriter("lexemeList.txt");
                _lexemeTableWriter.Write("lexeme\ttoken type" + Environment.NewLine);
                _fileStream = File.Open(_cleanCodeFilePath, FileMode.Open, FileAccess.Read);
                _binaryReader = new BinaryReader(_fileStream);
                _eof = false;

                while (true)
                {
                    char c = '\0';
                    if (!_eof)
                    {
                        c = _binaryReader.ReadChar();
                    }

                    switch (_state)
                    {
                        case 0:
                            switch (c)
                            {
                                //Keywords
                                case 'b'://begin
                                    SaveToBuffer(c, 1);
                                    break;
                                case 'c'://call, const
                                    SaveToBuffer(c, 6);
                                    break;
                                case 'd'://do
                                    SaveToBuffer(c, 14);
                                    break;
                                case 'e'://else, end
                                    SaveToBuffer(c, 16);
                                    break;
                                case 'i'://if
                                    SaveToBuffer(c, 19);
                                    break;
                                case 'o'://odd
                                    SaveToBuffer(c, 21);
                                    break;
                                case 'p'://procedure
                                    SaveToBuffer(c, 24);
                                    break;
                                case 'r'://read
                                    SaveToBuffer(c, 53);
                                    break;
                                case 't'://then
                                    SaveToBuffer(c, 33);
                                    break;
                                case 'v'://var
                                    SaveToBuffer(c, 37);
                                    break;
                                case 'w'://while, write
                                    SaveToBuffer(c, 40);
                                    break;
                                // Numbers and symbols
                                case '+':
                                    _buffer += c;
                                    WriteToken(_buffer, Token.PLUS_SYM);
                                    break;
                                case '-':
                                    _buffer += c;
                                    WriteToken(_buffer, Token.MINUS_SYM);
                                    break;
                                case '*':
                                    _buffer += c;
                                    WriteToken(_buffer, Token.MULT_SYM);
                                    break;
                                case ';':
                                    _buffer += c;
                                    WriteToken(_buffer, Token.SEMICOLON_SYM);
                                    break;
                                case '/':
                                    _buffer += c;
                                    WriteToken(_buffer, Token.SLASH_SYM);
                                    break;
                                case '(':
                                    _buffer += c;
                                    WriteToken(_buffer, Token.LPARENT_SYM);
                                    break;
                                case ')':
                                    _buffer += c;
                                    WriteToken(_buffer, Token.RPARENT_SYM);
                                    break;
                                case ':':
                                    SaveToBuffer(c, 58);
                                    break;
                                case ',':
                                    _buffer += c;
                                    WriteToken(_buffer, Token.COMMA_SYM);
                                    break;
                                case '.':
                                    _buffer += c;
                                    WriteToken(_buffer, Token.PERIOD_SYM);
                                    break;
                                case '=':
                                    _buffer += c;
                                    WriteToken(_buffer, Token.EQL_SYM);
                                    break;
                                case '<':
                                    SaveToBuffer(c, 59);
                                    break;
                                case '>':
                                    SaveToBuffer(c, 60);
                                    break;
                                case '"':
                                    _state = 61;
                                    break;
                                default:
                                    if (char.IsDigit(c))
                                        SaveToBuffer(c, 57);
                                    else if (char.IsLetter(c))
                                        SaveToBuffer(c, 45);
                                    else if (c == '\r' || c == '\n' || c == '\0')
                                    {
                                        //nothing
                                    }
                                    // If it's not ANY of the previous symbols or a space character
                                    //  Then it's an invalid symbol
                                    else if (!char.IsWhiteSpace(c))
                                    {
                                        _buffer += c;
                                        throw new Exception($"Invalid {_buffer} symbol!" + Environment.NewLine);
                                    }

                                    break;
                            }
                            break;
                        case 1:
                            NextOrIdentLoop(c, 'e', 2);
                            break;
                        case 2:
                            NextOrIdentLoop(c, 'g', 3);
                            break;
                        case 3:
                            NextOrIdentLoop(c, 'i', 4);
                            break;
                        case 4:
                            NextOrIdentLoop(c, 'n', 5);
                            break;
                        case 5:
                            EndOrIdentLoop(c, Token.BEGIN_SYM);
                            break;
                        case 6:
                            switch (c)
                            {
                                case 'a':
                                    SaveToBuffer(c, 7);
                                    break;
                                case 'o':
                                    SaveToBuffer(c, 10);
                                    break;
                                default:
                                    if (char.IsLetterOrDigit(c))
                                    {
                                        SaveToBuffer(c, 45);
                                    }
                                    else
                                    {
                                        WriteToken(_buffer, Token.IDENT_SYM);
                                        _state = 0;
                                        _fileStream.Seek(-1, SeekOrigin.Current);
                                    }
                                    break;
                            }
                            break;
                        case 7:
                            NextOrIdentLoop(c, 'l', 8);
                            break;
                        case 8:
                            NextOrIdentLoop(c, 'l', 9);
                            break;
                        case 9:
                            EndOrIdentLoop(c, Token.CALL_SYM);
                            break;
                        case 10:
                            NextOrIdentLoop(c, 'n', 11);
                            break;
                        case 11:
                            NextOrIdentLoop(c, 's', 12);
                            break;
                        case 12:
                            NextOrIdentLoop(c, 't', 13);
                            break;
                        case 13:
                            EndOrIdentLoop(c, Token.CONST_SYM);
                            break;
                        case 14:
                            NextOrIdentLoop(c, 'o', 15);
                            break;
                        case 15:
                            EndOrIdentLoop(c, Token.DO_SYM);
                            break;
                        case 16:
                            switch (c)
                            {
                                case 'n':
                                    SaveToBuffer(c, 17);
                                    break;
                                case 'l':
                                    SaveToBuffer(c, 46);
                                    break;
                                default:
                                    if (char.IsLetterOrDigit(c))
                                    {
                                        SaveToBuffer(c, 45);
                                    }
                                    else
                                    {
                                        WriteToken(_buffer, Token.IDENT_SYM);
                                        _state = 0;
                                        _fileStream.Seek(-1, SeekOrigin.Current);
                                    }
                                    break;

                            }
                            break;
                        case 17:
                            NextOrIdentLoop(c, 'd', 18);
                            break;
                        case 18:
                            EndOrIdentLoop(c, Token.END_SYM);
                            break;
                        case 19:
                            NextOrIdentLoop(c, 'f', 20);
                            break;
                        case 20:
                            EndOrIdentLoop(c, Token.IF_SYM);
                            break;
                        case 21:
                            NextOrIdentLoop(c, 'd', 22);
                            break;
                        case 22:
                            NextOrIdentLoop(c, 'd', 23);
                            break;
                        case 23:
                            EndOrIdentLoop(c, Token.ODD_SYM);
                            break;
                        case 24:
                            NextOrIdentLoop(c, 'r', 25);
                            break;
                        case 25:
                            NextOrIdentLoop(c, 'o', 26);
                            break;
                        case 26:
                            NextOrIdentLoop(c, 'c', 27);
                            break;
                        case 27:
                            NextOrIdentLoop(c, 'e', 28);
                            break;
                        case 28:
                            NextOrIdentLoop(c, 'd', 29);
                            break;
                        case 29:
                            NextOrIdentLoop(c, 'u', 30);
                            break;
                        case 30:
                            NextOrIdentLoop(c, 'r', 31);
                            break;
                        case 31:
                            NextOrIdentLoop(c, 'e', 32);
                            break;
                        case 32:
                            EndOrIdentLoop(c, Token.PROC_SYM);
                            break;
                        case 33:
                            NextOrIdentLoop(c, 'h', 34);
                            break;
                        case 34:
                            NextOrIdentLoop(c, 'e', 35);
                            break;
                        case 35:
                            NextOrIdentLoop(c, 'n', 36);
                            break;
                        case 36:
                            EndOrIdentLoop(c, Token.THEN_SYM);
                            break;
                        case 37:
                            NextOrIdentLoop(c, 'a', 38);
                            break;
                        case 38:
                            NextOrIdentLoop(c, 'r', 39);
                            break;
                        case 39: // end of var
                            EndOrIdentLoop(c, Token.VAR_SYM);
                            break;
                        case 40:
                            switch (c)
                            {
                                case 'h':
                                    SaveToBuffer(c, 41);
                                    break;
                                case 'r':
                                    SaveToBuffer(c, 49);
                                    break;
                                default:
                                    if (char.IsLetterOrDigit(c))
                                    {
                                        SaveToBuffer(c, 45);
                                    }
                                    else
                                    {
                                        WriteToken(_buffer, Token.IDENT_SYM);
                                        _state = 0;
                                        _fileStream.Seek(-1, SeekOrigin.Current);
                                    }
                                    break;
                            }
                            break;
                        case 41:
                            NextOrIdentLoop(c, 'i', 42);
                            break;
                        case 42: // from i for while
                            NextOrIdentLoop(c, 'l', 43);
                            break;
                        case 43: // from l for while
                            NextOrIdentLoop(c, 'e', 44);
                            break;
                        case 44: // end of while
                            EndOrIdentLoop(c, Token.WHILE_SYM);
                            break;
                        case 45: // identsym loop
                            EndOrIdentLoop(c, Token.IDENT_SYM);
                            break;
                        case 46:
                            NextOrIdentLoop(c, 's', 47);
                            break;
                        case 47:
                            NextOrIdentLoop(c, 'e', 48);
                            break;
                        case 48: // end of else
                            EndOrIdentLoop(c, Token.ELSE_SYM);
                            break;
                        case 49:
                            NextOrIdentLoop(c, 'i', 50);
                            break;
                        case 50: // from i for write
                            NextOrIdentLoop(c, 't', 51);
                            break;
                        case 51: // from t for write
                            NextOrIdentLoop(c, 'e', 52);
                            break;
                        case 52: // end of write
                            EndOrIdentLoop(c, Token.WRITE_SYM);
                            break;
                        case 53:
                            NextOrIdentLoop(c, 'e', 54);
                            break;
                        case 54:
                            NextOrIdentLoop(c, 'a', 55);
                            break;
                        case 55:
                            NextOrIdentLoop(c, 'd', 56);
                            break;
                        case 56:
                            EndOrIdentLoop(c, Token.READ_SYM);
                            break;
                        // Symbol states
                        case 57: // numbersym loop
                            if (char.IsDigit(c))
                            {
                                SaveToBuffer(c, 57);
                            }
                            //Invalid variable name. Starts with number
                            else if (char.IsLetter(c))
                            {
                                // Use a while loop to dump the entire variable name onto the buffer
                                ParseRestOfToken(c);

                                if (char.IsDigit(_buffer[0]))
                                {
                                    throw new Exception($"Variable {_buffer} cannot start with a digit!" + Environment.NewLine);
                                }
                            }
                            else
                            {
                                WriteToken(_buffer, Token.NUMBER_SYM);
                                _fileStream.Seek(-1, SeekOrigin.Current);
                            }
                            break;
                        case 58: // checking for becomesym
                            if (c == '=')
                            {
                                _buffer += c;
                                WriteToken(_buffer, Token.BECOMES_SYM);
                            }
                            else
                            {
                                if (!char.IsWhiteSpace(c))
                                    _buffer += c;
                                throw new Exception($"Invalid {_buffer} symbol!" + Environment.NewLine);
                            }
                            break;
                        case 59: // coming from <
                            if (c == '=')
                            {
                                _buffer += c;
                                WriteToken(_buffer, Token.LEQ_SYM);
                            }
                            else if (c == '>')
                            {
                                _buffer += c;
                                WriteToken(_buffer, Token.NEQ_SYM);
                            }
                            else if (char.IsWhiteSpace(c) || char.IsLetterOrDigit(c))
                            {
                                WriteToken(_buffer, Token.LES_SYM);
                                _fileStream.Seek(-1, SeekOrigin.Current);
                                // Else, put back the character needed to look ahead
                            }
                            else
                            {
                                _buffer += c;
                                throw new Exception($"Invalid {_buffer} symbol!" + Environment.NewLine);
                            }
                            break;
                        case 60: // coming from >
                            if (c == '=')
                            {
                                _buffer += c;
                                WriteToken(_buffer, Token.GEQ_SYM);
                            }
                            else if (char.IsWhiteSpace(c) || char.IsLetterOrDigit(c))
                            {
                                WriteToken(_buffer, Token.GTR_SYM);
                                // Else, put back the character needed to look ahead
                                _fileStream.Seek(-1, SeekOrigin.Current);
                            }
                            else
                            {
                                _buffer += c;
                                throw new Exception($"Invalid {_buffer} symbol!" + Environment.NewLine);
                            }
                            break;
                        case 61:
                            if (c == '"')
                            {
                                WriteToken(_buffer2, Token.STRING_SYM);
                                _state = 0;
                            }
                            else
                            {
                                SaveToString(c, 61);
                            }
                            break;
                    }

                    if (_eof)
                    {
                        _tokens.Complete();
                        
                        break;
                    }

                    if (_binaryReader.BaseStream.Position == _binaryReader.BaseStream.Length)
                    {
                        _eof = true;
                    }
                }

                _traceOutWriter.WriteLine($"Source code has been 'tokenized' successfully\t{DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss:ff")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(Environment.NewLine + $"Something happened during compilation process! {ex.Message} Check: {_outputPath}");
                _traceOutWriter.WriteLine(ex.Message + " " + ex.StackTrace);
                throw;
            }
            finally
            {
                _fileStream?.Close();
                _binaryReader?.Close();
                _lexemeTableWriter?.Close();
                _lexemeListWriter?.Close();
            }
        }

        private static void WriteToken(string buf, Token token)
        {
            _lexemeTableWriter.Write($"{buf}\t{(int)token}" + Environment.NewLine);

            if (token == Token.IDENT_SYM)
            {
                _lexemeListWriter.Write($"{(int)token} {buf} ");
                
                if (_compilerConfiguration.PrintLexemesOnScreen)
                {
                    Console.Write($"{(int)token} {buf} ");
                }

                _tokens.Post(new TokenValue(token, buf));
            }
            else if (token == Token.NUMBER_SYM)
            {
                _lexemeListWriter.Write($"{(int)token} {buf} ");
                
                if (_compilerConfiguration.PrintLexemesOnScreen)
                {
                    Console.Write($"{(int)token} {buf} ");
                }

                var result = long.TryParse(_buffer, out var num);

                if (!result)
                {
                    throw new Exception($"This is not a valid number! {_buffer}!");
                }

                // Check if number is greater than Constants.MaxIntegerValue
                if (num > Constants.MaxIntegerValue)
                {
                    throw new Exception($"{num} is too large. The maximum integer allowed is {Constants.MaxIntegerValue}" + Environment.NewLine);
                }

                _tokens.Post(new TokenValue(token, buf, num));
            }
            else if (token == Token.STRING_SYM)
            {
                _lexemeListWriter.Write($"{(int)token} {buf} ");

                if (_compilerConfiguration.PrintLexemesOnScreen)
                {
                    Console.Write($"{(int)token} {buf} ");
                }
                _tokens.Post(new TokenValue(token, buf, buf));
            }
            else
            {
                _lexemeListWriter.Write($"{(int)token} ");
                
                if (_compilerConfiguration.PrintLexemesOnScreen)
                {
                    Console.Write($"{(int)token} ");
                }

                _tokens.Post(new TokenValue(token, string.Empty));
            }

            // Put state to 0
            _state = 0;
            _buffer = string.Empty;
            _buffer2 = string.Empty;
        }

        private static void NextOrIdentLoop(char c, char charToCheck, int nextState)
        {
            // If it's the required char, go to the next state
            if (c == charToCheck)
            {
                SaveToBuffer(c, nextState);
                return;
            }

            // If it's alphanumeric
            if (char.IsLetterOrDigit(c))
            {
                //Go to state 45 (identsym loop)
                SaveToBuffer(c, 45);
            }
            else
            {
                WriteToken(_buffer, Token.IDENT_SYM);
                _state = 0;
                _fileStream.Seek(-1, SeekOrigin.Current);
            }
        }

        private static void EndOrIdentLoop(char c, Token token)
        {
            if (char.IsLetterOrDigit(c))
            {
                SaveToBuffer(c, 45);
            }
            else
            {
                WriteToken(_buffer, token);
                _state = 0;
                _fileStream.Seek(-1, SeekOrigin.Current);
            }
        }

        private static void SaveToString(char c, int st)
        {
            _state = st;
            _buffer2 += c;

            if (_buffer2.Length > Constants.MaxLengthOfString)
            {
                // Use a while loop to dump the entire variable name onto the buffer
                ParseRestOfToken(c);
                throw new Exception($"{_buffer2} is too long. The longest string should be of {Constants.MaxLengthOfString} characters!" + Environment.NewLine);
            }
        }

        private static void SaveToBuffer(char c, int st)
        {
            _state = st;
            _buffer += c;

            if (_buffer.Length > Constants.MaxIdentifierLength)
            {
                // Use a while loop to dump the entire variable name onto the buffer
                ParseRestOfToken(c);
                throw new Exception($"{_buffer} is too long. The longest string should be of {Constants.MaxIdentifierLength} characters!" + Environment.NewLine);
            }
        }

        private static void ParseRestOfToken(char c)
        {
            while (char.IsLetterOrDigit(c))
            {
                _buffer += c;

                if (!_eof)
                {
                    c = _binaryReader.ReadChar();
                }

                if (!char.IsLetterOrDigit(c))
                {
                    _fileStream.Seek(-1, SeekOrigin.Current);
                    break;
                }

                if (_eof)
                {
                    break;
                }

                if (_binaryReader.BaseStream.Position == _binaryReader.BaseStream.Length)
                {
                    _eof = true;
                }
            }
        }
        #endregion

        #region Code Parser
        private static async Task CodeParse()
        {
            await GetToken();
            if (_compilerConfiguration.PrintLexemesOnScreen)
            {
                Console.Write(Environment.NewLine);
            }
            Emit(Op.JMP, 0, 0, 1);
            BlockParse();
            if (_currentToken.Token != Token.PERIOD_SYM)
            {
                Error(ErrorType.EXPECTED_PERIOD);
            }
            Emit(Op.SIO, 0, 0, 3);
            foreach (var code in _generatedCode2)
            {
                if (code == null)
                {
                    break;
                }
                _assemblyCodeWriter.WriteLine(code);
                if (_compilerConfiguration.Execute)
                {
                    _generatedCode.Post(code);
                }
            }

            if (_compilerConfiguration.Execute)
            {
                _generatedCode.Complete();
            }
            _assemblyCodeWriter?.Close();
        }

        private static async void BlockParse()
        {
            var varC = 0;

            if (_currentToken.Token == Token.CONST_SYM)
            {
                do
                {
                    await GetToken();
                    if (_currentToken.Token != Token.IDENT_SYM)
                    {
                        Error(ErrorType.MUST_BE_FOLLOWED_BY_ID);
                    }

                    var existingRecord = _symbolTable[_currentToken.Name];

                    if (existingRecord != null && existingRecord.Level == _currentLevel)
                    {
                        Error(ErrorType.REDEFINITION_OF_SYM, _currentToken.Name);
                    }

                    var record = new NameRecord
                    {
                        Name = _currentToken.Name,
                        Kind = Token.CONST_SYM
                    };

                    await GetToken();

                    if (_currentToken.Token != Token.EQL_SYM)
                    {
                        if (_currentToken.Token == Token.BECOMES_SYM)
                        {
                            Error(ErrorType.USE_BECOME_INSTEAD);
                        }

                        Error(ErrorType.ID_MUST_BE_FOLLOWED_BY_EQL);
                    }

                    await GetToken();

                    if (_currentToken.Token != Token.NUMBER_SYM)
                    {
                        Error(ErrorType.EQL_MUST_BE_FOLLOWED_BY_NUMBER);
                    }

                    record.Value = _currentToken.Value;
                    record.Level = _currentLevel;

                    _symbolTable[record.Name] = record;

                    await GetToken();
                } while (_currentToken.Token == Token.COMMA_SYM);

                if (_currentToken.Token != Token.SEMICOLON_SYM)
                {
                    Error(ErrorType.MISSING_SEMICOLON_OR_COMMA);
                }

                await GetToken();
            }

            if (_currentToken.Token == Token.VAR_SYM)
            {
                do
                {
                    await GetToken();

                    if (_currentToken.Token != Token.IDENT_SYM)
                    {
                        Error(ErrorType.MUST_BE_FOLLOWED_BY_ID);
                    }

                    var existingRecord = _symbolTable[_currentToken.Name];

                    if (existingRecord != null && existingRecord.Level == _currentLevel)
                    {
                        Error(ErrorType.REDEFINITION_OF_SYM, _currentToken.Name);
                    }

                    var record = new NameRecord
                    {
                        Kind = Token.VAR_SYM,
                        Name = _currentToken.Name,
                        Address = varC + 4,
                        Level = _currentLevel
                    };

                    _symbolTable[record.Name] = record;

                    varC++;

                    await GetToken();
                } while (_currentToken.Token == Token.COMMA_SYM);

                if (_currentToken.Token != Token.SEMICOLON_SYM)
                {
                    Error(ErrorType.MISSING_SEMICOLON_OR_COMMA);
                }

                await GetToken();
            }

            while (_currentToken.Token == Token.PROC_SYM)
            {
                await GetToken();

                if (_currentToken.Token != Token.IDENT_SYM)
                {
                    Error(ErrorType.MUST_BE_FOLLOWED_BY_ID);
                }

                var existingRecord = _symbolTable[_currentToken.Name];

                if (existingRecord != null && (existingRecord.Level == _currentLevel || existingRecord.Kind == Token.PROC_SYM))
                {
                    Error(ErrorType.REDEFINITION_OF_SYM, _currentToken.Name);
                }

                var record = new NameRecord
                {
                    Kind = Token.PROC_SYM,
                    Name = _currentToken.Name,
                    Address = 0,
                    Level = _currentLevel
                };

                _symbolTable[record.Name] = record;

                await GetToken();

                if (_currentToken.Token != Token.SEMICOLON_SYM)
                {
                    Error(ErrorType.EXPECTED_SEMICOLON_OR_CURL_BRACE);
                }

                await GetToken();

                _currentLevel++;

                var temp = _currentProcedure;

                _currentProcedure = record;

                BlockParse();

                _currentProcedure = temp;

                _currentLevel--;

                if (_currentToken.Token != Token.SEMICOLON_SYM)
                {
                    Error(ErrorType.EXPECTED_SEMICOLON_OR_CURL_BRACE);
                }

                await GetToken();
            }

            if (_currentLevel == 0)
            {
                _generatedCode2[0].M = _codeCounter;
            }

            if (_currentProcedure != null)
            {
                _currentProcedure.Address = _codeCounter;
            }

            Emit(Op.INC, 0, 0, 4 + varC);

            StatementParse();

            if (_currentLevel != 0)
            {
                Emit(Op.RTN, 0, 0, 0);
            }

            _symbolTable.RemoveAll(i => i.Level == _currentLevel);
        }

        private static async void ConditionParse()
        {
            if (_currentToken.Token == Token.ODD_SYM)
            {
                await GetToken();
                ExpressionParse();
                Emit(Op.ODD, _rp, 0, 0);
            }
            else
            {
                ExpressionParse();

                if (!IsRelationalSymbol())
                {
                    if (_currentToken.Token == Token.BECOMES_SYM)
                    {
                        Error(ErrorType.USE_BECOME_INSTEAD);
                    }

                    Error(ErrorType.EXPECTED_REL_OPERATOR);
                }

                var condOp = _currentToken.Token;

                await GetToken();

                ExpressionParse();

                switch (condOp)
                {
                    case Token.EQL_SYM:
                        _rp--;
                        Emit(Op.EQL, _rp, _rp, _rp+1);
                        break;
                    case Token.NEQ_SYM:
                        _rp--;
                        Emit(Op.NEQ, _rp, _rp, _rp + 1);
                        break;
                    case Token.LES_SYM:
                        _rp--;
                        Emit(Op.LSS, _rp, _rp, _rp + 1);
                        break;
                    case Token.LEQ_SYM:
                        _rp--;
                        Emit(Op.LEQ, _rp, _rp, _rp + 1);
                        break;
                    case Token.GTR_SYM:
                        _rp--;
                        Emit(Op.GTR, _rp, _rp, _rp + 1);
                        break;
                    case Token.GEQ_SYM:
                        _rp--;
                        Emit(Op.GEQ, _rp, _rp, _rp + 1);
                        break;
                }
            }
        }

        private static async void StatementParse()
        {
            if (_currentToken.Token == Token.IDENT_SYM)
            {
                if (!_symbolTable.Contains(_currentToken.Name))
                {
                    Error(ErrorType.UNDECLARED_ID, _currentToken.Name);
                }

                var record = _symbolTable[_currentToken.Name];

                if (record.Kind != Token.VAR_SYM)
                {
                    Error(ErrorType.ASSIGNMENT_TO_CONST_OR_PROC_NOT_ALLOWED);
                }

                await GetToken();

                if (_currentToken.Token != Token.BECOMES_SYM)
                {
                    Error(ErrorType.EXPECTED_ASSIGNMENT_OP);
                }

                await GetToken();

                ExpressionParse();

                if (!record.Level.HasValue || !record.Address.HasValue)
                {
                    throw new Exception("current record does not have any level nor address");
                }

                Emit(Op.STO, _rp, _currentLevel - record.Level.Value, record.Address.Value);
                _rp--;
            }
            else if (_currentToken.Token == Token.CALL_SYM)
            {
                await GetToken();

                if (_currentToken.Token != Token.IDENT_SYM)
                {
                    Error(ErrorType.CALL_MUST_BE_FOLLOWED_BY_ID);
                }

                if (!_symbolTable.Contains(_currentToken.Name))
                {
                    Error(ErrorType.UNDECLARED_ID, _currentToken.Name);
                }

                var record = _symbolTable[_currentToken.Name];

                if (record.Kind != Token.PROC_SYM)
                {
                    Error(ErrorType.CALL_OF_CONST_OR_VAR);
                }

                if (!record.Level.HasValue || !record.Address.HasValue)
                {
                    throw new Exception("current record does not have any level nor address");
                }

                Emit(Op.CAL, 0, _currentLevel - record.Level.Value, record.Address.Value);

                await GetToken();
            }
            else if (_currentToken.Token == Token.BEGIN_SYM)
            {
                await GetToken();
                StatementParse();
                while (_currentToken.Token == Token.SEMICOLON_SYM)
                {
                    await GetToken();
                    StatementParse();
                }

                if (_currentToken.Token == Token.BEGIN_SYM || _currentToken.Token == Token.IF_SYM ||
                    _currentToken.Token == Token.WHILE_SYM ||
                    _currentToken.Token == Token.READ_SYM || _currentToken.Token == Token.WRITE_SYM)
                {
                    Error(ErrorType.MISSING_SEMICOLON_BETWEEN_STATEMENTS);
                }

                if (_currentToken.Token != Token.END_SYM)
                {
                    Error(ErrorType.EXPECTED_SEMICOLON_OR_CURL_BRACE);
                }

                await GetToken();
            }
            else if (_currentToken.Token == Token.WRITE_SYM)
            {
                await GetToken();
                if (_currentToken.Token != Token.IDENT_SYM)
                {
                    Error(ErrorType.WRITE_MUST_BE_FOLLOWED_BY_ID);
                }

                if (!_symbolTable.Contains(_currentToken.Name))
                {
                    Error(ErrorType.UNDECLARED_ID, _currentToken.Name);
                }

                var record = _symbolTable[_currentToken.Name];

                if (record.Kind == Token.CONST_SYM)
                {
                    Emit(Op.LIT, ++_rp, 0, (int)record.Value);
                }
                else if (record.Kind == Token.VAR_SYM)
                {
                    if (!record.Level.HasValue || !record.Address.HasValue)
                    {
                        throw new Exception("current record does not have any level nor address");
                    }

                    Emit(Op.LOD, ++_rp, _currentLevel - record.Level.Value, record.Address.Value);
                }

                Emit(Op.SIO, _rp, 0, 1);
                _rp--;

                await GetToken();
            } 
            else if (_currentToken.Token == Token.READ_SYM)
            {
                await GetToken();

                if (_currentToken.Token != Token.IDENT_SYM)
                {
                    Error(ErrorType.READ_MUST_BE_FOLLOWED_BY_ID);
                }

                if (!_symbolTable.Contains(_currentToken.Name))
                {
                    Error(ErrorType.UNDECLARED_ID, _currentToken.Name);
                }

                var record = _symbolTable[_currentToken.Name];

                if (record.Kind != Token.VAR_SYM)
                {
                    Error(ErrorType.ASSIGNMENT_TO_CONST_OR_PROC_NOT_ALLOWED);
                }

                Emit(Op.SIO, ++_rp, 0, 2);

                if (!record.Level.HasValue || !record.Address.HasValue)
                {
                    throw new Exception("current record does not have any level nor address");
                }

                Emit(Op.STO, _rp, _currentLevel - record.Level.Value, record.Address.Value);
                _rp--;
                await GetToken();
            }
            else
            {
                if (_currentToken.Token == Token.IF_SYM)
                {
                    await GetToken();
                    ConditionParse();

                    if (_currentToken.Token != Token.THEN_SYM)
                    {
                        Error(ErrorType.EXPECTED_THEN);
                    }

                    await GetToken();
                    
                    var temp = _codeCounter;

                    Emit(Op.JPC, _rp, 0, 0);

                    StatementParse();

                    if (_currentToken.Token == Token.ELSE_SYM)
                    {
                        var temp2 = _codeCounter;

                        Emit(Op.JMP, 0, 0, 1);

                        _generatedCode2[temp].M = _codeCounter;

                        await GetToken();

                        StatementParse();

                        _generatedCode2[temp2].M = _codeCounter;
                    }
                    else
                    {
                        _generatedCode2[temp].M = _codeCounter;
                    }
                } 
                else if (_currentToken.Token == Token.WHILE_SYM)
                {
                    var temp = _codeCounter;

                    await GetToken();

                    ConditionParse();

                    var temp2 = _codeCounter;

                    Emit(Op.JPC, _rp, 0, 0);

                    if (_currentToken.Token != Token.DO_SYM)
                    {
                        Error(ErrorType.EXPECTED_DO);
                    }

                    await GetToken();

                    StatementParse();

                    Emit(Op.JMP, 0, 0, temp);

                    _generatedCode2[temp2].M = _codeCounter;
                } else if (_currentToken.Token != Token.SEMICOLON_SYM && _currentToken.Token != Token.END_SYM &&
                           _currentToken.Token != Token.NUL_SYM)
                {
                    Error(ErrorType.EXPECTED_STATEMENT);
                }
            }
        }

        private static async void ExpressionParse()
        {
            Token addOp;

            if (_currentToken.Token == Token.PLUS_SYM || _currentToken.Token == Token.MINUS_SYM)
            {
                addOp = _currentToken.Token;
                await GetToken();
                TermParse();
                if (addOp == Token.MINUS_SYM)
                {
                    Emit(Op.NEG, _rp, 0, 0);
                }
            }
            else
            {
                TermParse();
            }

            while (_currentToken.Token == Token.PLUS_SYM || _currentToken.Token == Token.MINUS_SYM)
            {
                addOp = _currentToken.Token;
                await GetToken();
                TermParse();
                _rp--;
                Emit(addOp == Token.PLUS_SYM ? Op.ADD : Op.SUB, _rp, _rp, _rp + 1);
            }
        }

        private static async void TermParse()
        {
            FactorParse();

            while (_currentToken.Token == Token.MULT_SYM || _currentToken.Token == Token.SLASH_SYM)
            {
                var mulOp = _currentToken.Token;

                await GetToken();

                if (_currentToken.Token == Token.STRING_SYM)
                {
                    Error(ErrorType.OPERATOR_CANNOT_BE_APPLIED_TO_STRING);
                }

                FactorParse();
                _rp--;
                Emit(mulOp == Token.MULT_SYM ? Op.MUL : Op.DIV, _rp, _rp, _rp + 1);
            }
        }

        private static async void FactorParse()
        {
            if (_currentToken.Token == Token.IDENT_SYM)
            {
                if (!_symbolTable.Contains(_currentToken.Name))
                {
                    Error(ErrorType.UNDECLARED_ID, _currentToken.Name);
                }

                var record = _symbolTable[_currentToken.Name];

                if (record.Kind == Token.CONST_SYM)
                {
                    Emit(Op.LIT, ++_rp, 0, (int)record.Value);
                } 
                else if (record.Kind == Token.VAR_SYM)
                {
                    if (!record.Level.HasValue || !record.Address.HasValue)
                    {
                        throw new Exception("Record level cannot be null!");
                    }

                    Emit(Op.LOD, ++_rp, _currentLevel - record.Level.Value, record.Address.Value);
                }

                await GetToken();
            } 
            else if (_currentToken.Token == Token.NUMBER_SYM)
            {
                Emit(Op.LIT, ++_rp, 0, (int)(long)_currentToken.Value);
                await GetToken();
            }
            else if (_currentToken.Token == Token.STRING_SYM)
            {
                Emit(Op.LIT, ++_rp, 0, _currentToken.Value);
                await GetToken();

                if (_currentToken.Token != Token.PLUS_SYM && _currentToken.Token != Token.EQL_SYM && _currentToken.Token != Token.NEQ_SYM && IsOperator())
                {
                    Error(ErrorType.OPERATOR_CANNOT_BE_APPLIED_TO_STRING2);
                }
            }
            else if (_currentToken.Token == Token.LPARENT_SYM)
            {
                await GetToken();
                ExpressionParse();
                if (_currentToken.Token != Token.RPARENT_SYM)
                {
                    Error(ErrorType.MISSING_R_PAREN);
                }
                await GetToken();
            }
            else
            {
                Error(ErrorType.PRECEDING_FACTOR_CANT_BEGIN_WITH_SYM, Constants.SymbolLabel[_currentToken.Token]);
            }
        }

        private static bool IsRelationalSymbol()
        {
            return (int)(long)_currentToken.Token > 8 && (int)(long)_currentToken.Token < 15;
        }

        private static async Task GetToken()
        {
            if (await _tokens.OutputAvailableAsync())
            {
                _currentToken = _tokens.Receive();
            }
            else
            {
                _currentToken.Token = Token.NUL_SYM;
                _currentToken.Name = string.Empty;
                _currentToken.Value = 0;
            }
        }

        private static void Emit(Op op, int r, int l, object m)
        {
            if (_codeCounter >= Constants.MaxCodeLength)
            {
                Error(ErrorType.MAX_CODE_LENGTH_REACHED);
            }

            _generatedCode2[_codeCounter] = new Instruction
            {
                Code = op,
                L = l,
                M = m,
                R = r,
                Pos = _codeCounter
            };

            _codeCounter++;
        }

        private static void Error(ErrorType errorType, string value = null)
        {
            var errorMessage = Constants.ErrorMessage[errorType];
            Console.Write(errorMessage);

            if (errorType >= ErrorType.PRECEDING_FACTOR_CANT_BEGIN_WITH_SYM)
            {
                var errorMessage2 = $"{value}{Constants.ErrorMessage2[errorType]}";
                Console.Write(errorMessage2);
                errorMessage += errorMessage2;
            }

            throw new Exception(errorMessage);
        }

        private static bool IsOperator()
        {
            return _currentToken.Token == Token.MINUS_SYM || _currentToken.Token == Token.MULT_SYM ||
                _currentToken.Token == Token.PLUS_SYM || _currentToken.Token == Token.SLASH_SYM || 
                _currentToken.Token == Token.ODD_SYM || _currentToken.Token == Token.EQL_SYM ||
                _currentToken.Token == Token.GEQ_SYM || _currentToken.Token == Token.LEQ_SYM ||
                _currentToken.Token == Token.LES_SYM || _currentToken.Token == Token.MINUS_SYM || 
                _currentToken.Token == Token.NEQ_SYM;
        }

        #endregion
    }
}
