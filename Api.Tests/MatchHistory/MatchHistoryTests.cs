using Api.Controllers;
using Api.Entities;
using Api.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using Moq;

namespace Api.Tests.MatchHistoryTests;

public class GetUserMatchHistoryAsyncTests
{
    private static TournamentService BuildService(string dbName)
    {
        var factory = TestHelpers.BuildInMemoryDbContextFactory(dbName);
        var store = new TournamentStore();
        var gameService = TestHelpers.CreateGameService();
        return new TournamentService(gameService, store, factory);
    }

    [Fact]
    public async Task Returns_Empty_List_When_No_History_Exists()
    {
        var service = BuildService("NoHistory");
        var result = await service.GetUserMatchHistoryAsync(Guid.NewGuid());
        Assert.Empty(result);
    }

    [Fact]
    public async Task Returns_Only_Matches_Belonging_To_The_User()
    {
        var dbName = "OnlyMyMatches";
        var factory = TestHelpers.BuildInMemoryDbContextFactory(dbName);
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        await using var ctx = factory.CreateDbContext();

        // Match where user is Player 1
        ctx.MatchHistories.Add(new MatchHistory("CODE1", "TicTacToe", userId, "Me", otherId, "Opponent")
        {
            MatchStatus = "Finished",
            WinnerId = userId,
            Moves = new List<Move>()
        });

        // Match where user is Player 2
        ctx.MatchHistories.Add(new MatchHistory("CODE2", "ConnectFour", otherId, "Opponent", userId, "Me")
        {
            MatchStatus = "Draw",
            Moves = new List<Move>()
        });

        // Match that does not involve this user at all
        ctx.MatchHistories.Add(new MatchHistory("CODE3", "RockPaperScissors", otherId, "Stranger1", Guid.NewGuid(), "Stranger2")
        {
            MatchStatus = "Finished",
            Moves = new List<Move>()
        });

        await ctx.SaveChangesAsync();

        var service = new TournamentService(TestHelpers.CreateGameService(), new TournamentStore(), factory);
        var result = await service.GetUserMatchHistoryAsync(userId);

        Assert.Equal(2, result.Count);
        Assert.All(result, m =>
            Assert.True(m.PlayerOneId == userId || m.PlayerTwoId == userId));
    }

    [Fact]
    public async Task Returns_Matches_Ordered_By_Descending_Timestamp()
    {
        var dbName = "OrderedMatches";
        var factory = TestHelpers.BuildInMemoryDbContextFactory(dbName);
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        await using var ctx = factory.CreateDbContext();

        ctx.MatchHistories.Add(new MatchHistory("CODE1", "TicTacToe", userId, "Me", otherId, "Opp")
        {
            Timestamp = DateTime.UtcNow.AddHours(-2),
            Moves = new List<Move>()
        });
        ctx.MatchHistories.Add(new MatchHistory("CODE2", "ConnectFour", userId, "Me", otherId, "Opp")
        {
            Timestamp = DateTime.UtcNow,
            Moves = new List<Move>()
        });
        await ctx.SaveChangesAsync();

        var service = new TournamentService(TestHelpers.CreateGameService(), new TournamentStore(), factory);
        var result = await service.GetUserMatchHistoryAsync(userId);

        Assert.Equal(2, result.Count);
        Assert.True(result[0].Timestamp >= result[1].Timestamp);
    }

    [Fact]
    public async Task Preserves_All_Move_Data_After_Persistence()
    {
        var dbName = "MovesPreserved";
        var factory = TestHelpers.BuildInMemoryDbContextFactory(dbName);
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        await using var ctx = factory.CreateDbContext();

        ctx.MatchHistories.Add(new MatchHistory("CODE1", "TicTacToe", userId, "Me", otherId, "Opp")
        {
            WinnerId = userId,
            MatchStatus = "Finished",
            Moves = new List<Move>
            {
                new Move { UserId = userId,  Username = "Me",  MovesJson = "{\"X\":0,\"Y\":0}" },
                new Move { UserId = otherId, Username = "Opp", MovesJson = "{\"X\":1,\"Y\":1}" },
                new Move { UserId = userId,  Username = "Me",  MovesJson = "{\"X\":0,\"Y\":2}" }
            }
        });
        await ctx.SaveChangesAsync();

        var service = new TournamentService(TestHelpers.CreateGameService(), new TournamentStore(), factory);
        var result = await service.GetUserMatchHistoryAsync(userId);

        Assert.Single(result);
        Assert.Equal(3, result[0].Moves.Count);
        Assert.Equal("{\"X\":0,\"Y\":0}", result[0].Moves[0].MovesJson);
        Assert.Equal(userId, result[0].Moves[0].UserId);
    }

    [Fact]
    public async Task Draw_Match_Has_Null_WinnerId()
    {
        var dbName = "DrawMatch";
        var factory = TestHelpers.BuildInMemoryDbContextFactory(dbName);
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        await using var ctx = factory.CreateDbContext();

        ctx.MatchHistories.Add(new MatchHistory("CODE1", "RockPaperScissors", userId, "Me", otherId, "Opp")
        {
            WinnerId = null,
            MatchStatus = "Draw",
            Moves = new List<Move>()
        });
        await ctx.SaveChangesAsync();

        var service = new TournamentService(TestHelpers.CreateGameService(), new TournamentStore(), factory);
        var result = await service.GetUserMatchHistoryAsync(userId);

        Assert.Single(result);
        Assert.Null(result[0].WinnerId);
        Assert.Equal("Draw", result[0].MatchStatus);
    }
}

