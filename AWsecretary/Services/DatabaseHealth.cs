namespace AWsecretary.Services
{
    // 簡單旗標供中介與頁面檢查資料庫是否可用
    public class DatabaseHealth
    {
        public bool IsDatabaseAvailable { get; set; } = true;
    }
}