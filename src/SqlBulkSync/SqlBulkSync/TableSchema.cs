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
// ReSharper disable MemberCanBePrivate.Global
namespace WCOM.SqlBulkSync
{
    using System;
    using System.Data.SqlClient;

    public class TableSchema
    {
        public string TableName { get; private set; }
        public Column[] Columns { get; private set; }
        public string SyncTableName { get; private set; }
        public string DropStatment { get; set; }
        public string MergeStatement { get; set; }
        public string SourceSelectStatment { get; set; }
        public string CreateSyncTableStatement { get; set; }
        public TableVersion SourceVersion { get; set; }
        public TableVersion TargetVersion { get; set; }
        public string TargetStatePath { get; set; }
        public int BatchSize { get; set; }
        private TableSchema(
            string tableName,
            Column[] columns,
            TableVersion sourceVersion,
            TableVersion targetVersion,
            string targetStatePath,
            int? batchSize
            )
        {
            TableName = tableName;
            SyncTableName = string.Format(
                "sync.[{0}_{1}]",
                tableName,
                Guid.NewGuid()
                );
            Columns = columns;
            SourceVersion = sourceVersion;
            TargetVersion = targetVersion;

            CreateSyncTableStatement = this.GetCreateSyncTableStatement();
            SourceSelectStatment = this.GetSourceSelectStatment();
            MergeStatement = this.GetMergeStatement();
            DropStatment = this.GetDropStatment();
            TargetStatePath = targetStatePath;
            BatchSize = batchSize ?? 1000;
        }

        public static TableSchema LoadSchema(SqlConnection sourceConn, SqlConnection targetConn, string tableName, int? batchSize)
        {
            var columns = sourceConn.GetColumns(tableName);
            string targetStatePath;
            var targetVersion = targetConn.GetTargetVersion(tableName, out targetStatePath);
            return new TableSchema(
                tableName,
                columns,
                sourceConn.GetSourceVersion(tableName, columns),
                targetVersion,
                targetStatePath,
                batchSize
                );
        }
    }
}