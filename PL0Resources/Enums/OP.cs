namespace PL0Resources
{
    /// <summary>
    /// OP Codes
    /// </summary>
    public enum Op
    {
        /// <summary>
        /// None
        /// </summary>
        NONE, 
        /// <summary>
        /// Load literal
        /// </summary>
        LIT, 
        /// <summary>
        /// Return
        /// </summary>
        RTN,
        /// <summary>
        /// Load 
        /// </summary>
        LOD, 
        /// <summary>
        /// Store
        /// </summary>
        STO, 
        /// <summary>
        /// Call function
        /// </summary>
        CAL, 
        /// <summary>
        /// Increment
        /// </summary>
        INC, 
        /// <summary>
        /// Jump to instruction
        /// </summary>
        JMP, 
        /// <summary>
        /// Jump to instruction
        /// </summary>
        JPC,
        /// <summary>
        /// Signal EOP
        /// </summary>
        SIO, //9,10,11
        /// <summary>
        /// Negation
        /// </summary>
        NEG = 12,
        /// <summary>
        /// Addition
        /// </summary>
        ADD = 13,
        /// <summary>
        /// Subtraction
        /// </summary>
        SUB = 14,
        /// <summary>
        /// Multiplication
        /// </summary>
        MUL = 15,
        /// <summary>
        /// Division
        /// </summary>
        DIV = 16,
        /// <summary>
        /// Is Odd
        /// </summary>
        ODD = 17,
        /// <summary>
        /// Modulo
        /// </summary>
        MOD = 18,
        /// <summary>
        /// Is Equal
        /// </summary>
        EQL = 19,
        /// <summary>
        /// Not Equal
        /// </summary>
        NEQ = 20,
        /// <summary>
        /// Less than
        /// </summary>
        LSS = 21,
        /// <summary>
        /// Less or equal
        /// </summary>
        LEQ = 22,
        /// <summary>
        /// Greater than
        /// </summary>
        GTR = 23,
        /// <summary>
        /// Greater or equal
        /// </summary>
        GEQ = 24
    }
}
