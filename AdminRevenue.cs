namespace TravelAgency;

using MySql.Data.MySqlClient;

public static class AdminRevenue
{
  public record RevenueYearly_Data(
      int year,
      decimal total_revenue,
      int total_bookings
  );

  public static async Task<IResult> GetRevenueYearly(Config config)
  {
    List<RevenueYearly_Data> result = new();

    const string sql = """
            SELECT year, total_revenue, total_bookings
            FROM admin_revenue_yearly
            ORDER BY year DESC;
        """;

    try
    {
      using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, sql);

      while (reader.Read())
      {
        result.Add(new RevenueYearly_Data(
            reader.GetInt32(0),
            reader.GetDecimal(1),
            reader.GetInt32(2)
        ));
      }

      return Results.Ok(result);
    }
    catch (MySqlException)
    {
      return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
  }

  public record RevenueMonthly_Data(
      int year,
      int month,
      decimal total_revenue,
      int total_bookings
  );

  public static async Task<IResult> GetRevenueMonthly(Config config)
  {
    List<RevenueMonthly_Data> result = new();

    const string sql = """
            SELECT year, month, total_revenue, total_bookings
            FROM admin_revenue_monthly
            ORDER BY year DESC, month DESC;
        """;

    try
    {
      using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, sql);

      while (reader.Read())
      {
        result.Add(new RevenueMonthly_Data(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetDecimal(2),
            reader.GetInt32(3)
        ));
      }

      return Results.Ok(result);
    }
    catch (MySqlException)
    {
      return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
  }
}
