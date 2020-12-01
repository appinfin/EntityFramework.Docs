---
title: Efficient Updating - EF Core
description: Performance guide for efficient updating using Entity Framework Core
author: roji
ms.date: 12/1/2020
uid: core/miscellaneous/efficient-updating
---
# Efficient Updating

## Batching

EF Core helps minimize roundtrips by automatically batching together all updates in a single roundtrip. Consider the following:

```csharp
var blog = context.Blogs.Single(b => b.Name == "EF Core Blog");
blog.Url = "http://some.new.website";
context.Add(new Blog { Name = "Another blog"});
context.Add(new Blog { Name = "Yet another blog"});
context.SaveChanges();
```

The above loads a blog from the database, changes its name, and then adds two new blogs; to apply this, two SQL INSERT statements and one UPDATE statement are sent to the database. Rather than sending them one by one, as Blog instances are added, EF Core tracks these changes internally, and executes them in a single roundtrip when <xref:Microsoft.EntityFrameworkContext.DbContext.SaveChanges> is called.

The number of statements that EF batches in a single roundtrip depends on the database provider being used. For example, performance analysis has shown batching to be generally less efficient for SQL Server when less than 4 statements are involved. Similarly, the benefits of batching degrade after around 40 statements for SQL Server, so EF Core will by default only execute up to 42 statements in a single batch, and execute additional statements in separate roundtrips.

Users can also tweak these thresholds to achieve potentially higher performance - but benchmark carefully before modifying these:

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    => optionsBuilder.UseSqlServer(@"...", o => o
        .MinBatchSize(1)
        .MaxBatchSize(100))
```

## Bulk updates

Let's assume you want to give all Employees of a certain department a raise. A typical implementation for this in EF Core would look like the following

```csharp
foreach (var employee in context.Employees.Where(e => e.Department.Id == 10))
{
    employee.Salary += 1000;
}
context.SaveChanges();
```

While this is perfectly valid code, let's analyze what it does from a performance perspective:

* A database roundtrip is performed, to load all the relevant employees; note that this brings all the Employees' row data to the client, even if only the salary will be needed.
* EF Core's change tracking creates snapshots when loading the entities, and then compares those snapshots to the instances to find out which properties changed.
* A second database roundtrip is perform to save all the changes. While all changes are done in a single roundtrip thanks to batching, EF Core still sends an UPDATE statement per employee, which must be executed by the database.

Relational databases also support *bulk updates*, so the above could be rewritten as the following single SQL statement:

```sql
UPDATE [Employees] SET [Salary] = [Salary] + 1000 WHERE [DepartmentId] = 10;
```

This performs the entire operation in a single roundtrip, without loading or sending any actual data to the database, and without making use of EF's change tracking machinery, which does have an overhead cost.

Unfortunately, EF doesn't currently provide APIs for performing bulk updates. Until these are introduced, you can use raw SQL to perform the operation where performance is sensitive:

```csharp
context.Database.ExecuteSqlRaw("UPDATE [Employees] SET [Salary] = [Salary] + 1000 WHERE [DepartmentId] = {0}", departmentId);
```
