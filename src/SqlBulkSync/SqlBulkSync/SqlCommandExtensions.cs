// ----------------------------------------------------------------------------------------------
// Copyright (c) WCOM AB.
// ----------------------------------------------------------------------------------------------
// This source code is subject to terms and conditions of the Microsoft Public License. A 
// copy of the license can be found in the License.html file at the root of this distribution. 
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
    using System.Data.SqlClient;
    using System.Data;
    using System.Linq;

    public static class SqlCommandExtensions
    {
        public static void DropSyncTable(this SqlConnection targetConn, TableSchema tableSchema)
        {
            if (tableSchema == null || string.IsNullOrEmpty(tableSchema.SyncTableName))
                return;

            targetConn.Execute(
                commandType: CommandType.Text,
                commandTimeout: 300,
                sql: tableSchema.DropStatment
                );
        }

        public static void MergeData(this SqlConnection targetConn, TableSchema tableSchema)
        {
            var rowCount = targetConn.Query<long>(
                commandTimeout: 300,
                sql: tableSchema.MergeStatement
                ).First();
            Log.Success("{0} records affected", rowCount);
        }

        public static void BulkCopyData(this SqlConnection sourceConn, SqlConnection targetConn, TableSchema tableSchema)
        {
            using (var sourceCmd = new SqlCommand
            {
                Connection = sourceConn,
                CommandType = CommandType.Text,
                CommandText = tableSchema.SourceSelectStatment,
                CommandTimeout = 300
            })
            {
                using (var reader = sourceCmd.ExecuteReader())
                {
                    using (var bcp = new SqlBulkCopy(targetConn)
                    {
                        DestinationTableName = tableSchema.SyncTableName,
                        BatchSize = tableSchema.BatchSize
                    })
                    {
                        bcp.WriteToServer(reader);
                    }
                }
            }
        }

        public static void CreateSyncTable(this SqlConnection targetConn, TableSchema tableSchema)
        {
            targetConn.Execute(
                commandType: CommandType.Text,
                commandTimeout: 300,
                sql: tableSchema.CreateSyncTableStatement
                );
        }
    }
}