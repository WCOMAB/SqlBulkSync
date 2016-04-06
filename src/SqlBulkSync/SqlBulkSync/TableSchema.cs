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
        public string SyncNewOrUpdatedTableName { get; private set; }
        public string SyncDeletedTableName { get; private set; }
        public string DropNewOrUpdatedTableStatment { get; private set; }
        public string DropDeletedTableStatment { get; private set; }
        public string MergeNewOrUpdateStatement { get; private set; }
        public string DeleteStatement { get; private set; }
        public string SourceNewOrUpdatedSelectStatment { get; private set; }
        public string SourceDeletedSelectStatement { get; private set; }
        public string CreateNewOrUpdatedSyncTableStatement { get; private set; }
        public string CreateDeletedSyncTableStatement { get; private set; }
        public TableVersion SourceVersion { get; private set; }
        public TableVersion TargetVersion { get; private set; }
        public string TargetStatePath { get; private set; }
        public int BatchSize { get; private set; }
        private TableSchema(
            string tableName,
            Column[] columns,
            TableVersion sourceVersion,
            TableVersion targetVersion,
            string targetStatePath,
            int? batchSize
            )
        {
            var buffername = tableName.Replace("[", "").Replace("]", "");

            TableName = tableName;
            SyncNewOrUpdatedTableName = $"sync.[{buffername}_{Guid.NewGuid()}]";
            SyncDeletedTableName = $"sync.[{buffername}_{Guid.NewGuid()}]";
            Columns = columns;
            SourceVersion = sourceVersion;
            TargetVersion = targetVersion;

            CreateNewOrUpdatedSyncTableStatement = this.GetCreateNewOrUpdatedSyncTableStatement();
            CreateDeletedSyncTableStatement = this.GetCreateDeletedSyncTableStatement();

            SourceNewOrUpdatedSelectStatment = this.GetNewOrUpdatedAtSourceSelectStatment();
            SourceDeletedSelectStatement = this.GetDeletedAtSourceSelectStatement();
            MergeNewOrUpdateStatement = this.GetNewOrUpdatedMergeStatement();
            DeleteStatement = this.GetDeleteStatement();
            DropNewOrUpdatedTableStatment = SyncNewOrUpdatedTableName.GetDropStatment();
            DropDeletedTableStatment = SyncDeletedTableName.GetDropStatment();
            TargetStatePath = targetStatePath;
            BatchSize = batchSize ?? 1000;
        }


        public static TableSchema LoadSchema(SqlConnection sourceConn, SqlConnection targetConn, string tableName, int? batchSize, bool globalChangeTracking)
        {
            var columns = sourceConn.GetColumns(tableName);
            string targetStatePath;
            var targetVersion = targetConn.GetTargetVersion(tableName, out targetStatePath);
            return new TableSchema(
                tableName,
                columns,
                sourceConn.GetSourceVersion(tableName, globalChangeTracking, columns),
                targetVersion,
                targetStatePath,
                batchSize
                );
        }
    }
}