public class MatchHistoryControllerTests
{
    private static MatchHistoryController BuildController(
        Mock<ICurrentUserAccessor> accessor,
        Mock<ITournamentService> service,
        ClaimsPrincipal? principal = null)
    {
        var controller = new MatchHistoryController(service.Object, accessor.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal ?? TestHelpers.CreateClaimsPrincipal("testuser")
                }
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetMyHistory_Returns_401_When_User_Not_Authenticated()
    {
        var accessor = new Mock<ICurrentUserAccessor>();
        accessor.Setup(a => a.GetCurrentUser(It.IsAny<ClaimsPrincipal>())).Returns((User?)null);

        var service = new Mock<ITournamentService>();
        var controller = BuildController(accessor, service);

        var result = await controller.GetMyHistory();

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
        service.Verify(s => s.GetUserMatchHistoryAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetMyHistory_Returns_200_With_Correct_History()
    {
        var userId = Guid.NewGuid();
        var user = TestHelpers.BuildGuest("Me", userId);

        var accessor = new Mock<ICurrentUserAccessor>();
        accessor.Setup(a => a.GetCurrentUser(It.IsAny<ClaimsPrincipal>())).Returns(user);

        var history = new List<MatchHistory>
        {
            new MatchHistory("CODE1", "TicTacToe", userId, "Me", Guid.NewGuid(), "Opp")
            {
                MatchStatus = "Finished",
                WinnerId = userId,
                Moves = new List<Move>()
            }
        };

        var service = new Mock<ITournamentService>();
        service.Setup(s => s.GetUserMatchHistoryAsync(userId)).ReturnsAsync(history);

        var controller = BuildController(accessor, service);
        var result = await controller.GetMyHistory();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<List<MatchHistory>>(ok.Value);
        Assert.Single(returned);
        Assert.Equal("CODE1", returned[0].TournamentCode);
        service.Verify(s => s.GetUserMatchHistoryAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetMyHistory_Returns_Empty_List_When_No_Matches()
    {
        var userId = Guid.NewGuid();
        var user = TestHelpers.BuildGuest("NewUser", userId);

        var accessor = new Mock<ICurrentUserAccessor>();
        accessor.Setup(a => a.GetCurrentUser(It.IsAny<ClaimsPrincipal>())).Returns(user);

        var service = new Mock<ITournamentService>();
        service.Setup(s => s.GetUserMatchHistoryAsync(userId)).ReturnsAsync(new List<MatchHistory>());

        var controller = BuildController(accessor, service);
        var result = await controller.GetMyHistory();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<List<MatchHistory>>(ok.Value);
        Assert.Empty(returned);
    }

    [Fact]
    public async Task GetUserHistory_Returns_History_For_Any_UserId_Without_Auth()
    {
        var targetId = Guid.NewGuid();

        var history = new List<MatchHistory>
        {
            new MatchHistory("CODE1", "ConnectFour", targetId, "Target", Guid.NewGuid(), "Opp")
            {
                Moves = new List<Move>()
            }
        };

        var service = new Mock<ITournamentService>();
        service.Setup(s => s.GetUserMatchHistoryAsync(targetId)).ReturnsAsync(history);

        var accessor = new Mock<ICurrentUserAccessor>();
        var controller = BuildController(accessor, service);

        var result = await controller.GetUserHistory(targetId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<List<MatchHistory>>(ok.Value);
        Assert.Single(returned);
        Assert.Equal(targetId, returned[0].PlayerOneId);
    }
}

public class MatchHistoryEntityTests
{
    [Fact]
    public void Constructor_Sets_All_Player_Fields_Correctly()
    {
        var p1Id = Guid.NewGuid();
        var p2Id = Guid.NewGuid();

        var match = new MatchHistory("LOBBY1", "TicTacToe", p1Id, "Alice", p2Id, "Bob");

        Assert.Equal("LOBBY1", match.TournamentCode);
        Assert.Equal("TicTacToe", match.GameType);
        Assert.Equal(p1Id, match.PlayerOneId);
        Assert.Equal("Alice", match.PlayerOneUsername);
        Assert.Equal(p2Id, match.PlayerTwoId);
        Assert.Equal("Bob", match.PlayerTwoUsername);
        Assert.Equal("Finished", match.MatchStatus);
        Assert.NotEqual(Guid.Empty, match.Id);
    }

    [Fact]
    public void Each_New_MatchHistory_Gets_Unique_Id()
    {
        var m1 = new MatchHistory("C1", "TicTacToe");
        var m2 = new MatchHistory("C2", "TicTacToe");
        Assert.NotEqual(m1.Id, m2.Id);
    }

    [Fact]
    public void WinnerId_Is_Null_When_Match_Is_Draw()
    {
        var match = new MatchHistory("CODE", "RockPaperScissors")
        {
            MatchStatus = "Draw",
            WinnerId = null
        };

        Assert.Null(match.WinnerId);
        Assert.Equal("Draw", match.MatchStatus);
    }

    [Fact]
    public void Moves_Preserve_Correct_UserId_And_Json()
    {
        var userId = Guid.NewGuid();
        var match = new MatchHistory("CODE", "ConnectFour")
        {
            Moves = new List<Move>
            {
                new Move { UserId = userId, Username = "Alice", MovesJson = "{\"Column\":3}" }
            }
        };

        Assert.Single(match.Moves);
        Assert.Equal(userId, match.Moves[0].UserId);
        Assert.Equal("{\"Column\":3}", match.Moves[0].MovesJson);
    }
}