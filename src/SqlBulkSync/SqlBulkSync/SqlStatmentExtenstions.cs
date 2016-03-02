// ----------------------------------------------------------------------------------------------
// Copyright (c) WCOM AB.
// ----------------------------------------------------------------------------------------------
// This source code is subject to terms and conditions of the Microsoft Public License. A
// copy of the license can be found in the License.md file at the root of this distribution.
// If you cannot locate the  Microsoft Public License, please send an email to
// dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound
//  by the terms of the Microsoft Public License.
// ----------------------------------------------------------------------------------------------
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------------------------
// ReSharper disable ParameterTypeCanBeEnumerable.Global
namespace WCOM.SqlBulkSync
{
    using System;
    using System.Linq;

    public static class SqlStatmentExtenstions
    {
        public static string GetTableVersionStatement(
            string tableName,
            bool globalChangeTracking,
            Column[] columns
            )
        {
            if (globalChangeTracking)
            {
                return string.Format(
                    @"
SELECT  '{0}'                                               AS TableName,
        CHANGE_TRACKING_CURRENT_VERSION()                   AS CurrentVersion,
        CHANGE_TRACKING_MIN_VALID_VERSION(
            OBJECT_ID('{0}')
        )                                                   AS MinValidVersion,
        GETUTCDATE()                                        AS QueriedDateTime",
                    tableName
                    );
            }

            return string.Format(
                @"
SELECT  '{0}'                                               AS TableName,
        SYS_CHANGE_VERSION                                  AS CurrentVersion,
        CHANGE_TRACKING_MIN_VALID_VERSION(
            OBJECT_ID('{0}')
        )                                                   AS MinValidVersion,
        GETUTCDATE()                                        AS QueriedDateTime
    FROM  CHANGETABLE(VERSION  {0}, ({1}), ({1})) as t",
                tableName,
                string.Join(
                    ",",
                    columns
                        .Where(column => column.IsPrimary)
                        .Select(column => column.QuoteName)
                    )
                );
        }

        public static string GetDropStatment(this string tableName)
        {
            return string.Format(
                @"IF OBJECT_ID('{0}') IS NOT NULL
BEGIN
    DROP TABLE {0}
END",
                tableName
                );
        }

        public static string GetNewOrUpdatedMergeStatement(this TableSchema tableSchema)
        {
            var identityInsert = tableSchema.Columns.Any(column => column.IsIdentity);
            var statement = string.Format(
                @"{6};
MERGE {0} AS target
USING {1} AS source
ON {2}
WHEN NOT MATCHED BY TARGET
    THEN INSERT (
        {3}
    ) VALUES (
        {4}
    ){8}
WHEN MATCHED
    THEN UPDATE
        SET {5};
SELECT @@ROWCOUNT AS [RowCount];
{7}",
                tableSchema.TableName,
                tableSchema.SyncNewOrUpdatedTableName,
                string.Join(
                    " AND\r\n        ",
                    tableSchema.Columns.Where(column => column.IsPrimary).Select(
                        column => string.Concat(
                            "target.",
                            column.QuoteName,
                            " = source.",
                            column.QuoteName
                            )
                        )
                    ),
                string.Join(
                    ",\r\n        ",
                    tableSchema.Columns.Select(column => column.QuoteName)
                    ),
                string.Join(
                    ",\r\n        ",
                    tableSchema.Columns.Select(column => string.Concat("source.", column.QuoteName))
                    ),
                string.Join(
                    ",\r\n            ",
                    tableSchema.Columns.Where(column => !column.IsPrimary).Select(
                        column => string.Concat(
                            column.QuoteName,
                            " = source.",
                            column.QuoteName
                            )
                        )
                    ),
                (
                    identityInsert
                        ? $"SET IDENTITY_INSERT {tableSchema.TableName} ON"
                        : string.Empty
                    ),
                (
                    identityInsert
                        ? $"SET IDENTITY_INSERT {tableSchema.TableName} OFF"
                        : string.Empty
                    ),
                (
                    (tableSchema.TargetVersion.CurrentVersion <= 1)
                        ? @"
WHEN NOT MATCHED BY SOURCE
    THEN DELETE"
                        : string.Empty
                    )
                );
            return statement;
        }

        public static string GetDeleteStatement(this TableSchema tableSchema)
        {
            var statement = string.Format(
                @"DELETE FROM target
FROM {0} source
    INNER JOIN {1} target ON    {2}
SELECT @@ROWCOUNT AS [RowCount]",
                tableSchema.SyncDeletedTableName,
                tableSchema.TableName,
                string.Join(
                    " AND\r\n                                ",
                    tableSchema.Columns.Where(column => column.IsPrimary).Select(
                        column => string.Concat(
                            "target.",
                            column.QuoteName,
                            " = source.",
                            column.QuoteName
                            )
                        )
                    )
                );

            return statement;
        }

