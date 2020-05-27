Purifier for SQL Server sp_executesql operations
================================================

This simple desktop application converts SQL Server scripts that uses ``sp_executesql`` to simple ``SELECT`` queries.

```sql
exec sp_executesql N'SELECT * FROM [my_table] WHERE [id] = @id',N'@id bigint',@id=241
```

```sql
exec sp_executesql N'
update [history] set stop = @stop_time where id = @id
',N'@stop_time datetime,@id bigint',@stop_time='2020-05-26 03:25:10.500',@id=9200651
```
