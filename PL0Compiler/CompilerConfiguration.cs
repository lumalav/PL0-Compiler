using PL0Resources;

namespace PL0Compiler
{
    public class CompilerConfiguration
    {
        public bool PrintLexemesOnScreen { get; set; }
        public bool PrintAssemblyCode { get; set; }
        public bool Execute { get; set; }
        public string SourceCodeFilePath { get; set; }
        public VMConfiguration VmConfiguration { get; set; }
    }
}
