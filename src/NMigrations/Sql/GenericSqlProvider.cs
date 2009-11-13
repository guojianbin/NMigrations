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
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

using NMigrations.Core;

namespace NMigrations.Sql
{
    /// <summary>
    /// A general SQL generator implementation that implements the
    /// aspects that are common to all SQL dialects. Differences in
    /// SQL dialects are applied by subclassing <see cref="GenericSqlProvider"/>
    /// and overriding the specific methods.
    /// </summary>
    public abstract class GenericSqlProvider : ISqlProvider
    {
        #region ISqlProvider Members

        /// <summary>
        /// Generates the SQL commands for the specified <paramref name="database"/>.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <returns>An enumeration of SQL commands.</returns>
        public virtual IEnumerable<string> GenerateSqlCommands(Database database)
        {
            while (database.MigrationSteps.Count > 0)
            {
                //
                // Take next element from queue
                //
                IEnumerable<string> commands = null;
                var element = database.MigrationSteps.Dequeue();
                
                //
                // Build SQL statements
                //
                if (element is Table)
                {
                    if (element.Modifier == Modifier.Add)
                        commands = CreateTable(element as Table);
                    else if (element.Modifier == Modifier.Drop)
                        commands = DropTable(element as Table);
                    else if (element.Modifier == Modifier.Alter)
                        commands = AlterTable(element as Table);
                }
                else if (element is Index)
                {
                    if (element.Modifier == Modifier.Add)
                        commands = CreateIndex(element as Index);
                    else if (element.Modifier == Modifier.Drop)
                        commands = DropIndex(element as Index);
                    // Alter is not supported
                }
                else if (element is Insert)
                {
                    if (element.Modifier == Modifier.Add)
                        commands = Insert(element as Insert);
                    // Alter + Drop are not supported
                }
                else if (element is ForeignKeyConstraint)
                {
                    if (element.Modifier == Modifier.Add)
                        commands = AddForeignKeyConstraint(element as ForeignKeyConstraint);
                    else if (element.Modifier == Modifier.Drop)
                        commands = DropForeignKeyConstraint(element as ForeignKeyConstraint);
                    // Alter is not supported
                }
                else if (element is UniqueConstraint)
                {
                    if (element.Modifier == Modifier.Add)
                        commands = AddUniqueConstraint(element as UniqueConstraint);
                    else if (element.Modifier == Modifier.Drop)
                        commands = DropUniqueConstraint(element as UniqueConstraint);
                    // Alter is not supported
                }
                else if (element is PrimaryKeyConstraint)
                {
                    if (element.Modifier == Modifier.Add)
                        commands = AddPrimaryKeyConstraint(element as PrimaryKeyConstraint);
                    else if (element.Modifier == Modifier.Drop)
                        commands = DropPrimaryKeyConstraint(element as PrimaryKeyConstraint);
                    // Alter is not supported
                }
                else if (element is SqlStatement)
                {
                    if (element.Modifier == Modifier.Add)
                        commands = ExecuteSqlStatement(element as SqlStatement);
                    // Alter + Drop are not supported
                }

                //
                // Return statements
                //
                if (commands != null)
                {
                    foreach (string sql in commands)
                    {
                        yield return sql;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the SQL command that separates multiple SQL queries in one SQL script file of each other.
        /// </summary>
        /// <returns>The separator.</returns>
        public abstract string GetQuerySeparator();

        /// <summary>
        /// Builds the name for a primary key constraint.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="columnNames">The column names.</param>
        /// <returns>The primary key name.</returns>
        public string GetPrimaryKeyConstraintName(string tableName, string[] columnNames)
        {
            return "PK_" + tableName;
        }

        /// <summary>
        /// Builds the name for a foreign key constraint.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="columnNames">The column names.</param>
        /// <param name="relatedTableName">Name of the related table.</param>
        /// <param name="relatedColumnNames">The related column names.</param>
        /// <returns>The foreign key name.</returns>
        public string GetForeignKeyConstraintName(string tableName, string[] columnNames, string relatedTableName, string[] relatedColumnNames)
        {
            return "FK_" + tableName + "_" + relatedTableName;
        }

        /// <summary>
        /// Builds the name for a unique constraint.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="columnNames">The column names.</param>
        /// <returns>The unqiue constraint name.</returns>
        public string GetUniqueConstraintName(string tableName, string[] columnNames)
        {
            return "UQ_" + tableName + string.Join("", columnNames);
        }

        /// <summary>
        /// Builds the name for an index.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="columnNames">The column names.</param>
        /// <returns>The index name.</returns>
        public string GetIndexName(string tableName, string[] columnNames)
        {
            return "IX_" + tableName + "_" + string.Join("", columnNames);
        }

        #endregion

        #region Proected Methods

        #region Table Operations

        #region Create Table

        /// <summary>
        /// Enumerates the SQL commands that are necessary to create
        /// the specified <paramref name="table"/>.
        /// </summary>
        /// <param name="table">The table to create.</param>
        /// <returns>The SQL commands.</returns>
        protected virtual IEnumerable<string> CreateTable(Table table)
        {
            //
            // Build SQL fragments for columns
            //
            var columnFragments = new List<string>();
            table.Columns.ForEach(c => columnFragments.Add(BuildCreateTableColumn(c)));

            //
            // Build SQL fragement for constraints
            //
            var constraints = BuildCreateTableConstraints(table).ToArray();

            //
            // Pretty print columns
            //
            // Indent by one tabulator and a ',' + 'new line' after each column
            //
            for (int i = 0; i < columnFragments.Count; i++)
            {
                columnFragments[i] = "\t" + columnFragments[i];
                if (i != columnFragments.Count - 1 || constraints.Length > 0)
                {
                    columnFragments[i] += ",";
                }
                columnFragments[i] += Environment.NewLine;
            }

            //
            // Add constraint fragments
            //
            for (int i = 0; i < constraints.Length; i++)
            {
                string s = "\t" + constraints[i];
                if (i != constraints.Length - 1)
                {
                    s += ",";
                }
                s += Environment.NewLine;
                columnFragments.Add(s);
            }

            //
            // Build final command
            //
            yield return string.Format(
                "CREATE TABLE {0} (" + Environment.NewLine + "{1});",
                EscapeTableName(table.Name),
                string.Join(string.Empty, columnFragments.ToArray())
            );
        }

        /// <summary>
        /// Builds the SQL fragment that describes a column in a
        /// CREATE TABLE statement.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <returns>The SQL fragment.</returns>
        protected virtual string BuildCreateTableColumn(Column column)
        {
            //
            // Build basic elements (name + type)
            //
            string name = EscapeColumnName(column.Name);
            string type = BuildDataType(column.DataType.Value, column.Length, column.Scale, column.Precision);

            //
            // NULL constraint
            //
            string nullConstraint = (column.IsNullable ? "NULL" : "NOT NULL");

            //
            // Auto Increment
            //
            string autoIncrement = null;
            if (column.IsAutoIncrement)
            {
                autoIncrement = BuildAutoIncrement(1, 1);
            }

            //
            // Default
            //
            string defaultValue = null;
            if (column.DefaultValue != null)
            {
                defaultValue = "DEFAULT " + FormatValue(column.DefaultValue);
            }

            //
            // Build up everything
            //
            string sql = string.Format("{0} {1} {2}", name, type, nullConstraint);
            
            if (autoIncrement != null)
                sql += " " + autoIncrement;
            if (defaultValue != null)
                sql += " " + defaultValue;

            return sql;
        }

        /// <summary>
        /// Builds the SQL fragments that follow after the column list in the
        /// CREATE TABLE statement (like constraints or indices).
        /// </summary>
        /// <param name="table">The table.</param>
        /// <returns>The SQL fragments.</returns>
        protected virtual IEnumerable<string> BuildCreateTableConstraints(Table table)
        {
            //
            // Primary key
            //
            if (table.HasPrimaryKey())
            {
                var pk = table.GetPrimaryKeyConstraint();
                string sql = BuildCreateTablePrimaryKeyConstraint(pk);
                table.Database.MigrationSteps.Remove(pk);
                yield return sql;
            }

            //
            // Foreign keys
            //
            var fks = table.GetForeignKeyConstraints().ToArray();
            foreach (var fk in fks)
            {
                string sql = BuildCreateTableForeignKeyConstraint(fk);
                table.Database.MigrationSteps.Remove(fk);
                yield return sql;
            }

            //
            // Unique constraints
            //
            var uniques = table.GetUniqueConstraints().ToArray();
            foreach (var unique in uniques)
            {
                string sql = BuildCreateTableUniqueConstraint(unique);
                table.Database.MigrationSteps.Remove(unique);
                yield return sql;
            }
        }

        /// <summary>
        /// Builds the SQL fragment that describes a primary key constraint
        /// within a CREATE TABLE statement.
        /// </summary>
        /// <param name="pk">The primary key constraint.</param>
        /// <returns>The SQL fragment.</returns>
        protected virtual string BuildCreateTablePrimaryKeyConstraint(PrimaryKeyConstraint pk)
        {
            if (pk.Name != null)
            {
                return string.Format("CONSTRAINT {0} PRIMARY KEY ({1})",
                    EscapeConstraintName(pk.Name),
                    string.Join(", ", EscapeColumnNames(pk.ColumnNames))
                );
            }
            else
            {
                return string.Format("PRIMARY KEY ({0})",
                    string.Join(", ", EscapeColumnNames(pk.ColumnNames))
                );
            }
        }

        /// <summary>
        /// Builds the SQL fragment that describes a foreign key constraint
        /// within a CREATE TABLE statement.
        /// </summary>
        /// <param name="fk">The foreign key constraint.</param>
        /// <returns>The SQL fragment.</returns>
        protected virtual string BuildCreateTableForeignKeyConstraint(ForeignKeyConstraint fk)
        {
            if (fk.Name != null)
            {
                return string.Format("CONSTRAINT {0} FOREIGN KEY ({1}) REFERENCES {2} ({3})",
                    EscapeConstraintName(fk.Name),
                    string.Join(", ", EscapeColumnNames(fk.ColumnNames)),
                    EscapeTableName(fk.RelatedTableName),
                    string.Join(", ", EscapeColumnNames(fk.RelatedColumnNames))
                );
            }
            else
            {
                return string.Format("FOREIGN KEY ({0}) REFERENCES {1} ({2})",
                    string.Join(", ", EscapeColumnNames(fk.ColumnNames)),
                    EscapeTableName(fk.RelatedTableName),
                    string.Join(", ", EscapeColumnNames(fk.RelatedColumnNames))
                );
            }
        }

        /// <summary>
        /// Builds the SQL fragment that describes a unqiue constraint
        /// within a CREATE TABLE statement.
        /// </summary>
        /// <param name="uniqueConstraint">The unique constraint.</param>
        /// <returns>The SQL fragment.</returns>
        protected virtual string BuildCreateTableUniqueConstraint(UniqueConstraint uniqueConstraint)
        {
            if (uniqueConstraint.Name != null)
            {
                return string.Format("CONSTRAINT {0} UNIQUE ({1})",
                    EscapeConstraintName(uniqueConstraint.Name),
                    string.Join(", ", EscapeColumnNames(uniqueConstraint.ColumnNames))
                );
            }
            else
            {
                return string.Format("UNIQUE ({0})",
                    string.Join(", ", EscapeColumnNames(uniqueConstraint.ColumnNames))
                );
            }
        }

        #endregion

        #region Drop Table

        /// <summary>
        /// Enumerates the SQL commands that are necessary to drop
        /// the specified <paramref name="table"/>.
        /// </summary>
        /// <param name="table">The table to drop.</param>
        /// <returns>The SQL commands.</returns>
        protected virtual IEnumerable<string> DropTable(Table table)
        {
            yield return string.Format("DROP TABLE {0};", EscapeTableName(table.Name));
        }

        #endregion

        #region Alter Table

        /// <summary>
        /// Enumerates the SQL commands that are necessary to alter
        /// the specified <paramref name="table"/>.
        /// </summary>
        /// <param name="table">The table to alter.</param>
        /// <returns>The SQL commands.</returns>
        protected virtual IEnumerable<string> AlterTable(Table table)
        {
            //
            // Build SQL fragments for columns
            //
            var columnFragments = new List<string>();
            table.Columns.ForEach(c => columnFragments.Add(BuildAlterTableColumn(c)));

            //
            // Build final commands
            //
            foreach (string column in columnFragments)
            {
                yield return string.Format(
                    "ALTER TABLE {0} {1};", EscapeTableName(table.Name), column
                );
            }
        }

        /// <summary>
        /// Builds the SQL fragment that describes a column in an
        /// ALTER TABLE statement.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <returns>The SQL fragment.</returns>
        protected virtual string BuildAlterTableColumn(Column column)
        {
            if (column.Modifier == Modifier.Add)
            {
                return "ADD " + BuildCreateTableColumn(column);
            }
            else if (column.Modifier == Modifier.Drop)
            {
                return "DROP COLUMN " + EscapeColumnName(column.Name);
            }
            else
            {
                if (!string.IsNullOrEmpty(column.NewName))
                {
                    //
                    // Rename and new data type
                    //
                    if (column.DataType != null)
                    {
                        // Create Statement with new column name
                        string create = BuildCreateTableColumn(column);
                        create = create.Replace(EscapeColumnName(column.Name), EscapeColumnName(column.NewName));

                        return "CHANGE " + EscapeColumnName(column.Name) + " " + create;
                    }
                    //
                    // Rename
                    //
                    else
                    {
                        return "ALTER " + EscapeColumnName(column.Name) + " " + EscapeColumnName(column.NewName);
                    }
                }
                //
                // New data type
                //
                else
                {
                    return "MODIFY " + BuildCreateTableColumn(column);
                }
            }
        }

        #endregion

        #endregion

        #region Insert

        /// <summary>
        /// Generates the SQL statements that inserts the row described by
        /// the specified <paramref name="insert"/> object into the database.
        /// </summary>
        /// <param name="insert">The insert containing the row to insert.</param>
        /// <returns>The SQL statements.</returns>
        protected virtual IEnumerable<string> Insert(Insert insert)
        {
            //
            // Stringify column names and values
            //
            string[] columnNames = new string[insert.Row.Count];
            string[] columnValues = new string[insert.Row.Count];

            int i = 0;
            foreach (var key in insert.Row.Keys)
            {
                columnNames[i] = EscapeColumnName(key);
                columnValues[i++] = FormatValue(insert.Row[key]);
            }

            //
            // Build up command
            //
            yield return string.Format("INSERT INTO {0} ({1}) VALUES({2});",
                EscapeTableName(insert.Table.Name),
                string.Join(", ", columnNames), string.Join(", ", columnValues)
            );
        }

        #endregion

        #region Indices

        /// <summary>
        /// Enumerates the SQL commands that are necessary to create
        /// the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index to create.</param>
        /// <returns>The SQL commands.</returns>
        protected virtual IEnumerable<string> CreateIndex(Index index)
        {
            //
            // Escape column names
            //
            List<string> columns = new List<string>();
            Array.ForEach<string>(index.ColumnNames, c => columns.Add(EscapeColumnName(c)));

            //
            // Build SQL statement
            //
            yield return string.Format("CREATE INDEX {0} ON {1} ({2});",
                EscapeConstraintName(index.Name),
                EscapeTableName(index.Table.Name),
                string.Join(", ", columns.ToArray())
            );
        }

        /// <summary>
        /// Enumerates the SQL commands that are necessary to drop
        /// the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index to drop.</param>
        /// <returns>The SQL commands.</returns>
        protected virtual IEnumerable<string> DropIndex(Index index)
        {
            yield return string.Format("DROP INDEX {0};", EscapeConstraintName(index.Name));
        }

        #endregion

        #region Foreign Key Constraints

        /// <summary>
        /// Enumerates the SQL commands that are necessary to create
        /// the specified foreign key constraint (<paramref name="fk"/>).
        /// </summary>
        /// <param name="fk">The foreign key constraint.</param>
        /// <returns>The SQL commands.</returns>
        protected virtual IEnumerable<string> AddForeignKeyConstraint(ForeignKeyConstraint fk)
        {
            yield return string.Format(
                "ALTER TABLE {0} ADD CONSTRAINT {1} FOREIGN KEY ({2}) REFERENCES {3} ({4});",
                EscapeTableName(fk.Table.Name),
                EscapeConstraintName(fk.Name),
                string.Join(", ", EscapeColumnNames(fk.ColumnNames)),
                EscapeTableName(fk.RelatedTableName),
                string.Join(", ", EscapeColumnNames(fk.RelatedColumnNames))
            );
        }

        /// <summary>
        /// Enumerates the SQL commands that are necessary to drop
        /// the specified foreign key constraint (<paramref name="fk"/>).
        /// </summary>
        /// <param name="fk">The foreign key constraint.</param>
        /// <returns>The SQL commands.</returns>
        protected virtual IEnumerable<string> DropForeignKeyConstraint(ForeignKeyConstraint fk)
        {
            yield return string.Format("ALTER TABLE {0} DROP CONSTRAINT {1};",
                EscapeTableName(fk.Table.Name),
                EscapeConstraintName(fk.Name)
            );
        }

        #endregion

        #region Unique Constraints

        /// <summary>
        /// Enumerates the SQL commands that are necessary to create
        /// the specified <paramref name="uniqueConstraint"/>.
        /// </summary>
        /// <param name="uniqueConstraint">The unique constraint.</param>
        /// <returns>The SQL commands.</returns>
        protected virtual IEnumerable<string> AddUniqueConstraint(UniqueConstraint uniqueConstraint)
        {
            yield return string.Format(
                "ALTER TABLE {0} ADD CONSTRAINT {1} UNIQUE ({2});",
                EscapeTableName(uniqueConstraint.Table.Name),
                EscapeConstraintName(uniqueConstraint.Name),
                string.Join(", ", EscapeColumnNames(uniqueConstraint.ColumnNames))
            );
        }

        /// <summary>
        /// Enumerates the SQL commands that are necessary to drop
        /// the specified <paramref name="uniqueConstraint"/>.
        /// </summary>
        /// <param name="uniqueConstraint">The unique constraint.</param>
        /// <returns>The SQL commands.</returns>
        protected virtual IEnumerable<string> DropUniqueConstraint(UniqueConstraint uniqueConstraint)
        {
            yield return string.Format("ALTER TABLE {0} DROP CONSTRAINT {1};",
                EscapeTableName(uniqueConstraint.Table.Name),
                EscapeConstraintName(uniqueConstraint.Name)
            );
        }

        #endregion

        #region Primary Key Constraints

        /// <summary>
        /// Enumerates the SQL commands that are necessary to create
        /// the specified primary key constraint (<paramref name="pk"/>).
        /// </summary>
        /// <param name="pk">The primary key constraint.</param>
        /// <returns>The SQL commands.</returns>
        protected virtual IEnumerable<string> AddPrimaryKeyConstraint(PrimaryKeyConstraint pk)
        {
            yield return string.Format(
                "ALTER TABLE {0} ADD CONSTRAINT {1} PRIMARY KEY ({2});",
                EscapeTableName(pk.Table.Name),
                EscapeConstraintName(pk.Name),
                string.Join(", ", EscapeColumnNames(pk.ColumnNames))
            );
        }

        /// <summary>
        /// Enumerates the SQL commands that are necessary to drop
        /// the specified primary key constraint (<paramref name="pk"/>).
        /// </summary>
        /// <param name="pk">The primary key constraint.</param>
        /// <returns>The SQL commands.</returns>
        protected virtual IEnumerable<string> DropPrimaryKeyConstraint(PrimaryKeyConstraint pk)
        {
            yield return string.Format("ALTER TABLE {0} DROP CONSTRAINT {1};",
                EscapeTableName(pk.Table.Name),
                EscapeConstraintName(pk.Name)
            );
        }

        #endregion

        #region Sql Statement

        /// <summary>
        /// Enumerates the SQL commands that are necessary to execute
        /// the specified <paramref name="sql"/> statement.
        /// </summary>
        /// <param name="sql">The SQL statement.</param>
        /// <returns>The SQL commands.</returns>
        protected virtual IEnumerable<string> ExecuteSqlStatement(SqlStatement sql)
        {
            yield return sql.Sql + (sql.Sql.EndsWith(";") ? "" : ";");
        }

        #endregion

        #region Data Types

        /// <summary>
        /// Builds the <see cref="String"/> that represents the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="length">The length.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="precision">The precision.</param>
        /// <returns>The data type.</returns>
        protected abstract string BuildDataType(SqlTypes type, int? length, int? scale, int? precision);

        /// <summary>
        /// Formats the specified <paramref name="value"/> according the
        /// described data type that it can be used in a SQL statement.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The formatted value.</returns>
        protected virtual string FormatValue(object value)
        {
            var ci = System.Globalization.CultureInfo.InvariantCulture;

            //
            // NULL
            //
            if (value == null || value == DBNull.Value)
            {
                return "NULL";
            }
            //
            // Numbers
            //
            else if (value is int || value is uint ||
                     value is short || value is ushort ||
                     value is long || value is ulong ||
                     value is byte || value is float ||
                     value is double || value is decimal)
            {
                return (value as IFormattable).ToString(null, ci);
            }
            //
            // Date / Time
            //
            else if (value is DateTime)
            {
                string format = "yyyy-MM-dd";
                DateTime date = (DateTime)value;
                if (date.Hour != 0 || date.Minute != 0 || date.Second != 0)
                {
                    format += " HH:mm:ss";
                }

                return StringQuotes + date.ToString(format, ci) + StringQuotes;
            }
            //
            // Strings and anything else
            //
            else
            {
                return StringQuotes + EscapeString(value.ToString()) + StringQuotes;
            }
        }

        #endregion

        #region Escaping

        /// <summary>
        /// Escapes the specified <paramref name="tableName"/>
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns>The escaped table name.</returns>
        protected abstract string EscapeTableName(string tableName);

        /// <summary>
        /// Escapes the specified <paramref name="columnName"/>.
        /// </summary>
        /// <param name="columnName">Name of the column.</param>
        /// <returns>The escaped column name.</returns>
        protected abstract string EscapeColumnName(string columnName);

        /// <summary>
        /// Escapes the specified <paramref name="constraintName"/>.
        /// </summary>
        /// <param name="constraintName">Name of the constraint.</param>
        /// <returns>The escaped constraint name.</returns>
        protected abstract string EscapeConstraintName(string constraintName);

        /// <summary>
        /// Escapes the specified string <paramref name="value"/> that it can be
        /// surrounded by <see cref="StringQuotes"/> and used in a SQL query.
        /// </summary>
        /// <param name="value">The string value to escape.</param>
        /// <returns>The escaped string.</returns>
        protected virtual string EscapeString(string value)
        {
            return value.Replace(new string(StringQuotes, 1), new string(StringQuotes, 2));
        }

        /// <summary>
        /// Applies the <see cref="EscapeColumnName"/> method to an array of column names.
        /// </summary>
        /// <param name="columnNames">The column names.</param>
        /// <returns>The escaped column names.</returns>
        protected virtual string[] EscapeColumnNames(string[] columnNames)
        {
            string[] escapedColumnNames = new string[columnNames.Length];
            for (int i = 0; i < columnNames.Length; i++)
            {
                escapedColumnNames[i] = EscapeColumnName(columnNames[i]);
            }

            return escapedColumnNames;
        }

        #endregion

        #region Misc

        /// <summary>
        /// Builds the SQL fragment that describes an auto-increment column.
        /// </summary>
        /// <param name="seed">The initial value.</param>
        /// <param name="step">The increment step.</param>
        /// <returns>The SQL fragment.</returns>
        protected abstract string BuildAutoIncrement(int? seed, int? step);

        /// <summary>
        /// Gets the quotes used to surround a string (usually ' or ").
        /// </summary>
        /// <value>The string quotes.</value>
        protected virtual char StringQuotes
        {
            get { return '\''; }
        }

        #endregion

        #endregion
    }
}