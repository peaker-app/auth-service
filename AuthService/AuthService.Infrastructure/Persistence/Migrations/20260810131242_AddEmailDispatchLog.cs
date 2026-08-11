using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDispatchLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_dispatch_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false),
                    recipient_hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    sent_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_dispatch_log", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_recipient_sent",
                table: "email_dispatch_log",
                columns: new[] { "recipient_hash", "sent_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_sent",
                table: "email_dispatch_log",
                column: "sent_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_dispatch_log");
        }
    }
}
