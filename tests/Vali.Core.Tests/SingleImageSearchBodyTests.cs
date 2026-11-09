using Xunit;
using Vali.Core.Google;

namespace Vali.Core.Tests;

public class SingleImageSearchBodyTests
{
    [Fact]
    public void DefaultBodyIsTodaysBody() =>
        Assert.Equal(
            """[["apiv3",null,null,null,"US",null,null,null,null,null,[[false]]],[[null,null,59.9,10.75],50],[null,["en","US"],null,null,null,null,null,null,[2],null,[[[2,true,2],[3,true,2],[10,true,2]]]],[[1,2,3,4,8,6]]]""",
            GoogleApi.SingleImageSearchBody(59.9, 10.75, 50, officialOnly: false).Trim());

    [Fact]
    public void OfficialOnlyAsksForOfficialImageryOnly()
    {
        var body = GoogleApi.SingleImageSearchBody(59.9, 10.75, 3000, officialOnly: true);
        Assert.Contains("[[[2,true,2]]]", body);
        Assert.DoesNotContain("[3,true,2]", body);
        Assert.Contains("[[null,null,59.9,10.75],3000]", body);
    }
}
