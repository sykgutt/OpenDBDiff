// Standalone integration probe. See Documentacion/Ejecucion.md; not executed by MSTest.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using OpenDBDiff.SqlServer.Schema.Generates;
using OpenDBDiff.SqlServer.Schema.Model;
using OpenDBDiff.SqlServer.Schema.Options;
using OpenDBDiff.Abstractions.Schema;
using OpenDBDiff.Abstractions.Schema.Model;

class MemoryLab
{
    static string bin;
    static string root;
    static readonly string prefix = "OpenDBDiffMemoryTest_" + Guid.NewGuid().ToString("N");
    static string before = prefix + "_Actual", after = prefix + "_Desired";
    static string Cs(string db) { return "Server=localhost;Database=" + db + ";Integrated Security=True;TrustServerCertificate=True;"; }
    static int Main(string[] args)
    {
        bin = Path.GetFullPath(args[0]); root = Path.GetFullPath(args[1]);
        AppDomain.CurrentDomain.AssemblyResolve += (s,e) => System.Reflection.Assembly.LoadFrom(Path.Combine(bin,new AssemblyName(e.Name).Name+".dll"));
        try { Run(); Console.WriteLine("LAB PASSED"); return 0; }
        catch(Exception e) { Console.WriteLine(e.ToString()); return 1; }
        finally
        {
            foreach(var db in new[]{before, after})
                try { Exec("master", "IF DB_ID('" + db + "') IS NOT NULL BEGIN ALTER DATABASE ["+db+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ["+db+"]; END"); Console.WriteLine("Removed disposable database " + db); }
                catch(Exception e) { Console.WriteLine("Cleanup required: " + db + " " + e.Message); }
        }
    }
    static void Exec(string db, string sql)
    {
        using(var conn=new SqlConnection(Cs(db)))
        {
            conn.Open();
            foreach(var batch in Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline|RegexOptions.IgnoreCase))
                if(!string.IsNullOrWhiteSpace(batch)) using(var cmd=new SqlCommand(batch,conn)) { cmd.CommandTimeout=120; cmd.ExecuteNonQuery(); }
        }
    }
    static Database Read(string db) { return new Generate { ConnectionString=Cs(db), Options=new SqlOption() }.Process(); }
    static Database Compare() { return Generate.Compare(Read(before),Read(after)); }
    static int Scalar(string db, string sql)
    {
        using(var conn=new SqlConnection(Cs(db)))
        using(var cmd=new SqlCommand(sql,conn)) { conn.Open(); return (int)cmd.ExecuteScalar(); }
    }
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); Console.WriteLine("OK " + message); }
    static void Apply(string label)
    {
        var comparison=Compare();
        var sql=comparison.ToSqlDiff(new List<ISchemaBase>()).ToSQL();
        File.WriteAllText(Path.Combine(root,label+".sql"),sql);
        Exec(before,sql);
        var second=Compare();
        Check(second.Tables.All(t=>t.Status==ObjectStatus.Original),label+" second comparison has no table differences");
    }
    static void Run()
    {
        string dataPath;
        using(var conn=new SqlConnection(Cs("master")))
        {
            conn.Open(); using(var cmd=new SqlCommand("SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath'))",conn)) dataPath=(string)cmd.ExecuteScalar();
            using(var cmd=new SqlCommand("SELECT CONVERT(nvarchar(100),SERVERPROPERTY('ProductVersion'))",conn)) Console.WriteLine("Engine="+cmd.ExecuteScalar());
        }
        foreach(var db in new[]{before,after})
        {
            Exec("master","CREATE DATABASE ["+db+"]; ALTER DATABASE ["+db+"] ADD FILEGROUP MemoryData CONTAINS MEMORY_OPTIMIZED_DATA; ALTER DATABASE ["+db+"] ADD FILE (NAME=N'MemoryContainer', FILENAME=N'"+Path.Combine(dataPath,db+"_xtp").Replace("'","''")+"') TO FILEGROUP MemoryData;");
            Console.WriteLine("Created disposable database " + db);
        }
        Exec(after,@"CREATE TABLE dbo.[Transaction] (Id int NOT NULL, Value int NULL,
            CONSTRAINT PK_Transaction PRIMARY KEY NONCLUSTERED HASH (Id) WITH (BUCKET_COUNT=128),
            INDEX IX_Value NONCLUSTERED (Value), CONSTRAINT CK_Value CHECK (Value >= 0)) WITH (MEMORY_OPTIMIZED=ON,DURABILITY=SCHEMA_AND_DATA);
            CREATE TABLE dbo.MemoryStage (Id int NOT NULL, Other int NOT NULL CONSTRAINT DF_Stage_Other DEFAULT (0), INDEX IX_Id NONCLUSTERED HASH (Id) WITH (BUCKET_COUNT=128), CONSTRAINT UQ_Stage UNIQUE NONCLUSTERED HASH (Other, Id) WITH (BUCKET_COUNT=128)) WITH (MEMORY_OPTIMIZED=ON,DURABILITY=SCHEMA_ONLY);
            CREATE TABLE dbo.ParentMemory (Id int NOT NULL PRIMARY KEY NONCLUSTERED (Id)) WITH (MEMORY_OPTIMIZED=ON,DURABILITY=SCHEMA_AND_DATA);
            CREATE TABLE dbo.ChildMemory (Id int NOT NULL PRIMARY KEY NONCLUSTERED (Id), ParentId int NULL, CONSTRAINT FK_Child_Parent FOREIGN KEY (ParentId) REFERENCES dbo.ParentMemory(Id)) WITH (MEMORY_OPTIMIZED=ON,DURABILITY=SCHEMA_AND_DATA);
            CREATE TABLE dbo.zzzAfterTransaction (Id int NOT NULL);");
        var inventory=Read(after);
        Check(inventory.Tables.Count==5,"all tables including table after Transaction were loaded");
        Check(inventory.Tables.Count(t=>t.IsMemoryOptimized)==4,"memory classification");
        Check(inventory.Tables.First(t=>t.Name=="Transaction").Constraints.Single(c=>c.Type==Constraint.ConstraintType.PrimaryKey).Index.BucketCount==128,"HASH primary key metadata");
        Apply("01-create");
        Exec(before,"INSERT dbo.[Transaction](Id,Value) VALUES(1,10); INSERT dbo.MemoryStage(Id) VALUES(1);");
        Exec(after,@"ALTER TABLE dbo.[Transaction] ADD Extra int NULL;
            ALTER TABLE dbo.[Transaction] ADD Flag int NOT NULL CONSTRAINT DF_Transaction_Flag DEFAULT (7) WITH VALUES;
            ALTER TABLE dbo.[Transaction] ALTER INDEX PK_Transaction REBUILD WITH(BUCKET_COUNT=256);
            ALTER TABLE dbo.MemoryStage ALTER INDEX IX_Id REBUILD WITH(BUCKET_COUNT=256);
            ALTER TABLE dbo.[Transaction] ADD INDEX IX_Extra NONCLUSTERED (Extra);");
        Apply("02-alter");
        using(var conn=new SqlConnection(Cs(before)))
        { conn.Open(); using(var cmd=new SqlCommand("SELECT COUNT(*) FROM dbo.[Transaction] WHERE Id=1 AND Value=10 AND Flag=7",conn)) Check((int)cmd.ExecuteScalar()==1,"data preserved after ALTER"); }
        var nativeProcedure=@"CREATE PROCEDURE dbo.NativeTransactionRead
            WITH NATIVE_COMPILATION, SCHEMABINDING, EXECUTE AS OWNER
            AS BEGIN ATOMIC WITH (TRANSACTION ISOLATION LEVEL=SNAPSHOT, LANGUAGE=N'us_english')
                SELECT Id, Value FROM dbo.[Transaction] WHERE Id=1;
            END";
        Exec(before,nativeProcedure); Exec(after,nativeProcedure);
        Exec(after,"ALTER TABLE dbo.[Transaction] DROP CONSTRAINT DF_Transaction_Flag; ALTER TABLE dbo.[Transaction] ADD CONSTRAINT DF_Transaction_Flag DEFAULT (9) FOR Flag;");
        Apply("02b-default-change");
        Check(Scalar(before,"SELECT COUNT(*) FROM sys.procedures WHERE object_id=OBJECT_ID('dbo.NativeTransactionRead')")==1,
            "default-only change preserves schema-bound native procedure");
        Exec(after,"DROP TABLE dbo.MemoryStage;");
        Apply("03-drop");
        Exec(before,"DROP PROCEDURE dbo.NativeTransactionRead;"); Exec(after,"DROP PROCEDURE dbo.NativeTransactionRead;");
        Exec(after,"DROP TABLE dbo.[Transaction]; CREATE TABLE dbo.[Transaction](Id int NOT NULL, Value int NULL, Extra int NULL);");
        try { Compare().ToSqlDiff(new List<ISchemaBase>()); throw new Exception("Conversion must be blocked"); }
        catch(ScriptGenerationException e) { Check(e.Message.Contains("conversion between disk and memory"),"storage conversion blocked with reason"); }
        var subset=Compare();
        subset.ToSqlDiff(new List<ISchemaBase>{subset.Tables.First(t=>t.Name=="zzzAfterTransaction")});
        Console.WriteLine("OK selection excludes blocked conversion");
        var protectedFile=Path.Combine(root,"blocked-output.sql");
        File.WriteAllText(protectedFile,"DO NOT OVERWRITE");
        var cli=Path.GetFullPath(Path.Combine(bin,"..","..","..","OpenDBDiff.CLI","bin","Debug","OpenDBDiff.CLI.exe"));
        var start=new System.Diagnostics.ProcessStartInfo(cli, "-b \""+Cs(before)+"\" -a \""+Cs(after)+"\" -o \""+protectedFile+"\"") { UseShellExecute=false, CreateNoWindow=true, RedirectStandardOutput=true };
        using(var process=System.Diagnostics.Process.Start(start))
        {
            var output=process.StandardOutput.ReadToEnd(); process.WaitForExit();
            Check(process.ExitCode==1 && File.ReadAllText(protectedFile)=="DO NOT OVERWRITE", "CLI blocks conversion without overwriting output");
        }
    }
}
