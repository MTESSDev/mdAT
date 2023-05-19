// Copyright (c) 2007, Clarius Consulting, Manas Technology Solutions, InSTEDD, and Contributors.
// All rights reserved. Licensed under the BSD 3-Clause License; see License.txt.

using System;
using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;
using Moq;
using Moq.Language;
using Moq.Language.Flow;
using Moq.Properties;

namespace MDAT
{
    /// <summary>
    /// Defines async extension methods on IReturns.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ReturnsExtensions
    {
        public static object ReturnsOrThrows<TMock, TResult>(
                        this ISetup<TMock, TResult> setup,
                        ObjectOrException<TResult> objectOrException)
            where TMock : class
        {
            if (objectOrException.Exception is { })
            {
                var exceptionReturn = ResolveException(objectOrException.Exception);

                return setup.Throws(exceptionReturn);
            }

            return setup.Returns(objectOrException.Value!);
        }

        public static object ReturnsOrThrowsAsync<TMock, TResult>(
                this ISetup<TMock, Task<TResult>> setup,
                ObjectOrException<TResult> objectOrException)
                where TMock : class
        {
            if (objectOrException.Exception is { })
            {
                var exceptionReturn = ResolveException(objectOrException.Exception);

                return setup.Throws(exceptionReturn);
            }

            return setup.ReturnsAsync(objectOrException.Value!);
        }

        private static Exception ResolveException(TestException testException) =>
         testException.Name switch
         {
             "Exception" => new Exception(message: testException.Message),
             "AccessViolationException" => new AccessViolationException(message: testException.Message),
             "AggregateException" => new AggregateException(message: testException.Message),
             "AppDomainUnloadedException" => new AppDomainUnloadedException(message: testException.Message),
             "ApplicationException" => new ApplicationException(message: testException.Message),
             "ArgumentException" => new ArgumentException(message: testException.Message),
             "ArgumentNullException" => new ArgumentNullException(testException.Message),
             "ArgumentOutOfRangeException" => new ArgumentOutOfRangeException(paramName: testException.ParamName, message: testException.Message),
             "ArithmeticException" => new ArithmeticException(message: testException.Message),
             "ArrayTypeMismatchException" => new ArrayTypeMismatchException(message: testException.Message),
             "BadImageFormatException" => new BadImageFormatException(message: testException.Message),
             "CannotUnloadAppDomainException" => new CannotUnloadAppDomainException(message: testException.Message),
             "ContextMarshalException" => new ContextMarshalException(message: testException.Message),
             "DataMisalignedException" => new DataMisalignedException(message: testException.Message),
             "DivideByZeroException" => new DivideByZeroException(message: testException.Message),
             "DllNotFoundException" => new DllNotFoundException(message: testException.Message),
             "DuplicateWaitObjectException" => new DuplicateWaitObjectException(testException.Message),
             "EntryPointNotFoundException" => new EntryPointNotFoundException(message: testException.Message),
             "ExecutionEngineException" => new ExecutionEngineException(message: testException.Message),
             "FieldAccessException" => new FieldAccessException(message: testException.Message),
             "FormatException" => new FormatException(message: testException.Message),
             "IndexOutOfRangeException" => new IndexOutOfRangeException(message: testException.Message),
             "InsufficientMemoryException" => new InsufficientMemoryException(message: testException.Message),
             "InvalidCastException" => new InvalidCastException(message: testException.Message),
             "InvalidOperationException" => new InvalidOperationException(message: testException.Message),
             "InvalidProgramException" => new InvalidProgramException(message: testException.Message),
             "InvalidTimeZoneException" => new InvalidTimeZoneException(message: testException.Message),
             "MemberAccessException" => new MemberAccessException(message: testException.Message),
             "MethodAccessException" => new MethodAccessException(message: testException.Message),
             "MissingFieldException" => new MissingFieldException(message: testException.Message),
             "MissingMemberException" => new MissingMemberException(message: testException.Message),
             "MissingMethodException" => new MissingMethodException(message: testException.Message),
             "MulticastNotSupportedException" => new MulticastNotSupportedException(message: testException.Message),
             //"NotCancelableException" => new NotCancelableException(),
             "NotFiniteNumberException" => new NotFiniteNumberException(message: testException.Message),
             "NotImplementedException" => new NotImplementedException(message: testException.Message),
             "NotSupportedException" => new NotSupportedException(message: testException.Message),
             "NullReferenceException" => new NullReferenceException(message: testException.Message),
             "ObjectDisposedException" => new ObjectDisposedException(testException.ObjectName, message: testException.Message),
             "OperationCanceledException" => new OperationCanceledException(message: testException.Message),
             "OutOfMemoryException" => new OutOfMemoryException(message: testException.Message),
             "OverflowException" => new OverflowException(message: testException.Message),
             "PlatformNotSupportedException" => new PlatformNotSupportedException(message: testException.Message),
             "RankException" => new RankException(message: testException.Message),
             "StackOverflowException" => new StackOverflowException(message: testException.Message),
             "SystemException" => new SystemException(message: testException.Message),
             "TimeoutException" => new TimeoutException(message: testException.Message),
             "TimeZoneNotFoundException" => new TimeZoneNotFoundException(message: testException.Message),
             "TypeAccessException" => new TypeAccessException(message: testException.Message),
             //"TypeInitializationException" => new TypeInitializationException(),
             "TypeLoadException" => new TypeLoadException(message: testException.Message),
             "TypeUnloadedException" => new TypeUnloadedException(message: testException.Message),
             "UnauthorizedAccessException" => new UnauthorizedAccessException(message: testException.Message),
             "UriFormatException" => new UriFormatException(),
             "ConstraintException" => new ConstraintException(),
             "DataException" => new DataException(),
             "DBConcurrencyException" => new DBConcurrencyException(message: testException.Message),
             "DeleteRowInaccessibleException" => new DeletedRowInaccessibleException(),
             "DuplicateNameException" => new DuplicateNameException(),
             "EvaluateException" => new EvaluateException(),
             "InRowChangingEventException" => new InRowChangingEventException(),
             "InvalidConstraintException" => new InvalidConstraintException(),
             "InvalidExpressionException" => new InvalidExpressionException(),
             "MissingPrimaryKeyException" => new MissingPrimaryKeyException(),
             "NoNullAllowedException" => new NoNullAllowedException(),
             //"OperationAbortedException" => new OperationAbortedException(),
             "ReadOnlyException" => new ReadOnlyException(),
             "RowNotInTableException" => new RowNotInTableException(),
             "StrongTypingException" => new StrongTypingException(message: testException.Message),
             "SyntaxErrorException" => new SyntaxErrorException(),
             //"TypedDataSetGeneratorException" => new TypedDataSetGeneratorException(),
             "VersionNotFoundException" => new VersionNotFoundException(),
             "DirectoryNotFoundException" => new DirectoryNotFoundException(message: testException.Message),
             "DriveNotFoundException" => new DriveNotFoundException(message: testException.Message),
             "EndOfStreamException" => new EndOfStreamException(message: testException.Message),
             //"FileFormatException" => new FileFormatException(),
             "FileLoadException" => new FileLoadException(message: testException.Message),
             "FileNotFoundException" => new FileNotFoundException(message: testException.Message),
             "InternalBufferOverflowException" => new InternalBufferOverflowException(message: testException.Message),
             "InvalidDataException" => new InvalidDataException(message: testException.Message),
             "IOException" => new IOException(message: testException.Message),
             "PathTooLongException" => new PathTooLongException(message: testException.Message),
             _ => throw new NotImplementedException("Cannot find your Exception name in list."),
             //"PipeException" => new PipeException(),
         };
    }
}