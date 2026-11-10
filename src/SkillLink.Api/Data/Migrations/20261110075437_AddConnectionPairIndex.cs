using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillLink.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectionPairIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX ux_connections_pair
                ON connections (LEAST(requester_id, addressee_id), GREATEST(requester_id, addressee_id));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX ux_connections_pair;");
        }
    }
}
