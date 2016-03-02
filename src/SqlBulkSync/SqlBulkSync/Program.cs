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
    using Source.Common;
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    internal static class Program
    {
        private static void Main(string[] args)
        {
            var totalStopWatch = Stopwatch.StartNew();
            try
            {
                var syncJobPath = args.Skip(1).FirstOrDefault();
                switch (args.Select(CultureInfo.InvariantCulture.TextInfo.ToUpper).FirstOrDefault())
                {
                    case "PROCESS":
                    {
                        SyncRunner.Run(syncJobPath);
                        break;
                    }
                    case "PROCESS_GLOBAL_CHANGETRACKING":
                    {
                        SyncRunner.Run(globalChangeTracking: true, syncJobPath:syncJobPath);
                        break;
                    }
                    case "CREATETEMPLATE":
                    {
                        CreateTemplateFile(syncJobPath);
                        break;
                    }
                    default:
                    {
                        Usage();
                        Environment.Exit(1);
                        break;
                    }
                }
                
            }
            catch (Exception ex)
            {
                Log.Exception("{0}", ex);
                Environment.Exit(500);
            }
            finally
            {
                totalStopWatch.Stop();
                Log.HighLight("Total duration: {0}", totalStopWatch.Elapsed);
            }
        }

        

        public static void Usage()
        {
            Log.Info(
                "USAGE:\r\n\t{0} PROCESS|CREATETEMPLATE|PROCESS_GLOBAL_CHANGETRACKING [SyncJobFilePath]",
                Path.GetFileName(Assembly.GetExecutingAssembly().Location)
                );
        }

        private static void CreateTemplateFile(string syncJobPath)
        {
            if (string.IsNullOrWhiteSpace(syncJobPath) || File.Exists(syncJobPath))
            {
                Log.Error("Target file {0} invalid or already exists, won't replace.", syncJobPath);
                Usage();
                Environment.ExitCode = 3;
                return;
            }
            var syncJob = new SyncJob
            {
                SourceDbConnection = new SqlConnectionStringBuilder
                {
                    DataSource = "sourceserver",
                    InitialCatalog = "sourcedatabase",
                    UserID = "userid",
                    Password = "password"
                }.ToString(),
                TargetDbConnection = new SqlConnectionStringBuilder
                {
                    DataSource = "targetserver",
                    InitialCatalog = "targetdatabase",
                    UserID = "userid",
                    Password = "password"
                }.ToString(),
                Tables = new List<string>{ "dbo.Table1", "dbo.Table2"},
                BatchSize = 1000
            };
            Log.Info("Saving file to {0}", syncJobPath);
            syncJob.Save(syncJobPath);
            Log.Success("File saved");
        }    
    }
}
