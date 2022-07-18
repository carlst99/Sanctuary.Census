using FluentMigrator;

namespace Sanctuary.Census.Database.Migrations;

[Migration(20220717001)]
public class InitialTables20220717001 : Migration
{
    public override void Up()
    {
        Create.Table("currency")
            .WithColumn("currency_id").AsInt32().NotNullable().PrimaryKey()
            .WithColumn("name").AsInt32().NotNullable()
            .WithColumn("description").AsInt32().Nullable()
            .WithColumn("icon_image_set_id").AsInt32().NotNullable()
            .WithColumn("map_icon_image_set_id").AsInt32().NotNullable()
            .WithColumn("inventory_cap").AsInt32().Nullable();
    }

    public override void Down()
    {
        Delete.Table("currency");
    }
}
