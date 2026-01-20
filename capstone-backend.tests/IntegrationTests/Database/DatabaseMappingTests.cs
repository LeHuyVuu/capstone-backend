using capstone_backend.Data.Context;
using DotNetEnv;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                options.UseNpgsql(conn).UseSnakeCaseNamingConvention();
            });

            _services = services.BuildServiceProvider();
        }

        [Fact]
        public async Task All_tables_should_be_queryable()
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            // Lấy tất cả Entity có trong DbContext
            var entityTypes = db.Model.GetEntityTypes()
                .Where(e => !e.IsOwned())
                .Where(e => e.GetTableName() != null)
                .Where(e => e.ClrType != null);

            foreach (var entity in entityTypes)
            {

                if (entity.GetTableName() == "collectionvenue_location")
                    continue;

                // Mục đích: Gọi hàm CheckEntity<T>(db) với T là kiểu của entity hiện tại
                var method = this.GetType()
                    .GetMethod(nameof(CheckEntity), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(entity.ClrType);

                try
                {
                    // Invoke hàm CheckEntity
                    await (Task)method.Invoke(this, new object[] { db })!;
                }
                catch (Exception ex)
                {

                    throw new Exception($"[ERROR] Error Mapping Table '{entity.GetTableName()}' (Class: {entity.ClrType.Name}): {ex.InnerException?.Message ?? ex.Message}", ex);
                }
            }
        }

        // Helper method: Ép EF Core thực hiện query SELECT mapping
        private async Task CheckEntity<T>(MyDbContext db) where T : class
        {
            await db.Set<T>().AsNoTracking().FirstOrDefaultAsync();
        }
    }
}
