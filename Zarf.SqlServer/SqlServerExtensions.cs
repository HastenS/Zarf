﻿using Microsoft.Extensions.DependencyInjection;
using Zarf.Core;
using Zarf.Generators;
using Zarf.SqlServer.Generators;

namespace Zarf.SqlServer
{
    public static class SqlServerExtensions
    {
        public static IDbService UseSqlServer(this IServiceCollection serviceCollection, string connectionString)
        {
            return new SqlServerDbServiceBuilder().BuildService(connectionString, serviceCollection);
        }

        public static IDbService UseSqlServer(this IDbServiceBuilder serviceBuilder, string connectionString)
        {
            return new ServiceCollection().AddSqlServer().UseSqlServer(connectionString);
        }

        internal static IServiceCollection AddSqlServer(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ISQLGenerator, SqlServerGenerator>();

            serviceCollection.AddScoped<IDbEntityCommandFacotry>(
                p => new SqlServerDbEntityCommandFactory(p.GetService<IDbEntityConnectionFacotry>()));

            serviceCollection.AddSingleton<IDbServiceBuilder, SqlServerDbServiceBuilder>();

            return serviceCollection.AddZarf();
        }
    }
}
