using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // EF Core generates data arrays inside migration methods.
#pragma warning disable IDE0161 // EF Core generates block-scoped migration namespaces.

namespace OperationsHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDatabaseAndIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false),
                    Name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false),
                    DisplayName = table.Column<string>(type: "longtext", nullable: false),
                    UserName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: true),
                    SecurityStamp = table.Column<string>(type: "longtext", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "longtext", nullable: true),
                    PhoneNumber = table.Column<string>(type: "longtext", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "request_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_types", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    RoleId = table.Column<string>(type: "varchar(255)", nullable: false),
                    ClaimType = table.Column<string>(type: "longtext", nullable: true),
                    ClaimValue = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false),
                    ClaimType = table.Column<string>(type: "longtext", nullable: true),
                    ClaimValue = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "varchar(255)", nullable: false),
                    ProviderKey = table.Column<string>(type: "varchar(255)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "longtext", nullable: true),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false),
                    RoleId = table.Column<string>(type: "varchar(255)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false),
                    LoginProvider = table.Column<string>(type: "varchar(255)", nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false),
                    Value = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "service_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false),
                    request_number = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false),
                    requester_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    request_type_id = table.Column<Guid>(type: "char(36)", nullable: false),
                    department_id = table.Column<Guid>(type: "char(36)", nullable: true),
                    assignee_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: false),
                    version = table.Column<uint>(type: "int unsigned", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_requests_AspNetUsers_assignee_id",
                        column: x => x.assignee_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_AspNetUsers_requester_id",
                        column: x => x.requester_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_request_types_request_type_id",
                        column: x => x.request_type_id,
                        principalTable: "request_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false),
                    service_request_id = table.Column<Guid>(type: "char(36)", nullable: true),
                    event_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    actor_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    details = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_events_AspNetUsers_actor_id",
                        column: x => x.actor_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_audit_events_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "request_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false),
                    service_request_id = table.Column<Guid>(type: "char(36)", nullable: false),
                    assignee_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    assigned_by_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_request_assignments_AspNetUsers_assigned_by_id",
                        column: x => x.assigned_by_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_assignments_AspNetUsers_assignee_id",
                        column: x => x.assignee_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_assignments_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "request_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false),
                    service_request_id = table.Column<Guid>(type: "char(36)", nullable: false),
                    author_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    body = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_comments", x => x.id);
                    table.ForeignKey(
                        name: "FK_request_comments_AspNetUsers_author_id",
                        column: x => x.author_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_comments_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "request_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false),
                    service_request_id = table.Column<Guid>(type: "char(36)", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    changed_by_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    changed_at_utc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_request_status_history_AspNetUsers_changed_by_id",
                        column: x => x.changed_by_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_status_history_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "work_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false),
                    service_request_id = table.Column<Guid>(type: "char(36)", nullable: false),
                    author_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    hours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_logs_AspNetUsers_author_id",
                        column: x => x.author_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_logs_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "ADMINISTRATOR", "ADMINISTRATOR", "Administrator", "ADMINISTRATOR" },
                    { "MANAGER", "MANAGER", "Manager", "MANAGER" },
                    { "REQUESTER", "REQUESTER", "Requester", "REQUESTER" },
                    { "TECHNICIAN", "TECHNICIAN", "Technician", "TECHNICIAN" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "DisplayName", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[,]
                {
                    { "10000000-0000-4000-8000-000000000001", 0, "10000000-0000-4000-8000-000000000001", "Demo Requester", "requester@operationshub.local", true, false, null, "REQUESTER@OPERATIONSHUB.LOCAL", "REQUESTER@OPERATIONSHUB.LOCAL", "AQAAAAIAAYagAAAAEMWG1AkO1GvM7MAvuvUkbCR+Cpvj6T7Tsl1ncuhgnJh9xdXfQCihdQQydyF+Aoe1Bw==", null, false, "10000000-0000-4000-8000-000000000001", false, "requester@operationshub.local" },
                    { "10000000-0000-4000-8000-000000000002", 0, "10000000-0000-4000-8000-000000000002", "Demo Technician", "technician@operationshub.local", true, false, null, "TECHNICIAN@OPERATIONSHUB.LOCAL", "TECHNICIAN@OPERATIONSHUB.LOCAL", "AQAAAAIAAYagAAAAEKqXzGSJ4LCc4rgGriOFk/Xc7bs17cZqQ3C2/gt8XMYY6dnLxhKpTEaKj9vvWQb9bw==", null, false, "10000000-0000-4000-8000-000000000002", false, "technician@operationshub.local" },
                    { "10000000-0000-4000-8000-000000000003", 0, "10000000-0000-4000-8000-000000000003", "Demo Manager", "manager@operationshub.local", true, false, null, "MANAGER@OPERATIONSHUB.LOCAL", "MANAGER@OPERATIONSHUB.LOCAL", "AQAAAAIAAYagAAAAEIFCG1AJJI1TJ71/8LzrLcFdYDBixbAa/xRqEG2BKnuG7DzncTJyDNjMEnJLggm81w==", null, false, "10000000-0000-4000-8000-000000000003", false, "manager@operationshub.local" },
                    { "10000000-0000-4000-8000-000000000004", 0, "10000000-0000-4000-8000-000000000004", "Demo Administrator", "administrator@operationshub.local", true, false, null, "ADMINISTRATOR@OPERATIONSHUB.LOCAL", "ADMINISTRATOR@OPERATIONSHUB.LOCAL", "AQAAAAIAAYagAAAAENBuiU0xPMDBnGChDta1qRgLqc6Lsp7pxVIZz52nUIb1pZOGsmQaUYV42Z/pmCtAmg==", null, false, "10000000-0000-4000-8000-000000000004", false, "administrator@operationshub.local" }
                });

            migrationBuilder.InsertData(
                table: "departments",
                columns: new[] { "id", "created_at_utc", "is_active", "name" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 7, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Information Technology" },
                    { new Guid("20000000-0000-4000-8000-000000000002"), new DateTimeOffset(new DateTime(2026, 7, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Facilities" }
                });

            migrationBuilder.InsertData(
                table: "request_types",
                columns: new[] { "id", "created_at_utc", "description", "is_active", "name" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 7, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Request access to an internal system or service.", true, "Access request" },
                    { new Guid("30000000-0000-4000-8000-000000000002"), new DateTimeOffset(new DateTime(2026, 7, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Report an issue with a workplace or facility.", true, "Facilities issue" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { "REQUESTER", "10000000-0000-4000-8000-000000000001" },
                    { "TECHNICIAN", "10000000-0000-4000-8000-000000000002" },
                    { "MANAGER", "10000000-0000-4000-8000-000000000003" },
                    { "ADMINISTRATOR", "10000000-0000-4000-8000-000000000004" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_actor_id",
                table: "audit_events",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_service_request_id_occurred_at_utc",
                table: "audit_events",
                columns: new[] { "service_request_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_departments_name",
                table: "departments",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_assignments_assigned_by_id",
                table: "request_assignments",
                column: "assigned_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_assignments_assignee_id",
                table: "request_assignments",
                column: "assignee_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_assignments_service_request_id_assigned_at_utc",
                table: "request_assignments",
                columns: new[] { "service_request_id", "assigned_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_request_comments_author_id",
                table: "request_comments",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_comments_service_request_id_created_at_utc",
                table: "request_comments",
                columns: new[] { "service_request_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_request_status_history_changed_by_id",
                table: "request_status_history",
                column: "changed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_status_history_service_request_id_changed_at_utc",
                table: "request_status_history",
                columns: new[] { "service_request_id", "changed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_request_types_name",
                table: "request_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_assignee_id",
                table: "service_requests",
                column: "assignee_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_department_id",
                table: "service_requests",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_request_number",
                table: "service_requests",
                column: "request_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_request_type_id",
                table: "service_requests",
                column: "request_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_requester_id",
                table: "service_requests",
                column: "requester_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_status_created_at_utc",
                table: "service_requests",
                columns: new[] { "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_work_logs_author_id",
                table: "work_logs",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_logs_service_request_id_created_at_utc",
                table: "work_logs",
                columns: new[] { "service_request_id", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "audit_events");

            migrationBuilder.DropTable(
                name: "request_assignments");

            migrationBuilder.DropTable(
                name: "request_comments");

            migrationBuilder.DropTable(
                name: "request_status_history");

            migrationBuilder.DropTable(
                name: "work_logs");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "service_requests");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "request_types");
        }
    }
}
