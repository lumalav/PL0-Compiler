using System.Collections.Generic;

namespace PL0Resources
{
    public class Constants
    {
        public const ushort MaxNameTableSize = 500;
        public const ushort MaxCodeLength = 500;
        public const ushort ReservedWords = 15;
        public const ushort MaxIntegerValue = 32767;
        public const ushort MaxIdentifierLength = 11;
        public const ushort MaxDepthOfNesting = 5;
        public const ushort MaxLengthOfString = 256;
        public const ushort MaxStackHeight = 40;
        public const byte MaxLexiLevels = 3;

        public static readonly string[] SymbolName = {
            "", "", "", "", "+", "-",
            "*", "/", "odd", "=", "<>", "<", "<=",
            ">", ">=", "(", ")", ",", ";",
            ".", ":=", "begin", "end", "if", "then",
            "while", "do", "call", "const", "var", "procedure", "write",
            "read", "else"
        };

        public static readonly Dictionary<Token, string> SymbolLabel = new Dictionary<Token, string>
        {
            {Token.PLUS_SYM, "+"},
            {Token.MINUS_SYM, "-"},
            {Token.MULT_SYM, "*"},
            {Token.SLASH_SYM, "/"},
            {Token.ODD_SYM, "odd"},
            {Token.EQL_SYM, "="},
            {Token.NEQ_SYM, "<>"},
            {Token.LES_SYM, "<"},
            {Token.LEQ_SYM, "<="},
            {Token.GTR_SYM, ">"},
            {Token.GEQ_SYM, ">="},
            {Token.LPARENT_SYM, "("},
            {Token.RPARENT_SYM, ")"},
            {Token.COMMA_SYM, ","},
            {Token.SEMICOLON_SYM, ";"},
            {Token.END_SYM, "end"},
            {Token.BEGIN_SYM, "begin"},
            {Token.IF_SYM, "if"},
            {Token.THEN_SYM, "then"},
            {Token.ELSE_SYM, "else"},
            {Token.WHILE_SYM, "while"},
            {Token.WRITE_SYM, "write"},
            {Token.READ_SYM, "read"},
            {Token.CONST_SYM, "const"},
            {Token.CALL_SYM, "call"},
            {Token.PROC_SYM, "procedure"},
            {Token.VAR_SYM, "var"},
            {Token.DO_SYM, "do"}
        };

        public static readonly Dictionary<ErrorType, string> ErrorMessage = new Dictionary<ErrorType, string>
        {
            {ErrorType.MAX_CODE_LENGTH_REACHED, "Maximum code length reached.\n"},
            {ErrorType.USE_BECOME_INSTEAD, "Use = instead of :=.\n"},
            {ErrorType.EQL_MUST_BE_FOLLOWED_BY_NUMBER,"= must be followed by a number.\n"},
            {ErrorType.ID_MUST_BE_FOLLOWED_BY_EQL, "Identifier must be followed by =.\n"},
            {ErrorType.MUST_BE_FOLLOWED_BY_ID, "const, var, procedure must be followed by identifier.\n"},
            {ErrorType.MISSING_SEMICOLON_OR_COMMA, "Semicolon or comma missing.\n"},
            {ErrorType.INCORRECT_SYM_AFTER_PROCEDURE, "Incorrect symbol after procedure declaration.\n"},
            {ErrorType.EXPECTED_STATEMENT, "Statement expected.\n"},
            {ErrorType.INCORRECT_SYM_AFTER_STATEMENT_IN_BLOCK, "Incorrect symbol after statement part in block.\n"},
            {ErrorType.EXPECTED_PERIOD, "Period expected.\n"},
            {ErrorType.MISSING_SEMICOLON_BETWEEN_STATEMENTS, "Semicolon between statements missing.\n"},
            {ErrorType.ASSIGNMENT_TO_CONST_OR_PROC_NOT_ALLOWED, "Assignment to constant or procedure is not allowed.\n"},
            {ErrorType.EXPECTED_ASSIGNMENT_OP, "Assignment operator expected.\n"},
            {ErrorType.CALL_MUST_BE_FOLLOWED_BY_ID, "call must be followed by an identifier.\n"},
            {ErrorType.READ_MUST_BE_FOLLOWED_BY_ID, "read must be followed by an identifier.\n"},
            {ErrorType.WRITE_MUST_BE_FOLLOWED_BY_ID, "write must be followed by an identifier.\n"},
            {ErrorType.CALL_OF_CONST_OR_VAR, "Call of a constant or variable is meaningless.\n"},
            {ErrorType.EXPECTED_THEN, "then expected.\n"},
            {ErrorType.EXPECTED_SEMICOLON_OR_CURL_BRACE, "End expected.\n"},
            {ErrorType.EXPECTED_DO, "do expected.\n"},
            {ErrorType.INCORRECT_SYM_FOLLOWING_STATEMENT, "Incorrect symbol following statement.\n"},
            {ErrorType.EXPECTED_REL_OPERATOR, "Relational operator expected.\n"},
            {ErrorType.EXPRESSION_MUST_NOT_CONTAIN_PROC_ID, "Expression must not contain a procedure identifier.\n"},
            {ErrorType.MISSING_R_PAREN, "Right parenthesis missing.\n"},
            {ErrorType.OPERATOR_CANNOT_BE_APPLIED_TO_STRING, "Multiplication and Division operators cannot be applied to operands of type 'string'.\n" },
            {ErrorType.OPERATOR_CANNOT_BE_APPLIED_TO_STRING2, "The only operator allowed to operands of type 'string' is the '+'.\n" },
            {ErrorType.PRECEDING_FACTOR_CANT_BEGIN_WITH_SYM, "The preceding factor cannot begin with the '"},
            {ErrorType.EXPRESSION_CANT_BEGIN_WITH_SYM, "An expression cannot begin with the '"},
            {ErrorType.NUMBER_TOO_LARGE, "The number "},
            {ErrorType.UNDECLARED_ID, "'"},
            {ErrorType.REDEFINITION_OF_SYM, "Redefinition of '"}
        };

        public static readonly Dictionary<ErrorType, string> ErrorMessage2 = new Dictionary<ErrorType, string>
        {
            {ErrorType.PRECEDING_FACTOR_CANT_BEGIN_WITH_SYM, "' symbol.\n"},
            {ErrorType.EXPRESSION_CANT_BEGIN_WITH_SYM, "' symbol.\n"},
            {ErrorType.NUMBER_TOO_LARGE, " is too large.\n"},
            {ErrorType.UNDECLARED_ID, "' undeclared.\n"},
            {ErrorType.REDEFINITION_OF_SYM, "'\n"}
        };
    }
}