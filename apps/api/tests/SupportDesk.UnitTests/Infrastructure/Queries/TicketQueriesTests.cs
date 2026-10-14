using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Infrastructure.Queries;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Infrastructure.Queries;

public class TicketQueriesTests : IAsyncLifetime
{
    private readonly SupportDbContext _db;
    private readonly TicketQueries _sut;
    private readonly FixedClock _clock = new();

    public TicketQueriesTests()
    {
        var options = new DbContextOptionsBuilder<SupportDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _db = new SupportDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        _sut = new TicketQueries(_db, _clock);
    }

    public async Task InitializeAsync()
    {
        var now = FixedClock.DefaultNow;

        // Categories
        var general = new Category("General", requiresSpecialist: false, forcesCriticalPriority: false);
        var technical = new Category("Technical", requiresSpecialist: true, forcesCriticalPriority: false);

        // Customers
        var contoso = new Customer("Contoso", "contoso@example.com", null, CustomerTier.Premium, now);
        var fabrikam = new Customer("Fabrikam", "fabrikam@example.com", null, CustomerTier.Standard, now);

        // Agent
        var alex = new Agent("Alex Turner", "alex@example.com", maxOpenTickets: 10, now);

        _db.Set<Category>().AddRange(general, technical);
        _db.Set<Customer>().AddRange(contoso, fabrikam);
        _db.Set<Agent>().Add(alex);
        await _db.SaveChangesAsync();   // IDs are now generated

        // Tickets (use the real domain factory + builder style)
        var t1 = Ticket.Raise(
            "TCK-0001", "Server down", "Production server is unreachable.",
            contoso.Id, technical.Id, TicketPriority.High, now);
        t1.AssignTo(alex.Id, now);
        t1.ChangeStatus(TicketStatus.Open, now);

        var t2 = Ticket.Raise(
            "TCK-0002", "Invoice question", "Wrong amount on last invoice.",
            fabrikam.Id, general.Id, TicketPriority.Medium, now);

        var t3 = Ticket.Raise(
            "TCK-0003", "Login issue", "Cannot log in after password reset.",
            contoso.Id, technical.Id, TicketPriority.High, now);
        t3.AssignTo(alex.Id, now);
        t3.ChangeStatus(TicketStatus.InProgress, now);

        _db.Set<Ticket>().AddRange(t1, t2, t3);
        await _db.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetPagedAsync_ExactPriority_ReturnsOnlyMatchingTickets()
    {
        var query = new TicketQuery { Priority = TicketPriority.High, Page = 1, PageSize = 20 };

        var result = await _sut.GetPagedAsync(query, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, t => Assert.Equal(TicketPriority.High, t.Priority));
    }

    [Fact]
    public async Task GetPagedAsync_CombinationOfFilters_ReturnsIntersection()
    {
        // High priority AND customer named Contoso
        var contosoId = _db.Set<Customer>().Single(c => c.Name == "Contoso").Id;

        var query = new TicketQuery
        {
            Priority = TicketPriority.High,
            CustomerId = contosoId,
            Page = 1,
            PageSize = 20
        };

        var result = await _sut.GetPagedAsync(query, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, t =>
        {
            Assert.Equal(TicketPriority.High, t.Priority);
            Assert.Equal("Contoso", t.Customer.Name);
        });
    }

    [Fact]
    public async Task GetPagedAsync_SearchByCustomerName_ReturnsMatchingTickets()
    {
        var query = new TicketQuery { Search = "Contoso", Page = 1, PageSize = 20 };

        var result = await _sut.GetPagedAsync(query, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, t => Assert.Equal("Contoso", t.Customer.Name));
    }
}