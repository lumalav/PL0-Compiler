using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using PL0Resources;

namespace PL0VM
{
    public class VM
    {
        private static Instruction[] _code;
        private static object[] _stack;
        private static int[] _ar;
        private static int _currentActivationRecord;
        private static int _sp;
        private static int _bp;
        private static int _pc;
        private static bool _halt;
        private static object[] _rf;
        private static StreamWriter _traceOutWriter;
        private static VMConfiguration _vmConfiguration;

        public VM(VMConfiguration configuration)
        {
            _vmConfiguration = configuration ?? throw new ArgumentNullException($"VmConfiguration");
        }

        public void Start()
        {
            var outputPath = string.Empty;
            try
            {
                outputPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(),
                    $"PL_0_VM_ExecutionLog_{DateTime.Now.ToString("MM_dd_yyyy_hh_mm_ss")}.log");

                _traceOutWriter = new StreamWriter(outputPath);
                _rf = new object[8];
                _sp = 0;
                _bp = 1;
                _pc = 0;
                _stack = new object[Constants.MaxStackHeight];
                _code = new Instruction[Constants.MaxCodeLength];
                _ar = Enumerable.Repeat(-1, Constants.MaxLexiLevels).ToArray();
                _halt = false;

                if (string.IsNullOrWhiteSpace(_vmConfiguration.AssemblyCodePath) || !File.Exists(_vmConfiguration.AssemblyCodePath))
                {
                    throw new Exception($"The file {_vmConfiguration.AssemblyCodePath} does not exist!");
                }

                ReadInstructionsFromFile();

                if (_vmConfiguration.PrintExecutionTraceOnScreen)
                {
                    //print the header for execution time
                    Console.Write("Execution trace:" + Environment.NewLine +
                                  "\t\t\t\t\tpc\tbp\tsp\tregisters" + Environment.NewLine);
                    Console.Write("Initial values\t\t\t\t");
                }

                _traceOutWriter.Write("Execution trace:" + Environment.NewLine +
                                      "\t\t\t\t\tpc\tbp\tsp\tregisters" + Environment.NewLine);
                _traceOutWriter.Write("Initial values\t\t\t\t");

                DumpRegisters();
                DumpStack();

                if (_vmConfiguration.PrintExecutionTraceOnScreen)
                {
                    Console.Write(Environment.NewLine);
                }

                _traceOutWriter.Write(Environment.NewLine);
                //Read instructions until the halt instruction
                while (!_halt)
                {
                    var instruction = FetchCycle();
                    ExecuteCycle(instruction);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(Environment.NewLine + $"Something happened during execution! {ex.Message} Check: {outputPath}");
                _traceOutWriter.WriteLine(ex.Message + " " + ex.StackTrace);
            }
            finally
            {
                _traceOutWriter.Close();
            }
        }

        public async Task StartDirectPipeLine(BufferBlock<Instruction> generatedCode)
        {

            var outputPath = string.Empty;
            try
            {
                outputPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(),
                    $"PL_0_ExecutionLog_{DateTime.Now.ToString("MM_dd_yyyy_hh_mm_ss")}.log");

                _traceOutWriter = new StreamWriter(outputPath);
                _rf = new object[8];
                _sp = 0;
                _bp = 1;
                _pc = 0;
                _stack = new object[Constants.MaxStackHeight];
                _code = new Instruction[Constants.MaxCodeLength];
                _ar = Enumerable.Repeat(-1, Constants.MaxLexiLevels).ToArray();
                _halt = false;

                if (_vmConfiguration.PrintExecutionTraceOnScreen)
                {
                    //print the header for execution time
                    Console.Write("Execution trace:" + Environment.NewLine +
                                  "\t\t\t\t\tpc\tbp\tsp\tregisters" + Environment.NewLine);
                    Console.Write("Initial values\t\t\t\t");
                }

                _traceOutWriter.Write("Execution trace:" + Environment.NewLine +
                                      "\t\t\t\t\tpc\tbp\tsp\tregisters" + Environment.NewLine);
                _traceOutWriter.Write("Initial values\t\t\t\t");

                DumpRegisters();
                DumpStack();

                if (_vmConfiguration.PrintExecutionTraceOnScreen)
                {
                    Console.Write(Environment.NewLine);
                }

                _traceOutWriter.Write(Environment.NewLine);
                //Read instructions until the halt instruction
                var index = 0;
                while (await generatedCode.OutputAvailableAsync() && !_halt)
                {
                    var instruction = generatedCode.Receive();
                    _code[index++] = instruction;
                }

                while (!_halt)
                {
                    var instruction = FetchCycle();
                    ExecuteCycle(instruction);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(Environment.NewLine + $"Something happened during execution! {ex.Message} Check: {outputPath}");
                _traceOutWriter.WriteLine(ex.Message + " " + ex.StackTrace);
            }
            finally
            {
                _traceOutWriter.Close();
            }
        }

