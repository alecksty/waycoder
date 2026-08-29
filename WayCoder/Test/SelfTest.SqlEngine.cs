using WayCoder.Sql;

namespace WayCoder;

public static partial class SelfTest
{
    private static void TestChunk18(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[精简 SQL 引擎：建表/插入/查询]");
        var db = new SqlDatabase();
        var r = db.Execute(
            "CREATE TABLE users (id INTEGER, name TEXT, age INTEGER); " +
            "INSERT INTO users VALUES (1, 'alice', 30), (2, 'bob', 25), (3, NULL, 40);");
        Check("多语句建表+插入", r.Contains("已创建表 users") && r.Contains("已插入 3 行"));

        var sel = db.Execute("SELECT * FROM users;");
        Check("SELECT * 返回表头与数据", sel.Contains("id") && sel.Contains("name") && sel.Contains("age") && sel.Contains("alice") && sel.Contains("bob") && sel.Contains("NULL"));

        Check("SELECT 指定列", db.Execute("SELECT name FROM users WHERE age > 26;").Contains("alice"));

        Section("[精简 SQL 引擎：WHERE 表达式]");
        Check("WHERE IS NULL", db.Execute("SELECT id FROM users WHERE name IS NULL;").Contains("3"));
        Check("WHERE IS NOT NULL", db.Execute("SELECT id FROM users WHERE name IS NOT NULL;").Contains("1") && db.Execute("SELECT id FROM users WHERE name IS NOT NULL;").Contains("2"));
        Check("WHERE AND", db.Execute("SELECT name FROM users WHERE age > 20 AND age < 35;").Contains("alice") && db.Execute("SELECT name FROM users WHERE age > 20 AND age < 35;").Contains("bob"));
        Check("WHERE OR", db.Execute("SELECT id FROM users WHERE id = 1 OR id = 3;").Contains("1") && db.Execute("SELECT id FROM users WHERE id = 1 OR id = 3;").Contains("3"));
        Check("WHERE LIKE a%", db.Execute("SELECT name FROM users WHERE name LIKE 'a%';").Contains("alice"));
        Check("WHERE LIKE %o%", db.Execute("SELECT name FROM users WHERE name LIKE '%o%';").Contains("bob"));
        Check("WHERE IN", db.Execute("SELECT name FROM users WHERE age IN (25, 40);").Contains("bob"));
        Check("WHERE != ", db.Execute("SELECT name FROM users WHERE id != 2;").Contains("alice"));

        Section("[精简 SQL 引擎：排序/分页]");
        Check("ORDER BY DESC", db.Execute("SELECT name FROM users ORDER BY age DESC;").IndexOf("bob") > db.Execute("SELECT name FROM users ORDER BY age DESC;").IndexOf("alice"));
        Check("ORDER BY 列序号", db.Execute("SELECT name FROM users ORDER BY 1;").Contains("alice"));
        Check("LIMIT", db.Execute("SELECT * FROM users ORDER BY id LIMIT 2;").Contains("alice") && !db.Execute("SELECT * FROM users ORDER BY id LIMIT 2;").Contains("NULL"));
        Check("LIMIT OFFSET", db.Execute("SELECT name FROM users ORDER BY id LIMIT 1 OFFSET 1;").Contains("bob"));
        Check("LIMIT m,n", db.Execute("SELECT name FROM users ORDER BY id LIMIT 1, 1;").Contains("bob"));

        Section("[精简 SQL 引擎：聚合]");
        Check("COUNT(*)", db.Execute("SELECT COUNT(*) FROM users;").Contains("3"));
        Check("COUNT(col) 跳过 NULL", db.Execute("SELECT COUNT(name) FROM users;").Contains("2"));
        Check("SUM", db.Execute("SELECT SUM(age) FROM users;").Contains("95"));
        Check("AVG", db.Execute("SELECT AVG(age) FROM users;").Contains("31"));
        Check("MIN", db.Execute("SELECT MIN(age) FROM users;").Contains("25"));
        Check("MAX", db.Execute("SELECT MAX(age) FROM users;").Contains("40"));

        Section("[精简 SQL 引擎：改/删]");
        Check("UPDATE 算术", db.Execute("UPDATE users SET age = age + 1 WHERE name = 'bob';").Contains("已影响 1 行"));
        Check("UPDATE 后查询", db.Execute("SELECT age FROM users WHERE name = 'bob';").Contains("26"));
        Check("DELETE", db.Execute("DELETE FROM users WHERE name IS NULL;").Contains("已删除 1 行"));
        Check("DELETE 后 COUNT", db.Execute("SELECT COUNT(*) FROM users;").Contains("2"));

        Section("[精简 SQL 引擎：字符串转义]");
        var esc = new SqlDatabase();
        esc.Execute("CREATE TABLE t (a TEXT); INSERT INTO t VALUES ('it''s;fine');");
        Check("'' 转义不被分号拆断", esc.Execute("SELECT * FROM t;").Contains("it's;fine"));

        Section("[精简 SQL 引擎：持久化无损往返]");
        var tmp = Path.Combine(Path.GetTempPath(), "wc_sql_" + Guid.NewGuid().ToString("N")[..8] + ".db");
        try
        {
            var p = new SqlDatabase();
            p.Execute("CREATE TABLE mix (i INTEGER, d REAL, s TEXT, n TEXT); INSERT INTO mix VALUES (9223372036854775807, 3.14159, '中文', NULL);");
            p.Save(tmp);
            Check("保存文件存在", File.Exists(tmp));

            var q = SqlDatabase.Load(tmp);
            var outStr = q.Execute("SELECT * FROM mix;");
            Check("long 无损", outStr.Contains("9223372036854775807"));
            Check("double 无损", outStr.Contains("3.14159"));
            Check("string 无损", outStr.Contains("中文"));
            Check("null 无损", outStr.Contains("NULL"));
        }
        finally { try { File.Delete(tmp); } catch { } }
    }
}
