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
    using Source.Common;
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Linq;

    public static class SyncRunner
    {
        public static void Run(string syncJobPath)
        {
            SyncJob syncJob;
            if (!SyncJob.TryLoad(syncJobPath, out syncJob) || syncJob == null)
            {
                Log.Error("Failed to load SyncJob file {0}", syncJobPath);
                Program.Usage();
                Environment.Exit(1);
            }
            ProcessSync(syncJob);
        }

        private static void ProcessSync(SyncJob syncJob)
        {
            using (SqlConnection
                sourceConn = new SqlConnection(syncJob.SourceDbConnection),
                targetConn = new SqlConnection(syncJob.TargetDbConnection)
                )
            {
                Log.Info(
                    "Connecting to source database {0}.{1}",
                    sourceConn.DataSource,
                    sourceConn.Database
                    );
                sourceConn.Open();
                Log.Success("Connected {0}", sourceConn.ClientConnectionId);
                
                Log.Info(
                    "Connecting to target database {0}.{1}",
                    targetConn.DataSource,
                    targetConn.Database
                    );
                targetConn.Open();
                Log.Success("Connected {0}", targetConn.ClientConnectionId);

                Log.Info("Fetching table schemas");
                var schemaStopWatch = Stopwatch.StartNew();
                var tableSchemas = (
                    syncJob
                        .Tables ?? new List<string>()
                    )
                    .Select(
                        table => TableSchema.LoadSchema(sourceConn, targetConn, table, syncJob.BatchSize)
                    ).ToArray();
                schemaStopWatch.Stop();
                Log.Success("Found {0} tables, duration {1}", tableSchemas.Length, schemaStopWatch.Elapsed);

                Array.ForEach(
                    tableSchemas,
                    tableSchema =>
                    {
                        Log.Info("Begin {0}", tableSchema.TableName);
                        var syncStopWatch = Stopwatch.StartNew();
                        if (tableSchema.SourceVersion.Equals(tableSchema.TargetVersion))
                        {
                            Log.HighLight("Allready up to date");
                        }
                        else
                        {
                            SyncTable(targetConn, tableSchema, sourceConn);
                        }
                        syncStopWatch.Stop();
                        Log.Info("End {0}, duration {1}", tableSchema.TableName, syncStopWatch.Elapsed);
                        tableSchema.PersistsSourceTargetVersionState();
                    }
                    );
            }
        }
        private static void SyncTable(SqlConnection targetConn, TableSchema tableSchema, SqlConnection sourceConn)
        {
            try
            {
                targetConn.CreateSyncTable(tableSchema);
                sourceConn.BulkCopyData(targetConn, tableSchema);
                targetConn.MergeData(tableSchema);
            }
            finally
            {
                targetConn.DropSyncTable(tableSchema);
            }
        }
    }
}