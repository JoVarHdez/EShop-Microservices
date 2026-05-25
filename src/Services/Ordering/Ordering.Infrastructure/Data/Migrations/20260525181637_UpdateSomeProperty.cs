using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordering.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSomeProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShippingAddress_EmailAdress",
                table: "Orders",
                newName: "ShippingAddress_EmailAddress");

            migrationBuilder.RenameColumn(
                name: "BillingAddress_EmailAdress",
                table: "Orders",
                newName: "BillingAddress_EmailAddress");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShippingAddress_EmailAddress",
                table: "Orders",
                newName: "ShippingAddress_EmailAdress");

            migrationBuilder.RenameColumn(
                name: "BillingAddress_EmailAddress",
                table: "Orders",
                newName: "BillingAddress_EmailAdress");
        }
    }
}
