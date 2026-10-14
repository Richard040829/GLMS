using System.Text;
using GLMS.Web.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GLMS.Tests.Helpers;

public static class TestHelpers
{
    public static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ApplicationDbContext(options);
    }

    public static IFormFile CreateFile(string fileName, string content = "%PDF-1.4 test content")
    {
        var bytes = Encoding.ASCII.GetBytes(content);
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.Length).Returns(bytes.Length);
        mock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(bytes));
        mock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, CancellationToken>((target, ct) => new MemoryStream(bytes).CopyToAsync(target, ct));
        return mock.Object;
    }
}
