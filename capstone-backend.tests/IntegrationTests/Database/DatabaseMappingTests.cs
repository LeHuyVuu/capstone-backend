using capstone_backend.Data.Context;
using DotNetEnv;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace capstone_backend.tests.IntegrationTests.Database
{
    public class DatabaseMappingTests
    {
        private readonly IServiceProvider _services;

        public DatabaseMappingTests()
        {
            var conn = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION_STRING");
            var services = new ServiceCollection();
            services.AddDbContext<MyDbContext>(options =>
            {
                options.UseNpgsql(conn);
            });

            _services = services.BuildServiceProvider();
        }

        [Fact]
        public async Task All_tables_should_be_queryable()
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            var entities = db.Model.GetEntityTypes()
                .Where(e => !e.IsOwned())
                .Where(e => e.GetTableName() != null);

            foreach (var entity in entities)
            {
                var table = entity.GetTableName();
                var schema = entity.GetSchema() ?? "public";

                if (table == "collectionvenue_location")
                    break; // Skip join table

                var sql = $@"SELECT * FROM ""{schema}"".""{table}"" LIMIT 1";

                try
                {
                    await db.Database.ExecuteSqlRawAsync(sql);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to query table {schema}.{table}: {ex.Message}", ex);
                }
            }
        }
    }
}
