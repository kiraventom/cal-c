namespace cal_c.Enums
{
    public enum MathOperation
    {
        None,
        Add,
        Substract,
        Multiply,
        Divide
    }

    internal enum State
    {
        WaitingFirst,
        EnteringFirst,
        WaitingSecond,
        EnteringSecond,
        ShowingResult
    }

    internal enum InputType
    {
        None,
        Digit,
        Operation,
        Sign,
        DecimalPoint,
        Erase,
        Clear,
        Calculate,
        Incorrect
    }
}