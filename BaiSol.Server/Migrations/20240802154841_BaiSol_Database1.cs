using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BaiSol.Server.Migrations
{
    /// <inheritdoc />
    public partial class BaiSol_Database1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectProjId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Labor",
                columns: table => new
                {
                    LaborId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LaborDescript = table.Column<string>(type: "text", nullable: false),
                    LaborQOH = table.Column<int>(type: "integer", nullable: false),
                    LaborNumUnit = table.Column<int>(type: "integer", nullable: false),
                    LaborUnit = table.Column<string>(type: "text", nullable: false),
                    LaborCost = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Labor", x => x.LaborId);
                });

            migrationBuilder.CreateTable(
                name: "Supply",
                columns: table => new
                {
                    SuppId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MTLQuantity = table.Column<int>(type: "integer", nullable: true),
                    EQPTQuantity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supply", x => x.SuppId);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    EQPTId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EQPTCode = table.Column<string>(type: "text", nullable: false),
                    EQPTDescript = table.Column<string>(type: "text", nullable: false),
                    EQPTPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    EQPTQOH = table.Column<int>(type: "integer", nullable: false),
                    EQPTUnit = table.Column<string>(type: "text", nullable: false),
                    EQPTStatus = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MyProperty = table.Column<int>(type: "integer", nullable: false),
                    SupplySuppId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.EQPTId);
                    table.ForeignKey(
                        name: "FK_Equipment_Supply_SupplySuppId",
                        column: x => x.SupplySuppId,
                        principalTable: "Supply",
                        principalColumn: "SuppId");
                });

            migrationBuilder.CreateTable(
                name: "Material",
                columns: table => new
                {
                    MTLId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MTLCode = table.Column<string>(type: "text", nullable: false),
                    MTLDescript = table.Column<string>(type: "text", nullable: false),
                    MTLPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    MTLQOH = table.Column<int>(type: "integer", nullable: false),
                    MTLUnit = table.Column<string>(type: "text", nullable: false),
                    MTLStatus = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MyProperty = table.Column<int>(type: "integer", nullable: false),
                    SupplySuppId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Material", x => x.MTLId);
                    table.ForeignKey(
                        name: "FK_Material_Supply_SupplySuppId",
                        column: x => x.SupplySuppId,
                        principalTable: "Supply",
                        principalColumn: "SuppId");
                });

            migrationBuilder.CreateTable(
                name: "Project",
                columns: table => new
                {
                    ProjId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjName = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SupplySuppId = table.Column<int>(type: "integer", nullable: true),
                    LaborId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project", x => x.ProjId);
                    table.ForeignKey(
                        name: "FK_Project_Labor_LaborId",
                        column: x => x.LaborId,
                        principalTable: "Labor",
                        principalColumn: "LaborId");
                    table.ForeignKey(
                        name: "FK_Project_Supply_SupplySuppId",
                        column: x => x.SupplySuppId,
                        principalTable: "Supply",
                        principalColumn: "SuppId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ProjectProjId",
                table: "AspNetUsers",
                column: "ProjectProjId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_SupplySuppId",
                table: "Equipment",
                column: "SupplySuppId");

            migrationBuilder.CreateIndex(
                name: "IX_Material_SupplySuppId",
                table: "Material",
                column: "SupplySuppId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_LaborId",
                table: "Project",
                column: "LaborId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_SupplySuppId",
                table: "Project",
                column: "SupplySuppId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Project_ProjectProjId",
                table: "AspNetUsers",
                column: "ProjectProjId",
                principalTable: "Project",
                principalColumn: "ProjId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Project_ProjectProjId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "Material");

            migrationBuilder.DropTable(
                name: "Project");

            migrationBuilder.DropTable(
                name: "Labor");

            migrationBuilder.DropTable(
                name: "Supply");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ProjectProjId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProjectProjId",
                table: "AspNetUsers");
        }
    }
}
