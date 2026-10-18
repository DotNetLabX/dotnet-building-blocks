using System.Data.Common;

namespace System.Data.SqlClient;

internal sealed class SqlException(int number) : DbException("Legacy SQL Server error.")
{
    public int Number { get; } = number;
}
