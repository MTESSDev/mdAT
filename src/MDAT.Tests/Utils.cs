using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MDATTests
{
    public static class Utils
    {
        public static int Calculer(int val1, int val2)
        {
            return val1 + val2;
        }

        public static int CrashExceptionCheck(int val1, int val2)
        {
            throw new ArgumentOutOfRangeException("valx", "out of range!");    
        }

        public static MethodInfo GetMethodInfo(LambdaExpression expression)
        {
            var outermostExpression = expression.Body as MethodCallExpression;

            if (outermostExpression is null)
            {
                if (expression.Body is UnaryExpression ue && ue.Operand is MethodCallExpression me && me.Object is System.Linq.Expressions.ConstantExpression ce && ce.Value is MethodInfo mi)
                    return mi;
                throw new ArgumentException("Invalid Expression. Expression should consist of a Method call only.");
            }

            var method = outermostExpression.Method;
            if (method is null)
                throw new Exception($"Cannot find method for expression {expression}");

            return method;
        }
    }
}
