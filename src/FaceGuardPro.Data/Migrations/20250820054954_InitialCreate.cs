using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FaceGuardPro.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    employee_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    position = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    join_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    photo_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "authentication_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    authentication_result = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    face_match_score = table.Column<double>(type: "double precision", nullable: true),
                    liveness_score = table.Column<double>(type: "double precision", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    attempted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    processing_time = table.Column<TimeSpan>(type: "interval", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authentication_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_authentication_logs_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "face_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_data = table.Column<byte[]>(type: "bytea", nullable: false),
                    quality = table.Column<double>(type: "double precision", nullable: false),
                    metadata = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_face_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_face_templates_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "id", "created_at", "description", "name" },
                values: new object[,]
                {
                    { new Guid("660e8400-e29b-41d4-a716-446655440001"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(8830), "Create, update, delete employees", "ManageEmployees" },
                    { new Guid("660e8400-e29b-41d4-a716-446655440002"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(8832), "View employee information", "ViewEmployees" },
                    { new Guid("660e8400-e29b-41d4-a716-446655440003"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(8834), "Manage face templates", "ManageFaceTemplates" },
                    { new Guid("660e8400-e29b-41d4-a716-446655440004"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(8843), "Perform face authentication", "PerformAuthentication" },
                    { new Guid("660e8400-e29b-41d4-a716-446655440005"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(8845), "View system reports", "ViewReports" },
                    { new Guid("660e8400-e29b-41d4-a716-446655440006"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(8847), "System administration", "ManageSystem" }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "created_at", "description", "name" },
                values: new object[,]
                {
                    { new Guid("550e8400-e29b-41d4-a716-446655440001"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(8722), "Full system access", "Admin" },
                    { new Guid("550e8400-e29b-41d4-a716-446655440002"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(8725), "Employee management and authentication", "Operator" },
                    { new Guid("550e8400-e29b-41d4-a716-446655440003"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(8727), "Read-only access", "Viewer" }
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "created_at", "email", "first_name", "is_active", "last_login_at", "last_name", "password_hash", "updated_at", "username" },
                values: new object[] { new Guid("440e8400-e29b-41d4-a716-446655440001"), new DateTime(2025, 8, 20, 5, 49, 53, 320, DateTimeKind.Utc).AddTicks(2427), "admin@faceguardpro.com", "System", true, null, "Administrator", "$2a$11$Q4s0VglSWwtaksXyczdpLuYV8Oul9Dq5n0Sv3KOYxWzkqdGSiERGS", null, "admin" });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "id", "assigned_at", "permission_id", "role_id" },
                values: new object[,]
                {
                    { new Guid("770e8400-e29b-41d4-a716-446655440001"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(8960), new Guid("660e8400-e29b-41d4-a716-446655440001"), new Guid("550e8400-e29b-41d4-a716-446655440001") },
                    { new Guid("770e8400-e29b-41d4-a716-446655440002"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(8970), new Guid("660e8400-e29b-41d4-a716-446655440002"), new Guid("550e8400-e29b-41d4-a716-446655440001") },
                    { new Guid("770e8400-e29b-41d4-a716-446655440003"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(8973), new Guid("660e8400-e29b-41d4-a716-446655440003"), new Guid("550e8400-e29b-41d4-a716-446655440001") },
                    { new Guid("770e8400-e29b-41d4-a716-446655440004"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(8976), new Guid("660e8400-e29b-41d4-a716-446655440004"), new Guid("550e8400-e29b-41d4-a716-446655440001") },
                    { new Guid("770e8400-e29b-41d4-a716-446655440005"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(8978), new Guid("660e8400-e29b-41d4-a716-446655440005"), new Guid("550e8400-e29b-41d4-a716-446655440001") },
                    { new Guid("770e8400-e29b-41d4-a716-446655440006"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(8984), new Guid("660e8400-e29b-41d4-a716-446655440006"), new Guid("550e8400-e29b-41d4-a716-446655440001") },
                    { new Guid("780e8400-e29b-41d4-a716-446655440001"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(9002), new Guid("660e8400-e29b-41d4-a716-446655440001"), new Guid("550e8400-e29b-41d4-a716-446655440002") },
                    { new Guid("780e8400-e29b-41d4-a716-446655440002"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(9006), new Guid("660e8400-e29b-41d4-a716-446655440002"), new Guid("550e8400-e29b-41d4-a716-446655440002") },
                    { new Guid("780e8400-e29b-41d4-a716-446655440003"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(9008), new Guid("660e8400-e29b-41d4-a716-446655440003"), new Guid("550e8400-e29b-41d4-a716-446655440002") },
                    { new Guid("780e8400-e29b-41d4-a716-446655440004"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(9010), new Guid("660e8400-e29b-41d4-a716-446655440004"), new Guid("550e8400-e29b-41d4-a716-446655440002") },
                    { new Guid("780e8400-e29b-41d4-a716-446655440005"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(9012), new Guid("660e8400-e29b-41d4-a716-446655440005"), new Guid("550e8400-e29b-41d4-a716-446655440002") },
                    { new Guid("790e8400-e29b-41d4-a716-446655440001"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(9040), new Guid("660e8400-e29b-41d4-a716-446655440002"), new Guid("550e8400-e29b-41d4-a716-446655440003") },
                    { new Guid("790e8400-e29b-41d4-a716-446655440002"), new DateTime(2025, 8, 20, 5, 49, 53, 205, DateTimeKind.Utc).AddTicks(9044), new Guid("660e8400-e29b-41d4-a716-446655440005"), new Guid("550e8400-e29b-41d4-a716-446655440003") }
                });

            migrationBuilder.InsertData(
                table: "user_roles",
                columns: new[] { "id", "assigned_at", "role_id", "user_id" },
                values: new object[] { new Guid("880e8400-e29b-41d4-a716-446655440001"), new DateTime(2025, 8, 20, 5, 49, 53, 320, DateTimeKind.Utc).AddTicks(2832), new Guid("550e8400-e29b-41d4-a716-446655440001"), new Guid("440e8400-e29b-41d4-a716-446655440001") });

            migrationBuilder.CreateIndex(
                name: "IX_authentication_logs_employee_id",
                table: "authentication_logs",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_email",
                table: "employees",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_employee_id",
                table: "employees",
                column: "employee_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_face_templates_employee_id",
                table: "face_templates",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_name",
                table: "permissions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token",
                table: "refresh_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_role_id_permission_id",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_user_id_role_id",
                table: "user_roles",
                columns: new[] { "user_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authentication_logs");

            migrationBuilder.DropTable(
                name: "face_templates");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