        private static void ReadInstructionsFromFile()
        {
            var index = 0;
            foreach (var line in File.ReadAllLines(_vmConfiguration.AssemblyCodePath))
            {
                var spl = line.Split(new[] {' ', '\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
                if (spl.Length < 4)
                {
                    Console.Write("There was an error while reading the file! Wrong format!");
                    _halt = true;
                    break;
                }

                var r1 = int.TryParse(spl[0], out var op);
                var r2 = int.TryParse(spl[1], out var r);
                var r3 = int.TryParse(spl[2], out var l);


                if (!r1 || !r2 || !r3)
                {
                    throw new Exception("There was an error while reading the file. These are not numbers!");
                }

                if (op > 8 && op < 12)
                {
                    op = 9;
                }

                var r4 = int.TryParse(spl[3], out var m);
                
                if (!r4)
                {
                    var m2 = string.Join(" ", spl.Skip(3));

                    _code[index] = new Instruction
                    {
                        Pos = index++,
                        Code = (Op)op,
                        R = r,
                        L = l,
                        M = m2
                    };
                }
                else
                {
                    _code[index] = new Instruction
                    {
                        Pos = index++,
                        Code = (Op)op,
                        R = r,
                        L = l,
                        M = m
                    };
                }
            }
        }

        private static Instruction FetchCycle()
        {
            return _code[_pc++];
        }

        private static void ExecuteCycle(Instruction ir)
        {
            switch (ir.Code)
            {
                case Op.LIT:
                    _rf[ir.R] = ir.M;
                    break;
                case Op.RTN:
                    _sp = _bp - 1;
                    _bp = (int)_stack[_sp + 3];
                    _pc = (int)_stack[_sp + 4];
                    break;
                case Op.LOD:
                    _rf[ir.R] = _stack[Base(ir.L, _bp) + (int)ir.M];
                    break;
                case Op.STO:
                    _stack[Base(ir.L, _bp) + (int)ir.M] = _rf[ir.R]; 
                    break;
                case Op.CAL:
                    _stack[_sp + 1] = 0;                          // return value (FV)
                    _stack[_sp + 2] = Base(ir.L, _bp);     // static link (SL)
                    _stack[_sp + 3] = _bp;                        // dynamic link (DL)
                    _stack[_sp + 4] = _pc;                        // return address (RA)
                    _bp = _sp + 1;
                    _pc = (int)ir.M;                               //pc = M

                    //Record activation record separators
                    if (_currentActivationRecord < Constants.MaxLexiLevels)
                    {
                        _ar[_currentActivationRecord++] = _sp+1;
                    }
                    break;
                case Op.INC:
                    _sp += (int)ir.M; //sp = sp + M
                    break;
                case Op.JMP:
                    _pc = (int)ir.M; //pc = M
                    break;
                case Op.JPC:
                    if ((int)_rf[ir.R] == 0)
                    {
                        _pc = (int)ir.M; //then { pc = M; }
                    }
                    break;
                case Op.SIO:
                    //An Input/Output Operation
                    switch (ir.M)
                    {
                        case 1:
                            var finalValue = (_rf[ir.R] ?? "").ToString().Replace("\\n", "\n").Replace("\\r", "\r");
                            Console.Write($"{finalValue}");
                            break;
                        case 2:
                            var value = Console.ReadLine();
                            var isNum = int.TryParse(value, out var possible);
                            _rf[ir.R] = isNum ? possible : (object)value;
                            break;
                        case 3:
                            _halt = true;
                            break;
                    }
                    break;
                case Op.NEG:
                    _rf[ir.R] = -(int)_rf[ir.R];
                    break;
                case Op.ADD:
                    _rf[ir.R] = (dynamic)_rf[ir.L] + (dynamic)_rf[(int)ir.M];
                    break;
                case Op.SUB:
                    _rf[ir.R] = (int)_rf[ir.L] - (int)_rf[(int)ir.M];
                    break;
                case Op.MUL:
                    _rf[ir.R] = (int)_rf[ir.L] * (int)_rf[(int)ir.M];
                    break;
                case Op.DIV:
                    _rf[ir.R] = (int)_rf[ir.L] / (int)_rf[(int)ir.M];
                    break;
                case Op.ODD:
                    _rf[ir.R] = (int)_rf[ir.R] % 2;
                    break;
                case Op.MOD:
                    _rf[ir.R] = (int)_rf[ir.L] % (int)_rf[(int)ir.M];
                    break;
                case Op.EQL:
                    _rf[ir.R] = (dynamic)_rf[ir.L] == (dynamic)_rf[(int)ir.M] ? 1 : 0;
                    break;
                case Op.NEQ:
                    _rf[ir.R] = (dynamic)_rf[ir.L] != (dynamic)_rf[(int)ir.M] ? 1 : 0;
                    break;
                case Op.LSS:
                    _rf[ir.R] = (int)_rf[ir.L] < (int)_rf[(int)ir.M] ? 1 : 0;
                    break;
                case Op.LEQ:
                    _rf[ir.R] = (int)_rf[ir.L] <= (int)_rf[(int)ir.M] ? 1 : 0;
                    break;
                case Op.GTR:
                    _rf[ir.R] = (int)_rf[ir.L] > (int)_rf[(int)ir.M] ? 1 : 0;
                    break;
                case Op.GEQ:
                    _rf[ir.R] = (int)_rf[ir.L] >= (int)_rf[(int)ir.M] ? 1 : 0;
                    break;
                default:
                    throw new Exception("");
            }

            //Show the instruction to be executed
            DumpInstruction(ir);

            //Dump registers and stack
            DumpRegisters();
            DumpStack();

            if (_vmConfiguration.PrintExecutionTraceOnScreen)
            {
                Console.Write(Environment.NewLine);
            }
            _traceOutWriter.Write(Environment.NewLine);
        }

        private static void DumpInstruction(Instruction ir)
        {
            var value = $"{ir.Pos}\t{ir.Code.ToString()}\t{ir.R}\t{ir.L}\t{ir.M.ToString()}\t";
            if (_vmConfiguration.PrintExecutionTraceOnScreen)
            {
                Console.Write(value);
            }

            _traceOutWriter.Write(value);
        }

        private static void DumpRegisters()
        {
            var value = $"{_pc}\t{_bp}\t{_sp}\t{string.Join(" ", _rf)}";
            if (_vmConfiguration.PrintExecutionTraceOnScreen)
            {
                //Dump pc bp sp
                Console.Write(value);
            }
            _traceOutWriter.Write(value);
        }

        private static void DumpStack()
        {
            //    i goes through each element in the stack
            //    j goes through the markers for the "|"
            int i, j = 0;
            _traceOutWriter.Write(Environment.NewLine + "Stack: ");
            if (_vmConfiguration.PrintExecutionTraceOnScreen)
            {
                Console.Write(Environment.NewLine + "Stack: ");
            }

            for (i = 1; i <= _sp; i++)
            {
                if (j < Constants.MaxLexiLevels && _ar[j] == i)
                {
                    if (_vmConfiguration.PrintExecutionTraceOnScreen)
                    {
                        Console.Write("| ");
                    }
                    _traceOutWriter.Write("| ");
                    j++;
                }
                if (_vmConfiguration.PrintExecutionTraceOnScreen)
                {
                    Console.Write(_stack[i] + " ");
                }
                _traceOutWriter.Write(_stack[i] + " ");
            }

            _traceOutWriter.Write(Environment.NewLine);
            if (_vmConfiguration.PrintExecutionTraceOnScreen)
            {
                Console.Write(Environment.NewLine);
            }
        }

        private static int Base(int level, int b)
        {
            while (level > 0)
            {
                b = (int)_stack[b + 1];
                level--;
            }
            return b;
        }
    }
}