using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using cal_c.Enums;
using cal_c.Exception;

namespace cal_c
{
    internal class CalcEngine
    {
        public CalcEngine()
        {
            _firstNumberBuilder = new StringBuilder();
            _secondNumberBuilder = new StringBuilder();
            _currentState = State.WaitingFirst;
            Operation = MathOperation.None;
            Program.KeyPressed += OnKeyPressed;
        }

        public int FirstSign { get; private set; } = 1;
        public int SecondSign { get; private set; } = 1;
        public string First => _firstNumberBuilder.ToString();
        public string Second => _secondNumberBuilder.ToString();
        public MathOperation Operation { get; private set; }
        public Result Result { get; private set; }

        private const char DecimalPoint = '.';
        
        private State _currentState;
        private bool _firstHasDecimalPoint;
        private bool _secondHasDecimalPoint;
        private double? _first;
        private double? _second;
        private readonly StringBuilder _firstNumberBuilder;
        private readonly StringBuilder _secondNumberBuilder;

        private void OnKeyPressed(object sender, char c)
        {
            var inputType = ProcessInput(c);
            Action action = inputType switch
            {
                InputType.Digit => () => ProcessDigit(c),
                InputType.Operation => () => ProcessOperation(c),
                InputType.Sign => ProcessSign,
                InputType.DecimalPoint => ProcessDecimalPoint,
                InputType.Erase => ProcessEraseCommand,
                InputType.Clear => ProcessClearCommand,
                InputType.Calculate => ProcessCalculateCommand,
                InputType.Incorrect or InputType.None => () => { },
                _ => throw ExceptionFactory.CreateEnumException(inputType, nameof(inputType))
            };
            
            action.Invoke();
        }

        private InputType ProcessInput(char c)
        {
            if (int.TryParse(c.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out _))
                return InputType.Digit;

            return c switch
            {
                ',' or '.' => InputType.DecimalPoint,
                'e' => InputType.Erase,
                'c' => InputType.Clear,
                '=' => InputType.Calculate,
                '+' or '*' or '/' => InputType.Operation,
                '-' => _currentState switch
                {
                    State.WaitingFirst or State.WaitingSecond => InputType.Sign,
                    State.EnteringFirst => InputType.Operation,
                    _ => InputType.Incorrect
                },
                _ => InputType.Incorrect
            };
        }

        private void ProcessDigit(char c)
        {
            _currentState = _currentState switch
            {
                State.WaitingFirst => State.EnteringFirst,
                State.WaitingSecond => State.EnteringSecond,
                _ => _currentState
            };

            switch (_currentState)
            {
                case State.EnteringFirst:
                    // do not allow numbers like 0000, but allow 0.000
                    if (_firstNumberBuilder.Length == 1 && _firstNumberBuilder[0] == '0')
                        return;
                    
                    _firstNumberBuilder.Append(c);
                    break;
                
                case State.EnteringSecond:
                    if (_secondNumberBuilder.Length == 1 && _secondNumberBuilder[0] == '0')
                        return;
                    
                    _secondNumberBuilder.Append(c);
                    break;
                
                case State.WaitingFirst:
                case State.WaitingSecond:
                case State.ShowingResult:
                    return;
                
                default:
                    throw ExceptionFactory.CreateEnumException(_currentState, nameof(_currentState));
            }

            UpdateNumber();
        }

        private void ProcessOperation(char c)
        {
            if (_currentState is not State.EnteringFirst)
                return;

            Operation = ParseOperation(c);
            _currentState = State.WaitingSecond;
        }

        private void ProcessSign()
        {
            switch (_currentState)
            {
                case State.WaitingFirst:
                    FirstSign = -1;
                    break;
                
                case State.WaitingSecond:
                    SecondSign = -1;
                    break;
                
                case State.EnteringFirst:
                case State.EnteringSecond:
                case State.ShowingResult:
                    return;
                
                default:
                    throw ExceptionFactory.CreateEnumException(_currentState, nameof(_currentState));
            }
        }