        public static string GetCreateNewOrUpdatedSyncTableStatement(this TableSchema tableSchema)
        {
            var statement = string.Format(
                @"
CREATE TABLE {0}(
    {1},
 CONSTRAINT [PK_{3}] PRIMARY KEY CLUSTERED
(
    {2}
)
)",
                tableSchema.SyncNewOrUpdatedTableName,
                string.Join(
                    ",\r\n    ",
                    tableSchema.Columns.Select(
                        // ReSharper disable once UseStringInterpolation
                        column => string.Format(
                            "{0} {1} {2} {3}",
                            column.QuoteName,
                            column.Type,
                            !string.IsNullOrEmpty(column.Collation) ? "COLLATE " + column.Collation : "",
                            column.IsNullable ? "NULL" : "NOT NULL"
                            )
                        )
                    ),
                string.Join(
                    ",\r\n    ",
                    tableSchema.Columns
                        .Where(column => column.IsPrimary)
                        .Select(
                            column => column.QuoteName
                        )
                    ),
                Guid.NewGuid()
                );
            return statement;
        }

        public static string GetCreateDeletedSyncTableStatement(this TableSchema tableSchema)
        {
            var statement = string.Format(
                @"
CREATE TABLE {0}(
    {1},
 CONSTRAINT [PK_{2}] PRIMARY KEY CLUSTERED
(
    {3}
)
)",
                tableSchema.SyncDeletedTableName,
                string.Join(
                    ",\r\n    ",
                    tableSchema.Columns
                        .Where(column => column.IsPrimary)
                        .Select(
                        // ReSharper disable once UseStringInterpolation
                        column => string.Format(
                            "{0} {1} {2} {3}",
                            column.QuoteName,
                            column.Type,
                            !string.IsNullOrEmpty(column.Collation) ? "COLLATE " + column.Collation : "",
                            column.IsNullable ? "NULL" : "NOT NULL"
                            )
                        )
                    ),
                Guid.NewGuid(),
                string.Join(
                    ",\r\n    ",
                    tableSchema.Columns
                        .Where(column => column.IsPrimary)
                        .Select(
                            column => column.QuoteName
                        )
                    )
                );
            return statement;
        }

        public static string GetDeletedAtSourceSelectStatement(this TableSchema tableSchema)
        {
            if (tableSchema.Columns == null || tableSchema.Columns.Length == 0)
                throw new Exception("Columns missing");

            var primaryKeyColumns = tableSchema.Columns
                .Where(column => column.IsPrimary)
                .ToArray();

            if (tableSchema.TargetVersion.CurrentVersion <= 1)
            {
                // ReSharper disable once UseStringInterpolation
                return string.Format(
                    @"SELECT  TOP 0 {0}
    FROM {1} WITH(NOLOCK)",
                    string.Join(
                        ",\r\n        ",
                        primaryKeyColumns.Select(column => column.QuoteName)
                        )
                    ,
                    tableSchema.TableName
                    );
            }

            // ReSharper disable once UseStringInterpolation
            return string.Format(
                @"SELECT  {0}
    FROM CHANGETABLE(CHANGES {1}, {2}) ct
    WHERE ct.SYS_CHANGE_OPERATION = 'D'",
                string.Join(
                    ",\r\n        ",
                    tableSchema.Columns
                    .Where(column => column.IsPrimary)
                    .Select(column => string.Concat("ct.", column.QuoteName))
                    ),
                tableSchema.TableName,
                tableSchema.TargetVersion.CurrentVersion
                );
        }

        public static string GetNewOrUpdatedAtSourceSelectStatment(this TableSchema tableSchema)
        {
            if (tableSchema.Columns == null || tableSchema.Columns.Length == 0)
                throw new Exception("Columns missing");

            var statement = (tableSchema.TargetVersion.CurrentVersion <= 1)
                // ReSharper disable once UseStringInterpolation
                ? string.Format(
                    @"SELECT  {0}
    FROM {1} WITH(NOLOCK)",
                    string.Join(
                                ",\r\n        ",
                                tableSchema.Columns.Select(column => column.QuoteName)
                                ),
                    tableSchema.TableName
                    )
                : string.Format(
                    @"SELECT  {0}
    FROM CHANGETABLE(CHANGES {1}, {2}) ct
        INNER JOIN {1} t WITH(NOLOCK) ON {3}",
                    string.Join(
                        ",\r\n        ",
                        tableSchema.Columns.Select(column => string.Concat("t.", column.QuoteName))
                        ),
                    tableSchema.TableName,
                    tableSchema.TargetVersion.CurrentVersion,
                    string.Join(
                        " AND\r\n        ",
                        tableSchema.Columns.Where(column => column.IsPrimary).Select(
                            column => string.Concat(
                                "t.",
                                column.QuoteName,
                                " = ct.",
                                column.QuoteName
                                )
                            )
                        )
                    );
            return statement;
        }
    }
}