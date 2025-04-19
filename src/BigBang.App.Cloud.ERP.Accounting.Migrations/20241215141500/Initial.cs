namespace BigBang.App.Cloud.ERP.Accounting.Migrations._20241215141500
{
    [FluentMigrator.Migration(2024_12_15_14_15_00, "Initial")]
    public class Initial : FluentMigrator.ForwardOnlyMigration //Migration|ForwardOnlyMigration|AutoReversingMigration
    {
        public override void Up()
        {
            Execute.EmbeddedScript("_20241215141500.Initial_Up.sql");
        }
    }
}