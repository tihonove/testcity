using FluentAssertions;
using TestCity.Core.GitlabProjects;
using Xunit;

namespace TestCity.UnitTests.AccessChecking;

public class GitLabPathResolverTests
{
    [Fact]
    public async Task Test01()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var project = new GitLabProject
        {
            Id = "3",
            Title = "project1",
        };
        var subGroup = new GitLabGroup
        {
            Id = "2",
            Title = "subgroup1",
            Projects = [project]
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "group1",
            Groups = [subGroup],
            IsPublic = true,
        };
        gitLabProjectsService.AddRootGroup(group);
        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, new InMemoryGitLabEntityAccessContext());
        (await gitLabPathResolver.ResolveProjects(["group1", "subgroup1", "project1"])).Should().BeEquivalentTo(new[] { project });
        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["group1", "subgroup1", "project1"]);
        result.PathSlug.Should().HaveCount(3);
        result.PathSlug[0].Title.Should().Be("group1");
        result.PathSlug[1].Title.Should().Be("subgroup1");
        result.PathSlug[2].Title.Should().Be("project1");
    }

    [Fact]
    public async Task GetRootGroupsInfo_ShouldReturnOnlyPublicGroups_WhenNoAccessEntries()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var publicGroup = new GitLabGroup
        {
            Id = "1",
            Title = "public-group",
            IsPublic = true,
        };
        var privateGroup = new GitLabGroup
        {
            Id = "2",
            Title = "private-group",
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(publicGroup);
        gitLabProjectsService.AddRootGroup(privateGroup);

        var accessContext = new InMemoryGitLabEntityAccessContext();
        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.GetRootGroupsInfo();

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("public-group");
    }

    [Fact]
    public async Task GetRootGroupsInfo_ShouldReturnAccessiblePrivateGroups_WhenAccessEntriesExist()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var publicGroup = new GitLabGroup
        {
            Id = "1",
            Title = "public-group",
            IsPublic = true,
        };
        var privateGroupWithAccess = new GitLabGroup
        {
            Id = "2",
            Title = "private-group-accessible",
            IsPublic = false,
        };
        var privateGroupNoAccess = new GitLabGroup
        {
            Id = "3",
            Title = "private-group-no-access",
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(publicGroup);
        gitLabProjectsService.AddRootGroup(privateGroupWithAccess);
        gitLabProjectsService.AddRootGroup(privateGroupNoAccess);

        var accessContext = new InMemoryGitLabEntityAccessContext();
        accessContext.AddEntry(["private-group-accessible"], true);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.GetRootGroupsInfo();

        result.Should().HaveCount(2);
        result.Should().Contain(g => g.Title == "public-group");
        result.Should().Contain(g => g.Title == "private-group-accessible");
        result.Should().NotContain(g => g.Title == "private-group-no-access");
    }

    [Fact]
    public async Task GetRootGroupsInfo_ShouldReturnAllAccessibleGroups_WhenMultipleAccessEntriesExist()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var privateGroup1 = new GitLabGroup
        {
            Id = "1",
            Title = "private-group-1",
            IsPublic = false,
        };
        var privateGroup2 = new GitLabGroup
        {
            Id = "2",
            Title = "private-group-2",
            IsPublic = false,
        };
        var privateGroup3 = new GitLabGroup
        {
            Id = "3",
            Title = "private-group-3",
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(privateGroup1);
        gitLabProjectsService.AddRootGroup(privateGroup2);
        gitLabProjectsService.AddRootGroup(privateGroup3);

        var accessContext = new InMemoryGitLabEntityAccessContext();
        accessContext.AddEntry(["private-group-1"], true);
        accessContext.AddEntry(["private-group-2"], true);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.GetRootGroupsInfo();

        result.Should().HaveCount(2);
        result.Should().Contain(g => g.Title == "private-group-1");
        result.Should().Contain(g => g.Title == "private-group-2");
        result.Should().NotContain(g => g.Title == "private-group-3");
    }

    [Fact]
    public async Task GetRootGroupsInfo_ShouldFilterByFirstElementOfPathSlug_WhenAccessEntryHasNestedPath()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var rootGroup = new GitLabGroup
        {
            Id = "1",
            Title = "root-group",
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(rootGroup);

        var accessContext = new InMemoryGitLabEntityAccessContext();
        // Доступ к вложенному проекту должен открыть доступ к корневой группе
        accessContext.AddEntry(["root-group", "subgroup", "project"], true);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.GetRootGroupsInfo();

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("root-group");
    }

    [Fact]
    public async Task GetRootGroupsInfo_ShouldGrantAccessToParentGroup_WhenUserHasAccessToChildSubgroup()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var privateGroupWithChildAccess = new GitLabGroup
        {
            Id = "1",
            Title = "parent-group",
            IsPublic = false,
        };
        var privateGroupNoAccess = new GitLabGroup
        {
            Id = "2",
            Title = "other-group",
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(privateGroupWithChildAccess);
        gitLabProjectsService.AddRootGroup(privateGroupNoAccess);

        var accessContext = new InMemoryGitLabEntityAccessContext();
        // Доступ к подгруппе внутри parent-group
        accessContext.AddEntry(["parent-group", "child-subgroup"], true);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.GetRootGroupsInfo();

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("parent-group");
    }

    [Fact]
    public async Task GetRootGroupsInfo_ShouldGrantAccessToParentGroup_WhenUserHasAccessToChildProject()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var privateGroupWithProjectAccess = new GitLabGroup
        {
            Id = "1",
            Title = "group-with-project",
            IsPublic = false,
        };
        var privateGroupNoAccess = new GitLabGroup
        {
            Id = "2",
            Title = "other-group",
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(privateGroupWithProjectAccess);
        gitLabProjectsService.AddRootGroup(privateGroupNoAccess);

        var accessContext = new InMemoryGitLabEntityAccessContext();
        // Доступ к проекту внутри group-with-project
        accessContext.AddEntry(["group-with-project", "my-project"], true);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.GetRootGroupsInfo();

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("group-with-project");
    }

    [Fact]
    public async Task GetRootGroupsInfo_ShouldGrantAccessToParentGroup_WhenUserHasAccessToDeeplyNestedProject()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var group1 = new GitLabGroup
        {
            Id = "1",
            Title = "group1",
            IsPublic = false,
        };
        var group2 = new GitLabGroup
        {
            Id = "2",
            Title = "group2",
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(group1);
        gitLabProjectsService.AddRootGroup(group2);

        var accessContext = new InMemoryGitLabEntityAccessContext();
        // Доступ к проекту на глубине 4 уровня
        accessContext.AddEntry(["group1", "sub1", "sub2", "deep-project"], true);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.GetRootGroupsInfo();

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("group1");
    }

    [Fact]
    public async Task GetRootGroupsInfo_ShouldReturnMultipleGroups_WhenUserHasAccessToChildrenInDifferentGroups()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var group1 = new GitLabGroup
        {
            Id = "1",
            Title = "group1",
            IsPublic = false,
        };
        var group2 = new GitLabGroup
        {
            Id = "2",
            Title = "group2",
            IsPublic = false,
        };
        var group3 = new GitLabGroup
        {
            Id = "3",
            Title = "group3",
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(group1);
        gitLabProjectsService.AddRootGroup(group2);
        gitLabProjectsService.AddRootGroup(group3);

        var accessContext = new InMemoryGitLabEntityAccessContext();
        // Доступ к проектам в разных группах
        accessContext.AddEntry(["group1", "project1"], true);
        accessContext.AddEntry(["group2", "subgroup", "project2"], true);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.GetRootGroupsInfo();

        result.Should().HaveCount(2);
        result.Should().Contain(g => g.Title == "group1");
        result.Should().Contain(g => g.Title == "group2");
        result.Should().NotContain(g => g.Title == "group3");
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldAllowAccess_WhenEntityIsPublic()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var project = new GitLabProject
        {
            Id = "2",
            Title = "public-project",
            IsPublic = true,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "public-group",
            Projects = [project],
            IsPublic = true,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["public-group", "public-project"]);

        result.PathSlug.Should().HaveCount(2);
        result.PathSlug[0].Title.Should().Be("public-group");
        result.PathSlug[1].Title.Should().Be("public-project");
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldAllowAccess_WhenUserHasAccessEntry()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var project = new GitLabProject
        {
            Id = "2",
            Title = "private-project",
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "private-group",
            Projects = [project],
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        accessContext.AddEntry(["private-group", "private-project"], true);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["private-group", "private-project"]);

        result.PathSlug.Should().HaveCount(2);
        result.PathSlug[0].Title.Should().Be("private-group");
        result.PathSlug[1].Title.Should().Be("private-project");
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldDenyAccess_WhenAccessEntryIsFalse()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var project = new GitLabProject
        {
            Id = "2",
            Title = "restricted-project",
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "group",
            Projects = [project],
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        accessContext.AddEntry(["group", "restricted-project"], false);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var act = async () => await gitLabPathResolver.ResolveGroupOrProjectPath(["group", "restricted-project"]);

        await act.Should().ThrowAsync<AccessToEntityForbiddenException>();
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldAllowAccess_WhenNoAccessEntryForPrivateEntity()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var project = new GitLabProject
        {
            Id = "2",
            Title = "private-project",
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "private-group",
            Projects = [project],
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        // Отсутствие записи доступа не запрещает доступ - доступ запрещён только при явном HasAccess=false
        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["private-group", "private-project"]);

        result.PathSlug.Should().HaveCount(2);
        result.ResolvedEntity.Title.Should().Be("private-project");
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldAllowAccess_WhenParentHasAccessEntry()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var project = new GitLabProject
        {
            Id = "3",
            Title = "project",
            IsPublic = false,
        };
        var subGroup = new GitLabGroup
        {
            Id = "2",
            Title = "subgroup",
            Projects = [project],
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "group",
            Groups = [subGroup],
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        // Доступ к корневой группе даёт доступ ко всем вложенным сущностям
        accessContext.AddEntry(["group"], true);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["group", "subgroup", "project"]);

        result.PathSlug.Should().HaveCount(3);
        result.ResolvedEntity.Title.Should().Be("project");
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldDenyAccess_WhenParentHasNoAccessButChildIsPrivate()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var project = new GitLabProject
        {
            Id = "3",
            Title = "project",
            IsPublic = false,
        };
        var subGroup = new GitLabGroup
        {
            Id = "2",
            Title = "subgroup",
            Projects = [project],
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "group",
            Groups = [subGroup],
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        // Запрет доступа к корневой группе
        accessContext.AddEntry(["group"], false);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var act = async () => await gitLabPathResolver.ResolveGroupOrProjectPath(["group", "subgroup", "project"]);

        await act.Should().ThrowAsync<AccessToEntityForbiddenException>();
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldUseNearestAccessEntry_WhenMultipleEntriesExist()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var project = new GitLabProject
        {
            Id = "3",
            Title = "project",
            IsPublic = false,
        };
        var subGroup = new GitLabGroup
        {
            Id = "2",
            Title = "subgroup",
            Projects = [project],
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "group",
            Groups = [subGroup],
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        // Доступ к корневой группе разрешён, но к подгруппе запрещён
        accessContext.AddEntry(["group"], true);
        accessContext.AddEntry(["group", "subgroup"], false);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        // Доступ к подгруппе должен быть запрещён
        var act1 = async () => await gitLabPathResolver.ResolveGroupOrProjectPath(["group", "subgroup"]);
        await act1.Should().ThrowAsync<AccessToEntityForbiddenException>();

        // Доступ к проекту внутри подгруппы тоже запрещён
        var act2 = async () => await gitLabPathResolver.ResolveGroupOrProjectPath(["group", "subgroup", "project"]);
        await act2.Should().ThrowAsync<AccessToEntityForbiddenException>();
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldAllowAccess_WhenChildHasAccessEntryDespiteParentDenial()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var project = new GitLabProject
        {
            Id = "3",
            Title = "project",
            IsPublic = false,
        };
        var subGroup = new GitLabGroup
        {
            Id = "2",
            Title = "subgroup",
            Projects = [project],
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "group",
            Groups = [subGroup],
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        // Доступ к корневой группе запрещён
        accessContext.AddEntry(["group"], false);
        // Но к конкретному проекту разрешён
        accessContext.AddEntry(["group", "subgroup", "project"], true);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["group", "subgroup", "project"]);

        result.PathSlug.Should().HaveCount(3);
        result.ResolvedEntity.Title.Should().Be("project");
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldAllowAccess_WhenPublicChildInPrivateParent()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var project = new GitLabProject
        {
            Id = "3",
            Title = "public-project",
            IsPublic = true,
        };
        var subGroup = new GitLabGroup
        {
            Id = "2",
            Title = "private-subgroup",
            Projects = [project],
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "private-group",
            Groups = [subGroup],
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["private-group", "private-subgroup", "public-project"]);

        result.PathSlug.Should().HaveCount(3);
        result.ResolvedEntity.Title.Should().Be("public-project");
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldFilterOutPrivateChildProjects_WhenNoAccessEntry()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var publicProject = new GitLabProject
        {
            Id = "2",
            Title = "public-project",
            IsPublic = true,
        };
        var privateProject = new GitLabProject
        {
            Id = "3",
            Title = "private-project",
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "public-group",
            Projects = [publicProject, privateProject],
            IsPublic = true,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        accessContext.AddEntry(["public-group", "private-project"], false);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["public-group"]);

        result.ResolvedEntity.Should().BeOfType<GitLabGroup>();
        var resolvedGroup = (GitLabGroup)result.ResolvedEntity;
        resolvedGroup.Projects.Should().HaveCount(1);
        resolvedGroup.Projects[0].Title.Should().Be("public-project");
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldFilterOutPrivateChildGroups_WhenAccessDenied()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var publicSubGroup = new GitLabGroup
        {
            Id = "2",
            Title = "public-subgroup",
            IsPublic = true,
        };
        var privateSubGroup = new GitLabGroup
        {
            Id = "3",
            Title = "private-subgroup",
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "public-group",
            Groups = [publicSubGroup, privateSubGroup],
            IsPublic = true,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        accessContext.AddEntry(["public-group", "private-subgroup"], false);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["public-group"]);

        result.ResolvedEntity.Should().BeOfType<GitLabGroup>();
        var resolvedGroup = (GitLabGroup)result.ResolvedEntity;
        resolvedGroup.Groups.Should().HaveCount(1);
        resolvedGroup.Groups[0].Title.Should().Be("public-subgroup");
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldIncludePrivateChildProjects_WhenAccessGranted()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var publicProject = new GitLabProject
        {
            Id = "2",
            Title = "public-project",
            IsPublic = true,
        };
        var privateProject = new GitLabProject
        {
            Id = "3",
            Title = "private-project",
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "public-group",
            Projects = [publicProject, privateProject],
            IsPublic = true,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        accessContext.AddEntry(["public-group", "private-project"], true);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["public-group"]);

        result.ResolvedEntity.Should().BeOfType<GitLabGroup>();
        var resolvedGroup = (GitLabGroup)result.ResolvedEntity;
        resolvedGroup.Projects.Should().HaveCount(2);
        resolvedGroup.Projects.Should().Contain(p => p.Title == "public-project");
        resolvedGroup.Projects.Should().Contain(p => p.Title == "private-project");
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldRecursivelyFilterChildren_InNestedGroups()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var publicProject = new GitLabProject
        {
            Id = "4",
            Title = "public-project",
            IsPublic = true,
        };
        var privateProject = new GitLabProject
        {
            Id = "5",
            Title = "private-project",
            IsPublic = false,
        };
        var subSubGroup = new GitLabGroup
        {
            Id = "3",
            Title = "sub-subgroup",
            Projects = [publicProject, privateProject],
            IsPublic = true,
        };
        var subGroup = new GitLabGroup
        {
            Id = "2",
            Title = "subgroup",
            Groups = [subSubGroup],
            IsPublic = true,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "group",
            Groups = [subGroup],
            IsPublic = true,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        accessContext.AddEntry(["group", "subgroup", "sub-subgroup", "private-project"], false);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["group"]);

        result.ResolvedEntity.Should().BeOfType<GitLabGroup>();
        var resolvedGroup = (GitLabGroup)result.ResolvedEntity;
        resolvedGroup.Groups.Should().HaveCount(1);
        var resolvedSubGroup = resolvedGroup.Groups[0];
        resolvedSubGroup.Groups.Should().HaveCount(1);
        var resolvedSubSubGroup = resolvedSubGroup.Groups[0];
        resolvedSubSubGroup.Projects.Should().HaveCount(1);
        resolvedSubSubGroup.Projects[0].Title.Should().Be("public-project");
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldInheritAccessFromParent_ForChildEntities()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var privateProject1 = new GitLabProject
        {
            Id = "3",
            Title = "private-project-1",
            IsPublic = false,
        };
        var privateProject2 = new GitLabProject
        {
            Id = "4",
            Title = "private-project-2",
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "private-group",
            Projects = [privateProject1, privateProject2],
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        // Доступ к группе даёт доступ ко всем дочерним сущностям
        accessContext.AddEntry(["private-group"], true);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["private-group"]);

        result.ResolvedEntity.Should().BeOfType<GitLabGroup>();
        var resolvedGroup = (GitLabGroup)result.ResolvedEntity;
        resolvedGroup.Projects.Should().HaveCount(2);
        resolvedGroup.Projects.Should().Contain(p => p.Title == "private-project-1");
        resolvedGroup.Projects.Should().Contain(p => p.Title == "private-project-2");
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldDenyChildAccess_WhenExplicitlyDeniedDespiteParentAccess()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var project1 = new GitLabProject
        {
            Id = "2",
            Title = "accessible-project",
            IsPublic = false,
        };
        var project2 = new GitLabProject
        {
            Id = "3",
            Title = "restricted-project",
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "group",
            Projects = [project1, project2],
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        // Доступ к группе разрешён
        accessContext.AddEntry(["group"], true);
        // Но доступ к одному проекту явно запрещён
        accessContext.AddEntry(["group", "restricted-project"], false);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["group"]);

        result.ResolvedEntity.Should().BeOfType<GitLabGroup>();
        var resolvedGroup = (GitLabGroup)result.ResolvedEntity;
        resolvedGroup.Projects.Should().HaveCount(1);
        resolvedGroup.Projects[0].Title.Should().Be("accessible-project");
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldFilterMixedPublicAndPrivateEntities_BasedOnAccess()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var publicProject = new GitLabProject
        {
            Id = "2",
            Title = "public-project",
            IsPublic = true,
        };
        var privateProjectWithAccess = new GitLabProject
        {
            Id = "3",
            Title = "private-with-access",
            IsPublic = false,
        };
        var privateProjectNoAccess = new GitLabProject
        {
            Id = "4",
            Title = "private-no-access",
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "group",
            Projects = [publicProject, privateProjectWithAccess, privateProjectNoAccess],
            IsPublic = true,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        accessContext.AddEntry(["group", "private-with-access"], true);
        accessContext.AddEntry(["group", "private-no-access"], false);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["group"]);

        result.ResolvedEntity.Should().BeOfType<GitLabGroup>();
        var resolvedGroup = (GitLabGroup)result.ResolvedEntity;
        resolvedGroup.Projects.Should().HaveCount(2);
        resolvedGroup.Projects.Should().Contain(p => p.Title == "public-project");
        resolvedGroup.Projects.Should().Contain(p => p.Title == "private-with-access");
        resolvedGroup.Projects.Should().NotContain(p => p.Title == "private-no-access");
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldReturnEmptyChildren_WhenAllChildrenAreRestricted()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var project1 = new GitLabProject
        {
            Id = "2",
            Title = "restricted-project-1",
            IsPublic = false,
        };
        var project2 = new GitLabProject
        {
            Id = "3",
            Title = "restricted-project-2",
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "public-group",
            Projects = [project1, project2],
            IsPublic = true,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        accessContext.AddEntry(["public-group", "restricted-project-1"], false);
        accessContext.AddEntry(["public-group", "restricted-project-2"], false);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["public-group"]);

        result.ResolvedEntity.Should().BeOfType<GitLabGroup>();
        var resolvedGroup = (GitLabGroup)result.ResolvedEntity;
        resolvedGroup.Projects.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldGrantAccessToAllProjects_WhenGlobalAccessEntryIsTrue()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var project1 = new GitLabProject
        {
            Id = "2",
            Title = "private-project-1",
            IsPublic = false,
        };
        var project2 = new GitLabProject
        {
            Id = "3",
            Title = "private-project-2",
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "private-group",
            Projects = [project1, project2],
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        // Глобальное правило доступа ко всем проектам
        accessContext.AddEntry([], true);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["private-group"]);

        result.ResolvedEntity.Should().BeOfType<GitLabGroup>();
        var resolvedGroup = (GitLabGroup)result.ResolvedEntity;
        resolvedGroup.Projects.Should().HaveCount(2);
        resolvedGroup.Projects.Should().Contain(p => p.Title == "private-project-1");
        resolvedGroup.Projects.Should().Contain(p => p.Title == "private-project-2");
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldDenyAccessToAllProjects_WhenGlobalAccessEntryIsFalse()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var project = new GitLabProject
        {
            Id = "2",
            Title = "private-project",
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "private-group",
            Projects = [project],
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        // Глобальный запрет доступа ко всем проектам
        accessContext.AddEntry([], false);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var act = async () => await gitLabPathResolver.ResolveGroupOrProjectPath(["private-group", "private-project"]);

        await act.Should().ThrowAsync<AccessToEntityForbiddenException>();
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldOverrideGlobalAccessEntry_WithSpecificEntry()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var project1 = new GitLabProject
        {
            Id = "2",
            Title = "accessible-project",
            IsPublic = false,
        };
        var project2 = new GitLabProject
        {
            Id = "3",
            Title = "restricted-project",
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "private-group",
            Projects = [project1, project2],
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        // Глобальный доступ ко всем проектам
        accessContext.AddEntry([], true);
        // Но конкретный проект запрещён
        accessContext.AddEntry(["private-group", "restricted-project"], false);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["private-group"]);

        result.ResolvedEntity.Should().BeOfType<GitLabGroup>();
        var resolvedGroup = (GitLabGroup)result.ResolvedEntity;
        resolvedGroup.Projects.Should().HaveCount(1);
        resolvedGroup.Projects[0].Title.Should().Be("accessible-project");
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldGrantAccessToSpecificProject_WhenGlobalAccessIsDenied()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var project1 = new GitLabProject
        {
            Id = "2",
            Title = "restricted-project",
            IsPublic = false,
        };
        var project2 = new GitLabProject
        {
            Id = "3",
            Title = "accessible-project",
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "private-group",
            Projects = [project1, project2],
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        // Глобальный запрет доступа
        accessContext.AddEntry([], false);
        // Но конкретный проект разрешён
        accessContext.AddEntry(["private-group", "accessible-project"], true);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        // Доступ к группе запрещён
        var act1 = async () => await gitLabPathResolver.ResolveGroupOrProjectPath(["private-group"]);
        await act1.Should().ThrowAsync<AccessToEntityForbiddenException>();

        // Но доступ к конкретному проекту разрешён
        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["private-group", "accessible-project"]);
        result.ResolvedEntity.Should().BeOfType<GitLabProject>();
        result.ResolvedEntity.Title.Should().Be("accessible-project");
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldApplyGlobalAccessEntry_ToNestedGroups()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var project = new GitLabProject
        {
            Id = "3",
            Title = "private-project",
            IsPublic = false,
        };
        var subGroup = new GitLabGroup
        {
            Id = "2",
            Title = "private-subgroup",
            Projects = [project],
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "private-group",
            Groups = [subGroup],
            IsPublic = false,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        // Глобальный доступ ко всем проектам
        accessContext.AddEntry([], true);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["private-group", "private-subgroup", "private-project"]);

        result.PathSlug.Should().HaveCount(3);
        result.ResolvedEntity.Title.Should().Be("private-project");
    }

    [Fact]
    public async Task ResolveGroupOrProjectPath_ShouldFilterChildren_WhenGlobalAccessDeniesButSomeArePublic()
    {
        var gitLabProjectsService = new InMemoryGitLabProjectsService();

        var publicProject = new GitLabProject
        {
            Id = "2",
            Title = "public-project",
            IsPublic = true,
        };
        var privateProject = new GitLabProject
        {
            Id = "3",
            Title = "private-project",
            IsPublic = false,
        };
        var group = new GitLabGroup
        {
            Id = "1",
            Title = "public-group",
            Projects = [publicProject, privateProject],
            IsPublic = true,
        };

        gitLabProjectsService.AddRootGroup(group);
        var accessContext = new InMemoryGitLabEntityAccessContext();
        // Глобальный запрет доступа
        accessContext.AddEntry([], false);

        var gitLabPathResolver = new GitLabPathResolver(gitLabProjectsService, accessContext);

        var result = await gitLabPathResolver.ResolveGroupOrProjectPath(["public-group"]);

        result.ResolvedEntity.Should().BeOfType<GitLabGroup>();
        var resolvedGroup = (GitLabGroup)result.ResolvedEntity;
        // Публичный проект всё ещё доступен
        resolvedGroup.Projects.Should().HaveCount(1);
        resolvedGroup.Projects[0].Title.Should().Be("public-project");
    }
}
