using Microsoft.Data.SqlClient;
using PragmaticScaffolder.Core.Models;

namespace PragmaticScaffolder.Core.Services;

/// <summary>Reads database schema from SQL Server via information_schema and sys views.</summary>
public sealed class SqlServerSchemaReader
{
    public async Task<bool> TestConnectionAsync(string connectionString, CancellationToken ct = default)
    {
        try
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<DatabaseMetadata> ReadDatabaseAsync(string connectionString, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var db = new DatabaseMetadata
        {
            DatabaseName = conn.Database,
            ServerVersion = conn.ServerVersion ?? string.Empty
        };

        var schemas = await ReadSchemasAsync(conn, ct);
        foreach (var schema in schemas)
            schema.Tables = await ReadTablesAsync(conn, schema.Name, ct);

        db.Schemas = schemas;
        return db;
    }

    private static async Task<List<SchemaMetadata>> ReadSchemasAsync(SqlConnection conn, CancellationToken ct)
    {
        const string sql = """
            SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA
            WHERE SCHEMA_NAME NOT IN ('information_schema','guest','sys')
            ORDER BY SCHEMA_NAME
            """;

        var schemas = new List<SchemaMetadata>();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            schemas.Add(new SchemaMetadata { Name = reader.GetString(0) });
        return schemas;
    }

    private static async Task<List<TableMetadata>> ReadTablesAsync(
        SqlConnection conn, string schema, CancellationToken ct)
    {
        const string sql = """
            SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @schema AND TABLE_TYPE = 'BASE TABLE'
              AND TABLE_NAME NOT LIKE '\_%' ESCAPE '\'
            ORDER BY TABLE_NAME
            """;

        var tables = new List<TableMetadata>();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);

        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct))
                tables.Add(new TableMetadata { Schema = schema, Name = reader.GetString(0) });

        foreach (var table in tables)
        {
            table.Columns = await ReadColumnsAsync(conn, schema, table.Name, ct);
            table.ForeignKeys = await ReadForeignKeysAsync(conn, schema, table.Name, ct);
        }

        return tables;
    }

    private static async Task<List<ColumnMetadata>> ReadColumnsAsync(
        SqlConnection conn, string schema, string tableName, CancellationToken ct)
    {
        const string sql = """
            SELECT
                c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH,
                c.NUMERIC_PRECISION, c.NUMERIC_SCALE, c.IS_NULLABLE,
                c.COLUMN_DEFAULT, c.ORDINAL_POSITION,
                COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA+'.'+c.TABLE_NAME), c.COLUMN_NAME,'IsIdentity') AS IS_IDENTITY,
                COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA+'.'+c.TABLE_NAME), c.COLUMN_NAME,'IsComputed') AS IS_COMPUTED,
                CASE WHEN kcu.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_PK
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                ON kcu.TABLE_SCHEMA = c.TABLE_SCHEMA AND kcu.TABLE_NAME = c.TABLE_NAME
                AND kcu.COLUMN_NAME = c.COLUMN_NAME
                AND kcu.CONSTRAINT_NAME IN (
                    SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                    WHERE TABLE_SCHEMA = c.TABLE_SCHEMA AND TABLE_NAME = c.TABLE_NAME
                      AND CONSTRAINT_TYPE = 'PRIMARY KEY')
            WHERE c.TABLE_SCHEMA = @schema AND c.TABLE_NAME = @table
            ORDER BY c.ORDINAL_POSITION
            """;

        var columns = new List<ColumnMetadata>();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", tableName);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dataType = reader.GetString(1);
            var isNullable = reader.GetString(5) == "YES";
            var col = new ColumnMetadata
            {
                Name = reader.GetString(0),
                DataType = dataType,
                MaxLength = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                Precision = reader.IsDBNull(3) ? null : (int)reader.GetByte(3),
                Scale = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                IsNullable = isNullable,
                DefaultValue = reader.IsDBNull(6) ? null : reader.GetString(6),
                OrdinalPosition = reader.GetInt32(7),
                IsIdentity = reader.GetInt32(8) == 1,
                IsComputed = reader.GetInt32(9) == 1,
                IsPrimaryKey = reader.GetInt32(10) == 1
            };
            (col.ClrType, col.IsNullableClrType) = MapClrType(dataType, isNullable);
            columns.Add(col);
        }
        return columns;
    }

    private static async Task<List<ForeignKeyMetadata>> ReadForeignKeysAsync(
        SqlConnection conn, string schema, string tableName, CancellationToken ct)
    {
        const string sql = """
            SELECT
                fk.name,
                COL_NAME(fkc.parent_object_id, fkc.parent_column_id),
                OBJECT_SCHEMA_NAME(fkc.referenced_object_id),
                OBJECT_NAME(fkc.referenced_object_id),
                COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id)
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) = @schema
              AND OBJECT_NAME(fk.parent_object_id) = @table
            ORDER BY fk.name
            """;

        var fks = new List<ForeignKeyMetadata>();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", tableName);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            fks.Add(new ForeignKeyMetadata
            {
                Name = reader.GetString(0),
                ColumnName = reader.GetString(1),
                ReferencedSchema = reader.GetString(2),
                ReferencedTable = reader.GetString(3),
                ReferencedColumn = reader.GetString(4)
            });
        return fks;
    }

    private static (string clrType, bool isNullable) MapClrType(string sqlType, bool isNullable)
    {
        var (clr, _) = sqlType.ToLowerInvariant() switch
        {
            "bigint"                                                         => ("long", true),
            "int"                                                            => ("int", true),
            "smallint"                                                       => ("short", true),
            "tinyint"                                                        => ("byte", true),
            "bit"                                                            => ("bool", true),
            "decimal" or "numeric" or "money" or "smallmoney"               => ("decimal", true),
            "float"                                                          => ("double", true),
            "real"                                                           => ("float", true),
            "datetime" or "smalldatetime" or "datetime2"                    => ("DateTime", true),
            "date"                                                           => ("DateOnly", true),
            "time"                                                           => ("TimeOnly", true),
            "datetimeoffset"                                                 => ("DateTimeOffset", true),
            "uniqueidentifier"                                               => ("Guid", true),
            "binary" or "varbinary" or "image" or "timestamp" or "rowversion" => ("byte[]", false),
            "char" or "nchar" or "varchar" or "nvarchar" or "text" or "ntext" or "xml" => ("string", false),
            _                                                                => ("object", false)
        };
        var fullClr = isNullable ? $"{clr}?" : clr;
        return (fullClr, isNullable);
    }
}
