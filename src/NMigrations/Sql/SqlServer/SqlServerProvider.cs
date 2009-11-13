﻿/*
 *  Licensed to the Apache Software Foundation (ASF) under one or more
 *  contributor license agreements.  See the NOTICE file distributed with
 *  this work for additional information regarding copyright ownership.
 *  The ASF licenses this file to You under the Apache License, Version 2.0
 *  (the "License"); you may not use this file except in compliance with
 *  the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *
 */

using System;
using System.Data.Common;
using System.Collections.Generic;
using System.Linq;

namespace NMigrations.Sql.SqlServer
{
    /// <summary>
    /// The SQL provider class for Microsoft SQL Server.
    /// </summary>
    [SqlProvider("System.Data.SqlClient")]
    [SqlProvider("Microsoft.SqlServerCe.Client")]
    public class SqlServerProvider : GenericSqlProvider
    {
        #region ISqlProvider Members

        /// <summary>
        /// Gets the SQL command that separates multiple SQL queries in one SQL script file of each other.
        /// </summary>
        /// <returns>The separator.</returns>
        public override string GetQuerySeparator()
        {
            return "GO";
        }

        #endregion

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerProvider"/> class.
        /// </summary>
        public SqlServerProvider()
        {

        }

        #endregion

        #region Protected Methods

        #region Data Types

        /// <summary>
        /// Builds the <see cref="String"/> that represents the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="length">The length.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="precision">The precision.</param>
        /// <returns>The data type.</returns>
        protected override string BuildDataType(SqlTypes type, int? length, int? scale, int? precision)
        {
            switch (type)
            {
                case SqlTypes.Guid:
                    return "UNIQUEIDENTIFIER";
                case SqlTypes.TinyInt:
                    return "TINYINT";
                case SqlTypes.SmallInt:
                    return "SMALLINT";
                case SqlTypes.Int:
                    return "INT";
                case SqlTypes.BigInt:
                    return "BIGINT";
                case SqlTypes.Single:
                    return "FLOAT";
                case SqlTypes.Double:
                    return "REAL";
                case SqlTypes.Decimal:
                    if (scale != null && precision != null)
                        return string.Format("DECIMAL({0}, {1})", scale, precision);
                    else
                        return "DECIMAL";
                case SqlTypes.Currency:
                    if (scale != null && precision != null)
                        return string.Format("MONEY({0}, {1})", scale, precision);
                    else
                        return "MONEY";
                case SqlTypes.Boolean:
                    return "BIT";
                case SqlTypes.Char:
                    if (length != null)
                        return string.Format("CHAR({0})", length);
                    else
                        return "CHAR";
                case SqlTypes.VarChar:
                    if (length != null)
                        return string.Format("VARCHAR({0})", length);
                    else
                        return "VARCHAR";
                case SqlTypes.VarCharMax:
                    return "VARCHAR(MAX)";
                case SqlTypes.NChar:
                    if (length != null)
                        return string.Format("NCHAR({0})", length);
                    else
                        return "NCHAR";
                case SqlTypes.NVarChar:
                    if (length != null)
                        return string.Format("NVARCHAR({0})", length);
                    else
                        return "NVARCHAR";
                case SqlTypes.NVarCharMax:
                    return "NVARCHAR(MAX)";
                case SqlTypes.Text:
                    return "TEXT";
                case SqlTypes.NText:
                    return "NTEXT";
                case SqlTypes.Xml:
                    return "XML";
                case SqlTypes.Date:
                    return "DATE";
                case SqlTypes.Time:
                    return "TIME";
                case SqlTypes.DateTime:
                    return "DATETIME";
                case SqlTypes.TimeStamp:
                    return "TIMESTAMP";
                case SqlTypes.TimeSpan:
                    return "DATETIMEOFFSET";
            }

            return null;
        }

        #endregion

        #region Misc

        /// <summary>
        /// Builds the SQL fragment that describes an auto-increment column.
        /// </summary>
        /// <param name="seed">The initial value.</param>
        /// <param name="step">The increment step.</param>
        /// <returns>The SQL fragment.</returns>
        protected override string BuildAutoIncrement(int? seed, int? step)
        {
            return string.Format("IDENTITY({0}, {1})", seed ?? 1, step ?? 1);
        }


        #endregion

        #region Escaping

        /// <summary>
        /// Escapes the specified <paramref name="tableName"/>
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns>The escaped table name.</returns>
        protected override string EscapeTableName(string tableName)
        {
            return "[" + tableName + "]";
        }

        /// <summary>
        /// Escapes the specified <paramref name="columnName"/>.
        /// </summary>
        /// <param name="columnName">Name of the column.</param>
        /// <returns>The escaped column name.</returns>
        protected override string EscapeColumnName(string columnName)
        {
            return "[" + columnName + "]";
        }

        /// <summary>
        /// Escapes the specified <paramref name="constraintName"/>.
        /// </summary>
        /// <param name="constraintName">Name of the constraint.</param>
        /// <returns>The escaped constraint name.</returns>
        protected override string EscapeConstraintName(string constraintName)
        {
            return "[" + constraintName + "]";
        }

        #endregion

        #endregion
    }
}