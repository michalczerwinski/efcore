// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Microsoft.EntityFrameworkCore.SqlServer.Query.Internal
{
    /// <summary>
    ///     TODO
    /// </summary>
    public class SqlServerStringFormatTranslator : IMethodCallTranslator
    {
        private readonly ISqlExpressionFactory _sqlExpressionFactory;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public SqlServerStringFormatTranslator(ISqlExpressionFactory sqlExpressionFactory)
        {
            _sqlExpressionFactory = sqlExpressionFactory;
        }

        /// <summary>
        ///     TODO
        /// </summary>
        public SqlExpression? Translate(SqlExpression? instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            if (instance == null
                && method.Name == nameof(string.Format)
                && arguments.Count > 0
                && arguments[0] is SqlConstantExpression formatExpression
                && formatExpression.Type == typeof(string)
                && formatExpression.Value != null)
            {
                var concatArguments = CalculateConcatArguments((string)formatExpression.Value, arguments.Skip(1)).ToArray();

                return _sqlExpressionFactory.Function(
                        "CONCAT",
                        concatArguments,
                        true,
                        Enumerable.Repeat(true, concatArguments.Length),
                        typeof(string)
                    ); ;
            }

            return null;
        }

        private IEnumerable<SqlExpression> CalculateConcatArguments(string formatString, IEnumerable<SqlExpression> arguments)
        {
            var argumentArray = arguments.ToArray();

            var stringBuilder = new StringBuilder(formatString.Length);
            var parts = stringBuilder.AppendFormat(formatString, Enumerable.Repeat(char.MinValue, argumentArray.Length).ToArray())
                .ToString()
                .Split(char.MinValue);

            var argumentIndex = 0;

            foreach (var part in parts)
            {
                if (part != null)
                {
                    yield return new SqlConstantExpression(ConstantExpression.Constant(part), null);
                }

                if (argumentIndex < argumentArray.Length)
                {
                    yield return argumentArray[argumentIndex++];
                }
            }
        }
    }
}
