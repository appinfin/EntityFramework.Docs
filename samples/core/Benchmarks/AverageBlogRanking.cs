// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class AverageBlogRanking
    {
        public const int NumBlogs = 1000;

        [GlobalSetup]
        public void Setup()
        {
            using var context = new BloggingContext();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            context.SeedData();
        }

        #region Benchmarks
        [Benchmark]
        public double LoadEntities()
        {
            var sum = 0;
            var count = 0;
            using var ctx = new BloggingContext();
            foreach (var blog in ctx.Blogs)
            {
                sum += blog.Rating;
                count++;
            }

            return sum / count;
        }

        [Benchmark]
        public double LoadEntitiesNonTracking()
        {
            var sum = 0;
            var count = 0;
            using var ctx = new BloggingContext();
            foreach (var blog in ctx.Blogs.AsNoTracking())
            {
                sum += blog.Rating;
                count++;
            }

            return sum / count;
        }

        [Benchmark]
        public double ProjectOnlyRanking()
        {
            var sum = 0;
            var count = 0;
            using var ctx = new BloggingContext();
            foreach (var rating in ctx.Blogs.Select(b => b.Rating))
            {
                sum += rating;
                count++;
            }

            return sum / count;
        }

        [Benchmark(Baseline = true)]
        public double CalculateInDatabase()
        {
            using var ctx = new BloggingContext();
            return ctx.Blogs.Average(b => b.Rating);
        }
        #endregion Benchmarks

        public class BloggingContext : DbContext
        {
            public DbSet<Blog> Blogs { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseSqlServer(
                    @"Server=localhost;Database=test;User=SA;Password=Abcd5678;Connect Timeout=60;ConnectRetryCount=0");
                // @"Server=(localdb)\mssqllocaldb;Database=Blogging;Integrated Security=True");
            }

            public void SeedData()
            {
                Blogs.AddRange(
                    Enumerable.Range(0, NumBlogs).Select(i => new Blog
                    {
                        Name = $"Blog{i}",
                        Url = $"blog{i}.blogs.net",
                        CreationTime = new DateTime(2020, 1, 1),
                        Rating = i % 5
                    }));
                SaveChanges();
            }
        }

        public class Blog
        {
            public int BlogId { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
            public DateTime CreationTime { get; set; }
            public int Rating { get; set; }
        }
    }
}
