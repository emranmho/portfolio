---
title: "How I cut SQL Server query time 40%"
summary: "A production slow-query hunt at ReliSource: reading the actual execution plan, killing the implicit conversion, and proving the fix with before/after numbers."
date: 2026-07-08
tags: [sql-server, performance, war-story]
---

# How I cut SQL Server query time 40%

Resume bullets compress badly. "Tuned SQL Server indexing and stored procedures, cutting
average query time by ~40% on high-traffic tables" tells you nothing about *how* — and the
how is the transferable part. This is the full version of that bullet, from my time on the
site-reliability side of a .NET microservices stack at ReliSource.

## The symptom

A core listing endpoint on one of our highest-traffic tables had crept from acceptable to
painful. Tracing put nearly all of the request time in a single SQL Server query — the kind
of endpoint that looks fine in a demo and falls over under real production load, sanitized
here since the schema and business domain aren't mine to publish.

The first rule of a slow-query hunt: don't guess. SQL Server will tell you exactly what it
did — if you ask for the *actual* execution plan, not the estimated one.

## Reading the plan, not the query

The query *looked* fine. The plan said otherwise. Three findings, in increasing order of
impact:

### 1. The implicit conversion

The plan flagged a warning on the seek predicate:

```
CONVERT_IMPLICIT(nvarchar(50), [t].[CustomerRef]) = @p0
```

The column was `varchar`; the parameter arrived from the ORM as `nvarchar`. Because
`nvarchar` has higher type precedence, SQL Server converted **the column, for every row**
— which makes the predicate non-sargable and turns an index seek into an index scan. One
line in the mapping configuration (forcing the parameter to `varchar` /
`HasColumnType`) turned the scan back into a seek.

This is the single most common self-inflicted SQL Server wound in .NET codebases, because
.NET strings are UTF-16 and ORMs default string parameters to `nvarchar`.

### 2. The key lookup storm

The plan showed an index seek feeding a **key lookup** executed once per row in the result
set — the index found the rows but didn't *cover* the query, so every row went back to the
clustered index for the remaining columns. On a high-traffic table that lookup dominates the
plan's cost. Fix: add the hot columns as `INCLUDE` columns on the existing index rather than
widening the key.

```sql
CREATE NONCLUSTERED INDEX IX_Orders_CustomerRef
    ON dbo.Orders (CustomerRef, CreatedAt DESC)
    INCLUDE (Status, Total)
    WITH (DROP_EXISTING = ON);
```

### 3. Stale statistics on a skewed column

The plan's estimated-vs-actual row counts on one operator were off by an order of magnitude.
When the optimizer is that wrong, every downstream choice (join strategy, memory grant) is
wrong with it. The column had heavy skew and stats hadn't updated since a bulk import.
`UPDATE STATISTICS ... WITH FULLSCAN` plus a scheduled job for the skewed tables closed the
gap.

## Proving it

Never trust a single warm-cache run. The before/after protocol:

1. Capture the workload in **Query Store** for a week before touching anything.
2. Apply one change at a time — the point is knowing which change paid.
3. Compare the same week-slice after: p50/p95 duration, logical reads, plan stability.

Net effect across the high-traffic tables this technique was applied to: average query time
down **~40%**, measured from Query Store over real production workload, not a synthetic
benchmark — the number in the title.

## What I'd generalize

- **Actual plans or nothing.** The estimated plan hides exactly the problems that matter
  (conversions, cardinality misses, lookup counts).
- **Sargability beats hardware.** Both big wins were "let the index do its job" fixes, not
  rewrites.
- **One change per measurement.** Three fixes applied at once is one anecdote; applied
  separately it's three lessons.
- **Measure at the API boundary.** Users experience the endpoint, not the query. The
  status page philosophy of this site comes from the same place.
