#WCOM SqlBulkSync
WCOM SQL Bulk Sync is a lightweight, performant non-intrusive SQL Server data sync tool.
It doesn’t use any triggers or events, but instead uses the change tracking features available from SQL Server 2008 and up.
The tool was developed primary for syncing on premise SQL server data to Azure in an efficient way, where only the changes are transferred. But it will also work just fine between non cloud instances.

##Table of contents
1. [Usage](#usage)
    * [Example process](#example-process)
    * [Example create template](#example-create-template)
2. [HRON](#hron)
    * [Sample job sync file](#sample-job-sync-file)
    * [Sample table sync state file](#sample-table-sync-state-file)
3. [Sample sync](#sample-sync)
    * [CreateSourceDb.sql](#createsourcedbsql)
    * [CreateTargetDb.sql](#createtargetdbsql)
    * [CompanySync.hron](#companysynchron)
    * [RandomUpdate.sql](#randomupdatesql)

##Usage
```
SqlBulkSync.exe PROCESS|CREATETEMPLATE [SyncJobFilePath]
```
###Example process
```
SqlBulkSync.exe PROCESS OrderTablesToAzure.hron
```
The `process` scope is the one that does the data synchronization.
The sync job file contains source & target database connection and which tables to sync.
The tool currently has some opinions:
* It assumes table schema and naming are the same at source and target
* It assumes that change tracking is enabled
* And that a `sync` schema is present at the target(it's used for the sync tables as i.e. Azure cant BulkImport into temp tables)

Other than that it will dynamically investigate schema, create sync tables and only sync data from non-calculated columns.
###Example create template
```
SqlBulkSync.exe CREATETEMPLATE OrderTablesToAzure.hron
```

The "createtemplate" scope is used for creating an base job file to use as an template for sync process.

##HRON
The serialization format used for sync jobs and state is HRON, you can read more about it [here](https://github.com/mrange/hron).
###Sample job sync file
```
=SourceDbConnection
	Data Source=sourceserver;Initial Catalog=sourcedatabase;User ID=userid;Password=password
=TargetDbConnection
	Data Source=targetserver;Initial Catalog=targetdatabase;User ID=userid;Password=password
=Tables
	dbo.Table1
=Tables
	dbo.Table2
=BatchSize
	1000
```
The job sync file is the definition of what and where from/to sync.
* `SourceDbConnection` is the data connection string to the *instance/database* where the source data relies
* `TargetDbConnection` is the data connection string to the *instance/database* where the date should be synced to
* `Tables` one to many tables to sync
* `BatchSize` batch size of bulk copy to sync table

###Sample table sync state file
```
=CurrentVersion
	256
=MinValidVersion
	1
=QueriedDateTime
	2014-11-13 11:35:18
```
These files are created automatically in a SyncState folder in the same location as the program. One file per table is created, after every successful sync it's updated with the synced version. If file doesn't exist a full sync will occur. 

##Sample sync
A sample database for testing is avalilable in the [sample-sync](../sample-sync/) folder in the repositiory.

###CreateSourceDb.sql
The [CreateSourceDb.sql](../sample-sync/CreateSourceDb.sql) SQL script will create a sample source database, with schema, data and change tracking enabled.

###CreateTargetDb.sql
The [CreateTargetDb.sql](../sample-sync/CreateTargetDb.sql) SQL script will create an sample target database with schema but no data.

###CompanySync.hron
The [CompanySync.hron](../sample-sync/CompanySync.hron) job sync file is set to sync the sample Company table from `SourceDatabase` to `TargetDatabase` on `localhost` default instance using integrated security.

###RandomUpdate.sql
The [RandomUpdate.sql](../sample-sync/RandomUpdate.sql) SQL script just updates 5 random rows in the table to test the sync. 
