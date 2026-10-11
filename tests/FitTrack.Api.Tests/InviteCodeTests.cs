using FitTrack.Api.Features.Invites;

namespace FitTrack.Api.Tests;

public class InviteCodeTests
{
    [Fact]
    public void Codes_use_only_unambiguous_characters()
    {
        for (var i = 0; i < 100; i++)
        {
            var code = InviteCodes.Generate();
            Assert.Equal(InviteCodes.Length, code.Length);
            Assert.All(code, c => Assert.True(InviteCodes.Alphabet.Contains(c), $"Unexpected character {c}"));
        }
    }

    [Theory]
    [InlineData("abcd-efgh-23", "ABCDEFGH23")]
    [InlineData("  ABCD EFGH 23 ", "ABCDEFGH23")]
    [InlineData(null, "")]
    public void Typed_codes_are_normalized(string? typed, string expected)
    {
        Assert.Equal(expected, InviteCodes.Normalize(typed));
    }
}
