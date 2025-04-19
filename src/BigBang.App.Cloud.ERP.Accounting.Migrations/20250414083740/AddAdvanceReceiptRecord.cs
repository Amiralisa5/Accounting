namespace BigBang.App.Cloud.ERP.Accounting.Migrations._20250414083740
{
    [FluentMigrator.Migration(2025_04_14_08_37_40, "AddAdvanceReceiptRecord")]
    public class AddAdvanceReceiptRecord : FluentMigrator.Migration //Migration|ForwardOnlyMigration|AutoReversingMigration
    {
        public override void Up()
        {
            Execute.EmbeddedScript("_20250414083740.AddAdvanceReceiptRecord_Up.sql");
        }

        public override void Down()
        {
            Execute.EmbeddedScript("_20250414083740.AddAdvanceReceiptRecord_Down.sql");
        }
    }
}