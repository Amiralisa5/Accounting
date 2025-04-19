namespace BigBang.App.Cloud.ERP.Accounting.Migrations._20250409132444
{
    [FluentMigrator.Migration(2025_04_09_13_24_44, "AddTitleColumnToDB")]
    public class AddTitleColumnToDB : FluentMigrator.Migration //Migration|ForwardOnlyMigration|AutoReversingMigration
    {
        public override void Up()
        {
            Execute.EmbeddedScript("_20250409132444.AddTitleColumnToDB_Up.sql");
        }

        public override void Down()
        {
            Execute.EmbeddedScript("_20250409132444.AddTitleColumnToDB_Down.sql");
        }
    }
}