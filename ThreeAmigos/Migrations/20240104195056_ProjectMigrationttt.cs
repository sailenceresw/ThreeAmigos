using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThreeAmigos.Migrations
{
    public partial class ProjectMigrationttt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrdersHistory",
                columns: table => new
                {
                    OrderHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThreeAmigosUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdersHistory", x => x.OrderHistoryId);
                    table.ForeignKey(
                        name: "FK_OrdersHistory_AspNetUsers_ThreeAmigosUserId",
                        column: x => x.ThreeAmigosUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrdersHistory_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrdersHistory_OrderId",
                table: "OrdersHistory",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdersHistory_ThreeAmigosUserId",
                table: "OrdersHistory",
                column: "ThreeAmigosUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrdersHistory");
        }
    }
}
