namespace cal_c
{
    internal record Result
    {
        internal const string DivisionByZero = "Division by zero";
        
        public double Value { get; init; }
        public bool IsValid { get; init; } = true;
        public string Message { get; init; }
    }
}