// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Cosmos.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Cosmos.Infrastructure;
using Microsoft.EntityFrameworkCore.Cosmos.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Cosmos.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Cosmos.Query.Internal;
using Microsoft.EntityFrameworkCore.Cosmos.Query.Pipeline;
using Microsoft.EntityFrameworkCore.Cosmos.Storage.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Pipeline;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Utilities;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class CosmosServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityFrameworkCosmos([NotNull] this IServiceCollection serviceCollection)
        {
            Check.NotNull(serviceCollection, nameof(serviceCollection));

            var builder = new EntityFrameworkServicesBuilder(serviceCollection)
                .TryAdd<LoggingDefinitions, CosmosLoggingDefinitions>()
                .TryAdd<IDatabaseProvider, DatabaseProvider<CosmosOptionsExtension>>()
                .TryAdd<IDatabase, CosmosDatabaseWrapper>()
                .TryAdd<IExecutionStrategyFactory, CosmosExecutionStrategyFactory>()
                .TryAdd<IDbContextTransactionManager, CosmosTransactionManager>()
                .TryAdd<IModelValidator, CosmosModelValidator>()
                .TryAdd<IProviderConventionSetBuilder, CosmosConventionSetBuilder>()
                .TryAdd<IDatabaseCreator, CosmosDatabaseCreator>()
                .TryAdd<IQueryContextFactory, CosmosQueryContextFactory>()
                .TryAdd<ITypeMappingSource, CosmosTypeMappingSource>()

                // New Query pipeline
                .TryAdd<IQueryableMethodTranslatingExpressionVisitorFactory, CosmosQueryableMethodTranslatingExpressionVisitorFactory>()
                .TryAdd<IShapedQueryCompilingExpressionVisitorFactory, CosmosShapedQueryCompilingExpressionVisitorFactory>()

                .TryAdd<ISingletonOptions, ICosmosSingletonOptions>(p => p.GetService<ICosmosSingletonOptions>())
                .TryAddProviderSpecificServices(
                    b => b
                        .TryAddSingleton<ICosmosSingletonOptions, CosmosSingletonOptions>()
                        .TryAddSingleton<SingletonCosmosClientWrapper, SingletonCosmosClientWrapper>()
                        .TryAddSingleton<ISqlExpressionFactory, SqlExpressionFactory>()
                        .TryAddSingleton<IQuerySqlGeneratorFactory, QuerySqlGeneratorFactory>()
                        .TryAddSingleton<IMethodCallTranslatorProvider, CosmosMethodCallTranslatorProvider>()
                        .TryAddSingleton<IMemberTranslatorProvider, CosmosMemberTranslatorProvider>()
                        .TryAddScoped<CosmosClientWrapper, CosmosClientWrapper>()
                );

            builder.TryAddCoreServices();

            return serviceCollection;
        }
    }
}
