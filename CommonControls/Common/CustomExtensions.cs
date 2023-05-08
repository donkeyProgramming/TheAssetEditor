using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CommonControls.Common
{
    public static class CustomExtensions
    {
        
        
        public sealed class LogicalChaining
        {

            private bool _condition;
            private bool _isValid = true;
            private LogicalChaining(bool condition)
            {
                _condition = condition;
            }

            public static LogicalChaining If(bool condition) 
                => new LogicalChaining(condition);
            
            public LogicalChaining Then(Action action)
            {
                if (_isValid && _condition){
                    action();
                }
                return this;
            }

            //Enable style: _ => doStuff()
            public LogicalChaining Then(Action<bool> action) 
            {
                if (_isValid && _condition){
                    action(true);
                }
                return this;
            }
            
            public void Else(Action action)
            {
                if (_isValid && !_condition){
                    action();
                    _isValid = false;
                }
            }
            
            //Enable style: _ => doStuff()
            public void Else(Action<bool> action)
            {
                if (_isValid && !_condition){
                    action(true);
                    _isValid = false;
                }
            }
            
            
            public LogicalChaining ElseIf(bool condition)
            {
                if (!_isValid){
                    return this;
                }
                
                if (_condition){
                    _isValid = false; // Turn off all the rest sequence
                }else{
                    _condition = condition;
                }
                return this;
            }

            private static void LogicalChainingTest()
            {
                {
                    int x = 2;
                    If(true).Then(_ => x *= 2).Else(_ => x /= 2);
                    Debug.Assert(x == 4);
                }
                {
                    int x = 2, y = 3;
                    If(false).Then(_ => x *= 2).Else(_ => x /= 2);
                    Debug.Assert(x == 1);
                }
                {
                    int x = 2, y = 3;
                    If(false).Else(_ => x /= 2);
                    Debug.Assert(x == 1);
                }
                {
                    int x = 2, y = 3;
                    If(true).ElseIf(true).Then(_ => x *= 2);
                    Debug.Assert(x == 2);
                }
                {
                    int x = 2, y = 3;
                    If(false).ElseIf(true).Then(_ => x *= 2);
                    Debug.Assert(x == 4);
                }
            
            }
        }
        
        public static LogicalChaining If(bool condition) => LogicalChaining.If(condition);
        
        public static void For(int start, int end, int step, Action<int> action)
        {
            if (step == 0){
                throw new ArgumentException($"For-loop: infinite loop step is 0");
            }
            if (start >= end){
                throw new ArgumentException($"For-loop: start({start}) >= end({end})");
            }
            if (Math.Sign(end - start) != Math.Sign(step)){
                throw new ArgumentException($"For-loop: infinite loop start({start}), end({end}), step({step})");
            }
            for (int i = start; i < end; i+=step){
                action(i);
            }
        }

        public static void For(int start, int end, Action<int> action) 
            => For(start, end, 1, action);
        public static void For(int end, Action<int> action) 
            => For(0, end, 1, action);
        
        public static void For(long start, long end, long step, Action<long> action)
        {
            if (step == 0){
                throw new ArgumentException($"For-loop: infinite loop step is 0");
            }
            if (start >= end){
                throw new ArgumentException($"For-loop: start({start}) >= end({end})");
            }
            if (Math.Sign(end - start) != Math.Sign(step)){
                throw new ArgumentException($"For-loop: infinite loop start({start}), end({end}), step({step})");
            }
            for (long i = start; i < end; i+=step){
                action(i);
            }
        }

        public static void For(long start, long end, Action<long> action) 
            => For(start, end, 1, action);
        public static void For(long end, Action<long> action) 
            => For(0, end, 1, action);
        
        
        public static void For(uint start, uint end, uint step, Action<uint> action)
        {
            if (step == 0){
                throw new ArgumentException($"For-loop: infinite loop step is 0");
            }
            if (start >= end){
                throw new ArgumentException($"For-loop: start({start}) >= end({end})");
            }
            for (uint i = start; i < end; i+=step){
                action(i);
            }
        }
        
        public static void For(uint start, uint end, Action<uint> action) 
            => For(start, end, 1, action);
        public static void For(uint end, Action<uint> action) 
            => For(0, end, 1, action);

        public static void ForEach<T>(this List<T> list, Action<T, int> action)
        {
            for (int i = 0; i < list.Count; i++)
            {
                action(list[i], i);
            }
        }
        
        public static void ForEachReverse<T>(this List<T> list, Action<T> action)
        {
            for (int i = list.Count - 1; i >= 0 ; i--)
            {
                action(list[i]);
            }
        }
        
        public static void ForEachReverse<T>(this List<T> list, Action<T, int> action)
        {
            for (int i = list.Count - 1; i >= 0 ; i--)
            {
                action(list[i], i);
            }
        }
    }
}