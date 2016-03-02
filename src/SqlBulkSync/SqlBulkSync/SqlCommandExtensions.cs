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

using System;

namespace WCOM.SqlBulkSync
{
    using Dapper;
    using Source.Common;
    using System.Data.SqlClient;
    using System.Data;
    using System.Linq;

    public static class SqlCommandExtensions
    {
        public static void DropSyncTables(this SqlConnection targetConn, TableSchema tableSchema)
        {
            Array.ForEach(
                new[]
                {
                    new
                    {
                        Name = tableSchema.SyncNewOrUpdatedTableName,
                        DropStatement = tableSchema.DropNewOrUpdatedTableStatment
                    },
                    new
                    {
                        Name = tableSchema.SyncDeletedTableName,
                        DropStatement = tableSchema.DropDeletedTableStatment
                    }
                },
                table =>
                {
                    if (string.IsNullOrEmpty(tableSchema?.SyncNewOrUpdatedTableName))
                        return;
                    try
                    {
                        targetConn.Execute(
                            commandType: CommandType.Text,
                            commandTimeout: 500000,
                            sql: table.DropStatement
                            );
                        Log.Success("Sync table {0} dropped.", table.Name);
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(
                            "Failed to drop sync table {0}\r\n{1}",
                            tableSchema.SyncNewOrUpdatedTableName,
                            ex
                            );
                    }
                }
                );
        }

        public static void MergeData(this SqlConnection targetConn, TableSchema tableSchema)
        {
            var rowCount = targetConn.Query<long>(
                commandTimeout: 500000,
                sql: tableSchema.MergeNewOrUpdateStatement
                ).First();
            Log.Success("{0} records merged", rowCount);
        }

        public static void DeleteData(this SqlConnection targetConn, TableSchema tableSchema)
        {
            var rowCount = targetConn.Query<long>(
                commandTimeout: 500000,
                sql: tableSchema.DeleteStatement
                ).First();
            Log.Success("{0} records deleted", rowCount);
        }

        public static void BulkCopyData(this SqlConnection sourceConn, SqlConnection targetConn, TableSchema tableSchema)
        {
            Array.ForEach(
                new[]
                {
                    new
                    {
                        Name = tableSchema.SyncNewOrUpdatedTableName,
                        SelectStatement = tableSchema.SourceNewOrUpdatedSelectStatment,
                        Description = "new or updated"
                    },
                    new
                    {
                        Name = tableSchema.SyncDeletedTableName,
                        SelectStatement = tableSchema.SourceDeletedSelectStatement,
                        Description = "deleted"
                    }
                },
                table =>
                {
                    using (var sourceCmd = new SqlCommand
                    {
                        Connection = sourceConn,
                        CommandType = CommandType.Text,
                        CommandText = table.SelectStatement,
                        CommandTimeout = 500000
                    })
                    {
                        using (var reader = sourceCmd.ExecuteReader())
                        {
                            using (var bcp = new SqlBulkCopy(targetConn)
                            {
                                DestinationTableName = table.Name,
                                BatchSize = tableSchema.BatchSize,
                                NotifyAfter = tableSchema.BatchSize
                            })
                            {
                                bcp.SqlRowsCopied += (s, e) => Log.Info("{0} {1} rows copied", e.RowsCopied, table.Description);
                                bcp.WriteToServer(reader);
                                Log.Success("Bulk copy complete for {0} rows", table.Description);
                            }
                        }
                    }
                }
                );
        }

        public static void CreateSyncTables(this SqlConnection targetConn, TableSchema tableSchema)
        {
            Array.ForEach(
                new[]
                {
                    new
                    {
                        Name = tableSchema.SyncNewOrUpdatedTableName,
                        CreateStatement = tableSchema.CreateNewOrUpdatedSyncTableStatement
                    },
                    new
                    {
                        Name = tableSchema.SyncDeletedTableName,
                        CreateStatement = tableSchema.CreateDeletedSyncTableStatement
                    }
                },
                table =>
                {
                    targetConn.Execute(
                        commandType: CommandType.Text,
                        commandTimeout: 500,
                        sql: table.CreateStatement
                        );
                    Log.Success("Sync table {0} created.", table.Name, table.CreateStatement);
                }
                );
        }
    }
}