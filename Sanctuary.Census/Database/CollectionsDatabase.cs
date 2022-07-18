using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Sanctuary.Census.Database;

public class CollectionsDatabase
{
    private readonly IConfiguration _configuration;

    public CollectionsDatabase(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private IDbConnection CreateConnection()
        => new SqlConnection(_configuration.GetConnectionString("SqlConnection"));
}
