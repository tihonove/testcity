using System.Reflection;
using ClickHouse.Client.ADO;
using TestCity.Core.GitlabProjects;
using TestCity.Core.Storage;

namespace TestCity.Core.Clickhouse;

public class TestAnalyticsDatabaseSchema
{
    public static async Task ActualizeDatabaseSchemaAsync(ClickHouseConnection connection)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{typeof(TestAnalyticsDatabaseSchema).Namespace}.SchemaMigrations.Schema1.sql";
        await using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new Exception($"Resource {resourceName} not found");
        using var reader = new StreamReader(stream);
        var sqlScript = await reader.ReadToEndAsync();
        var statements = sqlScript.Split(["-- divider --"], StringSplitOptions.RemoveEmptyEntries);
        await using var command = connection.CreateCommand();
        foreach (var statement in statements)
        {
            command.CommandText = statement;
            await command.ExecuteNonQueryAsync();
        }
    }

    public static async Task InsertPredefinedProjects(ConnectionFactory connectionFactory)
    {
        var db = new TestCityDatabase(connectionFactory);
        if (await db.GitLabEntities.IsEmpty() || !await db.GitLabEntities.HasProject(24783))
        {
            var testGroups = new List<GitLabGroup>
            {
                new() {
                    Id = "7523",
                    Title = "forms",
                    Projects =
                    [
                        new() {
                            Id = "17358",
                            Title = "forms",
                            UseHooks = true
                        },
                        new() {
                            Id = "19371",
                            Title = "extern.forms",
                            UseHooks = true
                        },
                        new() {
                            Id = "807",
                            Title = "candy",
                            UseHooks = true
                        }
                    ],
                    MergeRunsFromJobs = false
                },
                new() {
                    Id = "53",
                    Title = "diadoc",
                    Projects =
                    [
                        new() {
                            Id = "182",
                            Title = "diadoc"
                        }
                    ],
                    MergeRunsFromJobs = false
                },
                new() {
                    Id = "23830",
                    Title = "fintech",
                    Groups =
                    [
                        new() {
                            Id = "33657",
                            Title = "dbo",
                            Projects =
                            [
                                new() {
                                    Id = "20289",
                                    Title = "remote-banking"
                                },
                                new() {
                                    Id = "20290",
                                    Title = "remote-banking-front"
                                }
                            ]
                        },
                        new() {
                            Id = "33580",
                            Title = "platform",
                            Projects =
                            [
                                new() {
                                    Id = "20479",
                                    Title = "banking"
                                },
                                new() {
                                    Id = "28459",
                                    Title = "event"
                                },
                                new() {
                                    Id = "27109",
                                    Title = "tarifficator"
                                },
                                new() {
                                    Id = "27462",
                                    Title = "customer"
                                },
                                new() {
                                    Id = "20588",
                                    Title = "bank-gateway"
                                },
                                new() {
                                    Id = "25705",
                                    Title = "printer"
                                },
                                new() {
                                    Id = "25963",
                                    Title = "audit"
                                },
                                new() {
                                    Id = "25483",
                                    Title = "crypto"
                                }
                            ]
                        }
                    ]
                },
                new() {
                    Id = "848",
                    Title = "focus",
                    Projects =
                    [
                        new() {
                            Id = "2189",
                            Title = "bingo"
                        }
                    ]
                },
                new() {
                    Id = "1546",
                    Title = "cpp.studio",
                    Projects =
                    [
                        new() {
                            Id = "2680",
                            Title = "plugin"
                        }
                    ],
                    MergeRunsFromJobs = false
                },
                new() {
                    Id = "2113",
                    Title = "marking",
                    Projects =
                    [
                        new() {
                            Id = "4845",
                            Title = "marking"
                        }
                    ],
                    MergeRunsFromJobs = false
                },
                new() {
                    Id = "0",
                    Title = "test-analytics",
                    Projects =
                    [
                        new() {
                            Id = "24783",
                            Title = "test-analytics",
                            UseHooks = true
                        }
                    ],
                    MergeRunsFromJobs = false
                },
                new() {
                    Id = "24609",
                    Title = "talk",
                    Projects =
                    [
                        new() {
                            Id = "23002",
                            Title = "talk-playwright"
                        }
                    ]
                },
                new() {
                    Id = "5078",
                    Title = "kedo",
                    Projects =
                    [
                        new() {
                            Id = "13070",
                            Title = "kedo"
                        }
                    ],
                    MergeRunsFromJobs = false
                },
                new() {
                    Id = "27200",
                    Title = "fiit",
                    Projects =
                    [
                        new() {
                            Id = "19564",
                            Title = "fiit-big-library"
                        }
                    ],
                    MergeRunsFromJobs = false
                },
                new () {
                    Id = "867",
                    Title = "KE.Infra",
                    Projects =
                    [
                        new() {
                            Id = "6212",
                            Title = "isup"
                        }
                    ],
                    MergeRunsFromJobs = false
                }
            };

            await db.GitLabEntities.UpsertEntitiesAsync(testGroups.ToGitLabEntityRecords(null));
        }
    }
}
