﻿namespace PL0Resources
{
    public enum ErrorType
    {
        MAX_CODE_LENGTH_REACHED,
        USE_BECOME_INSTEAD,
        EQL_MUST_BE_FOLLOWED_BY_NUMBER,
        ID_MUST_BE_FOLLOWED_BY_EQL,
        MUST_BE_FOLLOWED_BY_ID,
        MISSING_SEMICOLON_OR_COMMA,
        INCORRECT_SYM_AFTER_PROCEDURE,
        EXPECTED_STATEMENT,
        INCORRECT_SYM_AFTER_STATEMENT_IN_BLOCK,
        EXPECTED_PERIOD,
        MISSING_SEMICOLON_BETWEEN_STATEMENTS,
        ASSIGNMENT_TO_CONST_OR_PROC_NOT_ALLOWED,
        EXPECTED_ASSIGNMENT_OP,
        CALL_MUST_BE_FOLLOWED_BY_ID,
        READ_MUST_BE_FOLLOWED_BY_ID,
        WRITE_MUST_BE_FOLLOWED_BY_ID,
        CALL_OF_CONST_OR_VAR,
        EXPECTED_THEN,
        EXPECTED_SEMICOLON_OR_CURL_BRACE,
        EXPECTED_DO,
        INCORRECT_SYM_FOLLOWING_STATEMENT,
        EXPECTED_REL_OPERATOR,
        EXPRESSION_MUST_NOT_CONTAIN_PROC_ID,
        MISSING_R_PAREN,
        PRECEDING_FACTOR_CANT_BEGIN_WITH_SYM,
        EXPRESSION_CANT_BEGIN_WITH_SYM,
        NUMBER_TOO_LARGE,
        UNDECLARED_ID,
        REDEFINITION_OF_SYM,
        OPERATOR_CANNOT_BE_APPLIED_TO_STRING,
        OPERATOR_CANNOT_BE_APPLIED_TO_STRING2
    }
}
