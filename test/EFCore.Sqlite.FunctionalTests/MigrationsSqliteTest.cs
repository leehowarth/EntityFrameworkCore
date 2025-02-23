// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Microsoft.EntityFrameworkCore
{
    public class MigrationsSqliteTest : MigrationsTestBase<MigrationsSqliteFixture>
    {
        public MigrationsSqliteTest(MigrationsSqliteFixture fixture)
            : base(fixture)
        {
        }

        public override void Can_generate_migration_from_initial_database_to_initial()
        {
            base.Can_generate_migration_from_initial_database_to_initial();

            Assert.Equal(
                @"CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
    ""MigrationId"" TEXT NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY,
    ""ProductVersion"" TEXT NOT NULL
);

",
                Sql,
                ignoreLineEndingDifferences: true);
        }

        public override void Can_generate_no_migration_script()
        {
            base.Can_generate_no_migration_script();

            Assert.Equal(
                @"CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
    ""MigrationId"" TEXT NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY,
    ""ProductVersion"" TEXT NOT NULL
);

",
                Sql,
                ignoreLineEndingDifferences: true);
        }

        public override void Can_generate_up_scripts()
        {
            base.Can_generate_up_scripts();

            Assert.Equal(
                @"CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
    ""MigrationId"" TEXT NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY,
    ""ProductVersion"" TEXT NOT NULL
);

CREATE TABLE ""Table1"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Table1"" PRIMARY KEY,
    ""Foo"" INTEGER NOT NULL
);

INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
VALUES ('00000000000001_Migration1', '7.0.0-test');

ALTER TABLE ""Table1"" RENAME COLUMN ""Foo"" TO ""Bar"";

INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
VALUES ('00000000000002_Migration2', '7.0.0-test');

INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
VALUES ('00000000000003_Migration3', '7.0.0-test');

