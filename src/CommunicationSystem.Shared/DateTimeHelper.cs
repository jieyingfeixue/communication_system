namespace CommunicationSystem.Shared;

public static class DateTimeHelper
{
    /// <summary>
    /// 项目约定 CreateTime 存 UTC；EF 从 MySQL 读出 Kind=Unspecified，不能直接 ToUniversalTime()。
    /// </summary>
    public static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    public static bool WithinMinutes(DateTime createTime, int minutes) =>
        (DateTime.UtcNow - AsUtc(createTime)).TotalMinutes <= minutes;
}