        private void ProcessDecimalPoint()
        {
            switch (_currentState)
            {
                case State.EnteringFirst when !_firstHasDecimalPoint:
                    _firstNumberBuilder.Append(DecimalPoint);
                    _firstHasDecimalPoint = true;
                    break;
                
                case State.EnteringSecond when !_secondHasDecimalPoint:
                    _secondNumberBuilder.Append(DecimalPoint);
                    _secondHasDecimalPoint = true;
                    break;
                
                case State.WaitingFirst:
                case State.WaitingSecond:
                case State.ShowingResult:
                    return;
                
                default:
                    throw ExceptionFactory.CreateEnumException(_currentState, nameof(_currentState));
            }
        }

        private void ProcessEraseCommand()
        {
            switch (_currentState)
            {
                case State.WaitingFirst:
                    if (FirstSign is -1)
                        FirstSign = 1;
                    
                    return;

                case State.WaitingSecond when SecondSign is -1:
                    SecondSign = 1;
                    return;

                case State.WaitingSecond:
                    Operation = MathOperation.None;
                    _currentState = State.EnteringFirst;
                    return;

                case State.EnteringFirst:
                    if (_firstNumberBuilder[^1] == DecimalPoint)
                        _firstHasDecimalPoint = false;
                    
                    _firstNumberBuilder.Remove(_firstNumberBuilder.Length - 1, 1);
                    if (_firstNumberBuilder.Length == 0)
                        _currentState = State.WaitingFirst;
                    
                    break;

                case State.EnteringSecond:
                    if (_secondNumberBuilder[^1] == DecimalPoint)
                        _secondHasDecimalPoint = false;
                    
                    _secondNumberBuilder.Remove(_secondNumberBuilder.Length - 1, 1);
                    if (_secondNumberBuilder.Length == 0)
                        _currentState = State.WaitingSecond;
                    
                    break;

                case State.ShowingResult:
                    return;
                
                default:
                    throw ExceptionFactory.CreateEnumException(_currentState, nameof(_currentState));
            }

            UpdateNumber();
        }

        private void ProcessClearCommand()
        {
            _first = null;
            _second = null;
            Result = null;
            Operation = MathOperation.None;
            _firstNumberBuilder.Clear();
            _secondNumberBuilder.Clear();
            _firstHasDecimalPoint = false;
            _secondHasDecimalPoint = false;
            FirstSign = 1;
            SecondSign = 1;
            _currentState = State.WaitingFirst;
        }

        private void ProcessCalculateCommand()
        {
            if (_currentState is not State.EnteringSecond)
                return;

            Debug.Assert(_first != null, nameof(_first) + " != null");
            Debug.Assert(_second != null, nameof(_second) + " != null");

            Result = Calculate(_first.Value * FirstSign, _second.Value * SecondSign, Operation);
            _currentState = State.ShowingResult;
        }

        private static Result Calculate(double first, double second, MathOperation operation)
        {
            return operation switch
            {
                MathOperation.Add => new Result {Value = first + second},
                MathOperation.Substract => new Result {Value = first - second},
                MathOperation.Multiply => new Result {Value = first * second},
                MathOperation.Divide => second != 0
                    ? new Result {Value = first / second}
                    : new Result {IsValid = false, Message = Result.DivisionByZero},
                MathOperation.None => throw new InvalidOperationException(),
                _ => throw ExceptionFactory.CreateEnumException(operation, nameof(operation))
            };
        }

        private void UpdateNumber()
        {
            switch (_currentState)
            {
                case State.EnteringFirst:
                    _first = double.Parse(_firstNumberBuilder.ToString(), NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture);
                    break;

                case State.EnteringSecond:
                    _second = double.Parse(_secondNumberBuilder.ToString(), NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture);
                    break;

                case State.WaitingFirst:
                    _first = null;
                    break;

                case State.WaitingSecond:
                    _second = null;
                    break;

                case State.ShowingResult:
                    return;
                
                default:
                    throw ExceptionFactory.CreateEnumException(_currentState, nameof(_currentState));
            }
        }

        private static MathOperation ParseOperation(char c)
        {
            return c switch
            {
                '+' => MathOperation.Add,
                '-' => MathOperation.Substract,
                '*' => MathOperation.Multiply,
                '/' => MathOperation.Divide,
                _ => throw new ArgumentException($"{c} cannot be parsed as math operation")
            };
        }
    }
}