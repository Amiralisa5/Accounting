namespace BigBang.App.Cloud.ERP.Accounting.Migrations._20250415091426
{
    [FluentMigrator.Migration(2025_04_15_09_14_26, "AddAdvancePaymentRecord")]
    public class AddAdvancePaymentRecord : FluentMigrator.Migration //Migration|ForwardOnlyMigration|AutoReversingMigration
    {
        public override void Up()
        {
            Execute.EmbeddedScript("_20250415091426.AddAdvancePaymentRecord_Up.sql");
        }

        public override void Down()
        {
            Execute.EmbeddedScript("_20250415091426.AddAdvancePaymentRecord_Down.sql");
        }
    }
}