",
                Sql,
                ignoreLineEndingDifferences: true);
        }

        public override void Can_generate_one_up_script()
        {
            base.Can_generate_one_up_script();

            Assert.Equal(
                @"ALTER TABLE ""Table1"" RENAME COLUMN ""Foo"" TO ""Bar"";

INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
VALUES ('00000000000002_Migration2', '7.0.0-test');

",
                Sql,
                ignoreLineEndingDifferences: true);
        }

        public override void Can_generate_up_script_using_names()
        {
            base.Can_generate_up_script_using_names();

            Assert.Equal(
                @"ALTER TABLE ""Table1"" RENAME COLUMN ""Foo"" TO ""Bar"";

INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
VALUES ('00000000000002_Migration2', '7.0.0-test');

",
                Sql,
                ignoreLineEndingDifferences: true);
        }

        public override void Can_generate_idempotent_up_scripts()
        {
            Assert.Throws<NotSupportedException>(() => base.Can_generate_idempotent_up_scripts());
        }

        public override void Can_generate_down_scripts()
        {
            base.Can_generate_down_scripts();

            Assert.Equal(
                @"ALTER TABLE ""Table1"" RENAME COLUMN ""Bar"" TO ""Foo"";

DELETE FROM ""__EFMigrationsHistory""
WHERE ""MigrationId"" = '00000000000002_Migration2';

DROP TABLE ""Table1"";

DELETE FROM ""__EFMigrationsHistory""
WHERE ""MigrationId"" = '00000000000001_Migration1';

",
                Sql,
                ignoreLineEndingDifferences: true);
        }

        public override void Can_generate_one_down_script()
        {
            base.Can_generate_one_down_script();

            Assert.Equal(
                @"ALTER TABLE ""Table1"" RENAME COLUMN ""Bar"" TO ""Foo"";

DELETE FROM ""__EFMigrationsHistory""
WHERE ""MigrationId"" = '00000000000002_Migration2';

",
                Sql,
                ignoreLineEndingDifferences: true);
        }

        public override void Can_generate_down_script_using_names()
        {
            base.Can_generate_down_script_using_names();

            Assert.Equal(
                @"ALTER TABLE ""Table1"" RENAME COLUMN ""Bar"" TO ""Foo"";

DELETE FROM ""__EFMigrationsHistory""
WHERE ""MigrationId"" = '00000000000002_Migration2';

",
                Sql,
                ignoreLineEndingDifferences: true);
        }

        public override void Can_generate_idempotent_down_scripts()
        {
            Assert.Throws<NotSupportedException>(() => base.Can_generate_idempotent_down_scripts());
        }

        public override void Can_get_active_provider()
        {
            base.Can_get_active_provider();

            Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", ActiveProvider);
        }

        protected override void AssertFirstMigration(DbConnection connection)
        {
            var sql = GetDatabaseSchemaAsync(connection);
            Assert.Equal(
                @"
CreatedTable
    Id INTEGER NOT NULL
    ColumnWithDefaultToDrop INTEGER NULL DEFAULT 0
    ColumnWithDefaultToAlter INTEGER NULL DEFAULT 1

Foos
    Id INTEGER NOT NULL

sqlite_sequence
    name  NULL
    seq  NULL
",
                sql,
                ignoreLineEndingDifferences: true);
        }

        protected override void BuildSecondMigration(MigrationBuilder migrationBuilder)
        {
            base.BuildSecondMigration(migrationBuilder);

            for (var i = migrationBuilder.Operations.Count - 1; i >= 0; i--)
            {
                var operation = migrationBuilder.Operations[i];
                if (operation is AlterColumnOperation
                    || operation is DropColumnOperation)
                {
                    migrationBuilder.Operations.RemoveAt(i);
                }
            }
        }

        protected override void AssertSecondMigration(DbConnection connection)
        {
            var sql = GetDatabaseSchemaAsync(connection);
            Assert.Equal(
                @"
CreatedTable
    Id INTEGER NOT NULL
    ColumnWithDefaultToDrop INTEGER NULL DEFAULT 0
    ColumnWithDefaultToAlter INTEGER NULL DEFAULT 1

Foos
    Id INTEGER NOT NULL

sqlite_sequence
    name  NULL
    seq  NULL
",
                sql,
                ignoreLineEndingDifferences: true);
        }

        private string GetDatabaseSchemaAsync(DbConnection connection)
        {
            var builder = new IndentedStringBuilder();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT name
                    FROM sqlite_master
                    WHERE type = 'table'
                    ORDER BY name;";

                var tables = new List<string>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }

                var first = true;
                foreach (var table in tables)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        builder.DecrementIndent();
                    }

                    builder
                        .AppendLine()
                        .AppendLine(table)
                        .IncrementIndent();

                    command.CommandText = "PRAGMA table_info(" + table + ");";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            builder
                                .Append(reader[1]) // Name
                                .Append(" ")
                                .Append(reader[2]) // Type
                                .Append(" ")
                                .Append(reader.GetBoolean(3) ? "NOT NULL" : "NULL");

                            if (!reader.IsDBNull(4))
                            {
                                builder
                                    .Append(" DEFAULT ")
                                    .Append(reader[4]);
                            }

                            builder.AppendLine();
                        }
                    }
                }
            }

            return builder.ToString();
        }

        public override void Can_diff_against_2_2_model()
        {
            using (var context = new ModelSnapshot22.BloggingContext())
            {
                var snapshot = new BloggingContextModelSnapshot22();
                var sourceModel = snapshot.Model;
                var targetModel = context.Model;

                var modelDiffer = context.GetService<IMigrationsModelDiffer>();
                var operations = modelDiffer.GetDifferences(sourceModel, targetModel);

                Assert.Equal(0, operations.Count);
            }
        }

        public class BloggingContextModelSnapshot22 : ModelSnapshot
        {
            protected override void BuildModel(ModelBuilder modelBuilder)
            {
#pragma warning disable 612, 618
                modelBuilder
                    .HasAnnotation("ProductVersion", "2.2.4-servicing-10062");

                modelBuilder.Entity(
                    "ModelSnapshot22.Blog", b =>
                    {
                        b.Property<int>("Id")
                            .ValueGeneratedOnAdd();

                        b.Property<string>("Name");

                        b.HasKey("Id");

                        b.ToTable("Blogs");
                    });

                modelBuilder.Entity(
                    "ModelSnapshot22.Post", b =>
                    {
                        b.Property<int>("Id")
                            .ValueGeneratedOnAdd();

                        b.Property<int?>("BlogId");

                        b.Property<string>("Content");

                        b.Property<DateTime>("EditDate");

                        b.Property<string>("Title");

                        b.HasKey("Id");

                        b.HasIndex("BlogId");

                        b.ToTable("Post");
                    });

                modelBuilder.Entity(
                    "ModelSnapshot22.Post", b =>
                    {
                        b.HasOne("ModelSnapshot22.Blog", "Blog")
                            .WithMany("Posts")
                            .HasForeignKey("BlogId");
                    });
#pragma warning restore 612, 618
            }
        }
    }
}

namespace ModelSnapshot22
{
    public class Blog
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<Post> Posts { get; set; }
    }

    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime EditDate { get; set; }

        public Blog Blog { get; set; }
    }

    public class BloggingContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlite("DataSource=Test.db");

        public DbSet<Blog> Blogs { get; set; }
    }
}

