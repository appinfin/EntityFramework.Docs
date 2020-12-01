// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class RelatedEntityLoading
    {
        [Params(RelatedEntityLoadingMode.Eager, RelatedEntityLoadingMode.Explicit, RelatedEntityLoadingMode.Lazy)]
        public RelatedEntityLoadingMode RelatedEntityLoadingMode { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            if (RelatedEntityLoadingMode == RelatedEntityLoadingMode.Lazy)
            {
                using var context = new LazyBloggingContext();
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
                context.SeedData();
            }
            else
            {
                using var context = new BloggingContext();
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
                context.SeedData();
            }
        }

        [Benchmark]
        public void NoRelated()
        {

        }

        [Benchmark]
        public void Lazy()
        {

        }

        #region Non-lazy

        public class BloggingContext : DbContext
        {
            public DbSet<Blog> Blogs { get; set; }
            public DbSet<Post> Posts { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseSqlServer(
                    @"Server=localhost;Database=test;User=SA;Password=Abcd5678;Connect Timeout=60;ConnectRetryCount=0");
                // @"Server=(localdb)\mssqllocaldb;Database=Blogging;Integrated Security=True");
            }

            public void SeedData()
            {

            }
        }

        public class Blog
        {
            public int BlogId { get; set; }
            public string Url { get; set; }
            public int Rating { get; set; }
            public List<Post> Posts { get; set; }
        }

        public class Post
        {
            public int PostId { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }

            public int BlogId { get; set; }
            public Blog Blog { get; set; }
        }

        #endregion Non-lazy

        #region Lazy

        public class LazyBloggingContext : DbContext
        {
            public DbSet<Blog> Blogs { get; set; }
            public DbSet<Post> Posts { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseSqlServer(
                    @"Server=localhost;Database=test;User=SA;Password=Abcd5678;Connect Timeout=60;ConnectRetryCount=0");
                // @"Server=(localdb)\mssqllocaldb;Database=Blogging;Integrated Security=True");
            }

            public void SeedData()
            {

            }
        }

        public class LazyBlog
        {
            public int BlogId { get; set; }
            public string Url { get; set; }
            public int Rating { get; set; }
            public virtual List<LazyPost> Posts { get; set; }
        }

        public class LazyPost
        {
            public int PostId { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }

            public int BlogId { get; set; }
            public virtual Blog Blog { get; set; }
        }

        #endregion Lazy
    }

    public enum RelatedEntityLoadingMode
    {
        Eager,
        Explicit,
        Lazy
    }
}
