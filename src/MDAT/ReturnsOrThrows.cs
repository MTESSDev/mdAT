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

        public static object ReturnsOrThrowsAsync<TMock, TResult>(
                    this IReturnsThrows<TMock, Task<TResult>> setup,
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
         testException.ClassName switch
         {
             "System.Exception" => new Exception(message: testException.Message),
             "System.AccessViolationException" => new AccessViolationException(message: testException.Message),
             "System.AggregateException" => new AggregateException(message: testException.Message),
             "System.AppDomainUnloadedException" => new AppDomainUnloadedException(message: testException.Message),
             "System.ApplicationException" => new ApplicationException(message: testException.Message),
             "System.ArgumentException" => new ArgumentException(message: testException.Message),
             "System.ArgumentNullException" => new ArgumentNullException(testException.Message),
             "System.ArgumentOutOfRangeException" => new ArgumentOutOfRangeException(paramName: testException.ParamName, message: testException.Message),
             "System.ArithmeticException" => new ArithmeticException(message: testException.Message),
             "System.ArrayTypeMismatchException" => new ArrayTypeMismatchException(message: testException.Message),
             "System.BadImageFormatException" => new BadImageFormatException(message: testException.Message),
             "System.CannotUnloadAppDomainException" => new CannotUnloadAppDomainException(message: testException.Message),
             "System.ContextMarshalException" => new ContextMarshalException(message: testException.Message),
             "System.DataMisalignedException" => new DataMisalignedException(message: testException.Message),
             "System.DivideByZeroException" => new DivideByZeroException(message: testException.Message),
             "System.DllNotFoundException" => new DllNotFoundException(message: testException.Message),
             "System.DuplicateWaitObjectException" => new DuplicateWaitObjectException(testException.Message),
             "System.EntryPointNotFoundException" => new EntryPointNotFoundException(message: testException.Message),
             "System.ExecutionEngineException" => new ExecutionEngineException(message: testException.Message),
             "System.FieldAccessException" => new FieldAccessException(message: testException.Message),
             "System.FormatException" => new FormatException(message: testException.Message),
             "System.IndexOutOfRangeException" => new IndexOutOfRangeException(message: testException.Message),
             "System.InsufficientMemoryException" => new InsufficientMemoryException(message: testException.Message),
             "System.InvalidCastException" => new InvalidCastException(message: testException.Message),
             "System.InvalidOperationException" => new InvalidOperationException(message: testException.Message),
             "System.InvalidProgramException" => new InvalidProgramException(message: testException.Message),
             "System.InvalidTimeZoneException" => new InvalidTimeZoneException(message: testException.Message),
             "System.MemberAccessException" => new MemberAccessException(message: testException.Message),
             "System.MethodAccessException" => new MethodAccessException(message: testException.Message),
             "System.MissingFieldException" => new MissingFieldException(message: testException.Message),
             "System.MissingMemberException" => new MissingMemberException(message: testException.Message),
             "System.MissingMethodException" => new MissingMethodException(message: testException.Message),
             "System.MulticastNotSupportedException" => new MulticastNotSupportedException(message: testException.Message),
             //"NotCancelableException" => new NotCancelableException(),
             "System.NotFiniteNumberException" => new NotFiniteNumberException(message: testException.Message),
             "System.NotImplementedException" => new NotImplementedException(message: testException.Message),
             "System.NotSupportedException" => new NotSupportedException(message: testException.Message),
             "System.NullReferenceException" => new NullReferenceException(message: testException.Message),
             "System.ObjectDisposedException" => new ObjectDisposedException(testException.ObjectName, message: testException.Message),
             "System.OperationCanceledException" => new OperationCanceledException(message: testException.Message),
             "System.OutOfMemoryException" => new OutOfMemoryException(message: testException.Message),
             "System.OverflowException" => new OverflowException(message: testException.Message),
             "System.PlatformNotSupportedException" => new PlatformNotSupportedException(message: testException.Message),
             "System.RankException" => new RankException(message: testException.Message),
             "System.StackOverflowException" => new StackOverflowException(message: testException.Message),
             "System.SystemException" => new SystemException(message: testException.Message),
             "System.TimeoutException" => new TimeoutException(message: testException.Message),
             "System.TimeZoneNotFoundException" => new TimeZoneNotFoundException(message: testException.Message),
             "System.TypeAccessException" => new TypeAccessException(message: testException.Message),
             //"TypeInitializationException" => new TypeInitializationException(),
             "System.TypeLoadException" => new TypeLoadException(message: testException.Message),
             "System.TypeUnloadedException" => new TypeUnloadedException(message: testException.Message),
             "System.UnauthorizedAccessException" => new UnauthorizedAccessException(message: testException.Message),
             "System.UriFormatException" => new UriFormatException(),
             "System.Data.ConstraintException" => new ConstraintException(),
             "System.Data.DataException" => new DataException(),
             "System.Data.DBConcurrencyException" => new DBConcurrencyException(message: testException.Message),
             "System.Data.DeleteRowInaccessibleException" => new DeletedRowInaccessibleException(),
             "System.Data.DuplicateNameException" => new DuplicateNameException(),
             "System.Data.EvaluateException" => new EvaluateException(),
             "System.Data.InRowChangingEventException" => new InRowChangingEventException(),
             "System.Data.InvalidConstraintException" => new InvalidConstraintException(),
             "System.Data.InvalidExpressionException" => new InvalidExpressionException(),
             "System.Data.MissingPrimaryKeyException" => new MissingPrimaryKeyException(),
             "System.Data.NoNullAllowedException" => new NoNullAllowedException(),
             //"OperationAbortedException" => new OperationAbortedException(),
             "System.Data.ReadOnlyException" => new ReadOnlyException(),
             "System.Data.RowNotInTableException" => new RowNotInTableException(),
             "System.Data.StrongTypingException" => new StrongTypingException(message: testException.Message),
             "System.Data.SyntaxErrorException" => new SyntaxErrorException(),
             //"TypedDataSetGeneratorException" => new TypedDataSetGeneratorException(),
             "System.Data.VersionNotFoundException" => new VersionNotFoundException(),
             "System.IO.DirectoryNotFoundException" => new DirectoryNotFoundException(message: testException.Message),
             "System.IO.DriveNotFoundException" => new DriveNotFoundException(message: testException.Message),
             "System.IO.EndOfStreamException" => new EndOfStreamException(message: testException.Message),
             //"FileFormatException" => new FileFormatException(),
             "System.IO.FileLoadException" => new FileLoadException(message: testException.Message),
             "System.IO.FileNotFoundException" => new FileNotFoundException(message: testException.Message),
             "System.IO.InternalBufferOverflowException" => new InternalBufferOverflowException(message: testException.Message),
             "System.IO.InvalidDataException" => new InvalidDataException(message: testException.Message),
             "System.IO.IOException" => new IOException(message: testException.Message),
             "System.IO.PathTooLongException" => new PathTooLongException(message: testException.Message),
             _ => throw new NotImplementedException($"Cannot find your Exception name '{testException.ClassName}' in list."),
             //"PipeException" => new PipeException(),
         };
    }
}