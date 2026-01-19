using AuraORM.Tests.Fixtures;

namespace AuraORM.Tests.Tests.Query;

public class RawSqlQueryTests
{
    private readonly TestFixture _fixture = new();

    [Fact]
    public void パラメータなしの基本的な生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = "SELECT * FROM users;";

        // Act
        var query = _fixture.Db.RawSql(sqlText);
        var (resultSqlText, parameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void 一つのパラメータを持つ生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = "SELECT * FROM users WHERE id = @id;";
        var parameters = new List<(string Name, object? Value)> { ("id", 1) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void 複数のパラメータを持つ生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = "SELECT * FROM users WHERE name = @name AND age = @age;";
        var parameters = new List<(string Name, object? Value)>
        {
            ("name", "John"),
            ("age", 25)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void NULLパラメータを含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = "SELECT * FROM users WHERE deleted_at IS NULL OR deleted_at = @deletedAt;";
        var parameters = new List<(string Name, object? Value)> { ("deletedAt", null) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void 複雑なJOINを含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            SELECT u.id, u.name, o.order_id, o.total
            FROM users u
            INNER JOIN orders o ON u.id = o.user_id
            WHERE u.age > @minAge
            ORDER BY o.total DESC;";
        var parameters = new List<(string Name, object? Value)> { ("minAge", 18) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void サブクエリを含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            SELECT * FROM users
            WHERE id IN (
                SELECT user_id FROM orders
                WHERE total > @minTotal
            );";
        var parameters = new List<(string Name, object? Value)> { ("minTotal", 1000) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void パラメータがnullの場合でも生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = "SELECT * FROM users;";

        // Act
        var query = _fixture.Db.RawSql(sqlText, null);
        var (resultSqlText, parameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void 異なる型のパラメータを持つ生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = "SELECT * FROM users WHERE name = @name AND age = @age AND created_at = @createdAt AND is_active = @isActive;";
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0);
        var parameters = new List<(string Name, object? Value)>
        {
            ("name", "John"),
            ("age", 25),
            ("createdAt", createdAt),
            ("isActive", true)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void UPDATE文を含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = "UPDATE users SET name = @name, age = @age WHERE id = @id;";
        var parameters = new List<(string Name, object? Value)>
        {
            ("name", "Jane"),
            ("age", 30),
            ("id", 1)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void INSERT文を含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = "INSERT INTO users (name, age) VALUES (@name, @age);";
        var parameters = new List<(string Name, object? Value)>
        {
            ("name", "Bob"),
            ("age", 35)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void DELETE文を含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = "DELETE FROM users WHERE id = @id;";
        var parameters = new List<(string Name, object? Value)> { ("id", 1) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void ウィンドウ関数ROW_NUMBERを含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            SELECT 
                id, 
                name, 
                age,
                ROW_NUMBER() OVER (PARTITION BY department_id ORDER BY age DESC) as rank
            FROM users
            WHERE department_id = @departmentId;";
        var parameters = new List<(string Name, object? Value)> { ("departmentId", 10) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void 複数のウィンドウ関数を含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            SELECT 
                id,
                name,
                total,
                ROW_NUMBER() OVER (PARTITION BY user_id ORDER BY total DESC) as order_rank,
                SUM(total) OVER (PARTITION BY user_id) as user_total,
                LAG(total, 1) OVER (PARTITION BY user_id ORDER BY created_at) as prev_total,
                LEAD(total, 1) OVER (PARTITION BY user_id ORDER BY created_at) as next_total
            FROM orders
            WHERE user_id = @userId;";
        var parameters = new List<(string Name, object? Value)> { ("userId", 1) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void 再帰CTEを含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            WITH RECURSIVE category_tree AS (
                SELECT id, name, parent_id, 0 as level
                FROM categories
                WHERE id = @rootId
                UNION ALL
                SELECT c.id, c.name, c.parent_id, ct.level + 1
                FROM categories c
                INNER JOIN category_tree ct ON c.parent_id = ct.id
            )
            SELECT * FROM category_tree
            WHERE level <= @maxLevel;";
        var parameters = new List<(string Name, object? Value)>
        {
            ("rootId", 1),
            ("maxLevel", 5)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void GROUPING_SETSを含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            SELECT 
                COALESCE(department_id, -1) as department_id,
                COALESCE(region, 'ALL') as region,
                SUM(salary) as total_salary,
                COUNT(*) as employee_count
            FROM employees
            WHERE hire_date >= @startDate
            GROUP BY GROUPING SETS (
                (department_id, region),
                (department_id),
                (region),
                ()
            )
            ORDER BY department_id, region;";
        var startDate = new DateTime(2020, 1, 1);
        var parameters = new List<(string Name, object? Value)> { ("startDate", startDate) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void ROLLUPを含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            SELECT 
                year,
                quarter,
                month,
                SUM(revenue) as total_revenue
            FROM sales
            WHERE year = @year
            GROUP BY ROLLUP (year, quarter, month)
            ORDER BY year, quarter, month;";
        var parameters = new List<(string Name, object? Value)> { ("year", 2024) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void 複雑なCASE式と条件分岐を含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            SELECT 
                id,
                name,
                CASE 
                    WHEN age < 18 THEN '未成年'
                    WHEN age BETWEEN 18 AND 65 THEN '成人'
                    ELSE '高齢者'
                END as age_category,
                CASE 
                    WHEN total > @highThreshold THEN '高額'
                    WHEN total > @mediumThreshold THEN '中額'
                    ELSE '低額'
                END as price_category,
                CASE status
                    WHEN 1 THEN 'アクティブ'
                    WHEN 2 THEN '保留中'
                    WHEN 3 THEN '完了'
                    ELSE '不明'
                END as status_text
            FROM orders
            WHERE user_id = @userId;";
        var parameters = new List<(string Name, object? Value)>
        {
            ("highThreshold", 10000),
            ("mediumThreshold", 5000),
            ("userId", 1)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void 複数のJOINと複雑な条件を含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            SELECT 
                u.id,
                u.name,
                p.product_name,
                oi.quantity,
                oi.price,
                s.store_name,
                r.rating
            FROM users u
            LEFT JOIN orders o ON u.id = o.user_id AND o.status = @orderStatus
            INNER JOIN order_items oi ON o.id = oi.order_id
            INNER JOIN products p ON oi.product_id = p.id
            LEFT JOIN stores s ON p.store_id = s.id
            LEFT JOIN reviews r ON u.id = r.user_id AND p.id = r.product_id
            WHERE u.created_at >= @startDate
            ORDER BY o.created_at DESC, oi.quantity DESC;";
        var startDate = new DateTime(2024, 1, 1);
        var parameters = new List<(string Name, object? Value)>
        {
            ("orderStatus", 1),
            ("startDate", startDate)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void EXISTS句と相関サブクエリを含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            SELECT u.*
            FROM users u
            WHERE EXISTS (
                SELECT 1
                FROM orders o
                WHERE o.user_id = u.id
                AND o.total > @minOrderTotal
                AND o.created_at >= @startDate
            )
            AND NOT EXISTS (
                SELECT 1
                FROM banned_users bu
                WHERE bu.user_id = u.id
            );";
        var startDate = new DateTime(2024, 1, 1);
        var parameters = new List<(string Name, object? Value)>
        {
            ("minOrderTotal", 1000),
            ("startDate", startDate)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void UNION_ALLを含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            SELECT id, name, 'user' as type, created_at
            FROM users
            WHERE created_at >= @startDate
            UNION ALL
            SELECT id, name, 'admin' as type, created_at
            FROM admins
            WHERE created_at >= @startDate
            ORDER BY created_at DESC
            LIMIT @limit;";
        var startDate = new DateTime(2024, 1, 1);
        var parameters = new List<(string Name, object? Value)>
        {
            ("startDate", startDate),
            ("limit", 100)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void 複雑なHAVING句を含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            SELECT 
                department_id,
                COUNT(*) as employee_count,
                AVG(salary) as avg_salary,
                SUM(salary) as total_salary
            FROM employees
            WHERE hire_date >= @startDate
            GROUP BY department_id
            HAVING 
                COUNT(*) > @minEmployeeCount
                AND AVG(salary) > @minAvgSalary
                AND SUM(salary) BETWEEN @minTotalSalary AND @maxTotalSalary
            ORDER BY avg_salary DESC;";
        var startDate = new DateTime(2020, 1, 1);
        var parameters = new List<(string Name, object? Value)>
        {
            ("startDate", startDate),
            ("minEmployeeCount", 5),
            ("minAvgSalary", 50000),
            ("minTotalSalary", 100000),
            ("maxTotalSalary", 1000000)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void 複雑な文字列関数と日付関数を含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            SELECT 
                id,
                CONCAT(first_name, ' ', last_name) as full_name,
                UPPER(SUBSTRING(email, 1, POSITION('@' IN email) - 1)) as email_prefix,
                DATE_TRUNC('month', created_at) as registration_month,
                EXTRACT(YEAR FROM created_at) as registration_year,
                AGE(created_at, updated_at) as time_since_update,
                COALESCE(phone, 'N/A') as phone_number,
                REPLACE(address, @oldCity, @newCity) as updated_address
            FROM users
            WHERE created_at >= @startDate;";
        var startDate = new DateTime(2024, 1, 1);
        var parameters = new List<(string Name, object? Value)>
        {
            ("oldCity", "Tokyo"),
            ("newCity", "Osaka"),
            ("startDate", startDate)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void 複雑な集約関数とフィルタリングを含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            SELECT 
                user_id,
                COUNT(*) FILTER (WHERE status = 1) as active_orders,
                COUNT(*) FILTER (WHERE status = 2) as pending_orders,
                COUNT(*) FILTER (WHERE status = 3) as completed_orders,
                SUM(total) FILTER (WHERE status = 3) as completed_total,
                AVG(total) FILTER (WHERE created_at >= @startDate) as recent_avg_total,
                MAX(created_at) as last_order_date
            FROM orders
            WHERE user_id = @userId
            GROUP BY user_id;";
        var startDate = new DateTime(2024, 1, 1);
        var parameters = new List<(string Name, object? Value)>
        {
            ("userId", 1),
            ("startDate", startDate)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void 動的なテーブル名を含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            SELECT * FROM @tableName
            WHERE created_at >= @startDate
            ORDER BY id DESC
            LIMIT @limit;";
        var startDate = new DateTime(2024, 1, 1);
        var parameters = new List<(string Name, object? Value)>
        {
            ("tableName", "users_2024"),
            ("startDate", startDate),
            ("limit", 100)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void 複雑なMERGE文を含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            MERGE INTO users AS target
            USING (SELECT @id as id, @name as name, @email as email) AS source
            ON target.id = source.id
            WHEN MATCHED THEN
                UPDATE SET 
                    name = source.name,
                    email = source.email,
                    updated_at = CURRENT_TIMESTAMP
            WHEN NOT MATCHED THEN
                INSERT (id, name, email, created_at)
                VALUES (source.id, source.name, source.email, CURRENT_TIMESTAMP);";
        var parameters = new List<(string Name, object? Value)>
        {
            ("id", 1),
            ("name", "John Doe"),
            ("email", "john@example.com")
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void 複数のCTEと複雑な結合を含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            WITH active_users AS (
                SELECT id, name, email
                FROM users
                WHERE status = 1 AND last_login >= @recentDate
            ),
            user_orders AS (
                SELECT 
                    user_id,
                    COUNT(*) as order_count,
                    SUM(total) as total_spent
                FROM orders
                WHERE created_at >= @startDate
                GROUP BY user_id
            ),
            user_reviews AS (
                SELECT 
                    user_id,
                    AVG(rating) as avg_rating,
                    COUNT(*) as review_count
                FROM reviews
                GROUP BY user_id
            )
            SELECT 
                au.id,
                au.name,
                au.email,
                COALESCE(uo.order_count, 0) as order_count,
                COALESCE(uo.total_spent, 0) as total_spent,
                COALESCE(ur.avg_rating, 0) as avg_rating,
                COALESCE(ur.review_count, 0) as review_count
            FROM active_users au
            LEFT JOIN user_orders uo ON au.id = uo.user_id
            LEFT JOIN user_reviews ur ON au.id = ur.user_id
            WHERE COALESCE(uo.total_spent, 0) >= @minSpent
            ORDER BY uo.total_spent DESC;";
        var recentDate = new DateTime(2024, 6, 1);
        var startDate = new DateTime(2024, 1, 1);
        var parameters = new List<(string Name, object? Value)>
        {
            ("recentDate", recentDate),
            ("startDate", startDate),
            ("minSpent", 1000)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void 複雑なJSON関数を含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            SELECT 
                id,
                name,
                JSON_EXTRACT(metadata, '$.department') as department,
                JSON_EXTRACT(metadata, '$.tags[0]') as first_tag,
                JSON_ARRAY_LENGTH(JSON_EXTRACT(metadata, '$.tags')) as tag_count,
                JSON_OBJECT('id', id, 'name', name, 'age', age) as user_json
            FROM users
            WHERE JSON_EXTRACT(metadata, '$.status') = @status
            AND JSON_CONTAINS(metadata, @tag, '$.tags');";
        var parameters = new List<(string Name, object? Value)>
        {
            ("status", "active"),
            ("tag", "\"premium\"")
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }

    [Fact]
    public void 複雑な配列操作を含む生SQLクエリを生成できる()
    {
        // Arrange
        var sqlText = @"
            SELECT 
                id,
                name,
                ARRAY_LENGTH(tags, 1) as tag_count,
                ARRAY_AGG(DISTINCT category_id) as categories,
                UNNEST(tags) as tag
            FROM products
            WHERE @searchTag = ANY(tags)
            AND category_id = ANY(@categoryIds)
            GROUP BY id, name, tags
            HAVING ARRAY_LENGTH(tags, 1) >= @minTagCount;";
        var parameters = new List<(string Name, object? Value)>
        {
            ("searchTag", "electronics"),
            ("categoryIds", new[] { 1, 2, 3 }),
            ("minTagCount", 2)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var (resultSqlText, resultParameters) = query.Build();

        // Assert
        Assert.Equal(sqlText, resultSqlText);
        Assert.Equal(parameters, resultParameters);
    }
}
