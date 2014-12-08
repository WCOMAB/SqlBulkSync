// ----------------------------------------------------------------------------------------------
// Copyright (c) WCOM AB.
// ----------------------------------------------------------------------------------------------
// This source code is subject to terms and conditions of the Microsoft Public License. A 
// copy of the license can be found in the LICENSE.md file at the root of this distribution. 
// If you cannot locate the  Microsoft Public License, please send an email to 
// dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
//  by the terms of the Microsoft Public License.
// ----------------------------------------------------------------------------------------------
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------------------------
namespace WCOM.SqlBulkSync
{
    using Dapper;
    using Source.Common;
    using Source.HRON;
    using System;
    using System.Data.SqlClient;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    public static class SchemaExtensions
    {

        private static readonly DirectoryInfo SyncStateDirectory = new DirectoryInfo(Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "SyncState"
            ));

        public static void PersistsSourceTargetVersionState(this TableSchema tableSchema)
        {
            File.WriteAllText(
                tableSchema.TargetStatePath,
                HRONSerializer.ObjectAsString(
                    tableSchema.SourceVersion
                    ),
                Encoding.UTF8
                );
        }

        public static TableVersion GetTargetVersion(
            this SqlConnection conn,
            string tableName,
            out string targetStatePath
            )
        {
            if (!SyncStateDirectory.Exists)
                SyncStateDirectory.Create();

            targetStatePath = Path.Combine(
                SyncStateDirectory.FullName,
                Uri.EscapeDataString(
                    String.Concat(
                        conn.DataSource,
                        "_",
                        conn.Database,
                        "_",
                        tableName,
                        ".hron"
                        )
                    )
                );


            TableVersion targetVersion;
            HRONObjectParseError[] errors;
            if (
                File.Exists(targetStatePath) &&
                HRONSerializer.TryParseObject(
                    0,
                    File.ReadAllText(targetStatePath, Encoding.UTF8).ReadLines(),
                    out targetVersion,
                    out errors
                    ))
                return targetVersion;

            var tableVersion = new TableVersion
            {
                CurrentVersion = -1,
                MinValidVersion = -1
            };
            File.WriteAllText(
                targetStatePath,
                HRONSerializer.ObjectAsString(
                    tableVersion
                    ),
                Encoding.UTF8
                );
            return tableVersion;
        }

        public static TableVersion GetSourceVersion(this SqlConnection conn, string tableName, Column[] columns)
        {
            var tableVersionStatement = SqlStatmentExtenstions.GetTableVersionStatement(tableName, columns);
            var result = conn.Query<TableVersion>(
                tableVersionStatement
                ).FirstOrDefault();
            return result;
        }

        public static Column[] GetColumns(this IDbConnection sourceConn, string tableName)
        {
            return sourceConn
                .Query<Column>(
                    @"
    SELECT  c.Name              AS Name         ,
            tn.Type             AS Type         ,
            c.is_identity       AS IsIdentity   ,
            tn.IsPrimary        AS IsPrimary    ,
            c.is_nullable       AS IsNullable   ,
            QUOTENAME(c.Name)   AS QuoteName
        FROM sys.columns c
            INNER JOIN sys.types tp ON c.user_type_id = tp.user_type_id
            CROSS APPLY (
                SELECT CASE
                            WHEN tp.name IN('varchar', 'nvarchar')
                                THEN    tp.name +
                                        '(' +
                                        CASE c.max_length
                                            WHEN -1 THEN 'max'
                                            ELSE CAST(c.max_length as nvarchar(max))
                                        END
                                        +')'
                            WHEN tp.name = 'decimal'
                                THEN    tp.name +
                                        '(' +
                                            CAST(c.precision as nvarchar(max))
                                            + ', ' +
                                            CAST(c.scale as nvarchar(max))
                                        +')'
                            ELSE tp.name
                        END AS Type,
                        CASE WHEN EXISTS(SELECT 1
                                            FROM sys.indexes i 
                                                INNER JOIN sys.index_columns ic ON  i.object_id = ic.object_id  AND
                                                                                    i.index_id  = ic.index_id   AND
                                                                                    c.column_id = ic.column_id
                                            WHERE   i.is_primary_key  = 1 AND
                                                    i.object_id = c.object_id)
                            THEN CAST(1 AS bit)
                            ELSE CAST(0 AS bit)
                        END AS IsPrimary
            ) tn
    WHERE   object_id       = object_id(@TableName) AND
            c.is_computed   = 0",
                    new {TableName = tableName}
                )
                .ToArray();
        }
    }
}